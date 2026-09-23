using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace DoweLanCaster.Services;

public sealed class RokuPrivateListeningService : IAsyncDisposable
{
    private sealed record TrackedHelper(int ProcessId, DateTime StartTimeUtc);
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly string _trackingPath;
    private readonly Queue<string> _recentLog = new();
    private Process? _process;
    private bool _stopping;

    public RokuPrivateListeningService()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoweLanCaster");
        Directory.CreateDirectory(directory);
        _trackingPath = Path.Combine(directory, "private-listening-helper.json");
    }

    public bool IsRunning => _process is { HasExited: false };
    public bool IsConnected { get; private set; }
    public bool HasReceivedAudio { get; private set; }
    public int? HelperProcessId => IsRunning ? _process?.Id : null;
    public string PlaybackEndpoint { get; private set; } = "Windows default playback device";
    public string LifecycleState { get; private set; } = "Stopped";
    public IReadOnlyList<string> RecentLog { get { lock (_recentLog) return _recentLog.ToArray(); } }
    public event Action<string>? LogLine;
    public event Action? AudioReceived;
    public event Action? StateChanged;

    public async Task StartAsync(string rokuIpAddress, string? playbackEndpoint = null)
    {
        await _lifecycleLock.WaitAsync();
        try
        {
            if (IsRunning) return;
            await RecoverTrackedHelperAsync();
            IsConnected = false;
            HasReceivedAudio = false;
            PlaybackEndpoint = string.IsNullOrWhiteSpace(playbackEndpoint) ? "Windows default playback device" : playbackEndpoint;
            SetState("Starting");

            var jarPath = Path.Combine(AppContext.BaseDirectory, "tools", "rplistening", "RPListening.jar");
            var ffmpegDirectory = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg");
            var ffplayPath = Path.Combine(ffmpegDirectory, "ffplay.exe");
            if (!File.Exists(jarPath)) throw new FileNotFoundException("The Private Listening helper was not installed.", jarPath);
            if (!File.Exists(ffplayPath)) throw new FileNotFoundException("The Private Listening audio player was not installed.", ffplayPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = FindJavaPath(), UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-jar"); startInfo.ArgumentList.Add(jarPath);
            startInfo.ArgumentList.Add("-i"); startInfo.ArgumentList.Add(rokuIpAddress);
            startInfo.Environment["PATH"] = string.Join(Path.PathSeparator, ffmpegDirectory, Environment.GetEnvironmentVariable("PATH") ?? "");
            if (!string.Equals(PlaybackEndpoint, "Windows default playback device", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.Environment["SDL_AUDIODRIVER"] = "wasapi";
                startInfo.Environment["SDL_AUDIO_DEVICE_NAME"] = PlaybackEndpoint;
                AddLog($"Requested playback endpoint: {PlaybackEndpoint}");
            }

            var connection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Roku Private Listening.");
            _process.EnableRaisingEvents = true;
            PersistTracking(_process);
            AddLog($"Helper started (PID {_process.Id}).");
            _process.OutputDataReceived += (_, e) => HandleHelperLine(e.Data, connection);
            _process.ErrorDataReceived += (_, e) => HandleHelperLine(e.Data, connection);
            var startedProcess = _process;
            _process.Exited += (_, _) =>
            {
                AddLog($"Helper exited with code {SafeExitCode(startedProcess)}.");
                if (!_stopping)
                {
                    LifecycleState = "Helper exited";
                    connection.TrySetException(new InvalidOperationException("Private Listening stopped before Roku audio connected."));
                    StateChanged?.Invoke();
                }
            };
            _process.BeginOutputReadLine(); _process.BeginErrorReadLine();

            if (await Task.WhenAny(connection.Task, Task.Delay(TimeSpan.FromSeconds(12))) != connection.Task)
                throw new TimeoutException("Roku did not confirm Private Listening within 12 seconds.");
            await connection.Task;
            await Task.Delay(500);
            if (_process is null || _process.HasExited) throw new InvalidOperationException("Private Listening connected, but its audio session stopped immediately.");
        }
        catch { await StopCoreAsync(); throw; }
        finally { _lifecycleLock.Release(); }
    }

    public async Task StopAsync()
    {
        await _lifecycleLock.WaitAsync();
        try { await StopCoreAsync(); }
        finally { _lifecycleLock.Release(); }
    }

    private async Task StopCoreAsync()
    {
        _stopping = true;
        SetState("Stopping");
        try
        {
            if (_process is null)
                await RecoverTrackedHelperAsync();

            if (_process is { HasExited: false } process)
            {
                try { AddLog("Requesting graceful helper shutdown."); await process.StandardInput.WriteLineAsync(); await process.StandardInput.FlushAsync(); }
                catch (Exception ex) { AddLog($"Graceful shutdown request failed: {ex.Message}"); }
                if (!process.WaitForExit(2500))
                {
                    AddLog("Helper did not exit; terminating its process tree.");
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
                }
            }
        }
        catch (Exception ex) { AddLog($"Helper cleanup warning: {ex.Message}"); }
        finally
        {
            _process?.Dispose(); _process = null; TryDeleteTracking();
            IsConnected = false; HasReceivedAudio = false; SetState("Stopped");
            AddLog("Private Listening resources released.");
            _stopping = false;
        }
    }

    private void HandleHelperLine(string? line, TaskCompletionSource connection)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        AddLog(line);
        if (line == "PRIVATE_LISTENING_CONNECTED") { IsConnected = true; SetState("Connected; waiting for audio"); connection.TrySetResult(); }
        else if (line == "PRIVATE_LISTENING_AUDIO_RECEIVED") { HasReceivedAudio = true; SetState("Playing Roku audio"); AudioReceived?.Invoke(); }
        else if (line.StartsWith("PRIVATE_LISTENING_FAILED:", StringComparison.Ordinal) || line.Contains("Address already in use", StringComparison.OrdinalIgnoreCase) || line.Contains("Cannot run program \"ffplay\"", StringComparison.OrdinalIgnoreCase)) connection.TrySetException(new InvalidOperationException(line));
    }

    private async Task RecoverTrackedHelperAsync()
    {
        if (!File.Exists(_trackingPath)) return;
        try
        {
            var tracked = JsonSerializer.Deserialize<TrackedHelper>(await File.ReadAllTextAsync(_trackingPath));
            if (tracked is null) return;
            using var process = Process.GetProcessById(tracked.ProcessId);
            if (process.ProcessName.Equals("java", StringComparison.OrdinalIgnoreCase) && Math.Abs((process.StartTime.ToUniversalTime() - tracked.StartTimeUtc).TotalSeconds) < 2)
            {
                AddLog($"Recovering tracked orphan helper (PID {tracked.ProcessId}).");
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        catch (ArgumentException) { }
        catch (Exception ex) { AddLog($"Orphan recovery warning: {ex.Message}"); }
        finally { TryDeleteTracking(); }
    }

    private void PersistTracking(Process process) => File.WriteAllText(_trackingPath, JsonSerializer.Serialize(new TrackedHelper(process.Id, process.StartTime.ToUniversalTime())));
    private void TryDeleteTracking() { try { File.Delete(_trackingPath); } catch { } }
    private void SetState(string state) { LifecycleState = state; StateChanged?.Invoke(); }
    private void AddLog(string line)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {line}";
        lock (_recentLog) { _recentLog.Enqueue(entry); while (_recentLog.Count > 80) _recentLog.Dequeue(); }
        LogLine?.Invoke(line);
    }
    private static int? SafeExitCode(Process? process) { try { return process?.ExitCode; } catch { return null; } }
    public async ValueTask DisposeAsync() { await StopAsync(); _lifecycleLock.Dispose(); }

    private static string FindJavaPath()
    {
        var adoptiumDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Eclipse Adoptium");
        var candidates = new[] { Environment.GetEnvironmentVariable("JAVA_HOME") }.Concat(Directory.Exists(adoptiumDirectory) ? Directory.EnumerateDirectories(adoptiumDirectory, "jdk-*").OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase) : Enumerable.Empty<string>());
        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var java = Path.Combine(candidate!, "bin", "java.exe");
            if (File.Exists(java)) return java;
        }
        throw new FileNotFoundException("Java 11 or newer is required for Roku Private Listening. Install Eclipse Temurin JDK 11 or set JAVA_HOME.");
    }
}

using System.Diagnostics;
using System.IO;

namespace DoweLanCaster.Services;

public sealed class LocalFileHlsTranscoder : IAsyncDisposable
{
    private Process? _process;
    private readonly Queue<string> _recentLog = new();

    public string OutputDirectory { get; private set; } = "";
    public bool IsRunning => _process is { HasExited: false };

    public event Action<string>? LogLine;

    public async Task StartAsync(
        string ffmpegPath,
        string inputFile,
        string encoder,
        int bitrateKbps,
        CancellationToken token = default)
    {
        await StopAsync();

        if (!File.Exists(inputFile))
            throw new FileNotFoundException("Video file not found.", inputFile);

        OutputDirectory = Path.Combine(
            Path.GetTempPath(),
            "DoweLanCaster",
            "folder-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(OutputDirectory);

        string playlist = Path.Combine(OutputDirectory, "index.m3u8");
        string segments = Path.Combine(OutputDirectory, "seg-%06d.ts");

        string rate = $"{bitrateKbps}k";
        string buffer = $"{bitrateKbps * 2}k";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = OutputDirectory
        };

        foreach (var arg in new[] { "-hide_banner", "-y", "-i", inputFile })
            psi.ArgumentList.Add(arg);

        // Video: normalize anything FFmpeg can decode to Roku-friendly H.264.
        switch (encoder)
        {
            case "h264_nvenc":
                Add(psi, "-c:v", "h264_nvenc",
                    "-preset", "p3",
                    "-tune", "ll",
                    "-rc", "cbr",
                    "-profile:v", "high",
                    "-level:v", "4.1",
                    "-b:v", rate,
                    "-maxrate", rate,
                    "-bufsize", buffer);
                break;

            case "h264_amf":
                Add(psi, "-c:v", "h264_amf",
                    "-usage", "lowlatency",
                    "-quality", "speed",
                    "-profile:v", "high",
                    "-level:v", "4.1",
                    "-b:v", rate,
                    "-maxrate", rate,
                    "-bufsize", buffer);
                break;

            case "h264_qsv":
                Add(psi, "-c:v", "h264_qsv",
                    "-preset", "veryfast",
                    "-profile:v", "high",
                    "-level:v", "4.1",
                    "-b:v", rate,
                    "-maxrate", rate,
                    "-bufsize", buffer);
                break;

            default:
                Add(psi, "-c:v", "libx264",
                    "-preset", "veryfast",
                    "-profile:v", "high",
                    "-level:v", "4.1",
                    "-b:v", rate,
                    "-maxrate", rate,
                    "-bufsize", buffer);
                break;
        }

        Add(psi,
            "-pix_fmt", "yuv420p",
            "-c:a", "aac",
            "-b:a", "160k",
            "-ac", "2",
            "-ar", "48000",
            "-map", "0:v:0?",
            "-map", "0:a:0?",
            "-force_key_frames", "expr:gte(t,n_forced*2)",
            "-f", "hls",
            "-hls_segment_type", "mpegts",
            "-hls_time", "2",
            "-hls_list_size", "0",
            "-hls_flags", "independent_segments",
            "-hls_segment_filename", segments,
            playlist);

        var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            lock (_recentLog)
            {
                _recentLog.Enqueue(e.Data);
                while (_recentLog.Count > 25)
                    _recentLog.Dequeue();
            }

            LogLine?.Invoke(e.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException("FFmpeg could not be started.");

        process.BeginErrorReadLine();
        _process = process;

        // Wait until the first playable segment appears.
        for (var i = 0; i < 150; i++)
        {
            token.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                string details;
                lock (_recentLog)
                    details = string.Join(Environment.NewLine, _recentLog);

                throw new InvalidOperationException(
                    $"FFmpeg exited with code {process.ExitCode}.{Environment.NewLine}{details}");
            }

            if (File.Exists(playlist))
            {
                var text = await TryReadPlaylistAsync(playlist, token);
                if (text.Contains("#EXTINF:", StringComparison.Ordinal))
                    return;
            }

            await Task.Delay(100, token);
        }

        string timeoutDetails;
        lock (_recentLog)
            timeoutDetails = string.Join(Environment.NewLine, _recentLog);

        throw new TimeoutException(
            "FFmpeg did not create a playable folder-cast HLS playlist." +
            Environment.NewLine +
            timeoutDetails);
    }

    private static void Add(
        ProcessStartInfo psi,
        params string[] args)
    {
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
    }

    private static async Task<string> TryReadPlaylistAsync(
        string path,
        CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(token);
        }
        catch (IOException)
        {
            return "";
        }
    }

    public async Task StopAsync()
    {
        var process = _process;
        _process = null;

        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        if (!string.IsNullOrWhiteSpace(OutputDirectory) &&
            Directory.Exists(OutputDirectory))
        {
            try
            {
                Directory.Delete(OutputDirectory, recursive: true);
            }
            catch
            {
            }
        }

        OutputDirectory = "";

        lock (_recentLog)
            _recentLog.Clear();
    }

    public async ValueTask DisposeAsync() =>
        await StopAsync();
}

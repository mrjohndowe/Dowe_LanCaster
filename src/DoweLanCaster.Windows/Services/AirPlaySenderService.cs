using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DoweLanCaster.Services;

public sealed class AirPlaySenderService : IAsyncDisposable
{
    private const string PyatvVersion = "0.16.1";
    private const string EventLoopShim =
        "import asyncio; asyncio.set_event_loop(asyncio.new_event_loop()); " +
        "from pyatv.scripts.atvremote import main; main()";

    private readonly string _root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DoweLanCaster",
        "AirPlay");
    private Process? _pairingProcess;
    private Task<string>? _pairingOutput;
    private Task<string>? _pairingError;
    private string? _pendingIdentifier;
    private string? _pendingIpAddress;

    public event Action<string>? StatusChanged;

    private string PythonPath => Path.Combine(_root, "runtime", "Scripts", "python.exe");
    private string RuntimeMarkerPath => Path.Combine(_root, "runtime.ready");
    private string StoragePath => Path.Combine(_root, "pyatv.conf");
    private string DevicePath => Path.Combine(_root, "device.json");

    public async Task<string> StartPairingAsync(
        string ipAddress,
        CancellationToken token = default)
    {
        await StopPairingAsync();
        await EnsureRuntimeAsync(token);

        StatusChanged?.Invoke("Finding the Roku AirPlay service...");
        var identifier = await FindIdentifierAsync(ipAddress, token);
        if (string.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException(
                "The selected Roku did not advertise an AirPlay service. Turn on Roku Settings > Apple AirPlay and HomeKit > AirPlay, then try again.");

        var process = CreateAtvRemoteProcess(
            "-i", identifier,
            "--protocol", "airplay",
            "-t", "20",
            "pair");

        if (!process.Start())
            throw new InvalidOperationException("Could not start AirPlay pairing.");

        _pairingProcess = process;
        _pendingIdentifier = identifier;
        _pendingIpAddress = ipAddress;

        var prompt = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _pairingOutput = ReadPairingStreamAsync(
            process.StandardOutput,
            prompt,
            token);
        _pairingError = process.StandardError.ReadToEndAsync(token);

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await prompt.Task.WaitAsync(timeout.Token);
        }
        catch
        {
            await StopPairingAsync();
            throw new TimeoutException(
                "The Roku did not display an AirPlay PIN within 30 seconds.");
        }

        StatusChanged?.Invoke("Enter the PIN displayed on the Roku.");
        return identifier;
    }

    public async Task CompletePairingAsync(
        string pin,
        CancellationToken token = default)
    {
        if (_pairingProcess is null ||
            _pairingProcess.HasExited ||
            _pairingOutput is null ||
            _pairingError is null ||
            string.IsNullOrWhiteSpace(_pendingIdentifier) ||
            string.IsNullOrWhiteSpace(_pendingIpAddress))
        {
            throw new InvalidOperationException(
                "Start AirPlay pairing before entering the PIN.");
        }

        if (!Regex.IsMatch(pin, "^[0-9]{4,8}$"))
            throw new ArgumentException("Enter the 4- to 8-digit PIN shown on the Roku.");

        await _pairingProcess.StandardInput.WriteLineAsync(pin.AsMemory(), token);
        await _pairingProcess.StandardInput.FlushAsync(token);

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));

        try
        {
            await _pairingProcess.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            await StopPairingAsync();
            throw new TimeoutException("AirPlay pairing did not finish within 45 seconds.");
        }

        var output = await _pairingOutput;
        var error = await _pairingError;
        var exitCode = _pairingProcess.ExitCode;

        if (exitCode != 0 ||
            !output.Contains("Pairing seems to have succeeded", StringComparison.OrdinalIgnoreCase))
        {
            await StopPairingAsync();
            throw new InvalidOperationException(
                FirstUsefulLine(error, output, "The Roku rejected the AirPlay PIN."));
        }

        Directory.CreateDirectory(_root);
        var saved = new SavedAirPlayDevice
        {
            IpAddress = _pendingIpAddress,
            Identifier = _pendingIdentifier
        };
        await File.WriteAllTextAsync(
            DevicePath,
            JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }),
            token);

        await StopPairingAsync();
        StatusChanged?.Invoke("AirPlay pairing saved for this Windows user.");
    }

    public async Task PlayUrlAsync(
        string ipAddress,
        string url,
        CancellationToken token = default)
    {
        await EnsureRuntimeAsync(token);
        var saved = await LoadSavedDeviceAsync(token);

        if (saved is null ||
            !string.Equals(saved.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(saved.Identifier) ||
            !File.Exists(StoragePath))
        {
            throw new InvalidOperationException(
                "Pair this Roku from the AirPlay tab before starting an AirPlay cast.");
        }

        StatusChanged?.Invoke("Sending the stream to the paired Roku with AirPlay...");
        var result = await RunAtvRemoteAsync(
            token,
            "-i", saved.Identifier,
            "--protocol", "airplay",
            "-t", "20",
            $"play_url={url}");

        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                FirstUsefulLine(result.Error, result.Output, "AirPlay playback failed."));

        StatusChanged?.Invoke("AirPlay playback command sent to the Roku.");
    }

    public async Task<bool> HasSavedPairingAsync(
        string ipAddress,
        CancellationToken token = default)
    {
        var saved = await LoadSavedDeviceAsync(token);
        return saved is not null &&
               File.Exists(StoragePath) &&
               string.Equals(saved.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(saved.Identifier);
    }

    private async Task EnsureRuntimeAsync(CancellationToken token)
    {
        if (File.Exists(PythonPath) && File.Exists(RuntimeMarkerPath))
            return;

        var launcher = FindPythonLauncher();
        Directory.CreateDirectory(_root);
        StatusChanged?.Invoke("Installing the local AirPlay runtime for this Windows user...");

        var runtimeDirectory = Path.Combine(_root, "runtime");
        var create = await RunProcessAsync(
            launcher,
            new[] { "-3", "-m", "venv", runtimeDirectory },
            token,
            TimeSpan.FromMinutes(2));
        if (create.ExitCode != 0 || !File.Exists(PythonPath))
            throw new InvalidOperationException(
                FirstUsefulLine(create.Error, create.Output,
                    "Could not create the AirPlay runtime. Install Python 3 for Windows and try again."));

        var install = await RunProcessAsync(
            PythonPath,
            new[]
            {
                "-m", "pip", "install", "--disable-pip-version-check",
                $"pyatv=={PyatvVersion}"
            },
            token,
            TimeSpan.FromMinutes(5));
        if (install.ExitCode != 0)
            throw new InvalidOperationException(
                FirstUsefulLine(install.Error, install.Output,
                    "Could not install the local AirPlay sender runtime."));

        await File.WriteAllTextAsync(
            RuntimeMarkerPath,
            $"pyatv={PyatvVersion}",
            token);

        StatusChanged?.Invoke("AirPlay runtime installed.");
    }

    private async Task<string> FindIdentifierAsync(
        string ipAddress,
        CancellationToken token)
    {
        var result = await RunAtvRemoteAsync(token, "-t", "12", "scan");
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                FirstUsefulLine(result.Error, result.Output, "AirPlay discovery failed."));

        var blocks = Regex.Split(result.Output, "={20,}");
        foreach (var block in blocks)
        {
            if (!Regex.IsMatch(
                    block,
                    $@"Address:\s*{Regex.Escape(ipAddress)}(?:\s|$)",
                    RegexOptions.IgnoreCase))
                continue;

            var match = Regex.Match(
                block,
                @"Identifiers:\s*\r?\n\s*-\s*(?<id>[^\r\n]+)",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups["id"].Value.Trim();
        }

        return "";
    }

    private Process CreateAtvRemoteProcess(params string[] arguments)
    {
        var info = new ProcessStartInfo(PythonPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add(EventLoopShim);
        info.ArgumentList.Add("--storage-filename");
        info.ArgumentList.Add(StoragePath);
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        return new Process { StartInfo = info };
    }

    private async Task<ProcessResult> RunAtvRemoteAsync(
        CancellationToken token,
        params string[] arguments)
    {
        using var process = CreateAtvRemoteProcess(arguments);
        if (!process.Start())
            throw new InvalidOperationException("Could not start the AirPlay sender.");

        var output = process.StandardOutput.ReadToEndAsync(token);
        var error = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);
        return new ProcessResult(process.ExitCode, await output, await error);
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken token,
        TimeSpan timeoutDuration)
    {
        var info = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException($"Could not start {Path.GetFileName(fileName)}.");
        var output = process.StandardOutput.ReadToEndAsync(token);
        var error = process.StandardError.ReadToEndAsync(token);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(timeoutDuration);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
            catch
            {
                // The process may have exited between the timeout and cleanup.
            }

            throw new TimeoutException(
                $"{Path.GetFileName(fileName)} did not finish within {timeoutDuration.TotalSeconds:0} seconds.");
        }
        return new ProcessResult(process.ExitCode, await output, await error);
    }

    private static async Task<string> ReadPairingStreamAsync(
        StreamReader reader,
        TaskCompletionSource prompt,
        CancellationToken token)
    {
        var text = new StringBuilder();
        var buffer = new char[1];
        while (await reader.ReadAsync(buffer.AsMemory(), token) > 0)
        {
            text.Append(buffer[0]);
            if (text.ToString().Contains("Enter PIN on screen:", StringComparison.OrdinalIgnoreCase))
                prompt.TrySetResult();
        }

        return text.ToString();
    }

    private async Task<SavedAirPlayDevice?> LoadSavedDeviceAsync(CancellationToken token)
    {
        try
        {
            if (!File.Exists(DevicePath))
                return null;
            return JsonSerializer.Deserialize<SavedAirPlayDevice>(
                await File.ReadAllTextAsync(DevicePath, token));
        }
        catch
        {
            return null;
        }
    }

    private static string FindPythonLauncher()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim(), "py.exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            catch
            {
            }
        }

        var windowsLauncher = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "py.exe");
        if (File.Exists(windowsLauncher))
            return windowsLauncher;

        throw new FileNotFoundException(
            "Python 3 is required for direct AirPlay pairing. Install Python for Windows, including the py launcher, then try again.");
    }

    private static string FirstUsefulLine(
        string first,
        string second,
        string fallback)
    {
        return new[] { first, second }
            .SelectMany(value => value.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries))
            .Select(value => value.Trim())
            .FirstOrDefault(value =>
                !string.IsNullOrWhiteSpace(value) &&
                !value.StartsWith("Traceback", StringComparison.OrdinalIgnoreCase))
            ?? fallback;
    }

    private async Task StopPairingAsync()
    {
        var process = _pairingProcess;
        _pairingProcess = null;
        _pairingOutput = null;
        _pairingError = null;
        _pendingIdentifier = null;
        _pendingIpAddress = null;

        if (process is null)
            return;

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

    public async ValueTask DisposeAsync() => await StopPairingAsync();

    private sealed class SavedAirPlayDevice
    {
        public string IpAddress { get; set; } = "";
        public string Identifier { get; set; } = "";
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}

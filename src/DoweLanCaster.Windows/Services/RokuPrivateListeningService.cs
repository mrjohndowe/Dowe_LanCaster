using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DoweLanCaster.Services;

public sealed class RokuPrivateListeningService : IAsyncDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public Task StartAsync(string rokuIpAddress)
    {
        if (IsRunning)
            return Task.CompletedTask;

        var javaPath = FindJavaPath();
        var jarPath = Path.Combine(
            AppContext.BaseDirectory,
            "tools",
            "rplistening",
            "RPListening.jar");

        if (!File.Exists(jarPath))
            throw new FileNotFoundException("The Private Listening helper was not installed.", jarPath);

        var ffmpegDirectory = Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg");
        var startInfo = new ProcessStartInfo
        {
            FileName = javaPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true
        };
        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(jarPath);
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(rokuIpAddress);
        startInfo.Environment["PATH"] = string.Join(
            Path.PathSeparator,
            ffmpegDirectory,
            Environment.GetEnvironmentVariable("PATH") ?? string.Empty);

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Roku Private Listening.");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                await _process.StandardInput.WriteLineAsync();
                if (!_process.WaitForExit(5000))
                    _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                _process.Kill(entireProcessTree: true);
            }
        }

        _process?.Dispose();
        _process = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private static string FindJavaPath()
    {
        var adoptiumDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Eclipse Adoptium");
        var candidates = new[] { Environment.GetEnvironmentVariable("JAVA_HOME") }
            .Concat(Directory.Exists(adoptiumDirectory)
                ? Directory.EnumerateDirectories(adoptiumDirectory, "jdk-*")
                    .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                : Enumerable.Empty<string>());

        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var java = Path.Combine(candidate!, "bin", "java.exe");
            if (File.Exists(java))
                return java;
        }

        throw new FileNotFoundException(
            "Java 11 or newer is required for Roku Private Listening. Install Eclipse Temurin JDK 11 or set JAVA_HOME.");
    }
}

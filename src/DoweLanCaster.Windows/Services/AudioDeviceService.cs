using System.Diagnostics;
using System.Text.RegularExpressions;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class AudioDeviceService
{
    public async Task<IReadOnlyList<AudioCaptureDevice>> GetDevicesAsync(string ffmpegPath, CancellationToken token = default)
    {
        var psi = new ProcessStartInfo(ffmpegPath, "-hide_banner -list_devices true -f dshow -i dummy")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start FFmpeg.");
        var stderr = p.StandardError.ReadToEndAsync(token);
        var stdout = p.StandardOutput.ReadToEndAsync(token);
        await p.WaitForExitAsync(token);
        var text = (await stderr) + "\n" + (await stdout);

        var result = new List<AudioCaptureDevice>();
        bool audioSection = false;

        foreach (var line in text.Split('\n'))
        {
            if (line.Contains("DirectShow audio devices", StringComparison.OrdinalIgnoreCase))
            {
                audioSection = true;
                continue;
            }
            if (audioSection && line.Contains("DirectShow video devices", StringComparison.OrdinalIgnoreCase))
                break;
            if (!audioSection) continue;

            var match = Regex.Match(line, "\"(?<name>[^\"]+)\"");
            if (!match.Success) continue;
            var name = match.Groups["name"].Value.Trim();
            if (name.Length > 0 && result.All(x => !x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                result.Add(new AudioCaptureDevice { Name = name });
        }

        return result;
    }
}

using System.Diagnostics;
using System.Text.RegularExpressions;
using DoweLanCaster.Models;
using NAudio.CoreAudioApi;

namespace DoweLanCaster.Services;

public sealed class AudioBackendService
{
    public async Task<IReadOnlyList<AudioSourceOption>> GetAudioSourcesAsync(
        string ffmpegPath,
        CancellationToken token = default)
    {
        var result = new List<AudioSourceOption>
        {
            new() { Kind = AudioSourceKind.None, Name = "No Audio" },
            new()
            {
                Kind = AudioSourceKind.SystemLoopback,
                Name = "System Audio (Default Output)",
                DeviceName = "__SYSTEM_LOOPBACK__"
            }
        };

        using (var enumerator = new MMDeviceEnumerator())
        {
            foreach (var device in enumerator.EnumerateAudioEndPoints(
                         DataFlow.Render,
                         DeviceState.Active))
            {
                result.Add(new AudioSourceOption
                {
                    Kind = AudioSourceKind.SystemLoopback,
                    Name = $"PC playback — {device.FriendlyName}",
                    DeviceName = $"__LOOPBACK__:{device.ID}"
                });
            }
        }

        var devices = await GetDirectShowDevicesAsync(ffmpegPath, token);

        foreach (var device in devices)
        {
            var lower = device.ToLowerInvariant();

            if (lower.Contains("stereo mix") ||
                lower.Contains("what u hear") ||
                lower.Contains("wave out"))
            {
                result.Add(new AudioSourceOption
                {
                    Kind = AudioSourceKind.SystemLoopback,
                    Name = $"System Audio ({device})",
                    DeviceName = device
                });
            }
        }

        foreach (var device in devices)
        {
            result.Add(new AudioSourceOption
            {
                Kind = AudioSourceKind.DirectShow,
                Name = device,
                DeviceName = device
            });
        }

        return result;
    }

    private static async Task<IReadOnlyList<string>> GetDirectShowDevicesAsync(
        string ffmpegPath,
        CancellationToken token)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in new[]
                 {
                     "-hide_banner","-list_devices","true",
                     "-f","dshow","-i","dummy"
                 })
        {
            psi.ArgumentList.Add(arg);
        }

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Could not start FFmpeg.");

        var stderrTask = p.StandardError.ReadToEndAsync(token);
        await p.WaitForExitAsync(token);
        var text = await stderrTask;

        var devices = new List<string>();
        var inAudio = false;

        foreach (var line in text.Split('\n'))
        {
            if (line.Contains("DirectShow audio devices", StringComparison.OrdinalIgnoreCase))
            {
                inAudio = true;
                continue;
            }

            if (inAudio && line.Contains("DirectShow video devices", StringComparison.OrdinalIgnoreCase))
                break;

            if (!inAudio) continue;

            var match = Regex.Match(line, "\"(?<name>[^\"]+)\"");
            if (!match.Success) continue;

            var name = match.Groups["name"].Value.Trim();

            if (name.Length > 0 &&
                devices.All(x => !x.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                devices.Add(name);
            }
        }

        return devices;
    }
}

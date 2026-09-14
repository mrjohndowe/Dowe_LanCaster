using System.Diagnostics;

namespace DoweLanCaster.Services;

public sealed class EncoderDetectionService
{
    public async Task<IReadOnlyList<string>> DetectAsync(string ffmpegPath, CancellationToken token = default)
    {
        var psi = new ProcessStartInfo(ffmpegPath, "-hide_banner -encoders")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Could not start FFmpeg.");
        var stdout = p.StandardOutput.ReadToEndAsync(token);
        var stderr = p.StandardError.ReadToEndAsync(token);
        await p.WaitForExitAsync(token);
        var text = (await stdout) + "\n" + (await stderr);

        var encoders = new List<string>();

        await AddIfUsableAsync(ffmpegPath, text, encoders, "h264_nvenc", "NVIDIA NVENC", token);
        await AddIfUsableAsync(ffmpegPath, text, encoders, "h264_amf", "AMD AMF", token);
        await AddIfUsableAsync(ffmpegPath, text, encoders, "h264_qsv", "Intel Quick Sync", token);
        encoders.Add("CPU (libx264)");
        return encoders;
    }

    private static async Task AddIfUsableAsync(
        string ffmpegPath,
        string encoderOutput,
        ICollection<string> encoders,
        string ffmpegName,
        string displayName,
        CancellationToken token)
    {
        if (!encoderOutput.Contains(ffmpegName, StringComparison.OrdinalIgnoreCase))
            return;

        var arguments =
            $"-hide_banner -loglevel error -f lavfi -i color=c=black:s=128x72:r=30 " +
            $"-frames:v 1 -an -c:v {ffmpegName} -f null -";

        var psi = new ProcessStartInfo(ffmpegPath, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var probe = Process.Start(psi);
        if (probe is null)
            return;

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        var stdout = probe.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr = probe.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await probe.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdout, stderr);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            try
            {
                probe.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return;
        }

        if (probe.ExitCode == 0)
            encoders.Add(displayName);
    }

    public static string ToFFmpegEncoder(string friendly) => friendly switch
    {
        "NVIDIA NVENC" => "h264_nvenc",
        "AMD AMF" => "h264_amf",
        "Intel Quick Sync" => "h264_qsv",
        _ => "libx264"
    };
}

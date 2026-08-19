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
        if (text.Contains("h264_nvenc", StringComparison.OrdinalIgnoreCase)) encoders.Add("NVIDIA NVENC");
        if (text.Contains("h264_amf", StringComparison.OrdinalIgnoreCase)) encoders.Add("AMD AMF");
        if (text.Contains("h264_qsv", StringComparison.OrdinalIgnoreCase)) encoders.Add("Intel Quick Sync");
        encoders.Add("CPU (libx264)");
        return encoders;
    }

    public static string ToFFmpegEncoder(string friendly) => friendly switch
    {
        "NVIDIA NVENC" => "h264_nvenc",
        "AMD AMF" => "h264_amf",
        "Intel Quick Sync" => "h264_qsv",
        _ => "libx264"
    };
}

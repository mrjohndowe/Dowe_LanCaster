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

        AddIfAvailable(text, encoders, "h264_nvenc", "NVIDIA NVENC");
        AddIfAvailable(text, encoders, "h264_amf", "AMD AMF");
        AddIfAvailable(text, encoders, "h264_qsv", "Intel Quick Sync");
        AddIfAvailable(text, encoders, "h264_mf", "Microsoft Media Foundation");
        AddIfAvailable(text, encoders, "h264_d3d12va", "Direct3D 12 Video");
        AddIfAvailable(text, encoders, "h264_vulkan", "Vulkan Video");
        AddIfAvailable(text, encoders, "libopenh264", "CPU (OpenH264)");
        encoders.Add("CPU (libx264)");
        return encoders;
    }

    private static void AddIfAvailable(
        string encoderOutput,
        ICollection<string> encoders,
        string ffmpegName,
        string displayName)
    {
        if (encoderOutput.Contains(ffmpegName, StringComparison.OrdinalIgnoreCase))
            encoders.Add(displayName);
    }

    public static string ToFFmpegEncoder(string friendly) => friendly switch
    {
        "NVIDIA NVENC" => "h264_nvenc",
        "AMD AMF" => "h264_amf",
        "Intel Quick Sync" => "h264_qsv",
        "Microsoft Media Foundation" => "h264_mf",
        "Direct3D 12 Video" => "h264_d3d12va",
        "Vulkan Video" => "h264_vulkan",
        "CPU (OpenH264)" => "libopenh264",
        _ => "libx264"
    };
}

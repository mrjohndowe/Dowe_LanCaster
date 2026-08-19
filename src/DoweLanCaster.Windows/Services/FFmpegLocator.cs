using System.Diagnostics;
using System.IO;

namespace DoweLanCaster.Services;

public static class FFmpegLocator
{
    public static string? FindFFmpeg()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg", "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "ffmpeg", "ffmpeg.exe")
        };
        foreach (var p in candidates)
            if (File.Exists(p)) return Path.GetFullPath(p);

        return FindOnPath("ffmpeg.exe");
    }

    private static string? FindOnPath(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo("where.exe", exe)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(2500);
            return output.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries)
                         .FirstOrDefault(File.Exists);
        }
        catch { return null; }
    }
}

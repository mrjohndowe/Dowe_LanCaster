using System.Diagnostics;
using System.IO;

namespace DoweLanCaster.Services;

public static class YtDlpLocator
{
    public static string? Find()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "yt-dlp", "yt-dlp.exe"),
            Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "yt-dlp", "yt-dlp.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "yt-dlp.exe")
        };

        foreach (var candidate in candidates)
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);

        try
        {
            var psi = new ProcessStartInfo("where.exe", "yt-dlp.exe")
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

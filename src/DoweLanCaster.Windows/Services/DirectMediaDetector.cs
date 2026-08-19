using System.IO;

namespace DoweLanCaster.Services;

public static class DirectMediaDetector
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".m4v", ".mov", ".webm", ".mkv", ".avi",
            ".ts", ".m2ts", ".m3u8", ".mpg", ".mpeg"
        };

    public static bool IsDirectMediaUrl(string url, out string extension)
    {
        extension = "";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.AbsolutePath.TrimEnd('/');
        extension = Path.GetExtension(path);

        return Extensions.Contains(extension);
    }
}

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

        if (IsTeraBoxStreamingUrl(uri))
        {
            extension = ".m3u8";
            return true;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        extension = Path.GetExtension(path);

        return Extensions.Contains(extension);
    }

    public static bool IsTeraBoxStreamingUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        IsTeraBoxStreamingUrl(uri);

    private static bool IsTeraBoxStreamingUrl(Uri uri)
    {
        if ((uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps) ||
            !uri.AbsolutePath.Equals(
                "/share/streaming",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host;
        var isTeraBoxHost =
            host.Equals("terabox.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".terabox.com", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("1024terabox.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".1024terabox.com", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("1024tera.com", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".1024tera.com", StringComparison.OrdinalIgnoreCase);

        if (!isTeraBoxHost)
            return false;

        var query = ParseQuery(uri.Query);
        return query.TryGetValue("type", out var type) &&
            type.StartsWith("M3U8_", StringComparison.OrdinalIgnoreCase) &&
            query.ContainsKey("uk") &&
            query.ContainsKey("shareid") &&
            query.ContainsKey("fid") &&
            query.ContainsKey("sign") &&
            query.ContainsKey("timestamp");
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var part in query.TrimStart('?').Split(
                     '&',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var name = separator < 0 ? part : part[..separator];
            var value = separator < 0 ? "" : part[(separator + 1)..];
            values[Uri.UnescapeDataString(name)] =
                Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return values;
    }
}

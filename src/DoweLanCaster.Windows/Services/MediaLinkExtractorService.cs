using System.IO;
using System.Diagnostics;
using System.Text.Json;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class MediaLinkExtractorService
{
    public event Action<string>? LogLine;

    public async Task<ExtractedMedia> ExtractAsync(
        string ytDlpPath,
        string pageUrl,
        CancellationToken token = default)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Enter a valid http:// or https:// URL.");

        var psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string[] args =
        {
            "--no-playlist",
            "--no-warnings",
            "--dump-single-json",
            "--format",
            "best[protocol^=http]/best",
            pageUrl
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var jsonTask = process.StandardOutput.ReadToEndAsync(token);
        var stderrTask = ReadLogAsync(process.StandardError, token);

        await process.WaitForExitAsync(token);
        await stderrTask;

        string json = await jsonTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                "The site did not provide an extractable public/non-DRM video stream.");

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("yt-dlp returned no media information.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string mediaUrl = GetString(root, "url");

        if (string.IsNullOrWhiteSpace(mediaUrl))
            throw new InvalidOperationException(
                "The page was recognized, but no single directly streamable audio/video format was available.");

        var headers = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("http_headers", out var headerElement) &&
            headerElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in headerElement.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.String)
                    headers[property.Name] = property.Value.GetString() ?? "";
        }

        return new ExtractedMedia
        {
            PageUrl = pageUrl,
            Title = GetString(root, "title"),
            MediaUrl = mediaUrl,
            Protocol = GetString(root, "protocol"),
            Extension = GetString(root, "ext"),
            ThumbnailUrl = GetNullableString(root, "thumbnail"),
            IsLive = GetBool(root, "is_live"),
            HttpHeaders = headers
        };
    }

    private async Task ReadLogAsync(StreamReader reader, CancellationToken token)
    {
        while (!reader.EndOfStream)
        {
            token.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(token);
            if (!string.IsNullOrWhiteSpace(line))
                LogLine?.Invoke(line);
        }
    }

    private static string GetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? "";

        return "";
    }

    private static string? GetNullableString(JsonElement element, string name)
    {
        var value = GetString(element, name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool GetBool(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) &&
            (value.ValueKind == JsonValueKind.True ||
             value.ValueKind == JsonValueKind.False))
            return value.GetBoolean();

        return false;
    }
}

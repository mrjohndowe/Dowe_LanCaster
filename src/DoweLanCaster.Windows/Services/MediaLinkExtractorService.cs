using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class MediaLinkExtractorService
{
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
        "AppleWebKit/537.36 (KHTML, like Gecko) " +
        "Chrome/124.0.0.0 Safari/537.36";

    private static readonly Regex[] MediaPatterns =
    {
        new(@"<(?:video|source)[^>]+src\s*=\s*[\""'](?<url>[^\""']+)[\""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<meta[^>]+(?:property|name)\s*=\s*[\""'](?:og:video(?::url)?|twitter:player:stream)[\""'][^>]+content\s*=\s*[\""'](?<url>[^\""']+)[\""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<meta[^>]+content\s*=\s*[\""'](?<url>[^\""']+)[\""'][^>]+(?:property|name)\s*=\s*[\""'](?:og:video(?::url)?|twitter:player:stream)[\""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(?:file|src|url)\s*[:=]\s*[\""'](?<url>(?:https?:)?(?:\\?/){2}[^\""']+?\.(?:mp4|m4v|webm|m3u8)(?:\?[^\""']*)?)[\""']",
            RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(?<url>https?:\\?/\\?/[^\s\""'<>]+?\.(?:mp4|m4v|webm|m3u8)(?:\?[^\s\""'<>]*)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    public event Action<string>? LogLine;

    public async Task<ExtractedMedia> ExtractAsync(
        string? ytDlpPath,
        string pageUrl,
        CancellationToken token = default)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Enter a valid http:// or https:// URL.");

        Exception? ytDlpFailure = null;

        if (!string.IsNullOrWhiteSpace(ytDlpPath) && File.Exists(ytDlpPath))
        {
            try
            {
                return await ExtractWithYtDlpAsync(
                    ytDlpPath, pageUrl, token);
            }
            catch (Exception ex)
            {
                ytDlpFailure = ex;
                LogLine?.Invoke(
                    $"yt-dlp could not resolve the page; trying embedded-media detection: {ex.Message}");
            }
        }

        var embedded = await ExtractEmbeddedMediaAsync(uri, token);
        if (embedded is not null)
            return embedded;

        throw new InvalidOperationException(
            ytDlpFailure?.Message ??
            "The webpage did not expose a public, non-DRM MP4 or HLS video stream.");
    }

    private async Task<ExtractedMedia> ExtractWithYtDlpAsync(
        string ytDlpPath,
        string pageUrl,
        CancellationToken token)
    {
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
            "--dump-single-json",
            "--user-agent",
            BrowserUserAgent,
            "--referer",
            pageUrl,
            "--format",
            "bestvideo[protocol^=http]+bestaudio[protocol^=http]/" +
            "bestvideo+bestaudio/" +
            "best[protocol^=http][vcodec!=none][acodec!=none]/" +
            "best[vcodec!=none][acodec!=none]/" +
            "best[protocol^=http][vcodec!=none]/best",
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
        string? audioUrl = null;
        var videoHeaders = ReadHeaders(root);
        IReadOnlyDictionary<string, string> audioHeaders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("requested_formats", out var requestedFormats) &&
            requestedFormats.ValueKind == JsonValueKind.Array)
        {
            foreach (var format in requestedFormats.EnumerateArray())
            {
                var formatUrl = GetString(format, "url");
                if (string.IsNullOrWhiteSpace(formatUrl))
                    continue;

                var videoCodec = GetString(format, "vcodec");
                var audioCodec = GetString(format, "acodec");
                var hasVideo = !string.IsNullOrWhiteSpace(videoCodec) &&
                    !videoCodec.Equals("none", StringComparison.OrdinalIgnoreCase);
                var hasAudio = !string.IsNullOrWhiteSpace(audioCodec) &&
                    !audioCodec.Equals("none", StringComparison.OrdinalIgnoreCase);

                if (hasVideo && string.IsNullOrWhiteSpace(mediaUrl))
                {
                    mediaUrl = formatUrl;
                    videoHeaders = ReadHeaders(format);
                }

                if (hasAudio && !hasVideo && string.IsNullOrWhiteSpace(audioUrl))
                {
                    audioUrl = formatUrl;
                    audioHeaders = ReadHeaders(format);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(mediaUrl))
            throw new InvalidOperationException(
                "The page was recognized, but no single directly streamable audio/video format was available.");

        LogLine?.Invoke(
            string.IsNullOrWhiteSpace(audioUrl)
                ? "yt-dlp selected a combined media stream; FFmpeg will map its audio track."
                : "yt-dlp selected separate video and audio streams; FFmpeg will combine them.");

        return new ExtractedMedia
        {
            PageUrl = pageUrl,
            Title = GetString(root, "title"),
            MediaUrl = mediaUrl,
            AudioUrl = audioUrl,
            Protocol = GetString(root, "protocol"),
            Extension = GetString(root, "ext"),
            ThumbnailUrl = GetNullableString(root, "thumbnail"),
            IsLive = GetBool(root, "is_live"),
            HttpHeaders = videoHeaders,
            AudioHttpHeaders = audioHeaders
        };
    }

    private async Task<ExtractedMedia?> ExtractEmbeddedMediaAsync(
        Uri pageUri,
        CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, pageUri);
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        request.Headers.TryAddWithoutValidation(
            "Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            token);

        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is not null &&
            !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase) &&
            !mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) &&
            !mediaType.Contains("text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(token);
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in MediaPatterns)
        {
            foreach (Match match in pattern.Matches(html))
            {
                var decoded = DecodeMediaUrl(match.Groups["url"].Value);
                if (!Uri.TryCreate(pageUri, decoded, out var mediaUri) ||
                    (mediaUri.Scheme != Uri.UriSchemeHttp &&
                     mediaUri.Scheme != Uri.UriSchemeHttps))
                {
                    continue;
                }

                if (DirectMediaDetector.IsDirectMediaUrl(
                        mediaUri.AbsoluteUri,
                        out _))
                {
                    candidates.Add(mediaUri.AbsoluteUri);
                }
            }
        }

        var selected = candidates
            .OrderByDescending(ScoreMediaUrl)
            .FirstOrDefault();

        if (selected is null)
            return null;

        DirectMediaDetector.IsDirectMediaUrl(selected, out var extension);
        var isHls = extension.Equals(
            ".m3u8", StringComparison.OrdinalIgnoreCase);

        LogLine?.Invoke($"Embedded media found: {selected}");

        return new ExtractedMedia
        {
            PageUrl = pageUri.AbsoluteUri,
            Title = GetPageTitle(html),
            MediaUrl = selected,
            Protocol = isHls ? "HLS" : "Direct HTTP",
            Extension = extension.TrimStart('.'),
            IsLive = isHls,
            HttpHeaders = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Referer"] = pageUri.AbsoluteUri,
                ["User-Agent"] = BrowserUserAgent
            }
        };
    }

    private static string DecodeMediaUrl(string value)
    {
        var decoded = WebUtility.HtmlDecode(value.Trim())
            .Replace("\\/", "/", StringComparison.Ordinal)
            .Replace("\\u002F", "/", StringComparison.OrdinalIgnoreCase)
            .Replace("\\u0026", "&", StringComparison.OrdinalIgnoreCase);

        return decoded.StartsWith("//", StringComparison.Ordinal)
            ? "https:" + decoded
            : decoded;
    }

    private static int ScoreMediaUrl(string url)
    {
        var score = url.Contains(".m3u8", StringComparison.OrdinalIgnoreCase)
            ? 500
            : 1000;

        foreach (var resolution in new[] { 2160, 1440, 1080, 720, 480, 360, 240 })
        {
            if (url.Contains(resolution.ToString(), StringComparison.Ordinal))
                return score + resolution;
        }

        return score;
    }

    private static string GetPageTitle(string html)
    {
        var match = Regex.Match(
            html,
            @"<title[^>]*>(?<title>.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success
            ? WebUtility.HtmlDecode(match.Groups["title"].Value).Trim()
            : "Web video";
    }

    private async Task ReadLogAsync(StreamReader reader, CancellationToken token)
    {
        while (await reader.ReadLineAsync(token) is { } line)
        {
            token.ThrowIfCancellationRequested();
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

    private static IReadOnlyDictionary<string, string> ReadHeaders(
        JsonElement element)
    {
        var headers =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!element.TryGetProperty("http_headers", out var headerElement) ||
            headerElement.ValueKind != JsonValueKind.Object)
        {
            return headers;
        }

        foreach (var property in headerElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                headers[property.Name] = property.Value.GetString() ?? "";
        }

        return headers;
    }
}

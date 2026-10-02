using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DoweLanCaster.Services;

/// <summary>
/// Provides one stable AirPlay address for every cast type. The active tab
/// remains free to use its own streaming server, while Apple devices only
/// need to reach this listener on port 8765.
/// </summary>
public sealed class AirPlayHandoffServer : IAsyncDisposable
{
    // AirPlay can keep a media request open for the full length of a movie.
    // HttpClient's default 100-second timeout would cut that request off while
    // the encoder and the original cast continue normally.
    private static readonly HttpClient Client = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };
    private WebApplication? _app;
    private Uri? _source;
    private string _title = "Dowe LanCaster";
    private string _mediaType = "application/vnd.apple.mpegurl";
    private long _revision;

    public const int SharedPort = 8765;
    public bool IsRunning => _app is not null;

    public async Task StartAsync(CancellationToken token = default)
    {
        if (_app is not null)
            return;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{SharedPort}");
        var app = builder.Build();

        app.MapGet("/health", () => Results.Text("OK"));
        app.MapGet("/", () => Results.Json(new
        {
            app = "Dowe LanCaster",
            airPlay = "/airplay",
            stream = "/stream"
        }));

        app.MapGet("/airplay", () =>
        {
            if (_source is null)
                return Results.NotFound("No stream is currently prepared for AirPlay.");

            return Results.Content(
                AirPlayPage.Create(_title, "/stream", _mediaType, Interlocked.Read(ref _revision)),
                "text/html; charset=utf-8");
        });

        app.MapMethods("/stream/{**path}", new[] { "GET", "HEAD" }, ProxyStreamAsync);
        app.MapMethods("/stream", new[] { "GET", "HEAD" }, ProxyStreamAsync);
        app.MapGet("/control", ProxyControlAsync);

        await app.StartAsync(token);
        _app = app;
    }

    public void SetSource(string? sourceUrl, string title, string mediaType, long revision = 0)
    {
        _source = string.IsNullOrWhiteSpace(sourceUrl) ? null : new Uri(sourceUrl, UriKind.Absolute);
        _title = title;
        _mediaType = mediaType;
        Interlocked.Exchange(ref _revision, revision);
    }

    private async Task ProxyStreamAsync(HttpContext context)
    {
        var source = _source;
        if (source is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var path = context.Request.RouteValues["path"]?.ToString();
        var target = string.IsNullOrWhiteSpace(path)
            ? source
            : new Uri(new Uri(source.GetLeftPart(UriPartial.Authority)), "/" + path);

        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);
        if (context.Request.Headers.TryGetValue("Range", out var range))
            request.Headers.TryAddWithoutValidation("Range", range.ToString());

        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        CopyHeader(response.Content.Headers.ContentType, context.Response.Headers, "Content-Type");
        CopyHeader(response.Content.Headers.ContentLength, context.Response.Headers, "Content-Length");
        CopyHeader(response.Content.Headers.ContentRange, context.Response.Headers, "Content-Range");
        CopyHeader(response.Headers.AcceptRanges, context.Response.Headers, "Accept-Ranges");
        context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";

        if (HttpMethods.IsHead(context.Request.Method) || !response.IsSuccessStatusCode)
            return;

        var isPlaylist = response.Content.Headers.ContentType?.MediaType?
            .Equals("application/vnd.apple.mpegurl", StringComparison.OrdinalIgnoreCase) == true ||
            target.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

        if (isPlaylist)
        {
            var playlist = await response.Content.ReadAsStringAsync(context.RequestAborted);
            context.Response.ContentLength = null;
            await context.Response.WriteAsync(RewritePlaylist(playlist, source), context.RequestAborted);
            return;
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private async Task ProxyControlAsync(HttpContext context)
    {
        var source = _source;
        if (source is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var controlUri = new Uri(new Uri(source.GetLeftPart(UriPartial.Authority)), "/control" + context.Request.QueryString);
        using var response = await Client.GetAsync(controlUri, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            return;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(context.RequestAborted));
        var root = document.RootElement;
        await context.Response.WriteAsJsonAsync(new
        {
            active = root.TryGetProperty("active", out var active) && active.GetBoolean(),
            streamUrl = "/stream",
            mediaType = root.TryGetProperty("mediaType", out var mediaType) ? mediaType.GetString() : _mediaType,
            revision = root.TryGetProperty("revision", out var revision) ? revision.GetInt64() : Interlocked.Read(ref _revision)
        }, context.RequestAborted);
    }

    private static string RewritePlaylist(string playlist, Uri source)
    {
        var sourceDirectory = source.AbsolutePath[..(source.AbsolutePath.LastIndexOf('/') + 1)];
        var lines = playlist.Replace("\r\n", "\n").Split('\n');
        var rewritten = lines.Select(line =>
        {
            var value = line.Trim();
            return value.Length == 0 || value.StartsWith('#') || Uri.IsWellFormedUriString(value, UriKind.Absolute)
                ? line
                : "/stream" + sourceDirectory + value.TrimStart('/');
        });
        return string.Join('\n', rewritten);
    }

    private static void CopyHeader(object? value, IHeaderDictionary headers, string name)
    {
        if (value is not null)
            headers[name] = value.ToString();
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (_app is null)
            return;

        var app = _app;
        _app = null;
        _source = null;
        await app.StopAsync(token);
        await app.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}

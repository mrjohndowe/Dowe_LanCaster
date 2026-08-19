using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DoweLanCaster.Services;

public sealed class MediaStreamingServer : IAsyncDisposable
{
    private WebApplication? _app;
    private string? _filePath;

    public int Port { get; private set; } = 8765;
    public bool IsRunning => _app is not null;

    public async Task StartAsync(string filePath, int port = 8765, CancellationToken token = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Selected media file does not exist.", filePath);

        await StopAsync(token);
        _filePath = Path.GetFullPath(filePath);
        Port = port;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");
        var app = builder.Build();

        app.MapGet("/", () => Results.Json(new { app = "Dowe LanCaster", status = "ready", stream = "/media" }));
        app.MapGet("/health", () => Results.Text("OK"));

        app.MapMethods("/media", new[] { "GET", "HEAD" }, async context =>
        {
            if (_filePath is null || !File.Exists(_filePath))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var info = new FileInfo(_filePath);
            context.Response.Headers.AcceptRanges = "bytes";
            context.Response.ContentType = GetContentType(info.Extension);

            var range = context.Request.Headers.Range.ToString();
            if (!TryParseRange(range, info.Length, out long start, out long end))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentLength = info.Length;
                if (HttpMethods.IsHead(context.Request.Method)) return;

                await using var whole = File.OpenRead(_filePath);
                await whole.CopyToAsync(context.Response.Body, context.RequestAborted);
                return;
            }

            long length = end - start + 1;
            context.Response.StatusCode = 206;
            context.Response.ContentLength = length;
            context.Response.Headers.ContentRange = $"bytes {start}-{end}/{info.Length}";
            if (HttpMethods.IsHead(context.Request.Method)) return;

            await using var stream = File.OpenRead(_filePath);
            stream.Seek(start, SeekOrigin.Begin);

            byte[] buffer = new byte[1024 * 1024];
            long remaining = length;
            while (remaining > 0)
            {
                int wanted = (int)Math.Min(buffer.Length, remaining);
                int read = await stream.ReadAsync(buffer.AsMemory(0, wanted), context.RequestAborted);
                if (read == 0) break;
                await context.Response.Body.WriteAsync(buffer.AsMemory(0, read), context.RequestAborted);
                remaining -= read;
            }
        });

        await app.StartAsync(token);
        _app = app;
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (_app is null) return;
        var app = _app;
        _app = null;
        _filePath = null;
        await app.StopAsync(token);
        await app.DisposeAsync();
    }

    private static bool TryParseRange(string header, long size, out long start, out long end)
    {
        start = 0;
        end = size - 1;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return false;

        string value = header[6..].Split(',')[0].Trim();
        int dash = value.IndexOf('-');
        if (dash < 0) return false;

        string a = value[..dash].Trim();
        string b = value[(dash + 1)..].Trim();

        if (a.Length == 0)
        {
            if (!long.TryParse(b, out long suffix) || suffix <= 0) return false;
            suffix = Math.Min(suffix, size);
            start = size - suffix;
            end = size - 1;
            return true;
        }

        if (!long.TryParse(a, out start) || start < 0 || start >= size) return false;
        if (b.Length > 0)
        {
            if (!long.TryParse(b, out end)) return false;
            end = Math.Min(end, size - 1);
        }
        return end >= start;
    }

    private static string GetContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".m4v" => "video/x-m4v",
        ".mov" => "video/quicktime",
        ".mkv" => "video/x-matroska",
        ".webm" => "video/webm",
        ".mp3" => "audio/mpeg",
        ".m4a" => "audio/mp4",
        ".aac" => "audio/aac",
        _ => "application/octet-stream"
    };

    public async ValueTask DisposeAsync() => await StopAsync();
}

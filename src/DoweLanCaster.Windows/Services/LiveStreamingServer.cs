using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DoweLanCaster.Services;

public sealed class LiveStreamingServer : IAsyncDisposable
{
    private WebApplication? _app;
    private string _directory = "";
    private string? _controlStreamUrl;
    private string _controlMediaType = "hls";

    public int Port { get; private set; } = 8766;
    public event Action<string>? RequestLog;

    public async Task StartAsync(
        string directory,
        int port = 8766,
        CancellationToken token = default)
    {
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        if (_app is not null && port != Port)
            throw new InvalidOperationException("The streaming server is already running on a different port.");

        Port = port;
        _directory = directory;

        if (_app is not null)
            return;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");

        var app = builder.Build();

        app.MapGet("/health", () =>
        {
            RequestLog?.Invoke("GET /health -> 200");
            return Results.Text("OK");
        });

        app.MapGet("/control", () => Results.Json(new
        {
            active = !string.IsNullOrWhiteSpace(_controlStreamUrl),
            streamUrl = _controlStreamUrl ?? "",
            mediaType = _controlMediaType
        }));

        app.MapGet("/live/{file}", async (string file, HttpContext context) =>
        {
            string safe = Path.GetFileName(file);
            string path = Path.Combine(_directory, safe);

            if (!File.Exists(path))
            {
                RequestLog?.Invoke($"GET /live/{safe} -> 404");
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType =
                safe.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)
                    ? "application/vnd.apple.mpegurl"
                    : safe.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                        ? "video/mp2t"
                        : "application/octet-stream";

            context.Response.Headers.CacheControl =
                "no-cache, no-store, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";

            try
            {
                await using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                context.Response.ContentLength = fs.Length;
                await fs.CopyToAsync(
                    context.Response.Body,
                    1024 * 1024,
                    context.RequestAborted);

                RequestLog?.Invoke(
                    $"GET /live/{safe} -> 200 ({fs.Length} bytes)");
            }
            catch (FileNotFoundException)
            {
                RequestLog?.Invoke($"GET /live/{safe} -> 404 (expired)");
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });

        await app.StartAsync(token);
        _app = app;
    }

    public void SetControlState(string? streamUrl, string mediaType = "hls")
    {
        _controlStreamUrl = streamUrl;
        _controlMediaType = mediaType;
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (_app is null)
            return;

        var app = _app;
        _app = null;
        _controlStreamUrl = null;
        _directory = "";

        try
        {
            await app.StopAsync(token);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}

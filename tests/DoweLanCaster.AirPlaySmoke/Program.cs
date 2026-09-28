using System.Net;
using System.Net.Http.Headers;
using DoweLanCaster.Services;

if (args.Length != 1 || !File.Exists(args[0]))
    throw new ArgumentException("Pass one existing media file to the AirPlay smoke test.");

const int port = 18875;
await using var server = new MediaStreamingServer();
await server.StartAsync(Path.GetFullPath(args[0]), port);

using var client = new HttpClient();
var page = await client.GetAsync($"http://127.0.0.1:{port}/airplay");
page.EnsureSuccessStatusCode();
var html = await page.Content.ReadAsStringAsync();

if (page.Content.Headers.ContentType?.MediaType != "text/html" ||
    !html.Contains("x-webkit-airplay=\"allow\"", StringComparison.Ordinal) ||
    !html.Contains("<source src=\"/media\"", StringComparison.Ordinal))
{
    throw new InvalidOperationException("The AirPlay page is missing its required video markup.");
}

using var rangeRequest = new HttpRequestMessage(
    HttpMethod.Get,
    $"http://127.0.0.1:{port}/media");
rangeRequest.Headers.Range = new RangeHeaderValue(0, 1023);

var rangeResponse = await client.SendAsync(rangeRequest);
if (rangeResponse.StatusCode != HttpStatusCode.PartialContent ||
    rangeResponse.Content.Headers.ContentLength != 1024 ||
    rangeResponse.Content.Headers.ContentRange?.From != 0 ||
    rangeResponse.Content.Headers.ContentRange?.To != 1023)
{
    throw new InvalidOperationException("The media endpoint did not honor the AirPlay byte-range request.");
}

Console.WriteLine("AirPlay smoke test passed: page=200, attribute=present, media-range=206/1024 bytes.");

var hlsDirectory = Path.Combine(
    Path.GetTempPath(),
    "DoweLanCaster-AirPlaySmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(hlsDirectory);

try
{
    await File.WriteAllTextAsync(
        Path.Combine(hlsDirectory, "index.m3u8"),
        "#EXTM3U\n#EXT-X-VERSION:3\n#EXTINF:2.0,\nseg-000001.ts\n");
    await File.WriteAllBytesAsync(
        Path.Combine(hlsDirectory, "seg-000001.ts"),
        new byte[] { 0x47, 0x40, 0x00, 0x10 });

    await using var hlsServer = new LiveStreamingServer();
    await hlsServer.StartAsync(hlsDirectory, 18876);
    var streamUrl = "http://127.0.0.1:18876/live/index.m3u8";
    var revision = hlsServer.SetControlState(streamUrl);

    var hlsPage = await client.GetAsync("http://127.0.0.1:18876/airplay");
    hlsPage.EnsureSuccessStatusCode();
    var hlsHtml = await hlsPage.Content.ReadAsStringAsync();

    if (!hlsHtml.Contains(streamUrl, StringComparison.Ordinal) ||
        !hlsHtml.Contains("application/vnd.apple.mpegurl", StringComparison.Ordinal) ||
        !hlsHtml.Contains("video.src = withCacheBust(streamUrl, revision);", StringComparison.Ordinal) ||
        !hlsHtml.Contains("completedRevision=' + currentRevision", StringComparison.Ordinal) ||
        hlsHtml.Contains("window.location.href", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("The HLS AirPlay page does not preserve the AirPlay session during a stream transition.");
    }

    var nextStreamUrl = "http://127.0.0.1:18876/live/index.m3u8?item=next";
    var nextRevision = hlsServer.SetControlState(nextStreamUrl);
    var control = await client.GetStringAsync("http://127.0.0.1:18876/control");
    if (!control.Contains(nextStreamUrl, StringComparison.Ordinal) ||
        !control.Contains($"\"revision\":{nextRevision}", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("The HLS control endpoint did not publish the next folder item.");
    }

    var playlist = await client.GetAsync(streamUrl);
    playlist.EnsureSuccessStatusCode();
    if (playlist.Content.Headers.ContentType?.MediaType != "application/vnd.apple.mpegurl")
        throw new InvalidOperationException("The HLS playlist has the wrong AirPlay media type.");

    Console.WriteLine("AirPlay HLS smoke test passed: page=200, playlist=200, next-item control state and in-player transition present.");
}
finally
{
    Directory.Delete(hlsDirectory, recursive: true);
}

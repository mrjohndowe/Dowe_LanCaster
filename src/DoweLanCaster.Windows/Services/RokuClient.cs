using System.Net.Http;
using System.Xml.Linq;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class RokuClient : IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    public RokuDevice Device { get; }

    public RokuClient(RokuDevice device)
    {
        Device = device;
    }

    public async Task SendKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var safeKey = Uri.EscapeDataString(key);
        using var response = await _httpClient.PostAsync(
            $"http://{Device.IpAddress}:8060/keypress/{safeKey}",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task SendTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        foreach (var c in text)
        {
            var literal = Uri.EscapeDataString(c.ToString());

            using var response = await _httpClient.PostAsync(
                $"http://{Device.IpAddress}:8060/keypress/Lit_{literal}",
                content: null,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyList<RokuApp>> GetAppsAsync(
        CancellationToken cancellationToken = default)
    {
        var xml = await _httpClient.GetStringAsync(
            $"http://{Device.IpAddress}:8060/query/apps",
            cancellationToken);

        var doc = XDocument.Parse(xml);

        return doc.Descendants("app")
            .Select(x => new RokuApp
            {
                Id = x.Attribute("id")?.Value ?? "",
                Name = x.Value.Trim(),
                IconUrl =
                    $"http://{Device.IpAddress}:8060/query/icon/" +
                    Uri.EscapeDataString(x.Attribute("id")?.Value ?? "")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task LaunchAppAsync(
        string appId,
        CancellationToken cancellationToken = default)
    {
        var safeId = Uri.EscapeDataString(appId);

        using var response = await _httpClient.PostAsync(
            $"http://{Device.IpAddress}:8060/launch/{safeId}",
            content: null,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }


    public async Task LaunchDoweLanCasterAsync(
        string streamUrl,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"http://{Device.IpAddress}:8060/launch/dev" +
            $"?streamUrl={Uri.EscapeDataString(streamUrl)}&mediaType=video";

        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task LaunchDoweLanCasterLiveAsync(
        string streamUrl,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"http://{Device.IpAddress}:8060/launch/dev" +
            $"?streamUrl={Uri.EscapeDataString(streamUrl)}&mediaType=hls";

        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }


    public async Task<RokuMediaPlayerState> GetMediaPlayerStateAsync(
        CancellationToken cancellationToken = default)
    {
        var xml = await _httpClient.GetStringAsync(
            $"http://{Device.IpAddress}:8060/query/media-player",
            cancellationToken);

        var doc = XDocument.Parse(xml);
        var player = doc.Descendants("player").FirstOrDefault()
            ?? doc.Root;

        string state =
            player?.Attribute("state")?.Value
            ?? player?.Element("state")?.Value
            ?? "";

        static double? ParseDouble(string? value)
        {
            return double.TryParse(
                value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : null;
        }

        var position =
            ParseDouble(
                player?.Element("position")?.Value
                ?? player?.Attribute("position")?.Value);

        var duration =
            ParseDouble(
                player?.Element("duration")?.Value
                ?? player?.Attribute("duration")?.Value);

        return new RokuMediaPlayerState
        {
            State = state,
            PositionSeconds = position,
            DurationSeconds = duration
        };
    }

    public void Dispose() => _httpClient.Dispose();
}

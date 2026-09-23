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

    public async Task SetVolumeAsync(
        int level,
        CancellationToken cancellationToken = default)
    {
        if (level is < 0 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(level),
                "Volume must be between 0 and 100.");

        var current = await GetAudioDeviceStateAsync(cancellationToken);
        if (current.Volume is null)
            throw new InvalidOperationException("The Roku did not report its current volume.");

        var key = level < current.Volume ? "VolumeDown" : "VolumeUp";
        for (var i = 0; i < Math.Abs(level - current.Volume.Value); i++)
            await SendKeyAsync(key, cancellationToken);
    }

    public async Task<RokuAudioDeviceState> GetAudioDeviceStateAsync(
        CancellationToken cancellationToken = default)
    {
        var xml = await _httpClient.GetStringAsync(
            $"http://{Device.IpAddress}:8060/query/audio-device",
            cancellationToken);
        return ParseAudioDeviceState(xml);
    }

    public static RokuAudioDeviceState ParseAudioDeviceState(string xml)
    {
        var doc = XDocument.Parse(xml);
        var global = doc.Descendants("global").FirstOrDefault();
        var volumeText = global?.Element("volume")?.Value
            ?? global?.Attribute("volume")?.Value;
        var mutedText = global?.Element("muted")?.Value
            ?? global?.Attribute("muted")?.Value;
        var destinations = doc.Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "destination", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute("name")?.Value ?? element.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int? volume = int.TryParse(volumeText, out var parsedVolume) ? parsedVolume : null;
        bool? muted = mutedText?.Trim().ToLowerInvariant() switch
        {
            "true" or "1" => true,
            "false" or "0" => false,
            _ => null
        };
        return new RokuAudioDeviceState(volume, muted, destinations);
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
        string? controlUrl = null,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"http://{Device.IpAddress}:8060/launch/dev" +
            $"?streamUrl={Uri.EscapeDataString(streamUrl)}&mediaType=hls" +
            (string.IsNullOrWhiteSpace(controlUrl)
                ? ""
                : $"&controlUrl={Uri.EscapeDataString(controlUrl)}");

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

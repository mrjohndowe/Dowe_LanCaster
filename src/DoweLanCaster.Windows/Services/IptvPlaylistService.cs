using System.Text.RegularExpressions;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class IptvPlaylistService
{
    public IptvPlaylist LoadM3UPlaylist(string path)
    {
        var extension = Path.GetExtension(path);
        if (!extension.Equals(".m3u", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only M3U and M3U8 playlists are supported.");
        }

        var channels = new List<IptvChannel>();
        string? title = null;
        string? logoUrl = null;
        string? group = null;
        int channelNumber = 1;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                title = ParseExtInfTitle(line);
                logoUrl = ParseExtInfLogo(line);
                group = ParseExtInfGroup(line);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            channels.Add(new IptvChannel
            {
                Url = line,
                Title = title ?? $"Channel {channelNumber}",
                LogoUrl = logoUrl,
                Group = group,
                Number = channelNumber++
            });

            title = null;
            logoUrl = null;
            group = null;
        }

        return new IptvPlaylist
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Channels = channels
        };
    }

    private static string? ParseExtInfTitle(string line)
    {
        var separator = line.IndexOf(',');
        if (separator >= 0)
        {
            var titlePart = line[(separator + 1)..].Trim();
            return string.IsNullOrEmpty(titlePart) ? null : titlePart;
        }
        return null;
    }

    private static string? ParseExtInfLogo(string line)
    {
        var match = Regex.Match(line, @"tvg-logo=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ParseExtInfGroup(string line)
    {
        var match = Regex.Match(line, @"group-title=""([^""]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    public List<IptvChannel> FilterChannels(List<IptvChannel> channels, string? searchText = null, string? groupFilter = null)
    {
        var filtered = channels.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchLower = searchText.ToLower();
            filtered = filtered.Where(c =>
                c.Title.ToLower().Contains(searchLower) ||
                c.Url.ToLower().Contains(searchLower));
        }

        if (!string.IsNullOrWhiteSpace(groupFilter))
        {
            filtered = filtered.Where(c => c.Group?.Equals(groupFilter, StringComparison.OrdinalIgnoreCase) == true);
        }

        return filtered.ToList();
    }

    public List<string> GetGroups(List<IptvChannel> channels)
    {
        return channels
            .Where(c => !string.IsNullOrEmpty(c.Group))
            .Select(c => c.Group!)
            .Distinct()
            .OrderBy(g => g)
            .ToList();
    }
}

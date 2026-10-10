namespace DoweLanCaster.Models;

public sealed class IptvPlaylist
{
    public string Format { get; set; } = "DoweLanCaster.IptvPlaylist";
    public int Version { get; set; } = 1;
    public string? Name { get; set; }
    public List<IptvChannel> Channels { get; set; } = [];
}

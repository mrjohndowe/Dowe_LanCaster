namespace DoweLanCaster.Models;

public sealed class SavedLinkPlaylist
{
    public string Format { get; set; } = "DoweLanCaster.LinkPlaylist";
    public int Version { get; set; } = 1;
    public List<SavedLinkPlaylistItem> Items { get; set; } = [];
}

public sealed class SavedLinkPlaylistItem
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "Video link";
}

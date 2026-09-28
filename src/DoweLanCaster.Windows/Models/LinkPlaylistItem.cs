namespace DoweLanCaster.Models;

public sealed class LinkPlaylistItem
{
    public required string Url { get; init; }
    public string Title { get; set; } = "Video link";
    public int Number { get; set; }

    public string DisplayText => $"{Number}. {Title}{Environment.NewLine}{Url}";
}

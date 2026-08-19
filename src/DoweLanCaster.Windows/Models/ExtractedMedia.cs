namespace DoweLanCaster.Models;

public sealed class ExtractedMedia
{
    public string PageUrl { get; init; } = "";
    public string Title { get; init; } = "";
    public string MediaUrl { get; init; } = "";
    public string Protocol { get; init; } = "";
    public string Extension { get; init; } = "";
    public string? ThumbnailUrl { get; init; }
    public bool IsLive { get; init; }
    public IReadOnlyDictionary<string,string> HttpHeaders { get; init; } =
        new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
}

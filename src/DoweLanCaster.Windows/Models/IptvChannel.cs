namespace DoweLanCaster.Models;

public sealed class IptvChannel
{
    public required string Url { get; init; }
    public string Title { get; set; } = "Channel";
    public string? LogoUrl { get; set; }
    public string? Group { get; set; }
    public int Number { get; set; }

    public string DisplayText => $"{Number}. {Title}{(string.IsNullOrEmpty(Group) ? "" : $" [{Group}]")}{Environment.NewLine}{Url}";
}

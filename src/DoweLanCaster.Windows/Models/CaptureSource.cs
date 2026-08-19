namespace DoweLanCaster.Models;

public enum CaptureSourceType { Desktop, Monitor, Window }

public sealed class CaptureSource
{
    public CaptureSourceType Type { get; init; }
    public string Name { get; init; } = "";
    public string? WindowTitle { get; init; }
    public int Left { get; init; }
    public int Top { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public override string ToString() => Name;
}

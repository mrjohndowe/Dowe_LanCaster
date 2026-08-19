namespace DoweLanCaster.Models;

public enum AudioSourceKind { None, SystemLoopback, DirectShow }

public sealed class AudioSourceOption
{
    public AudioSourceKind Kind { get; init; }
    public string Name { get; init; } = "";
    public string? DeviceName { get; init; }
    public override string ToString() => Name;
}

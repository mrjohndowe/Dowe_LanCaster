namespace DoweLanCaster.Models;

public sealed class AudioCaptureDevice
{
    public string Name { get; init; } = "";
    public override string ToString() => Name;
}

namespace DoweLanCaster.Models;

public sealed record PlaybackEndpoint(string Id, string Name, bool IsDefault)
{
    public override string ToString() => IsDefault ? $"{Name} (Default)" : Name;
}

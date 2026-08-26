namespace DoweLanCaster.Models;

public sealed class RokuApp
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string IconUrl { get; set; } = "";

    public override string ToString() => Name;
}

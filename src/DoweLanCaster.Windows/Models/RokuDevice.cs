namespace DoweLanCaster.Models;

public sealed class RokuDevice
{
    public string Name { get; set; } = "Roku";
    public string IpAddress { get; set; } = "";
    public string Location { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string ModelName { get; set; } = "";
    public string ModelNumber { get; set; } = "";

    public override string ToString()
    {
        var model = string.IsNullOrWhiteSpace(ModelName) ? "" : $" - {ModelName}";
        return $"{Name}{model} ({IpAddress})";
    }
}

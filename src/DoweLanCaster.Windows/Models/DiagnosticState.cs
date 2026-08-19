namespace DoweLanCaster.Models;

public sealed class DiagnosticState
{
    public string Roku { get; set; } = "Not connected";
    public string Ffmpeg { get; set; } = "Not checked";
    public string YtDlp { get; set; } = "Not checked";
    public string Hls { get; set; } = "Stopped";
    public string StreamUrl { get; set; } = "";
    public string LastMessage { get; set; } = "";
}

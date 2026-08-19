namespace DoweLanCaster.Models;

public sealed class AppSettings
{
    public string? LastRokuIp { get; set; }
    public string? PreferredEncoder { get; set; }
    public int PreferredBitrateKbps { get; set; } = 8000;
    public int PreferredFps { get; set; } = 30;
    public string? PreferredAudioSource { get; set; }
    public bool IncludeSystemAudio { get; set; } = true;
    public string? LastFolderPath { get; set; }
    public bool FolderIncludeSubfolders { get; set; } = true;
    public string FolderRepeatMode { get; set; } = "Off";
    public bool FolderAutoPlayNext { get; set; } = true;
}

namespace DoweLanCaster.Models;

public sealed class AppSettings
{
    public string? LastRokuIp { get; set; }
    public string? LastRokuReleaseVersion { get; set; }
    public string? RokuDeveloperPasswordProtected { get; set; }
    public string? RokuDeveloperPasswordSalt { get; set; }
    public string? RokuDeveloperPasswordIp { get; set; }
    public string? PreferredEncoder { get; set; }
    public int PreferredBitrateKbps { get; set; } = 8000;
    public int PreferredFps { get; set; } = 30;
    public string? PreferredAudioSource { get; set; }
    public bool IncludeSystemAudio { get; set; } = true;
    public string PrivateListeningSpeaker { get; set; } = "Windows default playback device";
    public string? PrivateListeningSpeakerId { get; set; }
    public string? LastFolderPath { get; set; }
    public bool FolderIncludeSubfolders { get; set; } = true;
    public string FolderRepeatMode { get; set; } = "Off";
    public bool FolderAutoPlayNext { get; set; } = true;
    public bool UseDarkMode { get; set; }
    public bool UseAirPlayHandoff { get; set; }
    public string? VlcPath { get; set; }
    public int IptvMaxRetries { get; set; } = 3;
    public bool IptvAutoAdvance { get; set; } = true;
    public string? LastIptvPlaylistPath { get; set; }
}

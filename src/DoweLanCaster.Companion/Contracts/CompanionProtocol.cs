namespace DoweLanCaster.Companion.Contracts;

public static class CompanionProtocol
{
    public const int Version = 1;
    public const string ApiPrefix = "/api/v1";
    public const int DefaultPort = 8770;
    public static readonly TimeSpan RequestClockSkew = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(2);
}

public enum CompanionTab
{
    LinkCast, LiveCast, FolderCast, TeraBox, FileCast,
    Remote, AirPlay, Settings, Changelog, Diagnostics
}

public enum CompanionCommandType
{
    SelectTab, DiscoverRokus, SelectRoku, RemoteKey, SetVolume, SendText,
    RefreshApps, LaunchApp, TogglePrivateListening, SelectPrivateListeningEndpoint,
    LinkStart, LinkStop, LiveStart, LiveStop, FolderPlay, FolderPrevious,
    FolderNext, FolderStop, FileStart, FileStop, TeraBoxCast, TeraBoxStop
}

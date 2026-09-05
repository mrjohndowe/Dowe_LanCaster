namespace DoweLanCaster.Models;

public sealed class TeraBoxCredentials
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string PrivateSecret { get; set; } = "";
}

public sealed class TeraBoxSession
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string ApiDomain { get; set; } = "www.terabox.com";
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class TeraBoxDeviceCode
{
    public string DeviceCode { get; init; } = "";
    public string QrCodeDataUrl { get; init; } = "";
    public int ExpiresInSeconds { get; init; }
    public int PollIntervalSeconds { get; init; }
}

public sealed class TeraBoxFileItem
{
    public ulong FileId { get; init; }
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public ulong Size { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsDirectory { get; init; }

    public string TypeText => IsDirectory ? "Folder" : SizeText;

    public string SizeText => Size switch
    {
        >= 1_073_741_824 => $"{Size / 1_073_741_824d:0.##} GB",
        >= 1_048_576 => $"{Size / 1_048_576d:0.##} MB",
        >= 1024 => $"{Size / 1024d:0.##} KB",
        _ => $"{Size} bytes"
    };
}

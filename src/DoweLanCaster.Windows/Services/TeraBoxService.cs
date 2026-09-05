using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class TeraBoxService : IDisposable
{
    private const string AuthorizationHost = "https://www.terabox.com";
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly TeraBoxConnectionStore _store;

    public TeraBoxCredentials Credentials { get; private set; }
    public TeraBoxSession? Session { get; private set; }
    public bool IsConnected => Session is not null && !string.IsNullOrWhiteSpace(Session.AccessToken);

    public TeraBoxService(TeraBoxConnectionStore store)
    {
        _store = store;
        (Credentials, Session) = store.Load();
    }

    public void SaveCredentials(TeraBoxCredentials credentials)
    {
        Credentials = credentials;
        _store.Save(Credentials, Session);
    }

    public async Task<TeraBoxDeviceCode> BeginAuthorizationAsync(CancellationToken token = default)
    {
        RequireCredentials();
        var url = $"{AuthorizationHost}/oauth/devicecode?client_id={Uri.EscapeDataString(Credentials.ClientId)}";
        using var response = await _httpClient.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        ThrowIfError(document.RootElement, "TeraBox could not create an authorization code");
        var data = document.RootElement.GetProperty("data");
        return new TeraBoxDeviceCode
        {
            DeviceCode = ReadString(data, "device_code"),
            QrCodeDataUrl = ReadString(data, "qrcode_url"),
            ExpiresInSeconds = ReadInt(data, "expires_in", 300),
            PollIntervalSeconds = Math.Max(2, ReadInt(data, "interval", 2))
        };
    }

    public async Task CompleteAuthorizationAsync(TeraBoxDeviceCode deviceCode, CancellationToken token = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(deviceCode.ExpiresInSeconds);
        while (DateTimeOffset.UtcNow < expiresAt)
        {
            token.ThrowIfCancellationRequested();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            using var response = await _httpClient.PostAsync(
                $"{AuthorizationHost}/oauth/gettoken",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = Credentials.ClientId,
                    ["client_secret"] = Credentials.ClientSecret,
                    ["grant_type"] = "device_code",
                    ["code"] = deviceCode.DeviceCode,
                    ["timestamp"] = timestamp.ToString(),
                    ["sign"] = CreateSignature(timestamp)
                }),
                token);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            var error = ReadInt(document.RootElement, "errno", -1);
            if (error == 0)
            {
                var data = document.RootElement.GetProperty("data");
                Session = new TeraBoxSession
                {
                    AccessToken = ReadString(data, "access_token"),
                    RefreshToken = ReadString(data, "refresh_token"),
                    ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ReadInt(data, "expires_in", 172800))
                };
                await PopulateApiDomainAsync(token);
                _store.Save(Credentials, Session);
                return;
            }

            if (error is not 300001 and not 400001)
                ThrowIfError(document.RootElement, "TeraBox authorization failed");

            await Task.Delay(TimeSpan.FromSeconds(deviceCode.PollIntervalSeconds), token);
        }

        throw new TimeoutException("The TeraBox authorization code expired. Select Connect again for a new QR code.");
    }

    public async Task<IReadOnlyList<TeraBoxFileItem>> GetDirectoryAsync(
        string directory,
        CancellationToken token = default)
    {
        await EnsureSessionAsync(token);
        var items = new List<TeraBoxFileItem>();

        for (var page = 1; ; page++)
        {
            var url = BuildApiUrl("/openapi/api/list", new Dictionary<string, string>
            {
                ["access_tokens"] = Session!.AccessToken,
                ["order"] = "name",
                ["desc"] = "0",
                ["dir"] = string.IsNullOrWhiteSpace(directory) ? "/" : directory,
                ["num"] = "100",
                ["page"] = page.ToString()
            });
            using var response = await _httpClient.GetAsync(url, token);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            ThrowIfError(document.RootElement, "TeraBox could not list account files");
            if (!document.RootElement.TryGetProperty("list", out var list) || list.ValueKind != JsonValueKind.Array)
                break;

            var count = 0;
            foreach (var entry in list.EnumerateArray())
            {
                count++;
                var name = ReadString(entry, "server_filename");
                var isDirectory = ReadInt(entry, "isdir", 0) == 1;
                items.Add(new TeraBoxFileItem
                {
                    FileId = ReadUlong(entry, "fs_id"),
                    Name = name,
                    Path = ReadString(entry, "path"),
                    Size = ReadUlong(entry, "size"),
                    ThumbnailUrl = ReadThumbnail(entry),
                    IsDirectory = isDirectory
                });

            }

            if (count < 100)
                break;
            }

        return items
            .OrderByDescending(item => item.IsDirectory)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsPlayableVideo(TeraBoxFileItem item) =>
        !item.IsDirectory && IsVideo(item.Name);

    public async Task<string> GetStreamingUrlAsync(string path, CancellationToken token = default)
    {
        await EnsureSessionAsync(token);
        return BuildApiUrl("/openapi/api/streaming", new Dictionary<string, string>
        {
            ["access_tokens"] = Session!.AccessToken,
            ["path"] = path,
            ["type"] = "M3U8_AUTO_720"
        });
    }

    public void Disconnect()
    {
        Session = null;
        Credentials = new TeraBoxCredentials();
        _store.Clear();
    }

    private async Task EnsureSessionAsync(CancellationToken token)
    {
        if (Session is null)
            throw new InvalidOperationException("Connect a TeraBox account first.");
        if (Session.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var response = await _httpClient.PostAsync(
            $"{AuthorizationHost}/oauth/refreshtoken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = Credentials.ClientId,
                ["client_secret"] = Credentials.ClientSecret,
                ["refresh_token"] = Session.RefreshToken,
                ["timestamp"] = timestamp.ToString(),
                ["sign"] = CreateSignature(timestamp)
            }), token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        ThrowIfError(document.RootElement, "TeraBox could not refresh the account connection");
        var data = document.RootElement.GetProperty("data");
        Session.AccessToken = ReadString(data, "access_token");
        Session.RefreshToken = ReadString(data, "refresh_token");
        Session.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ReadInt(data, "expires_in", 172800));
        await PopulateApiDomainAsync(token);
        _store.Save(Credentials, Session);
    }

    private async Task PopulateApiDomainAsync(CancellationToken token)
    {
        using var response = await _httpClient.PostAsync(
            $"{AuthorizationHost}/oauth/tokeninfo",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["access_token"] = Session!.AccessToken
            }), token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        ThrowIfError(document.RootElement, "TeraBox could not validate the account connection");
        var domain = ReadString(document.RootElement.GetProperty("data"), "api_domain");
        Session.ApiDomain = string.IsNullOrWhiteSpace(domain) ? "www.terabox.com" : domain;
    }

    private string BuildApiUrl(string path, IReadOnlyDictionary<string, string> parameters)
    {
        var domain = Session?.ApiDomain ?? "www.terabox.com";
        var scheme = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? "" : "https://";
        return $"{scheme}{domain.TrimEnd('/')}{path}?" +
            string.Join("&", parameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private string CreateSignature(long timestamp)
    {
        var source = $"{Credentials.ClientId}_{timestamp}_{Credentials.ClientSecret}_{Credentials.PrivateSecret}";
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
    }

    private void RequireCredentials()
    {
        if (string.IsNullOrWhiteSpace(Credentials.ClientId) ||
            string.IsNullOrWhiteSpace(Credentials.ClientSecret) ||
            string.IsNullOrWhiteSpace(Credentials.PrivateSecret))
        {
            throw new InvalidOperationException("Enter the TeraBox client ID, client secret, and private secret first.");
        }
    }

    private static void ThrowIfError(JsonElement root, string prefix)
    {
        var error = ReadInt(root, "errno", -1);
        if (error == 0)
            return;
        var message = ReadString(root, "show_msg");
        throw new InvalidOperationException($"{prefix} (error {error}){(string.IsNullOrWhiteSpace(message) ? "." : $": {message}")}");
    }

    private static bool IsVideo(string name) =>
        new[] { ".mp4", ".m4v", ".mov", ".mkv", ".webm", ".avi", ".ts", ".m2ts" }
            .Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);

    private static string? ReadThumbnail(JsonElement entry)
    {
        if (!entry.TryGetProperty("thumbs", out var thumbs) || thumbs.ValueKind != JsonValueKind.Object)
            return null;
        foreach (var key in new[] { "url3", "url2", "url1", "icon" })
        {
            var value = ReadString(thumbs, key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.ToString() : "";
    private static int ReadInt(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : fallback;
    private static ulong ReadUlong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetUInt64(out var result) ? result : 0;

    public void Dispose() => _httpClient.Dispose();
}

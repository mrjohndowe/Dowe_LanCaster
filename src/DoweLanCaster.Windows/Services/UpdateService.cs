using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoweLanCaster.Services;

public sealed class UpdateService
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/mrjohndowe/Dowe_LanCaster/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public Version CurrentVersion { get; } = GetCurrentVersion();

    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(
            LatestReleaseUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(
                "GitHub returned an empty release response.");

        var latestVersion = ParseVersion(release.TagName)
            ?? throw new InvalidOperationException(
                $"The latest release tag '{release.TagName}' is not a valid version.");

        var installer = release.Assets.FirstOrDefault(
            asset =>
                asset.Name.StartsWith(
                    "Dowe-LanCaster-v",
                    StringComparison.OrdinalIgnoreCase) &&
                asset.Name.EndsWith(
                    "-Setup.exe",
                    StringComparison.OrdinalIgnoreCase));

        return new UpdateCheckResult(
            CurrentVersion,
            latestVersion,
            latestVersion > CurrentVersion,
            release.HtmlUrl,
            installer?.Name,
            installer?.DownloadUrl);
    }

    public async Task<string> DownloadInstallerAsync(
        UpdateCheckResult update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!update.IsUpdateAvailable ||
            string.IsNullOrWhiteSpace(update.InstallerName) ||
            string.IsNullOrWhiteSpace(update.InstallerDownloadUrl))
        {
            throw new InvalidOperationException(
                "The release does not contain a Windows installer.");
        }

        var updateDirectory = Path.Combine(
            Path.GetTempPath(),
            "DoweLanCaster",
            "Updates",
            update.LatestVersion.ToString());

        Directory.CreateDirectory(updateDirectory);

        var installerPath = Path.Combine(
            updateDirectory,
            Path.GetFileName(update.InstallerName));

        using var response = await HttpClient.GetAsync(
            update.InstallerDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var expectedBytes =
            response.Content.Headers.ContentLength.GetValueOrDefault();
        if (expectedBytes <= 0 || expectedBytes > 500_000_000)
        {
            throw new InvalidOperationException(
                "The installer size reported by GitHub is invalid.");
        }

        await using var source =
            await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = new FileStream(
            installerPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        var buffer = new byte[81920];
        long downloadedBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(
                buffer,
                cancellationToken);

            if (bytesRead == 0)
                break;

            await destination.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);

            downloadedBytes += bytesRead;
            progress?.Report(
                (double)downloadedBytes / expectedBytes * 100);
        }

        await destination.FlushAsync(cancellationToken);

        if (downloadedBytes != expectedBytes)
        {
            throw new InvalidOperationException(
                "The installer download did not complete successfully.");
        }

        return installerPath;
    }

    public static void StartInstaller(string installerPath)
    {
        var fullPath = Path.GetFullPath(installerPath);

        if (!File.Exists(fullPath) ||
            !string.Equals(
                Path.GetExtension(fullPath),
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException(
                "The downloaded Windows installer was not found.",
                fullPath);
        }

        Process.Start(
            new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments =
                    "/SILENT /SUPPRESSMSGBOXES /NORESTART " +
                    "/CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                UseShellExecute = true,
                Verb = "runas"
            });
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                "DoweLanCaster",
                GetCurrentVersion().ToString()));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));
        client.DefaultRequestHeaders.Add(
            "X-GitHub-Api-Version",
            "2022-11-28");

        return client;
    }

    private static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return ParseVersion(informationalVersion) ??
            assembly.GetName().Version ??
            new Version(0, 0, 0, 0);
    }

    private static Version? ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var clean = value.Trim().TrimStart('v', 'V');
        var metadataIndex = clean.IndexOfAny(['+', '-']);

        if (metadataIndex >= 0)
            clean = clean[..metadataIndex];

        return Version.TryParse(clean, out var version)
            ? version
            : null;
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset> Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
}

public sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    bool IsUpdateAvailable,
    string ReleaseUrl,
    string? InstallerName,
    string? InstallerDownloadUrl);

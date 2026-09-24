using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DoweLanCaster.Companion.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace DoweLanCaster.Services;

public sealed class CompanionPairingService : IAsyncDisposable
{
    private readonly object _gate = new();
    private WebApplication? _app;
    private CancellationTokenSource? _lifetime;
    private UdpClient? _discoverySocket;
    private Task? _discoveryTask;
    private string? _pairingCode;
    private DateTimeOffset _pairingExpiresAt;
    private readonly string _serverId = Guid.NewGuid().ToString("N");
    private string _activeTab = "Settings";

    public int Port { get; private set; } = CompanionProtocol.DefaultPort;
    public bool IsRunning => _app is not null;
    public string ServerId => _serverId;
    public string? PairingCode => _pairingCode;
    public DateTimeOffset PairingExpiresAt => _pairingExpiresAt;
    public event Action<string>? LogLine;
    public event Action<string, string>? CommandReceived;

    public void RegeneratePairingCode()
    {
        lock (_gate)
        {
            RotatePairingCode();
        }
    }

    public async Task StartAsync(int port = CompanionProtocol.DefaultPort)
    {
        lock (_gate)
        {
            if (_app is not null)
                return;
            Port = port;
            _lifetime = new CancellationTokenSource();
            RotatePairingCode();
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");
        var app = builder.Build();

        app.MapGet("/api/v1/health", () => Results.Ok(new
        {
            protocolVersion = CompanionProtocol.Version,
            service = "Dowe LanCaster Companion",
            serverId = _serverId,
            port = Port
        }));

        app.MapGet("/api/v1/state", () => Results.Ok(new
        {
            protocolVersion = CompanionProtocol.Version,
            serverId = _serverId,
            activeTab = _activeTab,
            tabInfo = GetTabInfo(_activeTab),
            companionConnected = true,
            servicePort = Port
        }));

        app.MapGet("/api/v1/pairing/info", () => Results.Ok(new
        {
            protocolVersion = CompanionProtocol.Version,
            serverId = _serverId,
            port = Port,
            pairingCode = _pairingCode,
            expiresAt = _pairingExpiresAt
        }));

        app.MapPost("/api/v1/pairing/approve", async (HttpRequest request) =>
        {
            var payload = await JsonSerializer.DeserializeAsync<PairingApproval>(
                request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    PropertyNameCaseInsensitive = true
                });
            if (payload is null || string.IsNullOrWhiteSpace(payload.Code) ||
                !string.Equals(payload.Code.Trim(), _pairingCode, StringComparison.Ordinal))
            {
                LogLine?.Invoke("Rejected companion pairing attempt: invalid code.");
                return Results.BadRequest(new { error = "invalid_pairing_code" });
            }

            if (DateTimeOffset.UtcNow > _pairingExpiresAt)
            {
                LogLine?.Invoke("Rejected companion pairing attempt: code expired.");
                return Results.BadRequest(new { error = "pairing_code_expired" });
            }

            var deviceId = string.IsNullOrWhiteSpace(payload.DeviceId)
                ? Guid.NewGuid().ToString("N")
                : payload.DeviceId.Trim();
            RotatePairingCode();
            LogLine?.Invoke($"Android companion paired: {deviceId}.");
            return Results.Ok(new
            {
                protocolVersion = CompanionProtocol.Version,
                serverId = _serverId,
                deviceId,
                message = "Pairing accepted."
            });
        });

        app.MapPost("/api/v1/commands/select-tab", async (HttpRequest request) =>
        {
            var command = await JsonSerializer.DeserializeAsync<CompanionCommand>(request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
            if (command is null || string.IsNullOrWhiteSpace(command.Tab))
                return Results.BadRequest(new { error = "invalid_tab" });
            _activeTab = command.Tab.Trim();
            CommandReceived?.Invoke("SelectTab", command.Tab.Trim());
            LogLine?.Invoke($"Android companion selected the {command.Tab} tab.");
            return Results.Ok(new { success = true, tab = command.Tab.Trim() });
        });

        app.MapPost("/api/v1/commands/remote-key", async (HttpRequest request) =>
        {
            var command = await JsonSerializer.DeserializeAsync<CompanionCommand>(request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
            if (command is null || string.IsNullOrWhiteSpace(command.Key))
                return Results.BadRequest(new { error = "invalid_key" });
            CommandReceived?.Invoke("RemoteKey", command.Key.Trim());
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/v1/commands/remote-text", async (HttpRequest request) =>
        {
            var command = await JsonSerializer.DeserializeAsync<CompanionCommand>(request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
            if (command is null || string.IsNullOrWhiteSpace(command.Value))
                return Results.BadRequest(new { error = "invalid_text" });
            CommandReceived?.Invoke("RemoteText", command.Value);
            return Results.Ok(new { success = true });
        });

        app.MapPost("/api/v1/commands/set-volume", async (HttpRequest request) =>
        {
            var command = await JsonSerializer.DeserializeAsync<CompanionCommand>(request.Body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true });
            if (command is null || string.IsNullOrWhiteSpace(command.Value))
                return Results.BadRequest(new { error = "invalid_volume" });
            CommandReceived?.Invoke("SetVolume", command.Value);
            return Results.Ok(new { success = true });
        });

        app.Lifetime.ApplicationStarted.Register(() =>
            LogLine?.Invoke($"Companion pairing service listening on port {Port}."));

        lock (_gate)
        {
            _app = app;
        }

        await app.StartAsync(_lifetime!.Token);
        StartDiscoveryResponder(_lifetime.Token);
    }

    private static object GetTabInfo(string tab) => tab switch
    {
        "Remote" => new { title = "Roku Remote", description = "Control navigation, playback, volume, power, text input, and private listening.", controls = new[] { "Home", "Back", "Replay", "Power", "D-pad", "Play/Pause", "Volume", "Keyboard text" } },
        "Link Cast" => new { title = "Link Cast", description = "Paste a media link, choose the encoder and quality, then cast it to the selected Roku.", controls = new[] { "Media link", "Analyze", "Encoder", "Video bitrate", "Start cast", "Stop cast" } },
        "Live Cast" => new { title = "Live Cast", description = "Stream the selected desktop, window, screen, and PC audio source to Roku.", controls = new[] { "Capture source", "PC audio", "Encoder", "Frame rate", "Video bitrate", "Start live cast", "Stop live cast" } },
        "Folder Cast" => new { title = "Folder Cast", description = "Play a folder playlist on Roku with next, previous, pause, and stop controls.", controls = new[] { "Folder playlist", "Play", "Previous", "Next", "Stop" } },
        "TeraBox" => new { title = "TeraBox", description = "Browse TeraBox in the embedded browser and cast a detected video to Roku.", controls = new[] { "Browser", "Back", "Home", "Refresh", "Cast detected video", "Stop cast" } },
        "Settings" => new { title = "Settings", description = "Manage encoder, audio, playback, AirPlay, companion, and application preferences.", controls = new[] { "Encoder", "Audio source", "Playback", "AirPlay", "Companion", "Save settings" } },
        "Diagnostics" => new { title = "Diagnostics", description = "Inspect Roku connectivity, streaming endpoints, FFmpeg, audio, and verbose logs.", controls = new[] { "Roku status", "FFmpeg status", "Audio status", "Verbose diagnostics", "Refresh" } },
        _ => new { title = tab, description = "This Dowe LanCaster tab is selected on the PC.", controls = Array.Empty<string>() }
    };

    public async Task StopAsync()
    {
        WebApplication? app;
        CancellationTokenSource? lifetime;
        lock (_gate)
        {
            app = _app;
            lifetime = _lifetime;
            _app = null;
            _lifetime = null;
            _pairingCode = null;
        }

        if (app is not null)
            await app.StopAsync();
        if (lifetime is not null)
            lifetime.Dispose();
        _discoverySocket?.Dispose();
        _discoverySocket = null;
        if (_discoveryTask is not null)
        {
            try { await _discoveryTask; } catch (OperationCanceledException) { }
            _discoveryTask = null;
        }
        if (app is not null)
            await app.DisposeAsync();
    }

    private void StartDiscoveryResponder(CancellationToken cancellationToken)
    {
        _discoverySocket = new UdpClient(8771)
        {
            EnableBroadcast = true
        };
        _discoveryTask = Task.Run(async () =>
        {
            var discoveryToken = Encoding.UTF8.GetBytes("DOWE_LANCASTER_DISCOVER");
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult request;
                try
                {
                    request = await _discoverySocket.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }

                if (!request.Buffer.AsSpan().SequenceEqual(discoveryToken))
                    continue;

                var response = Encoding.UTF8.GetBytes($"DOWE_LANCASTER_PC|{Port}");
                try
                {
                    await _discoverySocket.SendAsync(response, response.Length, request.RemoteEndPoint);
                    LogLine?.Invoke($"Android discovery response sent to {request.RemoteEndPoint.Address}.");
                }
                catch (ObjectDisposedException) { break; }
            }
        }, cancellationToken);
    }

    private void RotatePairingCode()
    {
        Span<byte> random = stackalloc byte[4];
        RandomNumberGenerator.Fill(random);
        var number = BitConverter.ToUInt32(random) % 1_000_000;
        _pairingCode = number.ToString("D6");
        _pairingExpiresAt = DateTimeOffset.UtcNow.Add(CompanionProtocol.PairingLifetime);
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private sealed record PairingApproval(string? Code, string? DeviceId);
    private sealed record CompanionCommand(string? Tab, string? Key, string? Value, string? DeviceId);
}

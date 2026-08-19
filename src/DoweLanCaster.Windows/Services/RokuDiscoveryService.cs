using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class RokuDiscoveryService
{
    private const string SsdpAddress = "239.255.255.250";
    private const int SsdpPort = 1900;
    private const int RokuPort = 8060;

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMilliseconds(1400)
    };

    public async Task<IReadOnlyList<RokuDevice>> DiscoverAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var found = new ConcurrentDictionary<string, RokuDevice>(
            StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(
            DiscoverSsdpAsync(found, timeout, cancellationToken),
            DiscoverSubnetAsync(found, cancellationToken));

        return found.Values
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<RokuDevice?> TryGetDeviceByIpAsync(
        string ip,
        CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(ip, out _))
            return null;

        return await ProbeRokuAsync(ip, cancellationToken);
    }

    private async Task DiscoverSsdpAsync(
        ConcurrentDictionary<string, RokuDevice> found,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        try
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork);

            udp.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            var request =
                "M-SEARCH * HTTP/1.1\r\n" +
                "HOST: 239.255.255.250:1900\r\n" +
                "MAN: \"ssdp:discover\"\r\n" +
                "MX: 3\r\n" +
                "ST: roku:ecp\r\n\r\n";

            var data = Encoding.ASCII.GetBytes(request);
            var endpoint = new IPEndPoint(
                IPAddress.Parse(SsdpAddress),
                SsdpPort);

            for (var i = 0; i < 3; i++)
            {
                await udp.SendAsync(data, data.Length, endpoint);
                await Task.Delay(150, cancellationToken);
            }

            var stopAt = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < stopAt &&
                   !cancellationToken.IsCancellationRequested)
            {
                var remaining = stopAt - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;

                var receiveTask = udp.ReceiveAsync();
                var delayTask = Task.Delay(remaining, cancellationToken);
                var completed = await Task.WhenAny(receiveTask, delayTask);

                if (completed != receiveTask)
                    break;

                var result = await receiveTask;
                var response = Encoding.UTF8.GetString(result.Buffer);
                var location = GetHeader(response, "LOCATION");

                if (string.IsNullOrWhiteSpace(location) ||
                    !Uri.TryCreate(location, UriKind.Absolute, out var uri))
                    continue;

                var device = await ProbeRokuAsync(uri.Host, cancellationToken);
                if (device is not null)
                    found.TryAdd(device.IpAddress, device);
            }
        }
        catch
        {
            // SSDP failure is non-fatal because subnet discovery runs too.
        }
    }

    private async Task DiscoverSubnetAsync(
        ConcurrentDictionary<string, RokuDevice> found,
        CancellationToken cancellationToken)
    {
        foreach (var localIp in GetLocalIPv4Addresses())
        {
            var bytes = localIp.GetAddressBytes();
            var tasks = new List<Task>();

            for (var host = 1; host <= 254; host++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var candidate =
                    $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{host}";

                tasks.Add(ProbeAndStoreAsync(
                    candidate,
                    found,
                    cancellationToken));

                if (tasks.Count >= 32)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
    }

    private async Task ProbeAndStoreAsync(
        string ip,
        ConcurrentDictionary<string, RokuDevice> found,
        CancellationToken cancellationToken)
    {
        if (found.ContainsKey(ip))
            return;

        try
        {
            using var tcp = new TcpClient();

            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeout.CancelAfter(TimeSpan.FromMilliseconds(350));

            await tcp.ConnectAsync(
                ip,
                RokuPort,
                timeout.Token);

            if (!tcp.Connected)
                return;

            var device = await ProbeRokuAsync(ip, cancellationToken);
            if (device is not null)
                found.TryAdd(ip, device);
        }
        catch
        {
        }
    }

    private async Task<RokuDevice?> ProbeRokuAsync(
        string ip,
        CancellationToken cancellationToken)
    {
        try
        {
            var xml = await _httpClient.GetStringAsync(
                $"http://{ip}:8060/query/device-info",
                cancellationToken);

            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            if (root is null)
                return null;

            string Value(string name) =>
                root.Element(name)?.Value?.Trim() ?? "";

            var modelName = FirstNonEmpty(
                Value("friendly-model-name"),
                Value("model-name"));

            var serial = Value("serial-number");

            if (string.IsNullOrWhiteSpace(modelName) &&
                string.IsNullOrWhiteSpace(serial))
                return null;

            return new RokuDevice
            {
                IpAddress = ip,
                Location = $"http://{ip}:8060/",
                Name = FirstNonEmpty(
                    Value("user-device-name"),
                    Value("friendly-device-name"),
                    modelName,
                    $"Roku {ip}"),
                SerialNumber = serial,
                ModelName = modelName,
                ModelNumber = Value("model-number")
            };
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<IPAddress> GetLocalIPv4Addresses()
    {
        var result = new List<IPAddress>();

        foreach (var networkInterface in
                 NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            if (networkInterface.NetworkInterfaceType
                is NetworkInterfaceType.Loopback
                or NetworkInterfaceType.Tunnel)
                continue;

            foreach (var address in
                     networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (IPAddress.IsLoopback(address.Address))
                    continue;

                if (address.Address.ToString().StartsWith("169.254."))
                    continue;

                result.Add(address.Address);
            }
        }

        return result;
    }

    private static string GetHeader(
        string response,
        string headerName)
    {
        foreach (var line in response.Split(
                     new[] { "\r\n", "\n" },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(':');

            if (separator <= 0)
                continue;

            var name = line[..separator].Trim();

            if (!name.Equals(
                    headerName,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            return line[(separator + 1)..].Trim();
        }

        return "";
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(
            value => !string.IsNullOrWhiteSpace(value))
        ?? "";
}

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DoweLanCaster.Services;

public static class NetworkHelper
{
    public static string? GetBestLocalIPv4ForRemote(string remoteIp)
    {
        if (IPAddress.TryParse(remoteIp, out var remote))
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remote, 8060);
                if (socket.LocalEndPoint is IPEndPoint local)
                    return local.Address.ToString();
            }
            catch { }
        }

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ua.Address))
                    return ua.Address.ToString();
        }

        return null;
    }
}

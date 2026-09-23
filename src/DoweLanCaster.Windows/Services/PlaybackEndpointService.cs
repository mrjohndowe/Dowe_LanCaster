using DoweLanCaster.Models;
using NAudio.CoreAudioApi;

namespace DoweLanCaster.Services;

public sealed class PlaybackEndpointService
{
    public IReadOnlyList<PlaybackEndpoint> GetActiveEndpoints()
    {
        using var enumerator = new MMDeviceEnumerator();
        string? defaultId = null;
        try
        {
            using var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            defaultId = defaultDevice.ID;
        }
        catch
        {
            // Windows can temporarily have no default endpoint.
        }

        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        return devices
            .Select(device => new PlaybackEndpoint(
                device.ID,
                device.FriendlyName,
                string.Equals(device.ID, defaultId, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(device => device.IsDefault)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}

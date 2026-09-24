using System.Security.Cryptography;
using System.Text;

namespace DoweLanCaster.Companion.Security;

public static class CommandCanonicalizer
{
    public static byte[] Create(
        int protocolVersion, string method, string pathAndQuery, string serverId,
        string sessionId, string deviceId, string requestId, long sequence,
        long timestampUnixMilliseconds, ReadOnlySpan<byte> body)
    {
        ValidateSingleLine(method, nameof(method));
        ValidateSingleLine(pathAndQuery, nameof(pathAndQuery));
        ValidateSingleLine(serverId, nameof(serverId));
        ValidateSingleLine(sessionId, nameof(sessionId));
        ValidateSingleLine(deviceId, nameof(deviceId));
        ValidateSingleLine(requestId, nameof(requestId));

        var bodyHash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        var canonical = string.Join('\n',
            protocolVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            method.ToUpperInvariant(), pathAndQuery, serverId, sessionId, deviceId,
            requestId, sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            timestampUnixMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bodyHash);
        return Encoding.UTF8.GetBytes(canonical);
    }

    private static void ValidateSingleLine(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\r') || value.Contains('\n'))
            throw new ArgumentException("Canonical fields must be non-empty single-line values.", name);
    }
}

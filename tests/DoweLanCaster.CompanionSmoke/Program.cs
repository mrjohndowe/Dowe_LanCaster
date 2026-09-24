using System.Security.Cryptography;
using System.Text;
using DoweLanCaster.Companion.Security;

var body = Encoding.UTF8.GetBytes("{\"tab\":\"Remote\",\"text\":\"Héllo 世界\"}");
var canonical = CommandCanonicalizer.Create(1, "post", "/api/v1/commands",
    "server-1", "session-1", "device-1", "request-1", 1,
    1_800_000_000_000, body);
using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var signature = key.SignData(canonical, HashAlgorithmName.SHA256,
    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
var publicKey = key.ExportSubjectPublicKeyInfo();
if (!EcdsaRequestVerifier.VerifyP256Sha256(publicKey, canonical, signature))
    throw new InvalidOperationException("Valid P-256 signature was rejected.");

body[1] ^= 1;
var changed = CommandCanonicalizer.Create(1, "post", "/api/v1/commands",
    "server-1", "session-1", "device-1", "request-1", 1,
    1_800_000_000_000, body);
if (EcdsaRequestVerifier.VerifyP256Sha256(publicKey, changed, signature))
    throw new InvalidOperationException("Changed request body was accepted.");

var replay = new ReplayGuard(new FixedTimeProvider(
    DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000)));
if (!replay.TryAccept("session-1", "request-1", 1, 1_800_000_000_000,
        TimeSpan.FromSeconds(60), out _))
    throw new InvalidOperationException("First request was rejected.");
if (replay.TryAccept("session-1", "request-1", 2, 1_800_000_000_000,
        TimeSpan.FromSeconds(60), out var duplicate) || duplicate != "duplicate_request")
    throw new InvalidOperationException("Duplicate request ID was accepted.");
if (replay.TryAccept("session-1", "request-2", 1, 1_800_000_000_000,
        TimeSpan.FromSeconds(60), out var sequence) || sequence != "replayed_sequence")
    throw new InvalidOperationException("Replayed sequence was accepted.");
Console.WriteLine("Companion protocol smoke checks passed.");

sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

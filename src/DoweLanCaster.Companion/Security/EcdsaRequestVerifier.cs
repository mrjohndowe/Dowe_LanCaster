using System.Security.Cryptography;

namespace DoweLanCaster.Companion.Security;

public static class EcdsaRequestVerifier
{
    public static bool VerifyP256Sha256(
        ReadOnlySpan<byte> subjectPublicKeyInfo,
        ReadOnlySpan<byte> canonicalRequest,
        ReadOnlySpan<byte> ieeeP1363Signature)
    {
        if (ieeeP1363Signature.Length != 64) return false;
        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out var bytesRead);
            return bytesRead == subjectPublicKeyInfo.Length && key.KeySize == 256 &&
                key.VerifyData(canonicalRequest, ieeeP1363Signature,
                    HashAlgorithmName.SHA256,
                    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (CryptographicException) { return false; }
    }
}

using System;
using System.Text;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Licensing.Revocation.Internal;

internal static class SignedRevocationListEnvelope
{
    public const string CurrentFormat = "MSRL1";
    public const int Es256SignatureLength = 64;
    private const int MaximumTokenLength = 1048576;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Creates a revocation-list object with the fixed MSRL1 format and ES256 algorithm.</summary>
    public static SignedRevocationList Create(string keyId, byte[] payload, byte[] signature)
    {
        string encodedKeyId = Base64Url.Encode(StrictUtf8.GetBytes(keyId));
        return new SignedRevocationList(
            CurrentFormat,
            LicenseAlgorithms.EcdsaP256Sha256,
            keyId,
            encodedKeyId,
            Base64Url.Encode(payload),
            signature.Length == 0 ? string.Empty : Base64Url.Encode(signature),
            (byte[])payload.Clone(),
            (byte[])signature.Clone());
    }

    /// <summary>Builds the canonical envelope bytes authenticated by the signature.</summary>
    public static byte[] BuildSigningInput(SignedRevocationList list)
    {
        string value = string.Concat(
            list.Format,
            ".",
            list.Algorithm,
            ".",
            list.EncodedKeyId,
            ".",
            list.EncodedPayload);
        return Encoding.ASCII.GetBytes(value);
    }

    /// <summary>Formats an authenticated list as a portable dot-separated token.</summary>
    public static string BuildToken(SignedRevocationList list)
    {
        return string.Concat(
            list.Format,
            ".",
            list.Algorithm,
            ".",
            list.EncodedKeyId,
            ".",
            list.EncodedPayload,
            ".",
            list.EncodedSignature);
    }

    /// <summary>Parses the MSRL1 structure without treating its contents as trusted.</summary>
    public static bool TryParse(string? token, out SignedRevocationList? list)
    {
        list = null;
        if (token is null || string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        string[] parts = token.Split('.');
        if (parts.Length != 5 ||
            !string.Equals(parts[0], CurrentFormat, StringComparison.Ordinal) ||
            string.IsNullOrEmpty(parts[1]) ||
            string.IsNullOrEmpty(parts[2]) ||
            string.IsNullOrEmpty(parts[3]) ||
            string.IsNullOrEmpty(parts[4]))
        {
            return false;
        }

        if (!Base64Url.TryDecode(parts[2], out byte[] keyIdBytes) ||
            !Base64Url.TryDecode(parts[3], out byte[] payload) ||
            !Base64Url.TryDecode(parts[4], out byte[] signature) ||
            signature.Length != Es256SignatureLength)
        {
            return false;
        }

        string keyId;
        try
        {
            keyId = StrictUtf8.GetString(keyIdBytes);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128)
        {
            return false;
        }

        list = new SignedRevocationList(
            parts[0], parts[1], keyId, parts[2], parts[3], parts[4], payload, signature);
        return true;
    }
}

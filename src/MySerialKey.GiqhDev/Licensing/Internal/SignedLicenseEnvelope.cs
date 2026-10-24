using System;
using System.Text;

namespace MySerialKey.GiqhDev.Licensing.Internal;

internal static class SignedLicenseEnvelope
{
    public const string CurrentFormat = "MSL1";
    public const int Es256SignatureLength = LicenseFormatLimits.Es256SignatureBytes;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Creates a signed-license object with the fixed MSL1 version and ES256 algorithm.</summary>
    public static SignedLicense Create(string keyId, byte[] payload, byte[] signature)
    {
        string encodedKeyId = Base64Url.Encode(StrictUtf8.GetBytes(keyId));
        return new SignedLicense(
            CurrentFormat,
            LicenseAlgorithms.EcdsaP256Sha256,
            keyId,
            encodedKeyId,
            Base64Url.Encode(payload),
            signature.Length == 0 ? string.Empty : Base64Url.Encode(signature),
            (byte[])payload.Clone(),
            (byte[])signature.Clone());
    }

    /// <summary>Builds the canonical envelope bytes authenticated by the ECDSA signature.</summary>
    public static byte[] BuildSigningInput(SignedLicense license)
    {
        string value = string.Concat(
            license.Format,
            ".",
            license.Algorithm,
            ".",
            license.EncodedKeyId,
            ".",
            license.EncodedPayload);
        return Encoding.ASCII.GetBytes(value);
    }

    /// <summary>Formats an authenticated license as a portable dot-separated MSL1 token.</summary>
    public static string BuildToken(SignedLicense license)
    {
        return string.Concat(
            license.Format,
            ".",
            license.Algorithm,
            ".",
            license.EncodedKeyId,
            ".",
            license.EncodedPayload,
            ".",
            license.EncodedSignature);
    }

    /// <summary>Parses the MSL1 envelope structure without treating its contents as trusted.</summary>
    public static bool TryParse(string? token, out SignedLicense? license)
    {
        license = null;
        if (token is null ||
            string.IsNullOrWhiteSpace(token) ||
            token.Length > LicenseFormatLimits.MaximumSignedLicenseTokenCharacters)
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
            !Base64Url.TryDecode(parts[4], out byte[] signature))
        {
            return false;
        }

        if (signature.Length != Es256SignatureLength)
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

        if (string.IsNullOrWhiteSpace(keyId) ||
            keyId.Length > LicenseFormatLimits.MaximumKeyIdCharacters ||
            payload.Length > LicenseFormatLimits.MaximumSignedLicensePayloadBytes)
        {
            return false;
        }

        license = new SignedLicense(
            parts[0],
            parts[1],
            keyId,
            parts[2],
            parts[3],
            parts[4],
            payload,
            signature);
        return true;
    }
}

using System;
using System.Security.Cryptography;
using System.Text;

namespace MySerialKey.GiqhDev.Licensing.Internal;

internal static class LicenseVerificationKeyEnvelope
{
    private const string CurrentFormat = "MSPK1";
    private const int CoordinateBytes = 32;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Exports only the public P-256 coordinates and their non-secret metadata.</summary>
    public static string BuildToken(LicenseVerificationKey verificationKey)
    {
        ECParameters parameters = verificationKey.ExportParameters();
        byte[] x = parameters.Q.X ?? Array.Empty<byte>();
        byte[] y = parameters.Q.Y ?? Array.Empty<byte>();
        if (x.Length != CoordinateBytes || y.Length != CoordinateBytes)
        {
            throw new CryptographicException("ES256 requires 32-byte P-256 public coordinates.");
        }

        return string.Concat(
            CurrentFormat,
            ".",
            verificationKey.Algorithm,
            ".",
            Base64Url.Encode(StrictUtf8.GetBytes(verificationKey.KeyId)),
            ".",
            Base64Url.Encode(x),
            ".",
            Base64Url.Encode(y));
    }

    /// <summary>Imports a bounded public-key token while accepting only the explicit ES256 policy.</summary>
    public static bool TryParse(string? token, out LicenseVerificationKey? verificationKey)
    {
        verificationKey = null;
        if (token is null ||
            string.IsNullOrWhiteSpace(token) ||
            token.Length > LicenseFormatLimits.MaximumPublicKeyTokenCharacters)
        {
            return false;
        }

        string[] parts = token.Split('.');
        if (parts.Length != 5 ||
            !string.Equals(parts[0], CurrentFormat, StringComparison.Ordinal) ||
            !LicenseAlgorithms.IsSupported(parts[1]) ||
            !Base64Url.TryDecode(parts[2], out byte[] keyIdBytes) ||
            !Base64Url.TryDecode(parts[3], out byte[] x) ||
            !Base64Url.TryDecode(parts[4], out byte[] y) ||
            x.Length != CoordinateBytes ||
            y.Length != CoordinateBytes)
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
            keyId.Length > LicenseFormatLimits.MaximumKeyIdCharacters)
        {
            return false;
        }

        try
        {
            using (ECDsa publicKey = ECDsa.Create())
            {
                publicKey.ImportParameters(new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint
                    {
                        X = x,
                        Y = y
                    }
                });
                verificationKey = new LicenseVerificationKey(keyId, publicKey);
            }

            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is CryptographicException ||
            exception is PlatformNotSupportedException)
        {
            verificationKey = null;
            return false;
        }
    }
}

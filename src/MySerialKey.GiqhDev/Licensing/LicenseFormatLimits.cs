namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Defines the public size limits enforced by the signed-license formats.
/// </summary>
public static class LicenseFormatLimits
{
    /// <summary>Gets the maximum number of characters in an MSL1 token.</summary>
    public const int MaximumSignedLicenseTokenCharacters = 131072;

    /// <summary>Gets the maximum number of bytes in an authenticated MSL1 payload.</summary>
    public const int MaximumSignedLicensePayloadBytes = 97280;

    /// <summary>Gets the maximum UTF-8 byte count of an individual license claim text.</summary>
    public const int MaximumClaimTextUtf8Bytes = 16384;

    /// <summary>Gets the maximum number of features supplied before canonical deduplication.</summary>
    public const int MaximumFeatures = 256;

    /// <summary>Gets the maximum number of characters in a signing-key identifier.</summary>
    public const int MaximumKeyIdCharacters = 128;

    /// <summary>Gets the exact byte count of an ES256 IEEE P1363 signature.</summary>
    public const int Es256SignatureBytes = 64;

    /// <summary>Gets the maximum number of characters in an MSPK1 public-key token.</summary>
    public const int MaximumPublicKeyTokenCharacters = 2048;
}

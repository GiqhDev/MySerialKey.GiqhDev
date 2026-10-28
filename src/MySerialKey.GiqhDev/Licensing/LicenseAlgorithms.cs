namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Defines the cryptographic algorithms supported by signed licenses.
/// </summary>
public static class LicenseAlgorithms
{
    /// <summary>
    /// ECDSA using the NIST P-256 curve and SHA-256 with IEEE P1363 signatures.
    /// </summary>
    public const string EcdsaP256Sha256 = "ES256";

    /// <summary>
    /// Reports whether an algorithm is explicitly allowed for license verification.
    /// </summary>
    public static bool IsSupported(string? algorithm)
    {
        return string.Equals(algorithm, EcdsaP256Sha256, System.StringComparison.Ordinal);
    }
}

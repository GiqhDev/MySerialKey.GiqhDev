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
}

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Resolves trusted public keys without requiring validators to know how they are stored.
/// </summary>
public interface ILicenseVerificationKeyProvider
{
    /// <summary>
    /// Finds a trusted key by its authenticated identifier and algorithm.
    /// </summary>
    /// <returns>The trusted key, or <see langword="null"/> when it is unknown.</returns>
    LicenseVerificationKey? Find(string keyId, string algorithm);
}

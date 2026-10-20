using System.Security.Cryptography;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Issues signed licenses. Implementations belong in trusted administrative or server code.
/// </summary>
public interface ILicenseSigner
{
    /// <summary>
    /// Signs claims using a private P-256 key.
    /// </summary>
    SignedLicense Sign(LicenseClaims claims, string keyId, ECDsa privateKey);
}

using System.Security.Cryptography;

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Issues portable signed revocation lists.</summary>
public interface IRevocationListSigner
{
    /// <summary>Signs validated revocation-list claims with an issuer private key.</summary>
    SignedRevocationList Sign(RevocationListClaims claims, string keyId, ECDsa privateKey);
}

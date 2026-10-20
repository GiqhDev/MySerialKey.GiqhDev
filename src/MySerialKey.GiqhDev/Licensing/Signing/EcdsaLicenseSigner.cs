using System;
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Licensing.Signing;

/// <summary>
/// Issues ES256 signed-license tokens.
/// </summary>
public sealed class EcdsaLicenseSigner : ILicenseSigner
{
    /// <inheritdoc />
    public SignedLicense Sign(LicenseClaims claims, string keyId, ECDsa privateKey)
    {
        Guard.NotNull(claims, nameof(claims));
        Guard.NotNull(keyId, nameof(keyId));
        Guard.NotNull(privateKey, nameof(privateKey));
        LicenseClaimsSerializer.ValidateForSigning(claims);

        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("Key identifier cannot be empty.", nameof(keyId));
        }

        if (privateKey.KeySize != 256)
        {
            throw new ArgumentException("ES256 requires a P-256 key.", nameof(privateKey));
        }

        byte[] payload = LicenseClaimsSerializer.Serialize(claims);
        SignedLicense unsignedLicense = SignedLicenseEnvelope.Create(keyId, payload, Array.Empty<byte>());
        byte[] signingInput = SignedLicenseEnvelope.BuildSigningInput(unsignedLicense);
        byte[] signature = privateKey.SignData(signingInput, HashAlgorithmName.SHA256);

        if (signature.Length != SignedLicenseEnvelope.Es256SignatureLength)
        {
            throw new CryptographicException("ES256 must produce a 64-byte IEEE P1363 signature.");
        }

        return SignedLicenseEnvelope.Create(keyId, payload, signature);
    }
}

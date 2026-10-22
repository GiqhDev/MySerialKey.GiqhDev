using System;
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Revocation.Internal;

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Issues ES256 signed offline revocation lists.</summary>
public sealed class EcdsaRevocationListSigner : IRevocationListSigner
{
    /// <inheritdoc />
    public SignedRevocationList Sign(
        RevocationListClaims claims,
        string keyId,
        ECDsa privateKey)
    {
        Guard.NotNull(claims, nameof(claims));
        Guard.NotNull(keyId, nameof(keyId));
        Guard.NotNull(privateKey, nameof(privateKey));
        RevocationListSerializer.ValidateForSigning(claims);

        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128)
        {
            throw new ArgumentException(
                "Key identifier must contain between 1 and 128 characters.",
                nameof(keyId));
        }

        if (privateKey.KeySize != 256)
        {
            throw new ArgumentException("ES256 requires a P-256 key.", nameof(privateKey));
        }

        byte[] payload = RevocationListSerializer.Serialize(claims);
        SignedRevocationList unsignedList =
            SignedRevocationListEnvelope.Create(keyId, payload, Array.Empty<byte>());
        byte[] signingInput = SignedRevocationListEnvelope.BuildSigningInput(unsignedList);
        byte[] signature = privateKey.SignData(signingInput, HashAlgorithmName.SHA256);

        if (signature.Length != SignedRevocationListEnvelope.Es256SignatureLength)
        {
            throw new CryptographicException(
                "ES256 must produce a 64-byte IEEE P1363 signature.");
        }

        return SignedRevocationListEnvelope.Create(keyId, payload, signature);
    }
}

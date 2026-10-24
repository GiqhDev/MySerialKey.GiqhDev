using System;
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Stores a trusted public key used to validate signed licenses.
/// </summary>
public sealed class LicenseVerificationKey
{
    private readonly ECParameters _parameters;

    /// <summary>
    /// Initializes a trusted ES256 public key.
    /// </summary>
    public LicenseVerificationKey(string keyId, ECDsa publicKey)
    {
        Guard.NotNull(keyId, nameof(keyId));
        Guard.NotNull(publicKey, nameof(publicKey));

        if (string.IsNullOrWhiteSpace(keyId) ||
            keyId.Length > LicenseFormatLimits.MaximumKeyIdCharacters)
        {
            throw new ArgumentException(
                "Key identifier must contain between 1 and 128 characters.",
                nameof(keyId));
        }

        if (publicKey.KeySize != 256)
        {
            throw new ArgumentException("ES256 requires a P-256 key.", nameof(publicKey));
        }

        KeyId = keyId;
        Algorithm = LicenseAlgorithms.EcdsaP256Sha256;
        _parameters = Clone(publicKey.ExportParameters(false));
    }

    /// <summary>
    /// Gets the stable key identifier.
    /// </summary>
    public string KeyId { get; }

    /// <summary>
    /// Gets the signing algorithm.
    /// </summary>
    public string Algorithm { get; }

    /// <summary>
    /// Exports this non-secret public key as a portable MSPK1 token.
    /// </summary>
    public string Export()
    {
        return LicenseVerificationKeyEnvelope.BuildToken(this);
    }

    /// <summary>
    /// Parses a portable MSPK1 public-key token.
    /// </summary>
    public static LicenseVerificationKey Parse(string token)
    {
        if (!TryParse(token, out LicenseVerificationKey? verificationKey))
        {
            throw new FormatException("The public verification-key token is malformed.");
        }

        return verificationKey!;
    }

    /// <summary>
    /// Attempts to parse a portable MSPK1 public-key token.
    /// </summary>
    public static bool TryParse(string? token, out LicenseVerificationKey? verificationKey)
    {
        return LicenseVerificationKeyEnvelope.TryParse(token, out verificationKey);
    }

    /// <summary>Creates a disposable ECDSA instance from the stored public parameters.</summary>
    internal ECDsa CreatePublicKey()
    {
        ECDsa key = ECDsa.Create();
        key.ImportParameters(Clone(_parameters));
        return key;
    }

    /// <summary>Returns a defensive copy of the stored public parameters for portable export.</summary>
    internal ECParameters ExportParameters()
    {
        return Clone(_parameters);
    }

    /// <summary>Copies public curve parameters so callers may safely dispose their original key object.</summary>
    private static ECParameters Clone(ECParameters parameters)
    {
        return new ECParameters
        {
            Curve = parameters.Curve,
            Q = new ECPoint
            {
                X = parameters.Q.X is null ? null : (byte[])parameters.Q.X.Clone(),
                Y = parameters.Q.Y is null ? null : (byte[])parameters.Q.Y.Clone()
            }
        };
    }
}

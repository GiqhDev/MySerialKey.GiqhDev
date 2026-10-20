using System;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Represents a portable, signed license token.
/// </summary>
public sealed class SignedLicense
{
    internal SignedLicense(
        string format,
        string algorithm,
        string keyId,
        string encodedKeyId,
        string encodedPayload,
        string encodedSignature,
        byte[] payload,
        byte[] signature)
    {
        Format = format;
        Algorithm = algorithm;
        KeyId = keyId;
        EncodedKeyId = encodedKeyId;
        EncodedPayload = encodedPayload;
        EncodedSignature = encodedSignature;
        Payload = payload;
        Signature = signature;
    }

    /// <summary>
    /// Gets the token format marker.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the cryptographic algorithm.
    /// </summary>
    public string Algorithm { get; }

    /// <summary>
    /// Gets the public-key identifier selected by the issuer.
    /// </summary>
    public string KeyId { get; }

    internal string EncodedKeyId { get; }
    internal string EncodedPayload { get; }
    internal string EncodedSignature { get; }
    internal byte[] Payload { get; }
    internal byte[] Signature { get; }

    /// <summary>
    /// Parses and validates the structure of a signed-license token.
    /// </summary>
    public static SignedLicense Parse(string token)
    {
        if (!TryParse(token, out SignedLicense? license))
        {
            throw new FormatException("The signed-license token is malformed.");
        }

        return license!;
    }

    /// <summary>
    /// Attempts to parse a signed-license token.
    /// </summary>
    public static bool TryParse(string? token, out SignedLicense? license)
    {
        return SignedLicenseEnvelope.TryParse(token, out license);
    }

    /// <summary>
    /// Returns the portable token representation.
    /// </summary>
    public override string ToString()
    {
        return SignedLicenseEnvelope.BuildToken(this);
    }
}

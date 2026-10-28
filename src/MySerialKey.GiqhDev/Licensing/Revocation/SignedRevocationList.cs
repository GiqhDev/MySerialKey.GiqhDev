using System;
using MySerialKey.GiqhDev.Licensing.Revocation.Internal;

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Represents a portable, signed offline revocation-list token.</summary>
public sealed class SignedRevocationList
{
    internal SignedRevocationList(
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

    /// <summary>Gets the token format marker.</summary>
    public string Format { get; }

    /// <summary>Gets the cryptographic algorithm.</summary>
    public string Algorithm { get; }

    /// <summary>Gets the public-key identifier selected by the issuer.</summary>
    public string KeyId { get; }

    internal string EncodedKeyId { get; }
    internal string EncodedPayload { get; }
    internal string EncodedSignature { get; }
    internal byte[] Payload { get; }
    internal byte[] Signature { get; }

    /// <summary>Parses the structure of a signed revocation-list token.</summary>
    public static SignedRevocationList Parse(string token)
    {
        if (!TryParse(token, out SignedRevocationList? list))
        {
            throw new FormatException("The signed revocation-list token is malformed.");
        }

        return list!;
    }

    /// <summary>Attempts to parse a signed revocation-list token.</summary>
    public static bool TryParse(string? token, out SignedRevocationList? list)
    {
        return SignedRevocationListEnvelope.TryParse(token, out list);
    }

    /// <summary>Returns the portable token representation.</summary>
    public override string ToString()
    {
        return SignedRevocationListEnvelope.BuildToken(this);
    }
}

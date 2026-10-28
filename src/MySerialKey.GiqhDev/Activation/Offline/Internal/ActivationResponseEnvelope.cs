using System.Text;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Activation.Offline.Internal;

internal static class ActivationResponseEnvelope
{
    public const string CurrentFormat = "MSAP1";
    private const int MaximumTokenLength = 262144;
    private const int SignatureLength = 64;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <summary>Creates an MSAP1 response and signs its authenticated envelope with ES256.</summary>
    public static ActivationResponse Create(
        string keyId,
        ActivationResponsePayload values,
        byte[] signature)
    {
        byte[] payload = ActivationResponseSerializer.Serialize(values);
        string encodedKeyId = Base64Url.Encode(StrictUtf8.GetBytes(keyId));
        return new ActivationResponse(
            CurrentFormat,
            LicenseAlgorithms.EcdsaP256Sha256,
            keyId,
            encodedKeyId,
            Base64Url.Encode(payload),
            signature.Length == 0 ? string.Empty : Base64Url.Encode(signature),
            payload,
            signature,
            values);
    }

    /// <summary>Builds the exact version, algorithm, key identifier, and payload bytes covered by the signature.</summary>
    public static byte[] BuildSigningInput(ActivationResponse response)
    {
        return Encoding.ASCII.GetBytes(string.Concat(
            response.Format,
            ".",
            response.Algorithm,
            ".",
            response.EncodedKeyId,
            ".",
            response.EncodedPayload));
    }

    /// <summary>Formats a signed activation response as a portable MSAP1 token.</summary>
    public static string BuildToken(ActivationResponse response)
    {
        return string.Concat(
            response.Format,
            ".",
            response.Algorithm,
            ".",
            response.EncodedKeyId,
            ".",
            response.EncodedPayload,
            ".",
            response.EncodedSignature);
    }

    /// <summary>Parses an MSAP1 token without trusting or validating its signature.</summary>
    public static bool TryParse(string? token, out ActivationResponse? response)
    {
        response = null;
        if (token is null || string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        string[] parts = token.Split('.');
        if (parts.Length != 5 ||
            !string.Equals(parts[0], CurrentFormat, StringComparison.Ordinal) ||
            string.IsNullOrEmpty(parts[1]) ||
            !Base64Url.TryDecode(parts[2], out byte[] keyIdBytes) ||
            !Base64Url.TryDecode(parts[3], out byte[] payload) ||
            !Base64Url.TryDecode(parts[4], out byte[] signature) ||
            signature.Length != SignatureLength)
        {
            return false;
        }

        try
        {
            string keyId = StrictUtf8.GetString(keyIdBytes);
            if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128)
            {
                return false;
            }

            ActivationResponsePayload values = ActivationResponseSerializer.Deserialize(payload);
            response = new ActivationResponse(
                parts[0],
                parts[1],
                keyId,
                parts[2],
                parts[3],
                parts[4],
                payload,
                signature,
                values);
            return true;
        }
        catch (Exception exception) when (
            exception is FormatException ||
            exception is DecoderFallbackException)
        {
            return false;
        }
    }
}

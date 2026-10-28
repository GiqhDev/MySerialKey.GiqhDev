using System.Text;
using MySerialKey.GiqhDev.Activation.Offline.Internal;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>
/// Represents a signed offline activation response containing a normal MSL1 license.
/// </summary>
public sealed class ActivationResponse
{
    private readonly byte[] _requestNonceHash;

    internal ActivationResponse(
        string format,
        string algorithm,
        string keyId,
        string encodedKeyId,
        string encodedPayload,
        string encodedSignature,
        byte[] payload,
        byte[] signature,
        ActivationResponsePayload values)
    {
        Format = format;
        Algorithm = algorithm;
        KeyId = keyId;
        EncodedKeyId = encodedKeyId;
        EncodedPayload = encodedPayload;
        EncodedSignature = encodedSignature;
        Payload = (byte[])payload.Clone();
        Signature = (byte[])signature.Clone();
        RequestId = values.RequestId;
        _requestNonceHash = (byte[])values.RequestNonceHash.Clone();
        ProductCode = values.ProductCode;
        InstallationId = values.InstallationId;
        IssuedAtUtc = values.IssuedAtUtc;
        ExpiresAtUtc = values.ExpiresAtUtc;
        License = values.License;
    }

    /// <summary>Gets the response format marker.</summary>
    public string Format { get; }

    /// <summary>Gets the signature algorithm.</summary>
    public string Algorithm { get; }

    /// <summary>Gets the response signing-key identifier.</summary>
    public string KeyId { get; }

    /// <summary>Gets the request identifier authenticated by the response.</summary>
    public string RequestId { get; }

    /// <summary>Gets a copy of the authenticated request nonce hash.</summary>
    public byte[] RequestNonceHash => (byte[])_requestNonceHash.Clone();

    /// <summary>Gets the authenticated product code.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the authenticated installation identifier.</summary>
    public string InstallationId { get; }

    /// <summary>Gets the response issue time.</summary>
    public DateTimeOffset IssuedAtUtc { get; }

    /// <summary>Gets the response import deadline.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the standard signed license carried by the response.</summary>
    public SignedLicense License { get; }

    internal string EncodedKeyId { get; }
    internal string EncodedPayload { get; }
    internal string EncodedSignature { get; }
    internal byte[] Payload { get; }
    internal byte[] Signature { get; }

    /// <summary>Parses a portable signed activation-response token.</summary>
    public static ActivationResponse Parse(string token)
    {
        if (!TryParse(token, out ActivationResponse? response))
        {
            throw new FormatException("The activation-response token is malformed.");
        }

        return response!;
    }

    /// <summary>Attempts to parse a portable signed activation-response token.</summary>
    public static bool TryParse(string? token, out ActivationResponse? response)
    {
        return ActivationResponseEnvelope.TryParse(token, out response);
    }

    /// <summary>Loads a UTF-8 activation-response token from a file.</summary>
    public static ActivationResponse Load(string path)
    {
        Guard.NotNull(path, nameof(path));
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > 1024 * 1024)
        {
            throw new FormatException("The activation-response file size is invalid.");
        }

        return Parse(File.ReadAllText(path, new UTF8Encoding(false, true)).Trim());
    }

    /// <summary>Saves the portable activation-response token as UTF-8 text.</summary>
    public void Save(string path)
    {
        Guard.NotNull(path, nameof(path));
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToString(), new UTF8Encoding(false));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ActivationResponseEnvelope.BuildToken(this);
    }
}

using System.Text;
using MySerialKey.GiqhDev.Activation.Offline.Internal;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>
/// Represents a portable, versioned offline activation request.
/// </summary>
public sealed class ActivationRequest
{
    private readonly byte[] _nonce;

    internal ActivationRequest(
        string requestId,
        byte[] nonce,
        string productCode,
        string installationId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? applicationVersion)
    {
        RequestId = requestId;
        _nonce = (byte[])nonce.Clone();
        ProductCode = productCode;
        InstallationId = installationId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ApplicationVersion = applicationVersion;
    }

    /// <summary>Gets the format marker.</summary>
    public string Format => ActivationRequestEnvelope.CurrentFormat;

    /// <summary>Gets the unique request identifier.</summary>
    public string RequestId { get; }

    /// <summary>Gets a copy of the cryptographic request nonce.</summary>
    public byte[] Nonce => (byte[])_nonce.Clone();

    /// <summary>Gets the target product.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the target installation.</summary>
    public string InstallationId { get; }

    /// <summary>Gets when the request was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Gets when the request must stop being accepted.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the optional consumer application version.</summary>
    public string? ApplicationVersion { get; }

    internal byte[] NonceBytes => _nonce;

    /// <summary>Creates a request using cryptographically random identity material.</summary>
    public static ActivationRequest Create(
        string productCode,
        string installationId,
        DateTimeOffset nowUtc,
        TimeSpan? validity = null,
        string? applicationVersion = null)
    {
        TimeSpan requestValidity = validity ?? TimeSpan.FromDays(7);
        if (requestValidity <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validity),
                "Request validity must be greater than zero.");
        }

        DateTimeOffset createdAtUtc = nowUtc.ToUniversalTime();
        var request = new ActivationRequest(
            Guid.NewGuid().ToString("N"),
            SecureRandom.GetBytes(32),
            productCode,
            installationId,
            createdAtUtc,
            createdAtUtc.Add(requestValidity),
            applicationVersion);
        ActivationRequestSerializer.Validate(request);
        return request;
    }

    /// <summary>Parses a portable activation-request token.</summary>
    public static ActivationRequest Parse(string token)
    {
        if (!TryParse(token, out ActivationRequest? request))
        {
            throw new FormatException("The activation-request token is malformed.");
        }

        return request!;
    }

    /// <summary>Attempts to parse a portable activation-request token.</summary>
    public static bool TryParse(string? token, out ActivationRequest? request)
    {
        return ActivationRequestEnvelope.TryParse(token, out request);
    }

    /// <summary>Loads a UTF-8 activation-request token from a file.</summary>
    public static ActivationRequest Load(string path)
    {
        return Parse(ReadToken(path));
    }

    /// <summary>Saves the portable activation-request token as UTF-8 text.</summary>
    public void Save(string path)
    {
        WriteToken(path, ToString());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ActivationRequestEnvelope.BuildToken(this);
    }

    private static string ReadToken(string path)
    {
        Guard.NotNull(path, nameof(path));
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > 1024 * 1024)
        {
            throw new FormatException("The activation-request file size is invalid.");
        }

        return File.ReadAllText(path, new UTF8Encoding(false, true)).Trim();
    }

    private static void WriteToken(string path, string token)
    {
        Guard.NotNull(path, nameof(path));
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, token, new UTF8Encoding(false));
    }
}

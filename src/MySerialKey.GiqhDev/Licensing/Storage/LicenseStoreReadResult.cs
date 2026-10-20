namespace MySerialKey.GiqhDev.Licensing.Storage;

/// <summary>
/// Represents a stored-license read result.
/// </summary>
public sealed class LicenseStoreReadResult
{
    internal LicenseStoreReadResult(
        LicenseStoreReadStatus status,
        SignedLicense? license,
        string? message)
    {
        Status = status;
        License = license;
        Message = message;
    }

    /// <summary>Gets the operation status.</summary>
    public LicenseStoreReadStatus Status { get; }

    /// <summary>Gets the parsed signed license when successful.</summary>
    public SignedLicense? License { get; }

    /// <summary>Gets a diagnostic message.</summary>
    public string? Message { get; }
}

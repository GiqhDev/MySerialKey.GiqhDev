namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Represents a typed signed-license validation result.
/// </summary>
public sealed class LicenseValidationResult
{
    private LicenseValidationResult(
        LicenseValidationStatus status,
        LicenseClaims? claims,
        string? message)
    {
        Status = status;
        Claims = claims;
        Message = message;
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsValid =>
        Status == LicenseValidationStatus.Valid ||
        Status == LicenseValidationStatus.GracePeriod;

    /// <summary>
    /// Gets the validation status.
    /// </summary>
    public LicenseValidationStatus Status { get; }

    /// <summary>
    /// Gets authenticated claims when the payload could be read.
    /// </summary>
    public LicenseClaims? Claims { get; }

    /// <summary>
    /// Gets a diagnostic message intended for logs or support tooling.
    /// </summary>
    public string? Message { get; }

    internal static LicenseValidationResult Success(
        LicenseClaims claims,
        LicenseValidationStatus status = LicenseValidationStatus.Valid)
    {
        return new LicenseValidationResult(status, claims, null);
    }

    internal static LicenseValidationResult Failure(
        LicenseValidationStatus status,
        string message,
        LicenseClaims? claims = null)
    {
        return new LicenseValidationResult(status, claims, message);
    }
}

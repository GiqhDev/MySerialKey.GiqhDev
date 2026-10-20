using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>Represents the result of importing a signed offline activation response.</summary>
public sealed class OfflineActivationImportResult
{
    private OfflineActivationImportResult(
        OfflineActivationImportStatus status,
        SignedLicense? license,
        LicenseValidationResult? licenseValidation,
        string? message)
    {
        Status = status;
        License = license;
        LicenseValidation = licenseValidation;
        Message = message;
    }

    /// <summary>Gets a value indicating whether the activation was applied.</summary>
    public bool IsSuccess => Status == OfflineActivationImportStatus.Applied;

    /// <summary>Gets the typed import status.</summary>
    public OfflineActivationImportStatus Status { get; }

    /// <summary>Gets the stored standard MSL1 license when the import succeeded.</summary>
    public SignedLicense? License { get; }

    /// <summary>Gets the embedded-license validation result when it was reached.</summary>
    public LicenseValidationResult? LicenseValidation { get; }

    /// <summary>Gets a diagnostic message intended for logs or support tooling.</summary>
    public string? Message { get; }

    internal static OfflineActivationImportResult Success(
        SignedLicense license,
        LicenseValidationResult licenseValidation)
    {
        return new OfflineActivationImportResult(
            OfflineActivationImportStatus.Applied,
            license,
            licenseValidation,
            null);
    }

    internal static OfflineActivationImportResult Failure(
        OfflineActivationImportStatus status,
        string message,
        LicenseValidationResult? licenseValidation = null)
    {
        return new OfflineActivationImportResult(status, null, licenseValidation, message);
    }
}

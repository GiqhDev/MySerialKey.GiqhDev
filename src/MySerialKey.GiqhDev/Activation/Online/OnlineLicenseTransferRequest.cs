using MySerialKey.GiqhDev.Activation.Online.Internal;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned request to transfer a license between installations.</summary>
public sealed class OnlineLicenseTransferRequest
{
    /// <summary>Initializes an online license-transfer request.</summary>
    public OnlineLicenseTransferRequest(
        SignedLicense currentLicense,
        string productCode,
        string applicationVersion,
        string sourceInstallationId,
        string targetInstallationId,
        string? operationId = null)
    {
        LicenseToken = OnlineContractValidation.LicenseToken(
            currentLicense,
            nameof(currentLicense));
        ProductCode = OnlineContractValidation.Required(productCode, nameof(productCode));
        ApplicationVersion = OnlineContractValidation.Required(
            applicationVersion,
            nameof(applicationVersion));
        SourceInstallationId = OnlineContractValidation.Required(
            sourceInstallationId,
            nameof(sourceInstallationId));
        TargetInstallationId = OnlineContractValidation.Required(
            targetInstallationId,
            nameof(targetInstallationId));
        if (string.Equals(
                SourceInstallationId,
                TargetInstallationId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The target installation must differ from the source installation.",
                nameof(targetInstallationId));
        }

        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
    }

    /// <summary>Gets the wire-contract schema version.</summary>
    public int SchemaVersion => ActivationContractVersions.Current;

    /// <summary>Gets the idempotency identifier for this logical operation.</summary>
    public string OperationId { get; }

    /// <summary>Gets the current portable MSL1 license token.</summary>
    public string LicenseToken { get; }

    /// <summary>Gets the product whose license is being transferred.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the consumer application version.</summary>
    public string ApplicationVersion { get; }

    /// <summary>Gets the currently licensed installation.</summary>
    public string SourceInstallationId { get; }

    /// <summary>Gets the installation that will receive the license.</summary>
    public string TargetInstallationId { get; }
}

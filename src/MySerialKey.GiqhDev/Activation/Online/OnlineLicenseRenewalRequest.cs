using MySerialKey.GiqhDev.Activation.Online.Internal;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned request to renew an existing signed license.</summary>
public sealed class OnlineLicenseRenewalRequest
{
    /// <summary>Initializes an online license-renewal request.</summary>
    public OnlineLicenseRenewalRequest(
        SignedLicense currentLicense,
        string productCode,
        string applicationVersion,
        string installationId,
        string? operationId = null)
    {
        LicenseToken = OnlineContractValidation.LicenseToken(
            currentLicense,
            nameof(currentLicense));
        ProductCode = OnlineContractValidation.Required(productCode, nameof(productCode));
        ApplicationVersion = OnlineContractValidation.Required(
            applicationVersion,
            nameof(applicationVersion));
        InstallationId = OnlineContractValidation.Required(
            installationId,
            nameof(installationId));
        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
    }

    /// <summary>Gets the wire-contract schema version.</summary>
    public int SchemaVersion => ActivationContractVersions.Current;

    /// <summary>Gets the idempotency identifier for this logical operation.</summary>
    public string OperationId { get; }

    /// <summary>Gets the current portable MSL1 license token.</summary>
    public string LicenseToken { get; }

    /// <summary>Gets the product being renewed.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the consumer application version.</summary>
    public string ApplicationVersion { get; }

    /// <summary>Gets the installation being renewed.</summary>
    public string InstallationId { get; }
}

using MySerialKey.GiqhDev.Activation.Online.Internal;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned request to activate a serial for an installation.</summary>
public sealed class OnlineActivationRequest
{
    /// <summary>Initializes an online activation request.</summary>
    public OnlineActivationRequest(
        string serial,
        string productCode,
        string applicationVersion,
        string installationId,
        string? operationId = null)
    {
        Serial = OnlineContractValidation.Required(serial, nameof(serial));
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

    /// <summary>Gets the serial supplied by the consumer.</summary>
    public string Serial { get; }

    /// <summary>Gets the product being activated.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the consumer application version.</summary>
    public string ApplicationVersion { get; }

    /// <summary>Gets the installation being activated.</summary>
    public string InstallationId { get; }
}

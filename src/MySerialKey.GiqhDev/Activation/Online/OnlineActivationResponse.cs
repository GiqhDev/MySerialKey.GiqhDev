using MySerialKey.GiqhDev.Activation.Online.Internal;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned activation or renewal service response.</summary>
public sealed class OnlineActivationResponse
{
    /// <summary>Initializes an online activation response.</summary>
    public OnlineActivationResponse(
        string operationId,
        string licenseToken,
        DateTimeOffset trustedTimeUtc,
        int schemaVersion = ActivationContractVersions.Current)
    {
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        SchemaVersion = schemaVersion;
        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
        LicenseToken = OnlineContractValidation.PortableLicenseToken(
            licenseToken,
            nameof(licenseToken));
        TrustedTimeUtc = trustedTimeUtc.ToUniversalTime();
    }

    /// <summary>Gets the wire-contract schema version.</summary>
    public int SchemaVersion { get; }

    /// <summary>Gets the echoed idempotency identifier.</summary>
    public string OperationId { get; }

    /// <summary>Gets the portable MSL1 license token returned by the service.</summary>
    public string LicenseToken { get; }

    /// <summary>Gets the trusted service time associated with the response.</summary>
    public DateTimeOffset TrustedTimeUtc { get; }
}

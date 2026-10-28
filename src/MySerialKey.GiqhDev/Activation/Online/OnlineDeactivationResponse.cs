using MySerialKey.GiqhDev.Activation.Online.Internal;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned successful deactivation response.</summary>
public sealed class OnlineDeactivationResponse
{
    /// <summary>Initializes an online deactivation response.</summary>
    public OnlineDeactivationResponse(
        string operationId,
        DateTimeOffset trustedTimeUtc,
        int schemaVersion = ActivationContractVersions.Current)
    {
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        SchemaVersion = schemaVersion;
        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
        TrustedTimeUtc = trustedTimeUtc.ToUniversalTime();
    }

    /// <summary>Gets the wire-contract schema version.</summary>
    public int SchemaVersion { get; }

    /// <summary>Gets the echoed idempotency identifier.</summary>
    public string OperationId { get; }

    /// <summary>Gets the trusted service time associated with the response.</summary>
    public DateTimeOffset TrustedTimeUtc { get; }
}

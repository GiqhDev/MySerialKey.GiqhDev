using MySerialKey.GiqhDev.Activation.Online.Internal;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a versioned, machine-readable activation-service error.</summary>
public sealed class ActivationServiceError
{
    /// <summary>Initializes an activation-service error contract.</summary>
    public ActivationServiceError(
        string operationId,
        string errorCode,
        string message,
        bool retryable,
        TimeSpan? retryAfter = null,
        int schemaVersion = ActivationContractVersions.Current)
    {
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }

        if (retryAfter.HasValue && retryAfter.Value < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryAfter));
        }

        SchemaVersion = schemaVersion;
        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
        ErrorCode = OnlineContractValidation.Required(errorCode, nameof(errorCode));
        Message = OnlineContractValidation.Required(message, nameof(message));
        Retryable = retryable;
        RetryAfter = retryAfter;
    }

    /// <summary>Gets the response schema version.</summary>
    public int SchemaVersion { get; }

    /// <summary>Gets the echoed logical operation identifier.</summary>
    public string OperationId { get; }

    /// <summary>Gets the stable, machine-readable service error code.</summary>
    public string ErrorCode { get; }

    /// <summary>Gets a human-readable diagnostic message.</summary>
    public string Message { get; }

    /// <summary>Gets a value indicating whether the same operation may be retried safely.</summary>
    public bool Retryable { get; }

    /// <summary>Gets the recommended retry delay when supplied by the service.</summary>
    public TimeSpan? RetryAfter { get; }
}

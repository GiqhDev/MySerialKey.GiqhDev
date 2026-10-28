using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Activation.Online.Internal;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Represents a typed result returned by an online activation client.</summary>
public sealed class ActivationClientResult
{
    /// <summary>Initializes a typed activation-client result.</summary>
    public ActivationClientResult(
        ActivationClientStatus status,
        string operationId,
        SignedLicense? license = null,
        LicenseValidationResult? licenseValidation = null,
        string? errorCode = null,
        string? message = null,
        TimeSpan? retryAfter = null)
    {
        Status = status;
        OperationId = OnlineContractValidation.OperationId(operationId, nameof(operationId));
        License = license;
        LicenseValidation = licenseValidation;
        ErrorCode = errorCode;
        Message = message;
        RetryAfter = retryAfter;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess => Status == ActivationClientStatus.Success;

    /// <summary>Gets the typed operation status.</summary>
    public ActivationClientStatus Status { get; }

    /// <summary>Gets the logical operation identifier.</summary>
    public string OperationId { get; }

    /// <summary>Gets the validated MSL1 license for activation or renewal.</summary>
    public SignedLicense? License { get; }

    /// <summary>Gets the detailed license-validation result when available.</summary>
    public LicenseValidationResult? LicenseValidation { get; }

    /// <summary>Gets the service-defined stable error code when available.</summary>
    public string? ErrorCode { get; }

    /// <summary>Gets a diagnostic message suitable for logs or support tooling.</summary>
    public string? Message { get; }

    /// <summary>Gets the server-requested delay before retrying when available.</summary>
    public TimeSpan? RetryAfter { get; }
}

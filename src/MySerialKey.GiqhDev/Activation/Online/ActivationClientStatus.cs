namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Describes the outcome of an online activation operation.</summary>
public enum ActivationClientStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,
    /// <summary>The network or remote endpoint was unavailable.</summary>
    NetworkUnavailable,
    /// <summary>The operation exceeded its configured time limit.</summary>
    Timeout,
    /// <summary>The service rejected authentication or authorization.</summary>
    Unauthorized,
    /// <summary>The service rate limit was reached.</summary>
    RateLimited,
    /// <summary>The service rejected the requested licensing operation.</summary>
    ServiceRejected,
    /// <summary>The response did not conform to the supported contract.</summary>
    InvalidResponse,
    /// <summary>The returned license failed cryptographic or contextual validation.</summary>
    InvalidLicense,
    /// <summary>The local license store could not complete the operation.</summary>
    StorageUnavailable
}

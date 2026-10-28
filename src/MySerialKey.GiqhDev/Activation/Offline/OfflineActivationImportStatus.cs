namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>Describes an offline activation import attempt.</summary>
public enum OfflineActivationImportStatus
{
    /// <summary>The response was validated and its MSL1 license was stored.</summary>
    Applied = 0,
    /// <summary>The activation request token is malformed.</summary>
    InvalidRequest,
    /// <summary>The activation response token is malformed.</summary>
    InvalidResponse,
    /// <summary>The activation request is not valid yet.</summary>
    RequestNotYetValid,
    /// <summary>The activation request has expired.</summary>
    RequestExpired,
    /// <summary>The response uses an unsupported signature algorithm.</summary>
    UnsupportedAlgorithm,
    /// <summary>No trusted public key matches the response key identifier.</summary>
    UnknownSigningKey,
    /// <summary>The response signature is invalid.</summary>
    InvalidSignature,
    /// <summary>The activation response is not valid yet.</summary>
    ResponseNotYetValid,
    /// <summary>The activation response has expired.</summary>
    ResponseExpired,
    /// <summary>The response was created for a different request.</summary>
    WrongRequest,
    /// <summary>The request or response belongs to another product.</summary>
    WrongProduct,
    /// <summary>The request or response belongs to another installation.</summary>
    WrongInstallation,
    /// <summary>The embedded MSL1 license did not pass normal license validation.</summary>
    InvalidLicense,
    /// <summary>The activation request was already consumed.</summary>
    ReplayDetected,
    /// <summary>The protected state or license store could not complete the import.</summary>
    StorageUnavailable
}

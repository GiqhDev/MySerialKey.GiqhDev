namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Describes the outcome of signed-license validation.
/// </summary>
public enum LicenseValidationStatus
{
    /// <summary>The license is valid.</summary>
    Valid = 0,
    /// <summary>The token envelope is malformed.</summary>
    InvalidEnvelope,
    /// <summary>The declared signing algorithm is not supported.</summary>
    UnsupportedAlgorithm,
    /// <summary>No trusted public key matches the token key identifier.</summary>
    UnknownSigningKey,
    /// <summary>The signature does not authenticate the token.</summary>
    InvalidSignature,
    /// <summary>The authenticated payload cannot be decoded.</summary>
    Corrupted,
    /// <summary>The payload schema is not supported.</summary>
    UnsupportedSchema,
    /// <summary>The license is not valid yet.</summary>
    NotYetValid,
    /// <summary>The license has expired.</summary>
    Expired,
    /// <summary>The license belongs to another product.</summary>
    WrongProduct,
    /// <summary>The license belongs to another installation.</summary>
    WrongInstallation
}

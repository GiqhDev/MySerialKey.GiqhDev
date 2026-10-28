namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Describes the outcome of validating an offline revocation list.</summary>
public enum RevocationListValidationStatus
{
    /// <summary>The list is authentic, current, and valid for the requested product.</summary>
    Valid = 0,
    /// <summary>The portable token does not have the expected envelope structure.</summary>
    InvalidEnvelope,
    /// <summary>The token declares a cryptographic algorithm that is not supported.</summary>
    UnsupportedAlgorithm,
    /// <summary>No trusted public key matches the token key identifier.</summary>
    UnknownSigningKey,
    /// <summary>The token signature is not valid.</summary>
    InvalidSignature,
    /// <summary>The authenticated payload cannot be decoded or contains invalid values.</summary>
    Corrupted,
    /// <summary>The authenticated payload uses an unsupported schema version.</summary>
    UnsupportedSchema,
    /// <summary>The list was issued after the trusted validation time.</summary>
    NotYetValid,
    /// <summary>The list is no longer current.</summary>
    Expired,
    /// <summary>The list belongs to a different product.</summary>
    WrongProduct,
    /// <summary>The list sequence is older than the minimum accepted sequence.</summary>
    RollbackDetected
}

namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Describes a trial operation or validation outcome.
/// </summary>
public enum TrialValidationStatus
{
    /// <summary>The trial exists and is currently active.</summary>
    Active = 0,
    /// <summary>No trial has been explicitly started.</summary>
    NotStarted,
    /// <summary>A start was requested for an existing trial.</summary>
    AlreadyStarted,
    /// <summary>The trial has reached its expiration time.</summary>
    Expired,
    /// <summary>The clock moved backward beyond the configured tolerance.</summary>
    ClockRollbackDetected,
    /// <summary>The consumer marked the trial as converted to a license.</summary>
    ConvertedToLicense,
    /// <summary>The persisted trial state is malformed or failed authentication.</summary>
    Corrupted,
    /// <summary>The protected state could not be read or written.</summary>
    StorageUnavailable
}

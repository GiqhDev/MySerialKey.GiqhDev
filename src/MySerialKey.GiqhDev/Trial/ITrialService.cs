namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Starts, reads, validates, and completes a product-scoped trial.
/// </summary>
public interface ITrialService
{
    /// <summary>Explicitly starts a trial when no state exists.</summary>
    TrialValidationResult Start(string productCode);

    /// <summary>Reads trial state without creating or updating it.</summary>
    TrialValidationResult Get(string productCode);

    /// <summary>Validates and records a successful run when active.</summary>
    TrialValidationResult Validate(
        string productCode,
        DateTimeOffset? trustedTimeUtc = null);

    /// <summary>Marks an existing trial as converted while preserving its history.</summary>
    TrialValidationResult MarkConvertedToLicense(
        string productCode,
        DateTimeOffset? trustedTimeUtc = null);
}

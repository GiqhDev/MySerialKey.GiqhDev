namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Represents a typed trial operation or validation result.
/// </summary>
public sealed class TrialValidationResult
{
    internal TrialValidationResult(
        TrialValidationStatus status,
        TrialState? state,
        TimeSpan remaining,
        string? message)
    {
        Status = status;
        State = state;
        Remaining = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        Message = message;
    }

    /// <summary>Gets the result status.</summary>
    public TrialValidationStatus Status { get; }

    /// <summary>Gets the persisted state when it could be recovered.</summary>
    public TrialState? State { get; }

    /// <summary>Gets the remaining trial time, never less than zero.</summary>
    public TimeSpan Remaining { get; }

    /// <summary>Gets an optional diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the trial is currently active.</summary>
    public bool IsActive => Status == TrialValidationStatus.Active;
}

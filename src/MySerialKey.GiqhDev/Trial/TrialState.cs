namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Represents persisted trial timestamps without application-specific policy.
/// </summary>
public sealed class TrialState
{
    internal TrialState(
        DateTimeOffset startedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset lastSuccessfulRunUtc,
        DateTimeOffset? lastTrustedTimeUtc,
        DateTimeOffset? convertedToLicenseAtUtc)
    {
        StartedAtUtc = startedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        LastSuccessfulRunUtc = lastSuccessfulRunUtc;
        LastTrustedTimeUtc = lastTrustedTimeUtc;
        ConvertedToLicenseAtUtc = convertedToLicenseAtUtc;
    }

    /// <summary>Gets the explicit trial start time.</summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>Gets the exclusive trial expiration time.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets the greatest time accepted by a successful validation.</summary>
    public DateTimeOffset LastSuccessfulRunUtc { get; }

    /// <summary>Gets the greatest trusted server time observed, when available.</summary>
    public DateTimeOffset? LastTrustedTimeUtc { get; }

    /// <summary>Gets when the consumer marked the trial as converted to a license.</summary>
    public DateTimeOffset? ConvertedToLicenseAtUtc { get; }

    internal TrialState WithSuccessfulRun(
        DateTimeOffset lastSuccessfulRunUtc,
        DateTimeOffset? lastTrustedTimeUtc)
    {
        return new TrialState(
            StartedAtUtc,
            ExpiresAtUtc,
            lastSuccessfulRunUtc,
            lastTrustedTimeUtc,
            ConvertedToLicenseAtUtc);
    }

    internal TrialState WithConversion(DateTimeOffset convertedToLicenseAtUtc)
    {
        return new TrialState(
            StartedAtUtc,
            ExpiresAtUtc,
            LastSuccessfulRunUtc,
            LastTrustedTimeUtc,
            convertedToLicenseAtUtc);
    }
}

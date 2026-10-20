namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Configures trial duration and local clock rollback tolerance.
/// </summary>
public sealed class TrialOptions
{
    /// <summary>Gets the suggested trial duration.</summary>
    public static TimeSpan SuggestedDuration { get; } = TimeSpan.FromDays(7);

    /// <summary>Gets the default clock rollback tolerance.</summary>
    public static TimeSpan DefaultClockRollbackTolerance { get; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes options with a seven-day duration and five-minute rollback tolerance.
    /// </summary>
    public TrialOptions()
        : this(SuggestedDuration, DefaultClockRollbackTolerance)
    {
    }

    /// <summary>Initializes options with a custom duration.</summary>
    public TrialOptions(TimeSpan duration)
        : this(duration, DefaultClockRollbackTolerance)
    {
    }

    /// <summary>Initializes options with a custom duration and rollback tolerance.</summary>
    public TrialOptions(TimeSpan duration, TimeSpan clockRollbackTolerance)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                "Trial duration must be greater than zero.");
        }

        if (clockRollbackTolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(clockRollbackTolerance),
                "Clock rollback tolerance cannot be negative.");
        }

        Duration = duration;
        ClockRollbackTolerance = clockRollbackTolerance;
    }

    /// <summary>Gets the exact trial duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the permitted backward clock adjustment.</summary>
    public TimeSpan ClockRollbackTolerance { get; }
}

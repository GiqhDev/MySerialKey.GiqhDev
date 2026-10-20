namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Provides the current time for trial evaluation.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current time.</summary>
    DateTimeOffset UtcNow { get; }
}

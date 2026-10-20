namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Provides time from the system UTC clock.
/// </summary>
public sealed class SystemClock : IClock
{
    private SystemClock()
    {
    }

    /// <summary>Gets the shared system-clock instance.</summary>
    public static SystemClock Instance { get; } = new SystemClock();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

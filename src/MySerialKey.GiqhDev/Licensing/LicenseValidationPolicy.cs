namespace MySerialKey.GiqhDev.Licensing;

/// <summary>Supplies consumer-controlled policy for periodic online refresh.</summary>
public sealed class LicenseValidationPolicy
{
    /// <summary>Initializes a refresh policy.</summary>
    public LicenseValidationPolicy(
        TimeSpan gracePeriod,
        bool isOnlineRefreshUnavailable = false)
    {
        if (gracePeriod < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(gracePeriod));
        }

        GracePeriod = gracePeriod;
        IsOnlineRefreshUnavailable = isOnlineRefreshUnavailable;
    }

    /// <summary>Gets the consumer-selected grace duration after the next check-in.</summary>
    public TimeSpan GracePeriod { get; }

    /// <summary>Gets whether a refresh was attempted but the service is unavailable.</summary>
    public bool IsOnlineRefreshUnavailable { get; }
}

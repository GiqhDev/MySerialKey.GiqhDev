namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Defines the conventional relative endpoints for activation service contract v1.</summary>
public static class ActivationServiceEndpoints
{
    /// <summary>Creates a license from a serial and installation.</summary>
    public const string Activation = "api/v1/activations";

    /// <summary>Returns the current license state for an installation.</summary>
    public const string LicenseQuery = "api/v1/activations/query";

    /// <summary>Renews a current signed license.</summary>
    public const string Renewal = "api/v1/activations/renewals";

    /// <summary>Atomically moves a license to another installation.</summary>
    public const string Transfer = "api/v1/activations/transfers";

    /// <summary>Deactivates a licensed installation.</summary>
    public const string Deactivation = "api/v1/activations/deactivations";

    /// <summary>Returns a nonce-bound signed trusted-time response.</summary>
    public const string TrustedTime = "api/v1/time";
}

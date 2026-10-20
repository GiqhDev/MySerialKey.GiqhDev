namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Configures the built-in HTTP online activation client.</summary>
public sealed class HttpOnlineActivationClientOptions
{
    /// <summary>Initializes options with conventional version-one endpoints.</summary>
    public HttpOnlineActivationClientOptions()
    {
        ActivationEndpoint = new Uri("api/v1/activations", UriKind.Relative);
        RenewalEndpoint = new Uri("api/v1/activations/renewals", UriKind.Relative);
        DeactivationEndpoint = new Uri("api/v1/activations/deactivations", UriKind.Relative);
    }

    /// <summary>Gets or sets the relative activation endpoint.</summary>
    public Uri ActivationEndpoint { get; set; }

    /// <summary>Gets or sets the relative renewal endpoint.</summary>
    public Uri RenewalEndpoint { get; set; }

    /// <summary>Gets or sets the relative deactivation endpoint.</summary>
    public Uri DeactivationEndpoint { get; set; }

    /// <summary>Gets or sets the number of retries after the first attempt.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Gets or sets the base delay used between transient failures.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets or sets the maximum accepted response-body size.</summary>
    public int MaximumResponseBytes { get; set; } = 1024 * 1024;

    internal void Validate()
    {
        ValidateEndpoint(ActivationEndpoint, nameof(ActivationEndpoint));
        ValidateEndpoint(RenewalEndpoint, nameof(RenewalEndpoint));
        ValidateEndpoint(DeactivationEndpoint, nameof(DeactivationEndpoint));
        if (MaxRetries < 0 || MaxRetries > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRetries));
        }

        if (RetryDelay < TimeSpan.Zero || RetryDelay > TimeSpan.FromMinutes(1))
        {
            throw new ArgumentOutOfRangeException(nameof(RetryDelay));
        }

        if (MaximumResponseBytes < 1024 || MaximumResponseBytes > 16 * 1024 * 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(MaximumResponseBytes));
        }
    }

    private static void ValidateEndpoint(Uri endpoint, string parameterName)
    {
        if (endpoint is null || endpoint.IsAbsoluteUri || string.IsNullOrWhiteSpace(endpoint.OriginalString))
        {
            throw new ArgumentException("The endpoint must be a non-empty relative URI.", parameterName);
        }
    }
}

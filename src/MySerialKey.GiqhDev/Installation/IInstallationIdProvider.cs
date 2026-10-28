namespace MySerialKey.GiqhDev.Installation;

/// <summary>
/// Provides a stable, product-scoped installation identifier.
/// </summary>
public interface IInstallationIdProvider
{
    /// <summary>
    /// Gets an existing identifier or creates it atomically.
    /// </summary>
    string GetOrCreate(string productCode);
}

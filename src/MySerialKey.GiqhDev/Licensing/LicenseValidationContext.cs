using System;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Supplies application-specific values used while validating a license.
/// </summary>
public sealed class LicenseValidationContext
{
    /// <summary>
    /// Initializes a new validation context.
    /// </summary>
    public LicenseValidationContext(
        string productCode,
        DateTimeOffset nowUtc,
        string? installationId = null)
    {
        Guard.NotNull(productCode, nameof(productCode));
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
        }

        ProductCode = productCode;
        NowUtc = nowUtc;
        InstallationId = installationId;
    }

    /// <summary>
    /// Gets the expected product code.
    /// </summary>
    public string ProductCode { get; }

    /// <summary>
    /// Gets the current trusted time.
    /// </summary>
    public DateTimeOffset NowUtc { get; }

    /// <summary>
    /// Gets the current installation identifier.
    /// </summary>
    public string? InstallationId { get; }
}

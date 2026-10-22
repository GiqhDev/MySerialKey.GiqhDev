using System;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Revocation;

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
        string? installationId = null,
        LicenseValidationPolicy? refreshPolicy = null,
        ValidatedRevocationList? revocationList = null)
    {
        Guard.NotNull(productCode, nameof(productCode));
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
        }

        ProductCode = productCode;
        NowUtc = nowUtc;
        InstallationId = installationId;
        RefreshPolicy = refreshPolicy;

        if (revocationList is not null &&
            !string.Equals(
                revocationList.ProductCode,
                productCode,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The revocation list belongs to another product.",
                nameof(revocationList));
        }

        RevocationList = revocationList;
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

    /// <summary>Gets the optional consumer policy for an unavailable online refresh.</summary>
    public LicenseValidationPolicy? RefreshPolicy { get; }

    /// <summary>Gets the optional authenticated offline revocation list.</summary>
    public ValidatedRevocationList? RevocationList { get; }
}

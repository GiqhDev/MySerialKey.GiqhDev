using System;
using System.Collections.Generic;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Describes the claims protected by a signed license.
/// </summary>
public sealed class LicenseClaims
{
    /// <summary>
    /// Initializes a new license claim set.
    /// </summary>
    public LicenseClaims(string licenseId, string productCode, DateTimeOffset issuedAtUtc)
    {
        LicenseId = licenseId;
        ProductCode = productCode;
        IssuedAtUtc = issuedAtUtc;
        NotBeforeUtc = issuedAtUtc;
        Features = Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the payload schema version.
    /// </summary>
    public int SchemaVersion { get; set; } = 2;

    /// <summary>
    /// Gets or sets the unique license identifier.
    /// </summary>
    public string LicenseId { get; set; }

    /// <summary>
    /// Gets or sets the product that may consume the license.
    /// </summary>
    public string ProductCode { get; set; }

    /// <summary>
    /// Gets or sets the stable customer identifier.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the product edition.
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Gets or sets the UTC issuance time.
    /// </summary>
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the first UTC instant at which the license is valid.
    /// </summary>
    public DateTimeOffset NotBeforeUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional UTC expiration instant.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional UTC instant after which an online refresh is required.
    /// </summary>
    public DateTimeOffset? NextCheckInUtc { get; set; }

    /// <summary>Gets or sets the signed entitlement state.</summary>
    public LicenseEntitlementStatus EntitlementStatus { get; set; }

    /// <summary>
    /// Gets or sets the optional installation to which the license is bound.
    /// </summary>
    public string? InstallationId { get; set; }

    /// <summary>
    /// Gets or sets the terminal entitlement recorded by the issuer.
    /// </summary>
    public int MaxTerminals { get; set; } = 1;

    /// <summary>
    /// Gets or sets the enabled product features.
    /// </summary>
    public IReadOnlyList<string> Features { get; set; }
}

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Describes the authenticated contents of an offline revocation list.</summary>
public sealed class RevocationListClaims
{
    /// <summary>Initializes revocation-list claims for one product and sequence.</summary>
    public RevocationListClaims(
        string productCode,
        long sequenceNumber,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ProductCode = productCode;
        SequenceNumber = sequenceNumber;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedLicenseIds = Array.Empty<string>();
    }

    /// <summary>Gets the payload schema version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets the product to which this list applies.</summary>
    public string ProductCode { get; set; }

    /// <summary>Gets or sets the monotonically increasing list sequence.</summary>
    public long SequenceNumber { get; set; }

    /// <summary>Gets or sets the UTC issue time.</summary>
    public DateTimeOffset IssuedAtUtc { get; set; }

    /// <summary>Gets or sets the exclusive UTC expiration time.</summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets the revoked license identifiers.</summary>
    public IReadOnlyList<string> RevokedLicenseIds { get; set; }
}

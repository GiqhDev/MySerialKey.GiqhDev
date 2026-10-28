namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Represents revocation claims that passed signature and contextual validation.</summary>
public sealed class ValidatedRevocationList
{
    private readonly HashSet<string> _revokedLicenseIds;

    internal ValidatedRevocationList(RevocationListClaims claims)
    {
        ProductCode = claims.ProductCode;
        SequenceNumber = claims.SequenceNumber;
        IssuedAtUtc = claims.IssuedAtUtc;
        ExpiresAtUtc = claims.ExpiresAtUtc;
        string[] identifiers = claims.RevokedLicenseIds.ToArray();
        RevokedLicenseIds = Array.AsReadOnly(identifiers);
        _revokedLicenseIds = new HashSet<string>(
            identifiers,
            StringComparer.Ordinal);
    }

    /// <summary>Gets the authenticated product code.</summary>
    public string ProductCode { get; }

    /// <summary>Gets the authenticated monotonically increasing sequence.</summary>
    public long SequenceNumber { get; }

    /// <summary>Gets the authenticated UTC issue time.</summary>
    public DateTimeOffset IssuedAtUtc { get; }

    /// <summary>Gets the authenticated exclusive UTC expiration time.</summary>
    public DateTimeOffset ExpiresAtUtc { get; }

    /// <summary>Gets an immutable snapshot of authenticated revoked identifiers.</summary>
    public IReadOnlyList<string> RevokedLicenseIds { get; }

    /// <summary>Determines whether an exact license identifier is revoked.</summary>
    public bool Contains(string licenseId)
    {
        return _revokedLicenseIds.Contains(licenseId);
    }
}

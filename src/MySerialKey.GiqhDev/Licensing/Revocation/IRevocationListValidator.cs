using System.Collections.Generic;

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Authenticates signed offline revocation lists for a product and trusted time.</summary>
public interface IRevocationListValidator
{
    /// <summary>Validates a list and optionally rejects sequences below a known floor.</summary>
    RevocationListValidationResult Validate(
        SignedRevocationList list,
        string productCode,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys,
        long? minimumSequenceNumber = null);
}

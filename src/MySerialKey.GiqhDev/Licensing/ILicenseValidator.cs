using System.Collections.Generic;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Validates signed licenses using trusted public keys.
/// </summary>
public interface ILicenseValidator
{
    /// <summary>
    /// Authenticates and validates a signed license.
    /// </summary>
    LicenseValidationResult Validate(
        SignedLicense license,
        LicenseValidationContext context,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys);
}

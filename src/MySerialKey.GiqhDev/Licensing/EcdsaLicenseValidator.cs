using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Licensing;

/// <summary>
/// Validates ES256 signed-license tokens.
/// </summary>
public sealed class EcdsaLicenseValidator : ILicenseValidator
{
    /// <inheritdoc />
    public LicenseValidationResult Validate(
        SignedLicense license,
        LicenseValidationContext context,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        Guard.NotNull(license, nameof(license));
        Guard.NotNull(context, nameof(context));
        Guard.NotNull(trustedKeys, nameof(trustedKeys));

        if (!string.Equals(license.Algorithm, LicenseAlgorithms.EcdsaP256Sha256, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.UnsupportedAlgorithm,
                "The signed-license algorithm is not supported.");
        }

        LicenseVerificationKey? verificationKey = FindKey(license, trustedKeys);
        if (verificationKey is null)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.UnknownSigningKey,
                "No trusted public key matches the signed-license key identifier.");
        }

        bool signatureIsValid;
        using (ECDsa publicKey = verificationKey.CreatePublicKey())
        {
            signatureIsValid = publicKey.VerifyData(
                SignedLicenseEnvelope.BuildSigningInput(license),
                license.Signature,
                HashAlgorithmName.SHA256);
        }

        if (!signatureIsValid)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.InvalidSignature,
                "The signed-license signature is invalid.");
        }

        LicenseClaims claims;
        try
        {
            claims = LicenseClaimsSerializer.Deserialize(license.Payload);
        }
        catch (Exception exception) when (
            exception is FormatException ||
            exception is ArgumentException ||
            exception is InvalidOperationException ||
            exception is System.IO.IOException)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.Corrupted,
                "The authenticated license payload is corrupted.");
        }

        if (claims.SchemaVersion != LicenseClaimsSerializer.CurrentSchemaVersion)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.UnsupportedSchema,
                "The license payload schema is not supported.",
                claims);
        }

        try
        {
            LicenseClaimsSerializer.ValidateForSigning(claims);
        }
        catch (ArgumentException)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.Corrupted,
                "The authenticated license payload contains invalid claims.");
        }

        if (!string.Equals(claims.ProductCode, context.ProductCode, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.WrongProduct,
                "The license belongs to another product.",
                claims);
        }

        if (claims.InstallationId is not null &&
            !string.Equals(claims.InstallationId, context.InstallationId, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.WrongInstallation,
                "The license belongs to another installation.",
                claims);
        }

        DateTimeOffset nowUtc = context.NowUtc.ToUniversalTime();
        if (nowUtc < claims.NotBeforeUtc)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.NotYetValid,
                "The license is not valid yet.",
                claims);
        }

        if (claims.ExpiresAtUtc.HasValue && nowUtc >= claims.ExpiresAtUtc.Value)
        {
            return LicenseValidationResult.Failure(
                LicenseValidationStatus.Expired,
                "The license has expired.",
                claims);
        }

        return LicenseValidationResult.Success(claims);
    }

    private static LicenseVerificationKey? FindKey(
        SignedLicense license,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        foreach (LicenseVerificationKey key in trustedKeys)
        {
            if (string.Equals(key.KeyId, license.KeyId, StringComparison.Ordinal) &&
                string.Equals(key.Algorithm, license.Algorithm, StringComparison.Ordinal))
            {
                return key;
            }
        }

        return null;
    }
}

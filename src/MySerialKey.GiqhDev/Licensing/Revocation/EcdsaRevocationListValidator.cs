using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing.Revocation.Internal;

namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Authenticates and validates ES256 offline revocation lists.</summary>
public sealed class EcdsaRevocationListValidator
{
    /// <summary>Validates a list for a product, trusted time, and optional sequence floor.</summary>
    public RevocationListValidationResult Validate(
        SignedRevocationList list,
        string productCode,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys,
        long? minimumSequenceNumber = null)
    {
        return ValidateCore(
            list,
            productCode,
            nowUtc,
            trustedKeys,
            minimumSequenceNumber,
            validateLifetime: true);
    }

    /// <summary>Authenticates stored claims while ignoring only their elapsed lifetime.</summary>
    internal RevocationListValidationResult AuthenticateStored(
        SignedRevocationList list,
        string productCode,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        return ValidateCore(
            list,
            productCode,
            DateTimeOffset.MinValue,
            trustedKeys,
            null,
            validateLifetime: false);
    }

    /// <summary>Performs the shared cryptographic, payload, product, lifetime, and sequence checks.</summary>
    private static RevocationListValidationResult ValidateCore(
        SignedRevocationList list,
        string productCode,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys,
        long? minimumSequenceNumber,
        bool validateLifetime)
    {
        Guard.NotNull(list, nameof(list));
        Guard.NotNull(productCode, nameof(productCode));
        Guard.NotNull(trustedKeys, nameof(trustedKeys));

        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
        }

        if (minimumSequenceNumber.HasValue && minimumSequenceNumber.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumSequenceNumber),
                "Minimum sequence must be greater than zero.");
        }

        if (!string.Equals(
            list.Algorithm,
            LicenseAlgorithms.EcdsaP256Sha256,
            StringComparison.Ordinal))
        {
            return Failure(
                RevocationListValidationStatus.UnsupportedAlgorithm,
                "The revocation-list algorithm is not supported.");
        }

        LicenseVerificationKey? verificationKey = FindKey(list, trustedKeys);
        if (verificationKey is null)
        {
            return Failure(
                RevocationListValidationStatus.UnknownSigningKey,
                "No trusted public key matches the revocation-list key identifier.");
        }

        bool signatureIsValid;
        using (ECDsa publicKey = verificationKey.CreatePublicKey())
        {
            signatureIsValid = publicKey.VerifyData(
                SignedRevocationListEnvelope.BuildSigningInput(list),
                list.Signature,
                HashAlgorithmName.SHA256);
        }

        if (!signatureIsValid)
        {
            return Failure(
                RevocationListValidationStatus.InvalidSignature,
                "The revocation-list signature is invalid.");
        }

        RevocationListClaims claims;
        try
        {
            claims = RevocationListSerializer.Deserialize(list.Payload);
        }
        catch (Exception exception) when (
            exception is FormatException ||
            exception is ArgumentException ||
            exception is InvalidOperationException ||
            exception is System.IO.IOException)
        {
            return Failure(
                RevocationListValidationStatus.Corrupted,
                "The authenticated revocation-list payload is corrupted.");
        }

        if (!RevocationListSerializer.IsSupportedSchema(claims.SchemaVersion))
        {
            return Failure(
                RevocationListValidationStatus.UnsupportedSchema,
                "The revocation-list payload schema is not supported.");
        }

        try
        {
            RevocationListSerializer.ValidateForSigning(claims);
        }
        catch (ArgumentException)
        {
            return Failure(
                RevocationListValidationStatus.Corrupted,
                "The authenticated revocation list contains invalid claims.");
        }

        if (!string.Equals(claims.ProductCode, productCode, StringComparison.Ordinal))
        {
            return Failure(
                RevocationListValidationStatus.WrongProduct,
                "The revocation list belongs to another product.");
        }

        DateTimeOffset currentUtc = nowUtc.ToUniversalTime();
        if (validateLifetime && currentUtc < claims.IssuedAtUtc)
        {
            return Failure(
                RevocationListValidationStatus.NotYetValid,
                "The revocation list is not valid yet.");
        }

        if (validateLifetime && currentUtc >= claims.ExpiresAtUtc)
        {
            return Failure(
                RevocationListValidationStatus.Expired,
                "The revocation list has expired.");
        }

        if (minimumSequenceNumber.HasValue &&
            claims.SequenceNumber < minimumSequenceNumber.Value)
        {
            return Failure(
                RevocationListValidationStatus.RollbackDetected,
                "The revocation list sequence is older than the accepted sequence.");
        }

        return new RevocationListValidationResult(
            RevocationListValidationStatus.Valid,
            new ValidatedRevocationList(claims),
            null);
    }

    /// <summary>Creates a failed typed result without exposing unauthenticated claims.</summary>
    private static RevocationListValidationResult Failure(
        RevocationListValidationStatus status,
        string message)
    {
        return new RevocationListValidationResult(status, null, message);
    }

    /// <summary>Finds a trusted key matching both key identifier and algorithm.</summary>
    private static LicenseVerificationKey? FindKey(
        SignedRevocationList list,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        foreach (LicenseVerificationKey key in trustedKeys)
        {
            if (string.Equals(key.KeyId, list.KeyId, StringComparison.Ordinal) &&
                string.Equals(key.Algorithm, list.Algorithm, StringComparison.Ordinal))
            {
                return key;
            }
        }

        return null;
    }
}

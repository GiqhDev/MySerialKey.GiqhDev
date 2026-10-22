using System;
using System.Collections.Generic;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Validates and imports signed revocation lists without accepting sequence rollback.</summary>
public sealed class RevocationListImporter
{
    private readonly IRevocationListStore _store;
    private readonly EcdsaRevocationListValidator _validator;
    private readonly object _syncRoot = new object();

    /// <summary>Initializes an importer over a protected or custom revocation-list store.</summary>
    public RevocationListImporter(
        IRevocationListStore store,
        EcdsaRevocationListValidator validator)
    {
        Guard.NotNull(store, nameof(store));
        Guard.NotNull(validator, nameof(validator));
        _store = store;
        _validator = validator;
    }

    /// <summary>Authenticates and stores a candidate only when it is current and advances state.</summary>
    public RevocationListImportResult Import(
        string productCode,
        SignedRevocationList candidate,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        lock (_syncRoot)
        {
            return ImportCore(productCode, candidate, nowUtc, trustedKeys);
        }
    }

    /// <summary>Runs the authenticated read-compare-write operation under the importer lock.</summary>
    private RevocationListImportResult ImportCore(
        string productCode,
        SignedRevocationList candidate,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        Guard.NotNull(candidate, nameof(candidate));
        RevocationListValidationResult candidateValidation = _validator.Validate(
            candidate,
            productCode,
            nowUtc,
            trustedKeys);
        if (!candidateValidation.IsValid)
        {
            return Result(
                RevocationListImportStatus.InvalidCandidate,
                candidateValidation,
                candidateValidation.Message);
        }

        RevocationListStoreReadResult current = _store.Load(productCode);
        if (current.Status == RevocationListStoreReadStatus.Corrupted)
        {
            return Result(
                RevocationListImportStatus.StoredListInvalid,
                candidateValidation,
                current.Message);
        }

        if (current.Status == RevocationListStoreReadStatus.Unavailable)
        {
            return Result(
                RevocationListImportStatus.StorageUnavailable,
                candidateValidation,
                current.Message);
        }

        if (current.Status == RevocationListStoreReadStatus.Success)
        {
            if (string.Equals(
                current.List!.ToString(),
                candidate.ToString(),
                StringComparison.Ordinal))
            {
                return Result(
                    RevocationListImportStatus.AlreadyCurrent,
                    candidateValidation,
                    null);
            }

            RevocationListValidationResult currentAuthentication =
                _validator.AuthenticateStored(current.List, productCode, trustedKeys);
            if (!currentAuthentication.IsValid)
            {
                return Result(
                    RevocationListImportStatus.StoredListInvalid,
                    candidateValidation,
                    currentAuthentication.Message);
            }

            if (candidateValidation.List!.SequenceNumber <=
                currentAuthentication.List!.SequenceNumber)
            {
                return Result(
                    RevocationListImportStatus.RollbackDetected,
                    candidateValidation,
                    "The candidate does not advance the authenticated sequence number.");
            }
        }

        if (!_store.Save(productCode, candidate))
        {
            return Result(
                RevocationListImportStatus.StorageUnavailable,
                candidateValidation,
                "The protected revocation-list token could not be saved.");
        }

        return Result(RevocationListImportStatus.Imported, candidateValidation, null);
    }

    /// <summary>Creates a typed import result.</summary>
    private static RevocationListImportResult Result(
        RevocationListImportStatus status,
        RevocationListValidationResult? validation,
        string? message)
    {
        return new RevocationListImportResult(status, validation, message);
    }
}

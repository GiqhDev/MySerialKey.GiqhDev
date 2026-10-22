namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Describes the outcome of importing a signed revocation list.</summary>
public enum RevocationListImportStatus
{
    /// <summary>The authenticated token replaced the current protected token.</summary>
    Imported = 0,
    /// <summary>The exact same authenticated token was already stored.</summary>
    AlreadyCurrent,
    /// <summary>The candidate failed cryptographic or semantic validation.</summary>
    InvalidCandidate,
    /// <summary>The candidate does not advance the last authenticated sequence.</summary>
    RollbackDetected,
    /// <summary>The existing stored token cannot be authenticated safely.</summary>
    StoredListInvalid,
    /// <summary>The protected storage provider cannot complete the operation.</summary>
    StorageUnavailable
}

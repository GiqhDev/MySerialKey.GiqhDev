namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Represents a protected revocation-list import result.</summary>
public sealed class RevocationListImportResult
{
    internal RevocationListImportResult(
        RevocationListImportStatus status,
        RevocationListValidationResult? validation,
        string? message)
    {
        Status = status;
        Validation = validation;
        Message = message;
    }

    /// <summary>Gets the typed import outcome.</summary>
    public RevocationListImportStatus Status { get; }

    /// <summary>Gets candidate validation details when available.</summary>
    public RevocationListValidationResult? Validation { get; }

    /// <summary>Gets a diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the requested token is now the current protected token.</summary>
    public bool IsSuccess =>
        Status == RevocationListImportStatus.Imported ||
        Status == RevocationListImportStatus.AlreadyCurrent;
}

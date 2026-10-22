namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Represents a stored revocation-list read result.</summary>
public sealed class RevocationListStoreReadResult
{
    internal RevocationListStoreReadResult(
        RevocationListStoreReadStatus status,
        SignedRevocationList? list,
        string? message)
    {
        Status = status;
        List = list;
        Message = message;
    }

    /// <summary>Gets the operation status.</summary>
    public RevocationListStoreReadStatus Status { get; }

    /// <summary>Gets the structurally parsed token when successful.</summary>
    public SignedRevocationList? List { get; }

    /// <summary>Gets a diagnostic message.</summary>
    public string? Message { get; }
}

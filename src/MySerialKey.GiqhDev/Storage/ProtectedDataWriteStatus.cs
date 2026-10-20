namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Describes a protected-store write operation.
/// </summary>
public enum ProtectedDataWriteStatus
{
    /// <summary>The entry was written.</summary>
    Success = 0,
    /// <summary>The entry already existed during create-only storage.</summary>
    AlreadyExists,
    /// <summary>The backing store could not be accessed.</summary>
    Unavailable
}

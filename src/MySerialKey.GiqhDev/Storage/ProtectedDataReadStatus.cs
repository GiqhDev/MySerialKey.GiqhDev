namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Describes a protected-store read operation.
/// </summary>
public enum ProtectedDataReadStatus
{
    /// <summary>The entry was read and authenticated.</summary>
    Success = 0,
    /// <summary>The entry does not exist.</summary>
    NotFound,
    /// <summary>The entry is malformed or failed authentication.</summary>
    Corrupted,
    /// <summary>The backing store could not be accessed.</summary>
    Unavailable
}

namespace MySerialKey.GiqhDev.Licensing.Storage;

/// <summary>
/// Describes a stored-license read operation.
/// </summary>
public enum LicenseStoreReadStatus
{
    /// <summary>A valid token was loaded.</summary>
    Success = 0,
    /// <summary>No token is stored.</summary>
    NotFound,
    /// <summary>The stored value is corrupt.</summary>
    Corrupted,
    /// <summary>The store is unavailable.</summary>
    Unavailable
}

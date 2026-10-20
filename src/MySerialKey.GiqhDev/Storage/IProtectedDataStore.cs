namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Persists protected data using product and entry scopes.
/// </summary>
public interface IProtectedDataStore
{
    /// <summary>
    /// Reads an entry.
    /// </summary>
    ProtectedDataReadResult Read(string productCode, string entryName);

    /// <summary>
    /// Creates an entry only when it does not already exist.
    /// </summary>
    ProtectedDataWriteResult TryCreate(string productCode, string entryName, byte[] value);

    /// <summary>
    /// Creates or replaces an entry atomically.
    /// </summary>
    ProtectedDataWriteResult Write(string productCode, string entryName, byte[] value);

    /// <summary>
    /// Removes an entry when present.
    /// </summary>
    bool Delete(string productCode, string entryName);
}

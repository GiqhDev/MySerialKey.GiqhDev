namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Protects persisted bytes independently from the storage mechanism.
/// </summary>
public interface IDataProtector
{
    /// <summary>
    /// Protects plaintext for a specific purpose.
    /// </summary>
    byte[] Protect(byte[] plaintext, string purpose);

    /// <summary>
    /// Recovers and authenticates protected bytes for a specific purpose.
    /// </summary>
    byte[] Unprotect(byte[] protectedData, string purpose);
}

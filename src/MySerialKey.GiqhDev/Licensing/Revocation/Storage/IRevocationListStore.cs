namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Persists portable signed revocation lists independently from application data.</summary>
public interface IRevocationListStore
{
    /// <summary>Loads the current token for a product.</summary>
    RevocationListStoreReadResult Load(string productCode);

    /// <summary>Saves the current token for a product.</summary>
    bool Save(string productCode, SignedRevocationList list);

    /// <summary>Removes the current token for a product.</summary>
    bool Delete(string productCode);
}

namespace MySerialKey.GiqhDev.Licensing.Storage;

/// <summary>
/// Persists portable signed licenses independently from application data.
/// </summary>
public interface ILicenseStore
{
    /// <summary>Loads a product license.</summary>
    LicenseStoreReadResult Load(string productCode);

    /// <summary>Saves a product license.</summary>
    bool Save(string productCode, SignedLicense license);

    /// <summary>Removes a product license.</summary>
    bool Delete(string productCode);
}

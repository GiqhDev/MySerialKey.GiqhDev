using System.Text;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Storage;

namespace MySerialKey.GiqhDev.Licensing.Storage;

/// <summary>
/// Stores signed-license tokens in an <see cref="IProtectedDataStore"/>.
/// </summary>
public sealed class ProtectedLicenseStore : ILicenseStore
{
    private const string EntryName = "signed-license";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly IProtectedDataStore _store;

    /// <summary>
    /// Initializes the license store.
    /// </summary>
    public ProtectedLicenseStore(IProtectedDataStore store)
    {
        Guard.NotNull(store, nameof(store));
        _store = store;
    }

    /// <inheritdoc />
    public LicenseStoreReadResult Load(string productCode)
    {
        ProtectedDataReadResult result = _store.Read(productCode, EntryName);
        switch (result.Status)
        {
            case ProtectedDataReadStatus.NotFound:
                return new LicenseStoreReadResult(
                    LicenseStoreReadStatus.NotFound,
                    null,
                    result.Message);
            case ProtectedDataReadStatus.Corrupted:
                return new LicenseStoreReadResult(
                    LicenseStoreReadStatus.Corrupted,
                    null,
                    result.Message);
            case ProtectedDataReadStatus.Unavailable:
                return new LicenseStoreReadResult(
                    LicenseStoreReadStatus.Unavailable,
                    null,
                    result.Message);
        }

        try
        {
            string token = StrictUtf8.GetString(result.Value!);
            if (!SignedLicense.TryParse(token, out SignedLicense? license))
            {
                return new LicenseStoreReadResult(
                    LicenseStoreReadStatus.Corrupted,
                    null,
                    "The stored signed-license token is malformed.");
            }

            return new LicenseStoreReadResult(
                LicenseStoreReadStatus.Success,
                license,
                null);
        }
        catch (DecoderFallbackException)
        {
            return new LicenseStoreReadResult(
                LicenseStoreReadStatus.Corrupted,
                null,
                "The stored signed-license token is not valid UTF-8.");
        }
    }

    /// <inheritdoc />
    public bool Save(string productCode, SignedLicense license)
    {
        Guard.NotNull(license, nameof(license));
        ProtectedDataWriteResult result = _store.Write(
            productCode,
            EntryName,
            StrictUtf8.GetBytes(license.ToString()));
        return result.Status == ProtectedDataWriteStatus.Success;
    }

    /// <inheritdoc />
    public bool Delete(string productCode)
    {
        return _store.Delete(productCode, EntryName);
    }
}

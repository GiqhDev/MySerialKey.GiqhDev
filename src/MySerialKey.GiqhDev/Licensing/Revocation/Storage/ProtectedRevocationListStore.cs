using System.Text;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Storage;

namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Stores signed revocation-list tokens in an <see cref="IProtectedDataStore"/>.</summary>
public sealed class ProtectedRevocationListStore : IRevocationListStore
{
    private const string EntryName = "signed-revocation-list";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly IProtectedDataStore _store;

    /// <summary>Initializes the protected revocation-list store.</summary>
    public ProtectedRevocationListStore(IProtectedDataStore store)
    {
        Guard.NotNull(store, nameof(store));
        _store = store;
    }

    /// <inheritdoc />
    public RevocationListStoreReadResult Load(string productCode)
    {
        ProtectedDataReadResult result = _store.Read(productCode, EntryName);
        switch (result.Status)
        {
            case ProtectedDataReadStatus.NotFound:
                return Result(RevocationListStoreReadStatus.NotFound, null, result.Message);
            case ProtectedDataReadStatus.Corrupted:
                return Result(RevocationListStoreReadStatus.Corrupted, null, result.Message);
            case ProtectedDataReadStatus.Unavailable:
                return Result(RevocationListStoreReadStatus.Unavailable, null, result.Message);
        }

        try
        {
            string token = StrictUtf8.GetString(result.Value!);
            if (!SignedRevocationList.TryParse(token, out SignedRevocationList? list))
            {
                return Result(
                    RevocationListStoreReadStatus.Corrupted,
                    null,
                    "The stored signed revocation-list token is malformed.");
            }

            return Result(RevocationListStoreReadStatus.Success, list, null);
        }
        catch (DecoderFallbackException)
        {
            return Result(
                RevocationListStoreReadStatus.Corrupted,
                null,
                "The stored signed revocation-list token is not valid UTF-8.");
        }
    }

    /// <inheritdoc />
    public bool Save(string productCode, SignedRevocationList list)
    {
        Guard.NotNull(list, nameof(list));
        ProtectedDataWriteResult result = _store.Write(
            productCode,
            EntryName,
            StrictUtf8.GetBytes(list.ToString()));
        return result.Status == ProtectedDataWriteStatus.Success;
    }

    /// <inheritdoc />
    public bool Delete(string productCode)
    {
        return _store.Delete(productCode, EntryName);
    }

    /// <summary>Creates a typed read result while keeping the switch branches concise.</summary>
    private static RevocationListStoreReadResult Result(
        RevocationListStoreReadStatus status,
        SignedRevocationList? list,
        string? message)
    {
        return new RevocationListStoreReadResult(status, list, message);
    }
}

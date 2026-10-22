using System.Text;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Storage;

namespace MySerialKey.GiqhDev.Installation;

/// <summary>
/// Persists a random installation identifier in a protected store.
/// </summary>
public sealed class PersistentInstallationIdProvider : IInstallationIdProvider
{
    private const string EntryName = "installation-id";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly IProtectedDataStore _store;

    /// <summary>
    /// Initializes the provider.
    /// </summary>
    public PersistentInstallationIdProvider(IProtectedDataStore store)
    {
        Guard.NotNull(store, nameof(store));
        _store = store;
    }

    /// <inheritdoc />
    public string GetOrCreate(string productCode)
    {
        ValidateProductCode(productCode);

        ProtectedDataReadResult existing = _store.Read(productCode, EntryName);
        if (existing.Status == ProtectedDataReadStatus.Success)
        {
            return Decode(existing.Value!);
        }

        if (existing.Status != ProtectedDataReadStatus.NotFound)
        {
            throw new InstallationIdException(
                existing.Message ?? "The installation identifier could not be read.");
        }

        string candidate = Guid.NewGuid().ToString("N");
        ProtectedDataWriteResult create = _store.TryCreate(
            productCode,
            EntryName,
            StrictUtf8.GetBytes(candidate));

        if (create.Status == ProtectedDataWriteStatus.Success)
        {
            return candidate;
        }

        if (create.Status == ProtectedDataWriteStatus.AlreadyExists)
        {
            ProtectedDataReadResult winner = _store.Read(productCode, EntryName);
            if (winner.Status == ProtectedDataReadStatus.Success)
            {
                return Decode(winner.Value!);
            }

            throw new InstallationIdException(
                winner.Message ?? "The existing installation identifier could not be read.");
        }

        throw new InstallationIdException(
            create.Message ?? "The installation identifier could not be stored.");
    }

    /// <summary>Decodes and validates the persisted UTF-8 installation identifier.</summary>
    private static string Decode(byte[] value)
    {
        try
        {
            string installationId = StrictUtf8.GetString(value);
            if (!Guid.TryParseExact(installationId, "N", out _))
            {
                throw new InstallationIdException(
                    "The stored installation identifier is corrupted.");
            }

            return installationId;
        }
        catch (DecoderFallbackException)
        {
            throw new InstallationIdException(
                "The stored installation identifier is corrupted.");
        }
    }

    /// <summary>Rejects empty or excessively large product scopes before storage access.</summary>
    private static void ValidateProductCode(string productCode)
    {
        Guard.NotNull(productCode, nameof(productCode));
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Stores protected values in product-scoped files with atomic replacement.
/// </summary>
public sealed class FileProtectedDataStore : IProtectedDataStore
{
    private const int MaximumValueBytes = 1024 * 1024;
    private const int MaximumStoredBytes = MaximumValueBytes + (64 * 1024);
    private static readonly byte[] FileMarker = { 0x4D, 0x53, 0x4B, 0x32 };
    private readonly string _rootPath;
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a file-backed protected store.
    /// </summary>
    public FileProtectedDataStore(string rootPath, IDataProtector protector)
    {
        Guard.NotNull(rootPath, nameof(rootPath));
        Guard.NotNull(protector, nameof(protector));

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Storage root path cannot be empty.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        _protector = protector;
    }

    /// <inheritdoc />
    public ProtectedDataReadResult Read(string productCode, string entryName)
    {
        ValidateKey(productCode, nameof(productCode));
        ValidateKey(entryName, nameof(entryName));
        string path = GetEntryPath(productCode, entryName);

        try
        {
            if (!File.Exists(path))
            {
                return ProtectedDataReadResult.Failure(
                    ProtectedDataReadStatus.NotFound,
                    "The protected entry does not exist.");
            }

            byte[] record = ReadBounded(path);
            if (record.Length <= FileMarker.Length ||
                !StartsWith(record, FileMarker))
            {
                return ProtectedDataReadResult.Failure(
                    ProtectedDataReadStatus.Corrupted,
                    "The protected entry marker is invalid.");
            }

            var protectedBytes = new byte[record.Length - FileMarker.Length];
            Buffer.BlockCopy(
                record,
                FileMarker.Length,
                protectedBytes,
                0,
                protectedBytes.Length);

            try
            {
                byte[] value = _protector.Unprotect(
                    protectedBytes,
                    BuildPurpose(productCode, entryName));
                return ProtectedDataReadResult.Success(value);
            }
            catch (CryptographicException)
            {
                return ProtectedDataReadResult.Failure(
                    ProtectedDataReadStatus.Corrupted,
                    "The protected entry failed authentication.");
            }
            catch (FormatException)
            {
                return ProtectedDataReadResult.Failure(
                    ProtectedDataReadStatus.Corrupted,
                    "The protected entry is malformed.");
            }
        }
        catch (IOException exception)
        {
            return ProtectedDataReadResult.Failure(
                ProtectedDataReadStatus.Unavailable,
                exception.Message);
        }
        catch (FormatException exception)
        {
            return ProtectedDataReadResult.Failure(
                ProtectedDataReadStatus.Corrupted,
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProtectedDataReadResult.Failure(
                ProtectedDataReadStatus.Unavailable,
                exception.Message);
        }
    }

    /// <inheritdoc />
    public ProtectedDataWriteResult TryCreate(
        string productCode,
        string entryName,
        byte[] value)
    {
        ValidateWrite(productCode, entryName, value);
        string path = GetEntryPath(productCode, entryName);

        try
        {
            EnsureDirectory(path);
            byte[] record = CreateRecord(productCode, entryName, value);
            using (var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(record, 0, record.Length);
                stream.Flush();
            }

            return ProtectedDataWriteResult.Success();
        }
        catch (IOException exception)
        {
            return File.Exists(path)
                ? ProtectedDataWriteResult.AlreadyExists()
                : ProtectedDataWriteResult.Unavailable(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProtectedDataWriteResult.Unavailable(exception.Message);
        }
    }

    /// <inheritdoc />
    public ProtectedDataWriteResult Write(
        string productCode,
        string entryName,
        byte[] value)
    {
        ValidateWrite(productCode, entryName, value);
        string path = GetEntryPath(productCode, entryName);
        string? temporaryPath = null;

        try
        {
            EnsureDirectory(path);
            byte[] record = CreateRecord(productCode, entryName, value);
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.Write(record, 0, record.Length);
                stream.Flush();
            }

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                try
                {
                    File.Move(temporaryPath, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
            }

            temporaryPath = null;
            return ProtectedDataWriteResult.Success();
        }
        catch (IOException exception)
        {
            return ProtectedDataWriteResult.Unavailable(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return ProtectedDataWriteResult.Unavailable(exception.Message);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                TryDelete(temporaryPath);
            }
        }
    }

    /// <inheritdoc />
    public bool Delete(string productCode, string entryName)
    {
        ValidateKey(productCode, nameof(productCode));
        ValidateKey(entryName, nameof(entryName));
        string path = GetEntryPath(productCode, entryName);

        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private byte[] CreateRecord(
        string productCode,
        string entryName,
        byte[] value)
    {
        byte[] protectedBytes = _protector.Protect(
            value,
            BuildPurpose(productCode, entryName));
        if (protectedBytes.Length > MaximumStoredBytes)
        {
            throw new ArgumentException("Protected value is too large.", nameof(value));
        }

        var record = new byte[FileMarker.Length + protectedBytes.Length];
        Buffer.BlockCopy(FileMarker, 0, record, 0, FileMarker.Length);
        Buffer.BlockCopy(
            protectedBytes,
            0,
            record,
            FileMarker.Length,
            protectedBytes.Length);
        return record;
    }

    private static byte[] ReadBounded(string path)
    {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length <= 0 || fileInfo.Length > MaximumStoredBytes)
        {
            throw new FormatException("Protected entry size is invalid.");
        }

        byte[] record = File.ReadAllBytes(path);
        if (record.Length <= 0 || record.Length > MaximumStoredBytes)
        {
            throw new FormatException("Protected entry size is invalid.");
        }

        return record;
    }

    private static bool StartsWith(byte[] value, byte[] prefix)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Length; i++)
        {
            if (value[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private string GetEntryPath(string productCode, string entryName)
    {
        return Path.Combine(
            _rootPath,
            HashName(productCode),
            HashName(entryName) + ".bin");
    }

    private static string HashName(string value)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte item in hash)
            {
                builder.Append(item.ToString("x2"));
            }

            return builder.ToString();
        }
    }

    private static string BuildPurpose(string productCode, string entryName)
    {
        return productCode.Length + ":" + productCode + ":" + entryName;
    }

    private static void EnsureDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (directory is null)
        {
            throw new InvalidOperationException("Protected entry directory is invalid.");
        }

        Directory.CreateDirectory(directory);
    }

    private static void ValidateWrite(
        string productCode,
        string entryName,
        byte[] value)
    {
        ValidateKey(productCode, nameof(productCode));
        ValidateKey(entryName, nameof(entryName));
        Guard.NotNull(value, nameof(value));
        if (value.Length > MaximumValueBytes)
        {
            throw new ArgumentException("Value is too large.", nameof(value));
        }
    }

    private static void ValidateKey(string value, string parameterName)
    {
        Guard.NotNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Storage key cannot be empty.", parameterName);
        }

        if (value.Length > 1024)
        {
            throw new ArgumentException("Storage key is too long.", parameterName);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

using System.Security.Cryptography;
using System.Text;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Protects data on Windows using DPAPI.
/// </summary>
/// <remarks>
/// This implementation is supported only on Windows.
/// </remarks>
#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public sealed class DpapiDataProtector : IDataProtector
{
    private const string PurposePrefix = "MySerialKey.GiqhDev/v2/";
    private readonly DataProtectionScope _scope;

    /// <summary>
    /// Initializes a DPAPI protector.
    /// </summary>
    /// <param name="useMachineScope">
    /// True to allow all authorized users on the machine to recover data;
    /// false to bind data to the current Windows user.
    /// </param>
    public DpapiDataProtector(bool useMachineScope = false)
    {
        _scope = useMachineScope
            ? DataProtectionScope.LocalMachine
            : DataProtectionScope.CurrentUser;
    }

    /// <inheritdoc />
    public byte[] Protect(byte[] plaintext, string purpose)
    {
        Guard.NotNull(plaintext, nameof(plaintext));
        ValidatePurpose(purpose);
        return ProtectedData.Protect(plaintext, CreateEntropy(purpose), _scope);
    }

    /// <inheritdoc />
    public byte[] Unprotect(byte[] protectedData, string purpose)
    {
        Guard.NotNull(protectedData, nameof(protectedData));
        ValidatePurpose(purpose);
        return ProtectedData.Unprotect(protectedData, CreateEntropy(purpose), _scope);
    }

    /// <summary>Derives purpose-specific DPAPI entropy so entries cannot be swapped between uses.</summary>
    private static byte[] CreateEntropy(string purpose)
    {
        byte[] purposeBytes = Encoding.UTF8.GetBytes(PurposePrefix + purpose);
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(purposeBytes);
        }
    }

    /// <summary>Validates that the protection purpose is present and reasonably bounded.</summary>
    private static void ValidatePurpose(string purpose)
    {
        Guard.NotNull(purpose, nameof(purpose));
        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Protection purpose cannot be empty.", nameof(purpose));
        }
    }
}

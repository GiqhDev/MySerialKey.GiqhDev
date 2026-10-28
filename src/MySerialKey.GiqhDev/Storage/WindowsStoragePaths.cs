#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Provides conventional Windows roots for protected MySerialKey data.
/// </summary>
#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public static class WindowsStoragePaths
{
    /// <summary>
    /// Gets the machine-wide root under the Windows common application-data folder.
    /// </summary>
    public static string GetMachineRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MySerialKey.GiqhDev");
    }

    /// <summary>
    /// Gets the current-user root under the Windows local application-data folder.
    /// </summary>
    public static string GetCurrentUserRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MySerialKey.GiqhDev");
    }
}

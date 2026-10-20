using System.Security.AccessControl;
using System.Security.Principal;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Applies an explicit Windows ACL to a machine-wide storage directory.
/// </summary>
#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public static class WindowsStoragePermissions
{
    /// <summary>
    /// Restricts a directory to Local System, local administrators, and the
    /// explicitly authorized application identities.
    /// </summary>
    /// <remarks>
    /// Call this from an installer or another elevated setup process. Existing
    /// inherited access rules are removed. Authorized identities receive Modify
    /// access inherited by child files and directories.
    /// </remarks>
    public static void ApplyRestrictedMachineAcl(
        string directoryPath,
        params SecurityIdentifier[] authorizedIdentities)
    {
        Guard.NotNull(directoryPath, nameof(directoryPath));
        Guard.NotNull(authorizedIdentities, nameof(authorizedIdentities));

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException(
                "Storage directory path cannot be empty.",
                nameof(directoryPath));
        }

        if (authorizedIdentities.Length == 0)
        {
            throw new ArgumentException(
                "At least one authorized application identity is required.",
                nameof(authorizedIdentities));
        }

        string fullPath = Path.GetFullPath(directoryPath);
        Directory.CreateDirectory(fullPath);

        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        AddRule(
            security,
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl);
        AddRule(
            security,
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl);

        foreach (SecurityIdentifier identity in authorizedIdentities)
        {
            if (identity is null)
            {
                throw new ArgumentException(
                    "Authorized identities cannot contain null values.",
                    nameof(authorizedIdentities));
            }

            AddRule(security, identity, FileSystemRights.Modify | FileSystemRights.Synchronize);
        }

        new DirectoryInfo(fullPath).SetAccessControl(security);
    }

    private static void AddRule(
        DirectorySecurity security,
        SecurityIdentifier identity,
        FileSystemRights rights)
    {
        security.AddAccessRule(new FileSystemAccessRule(
            identity,
            rights,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
    }
}

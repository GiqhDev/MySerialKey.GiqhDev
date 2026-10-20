using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Diagnostics;
using System.Text;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Installation;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;
using MySerialKey.GiqhDev.Licensing.Storage;
using MySerialKey.GiqhDev.Storage;
using MySerialKeyDpapiDataProtector = MySerialKey.GiqhDev.Storage.DpapiDataProtector;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public sealed class ProtectedStorageTests
{
    [TestMethod]
    public void DpapiDataProtector_RoundTripsAndAuthenticatesPurpose()
    {
        byte[] value = Encoding.UTF8.GetBytes("portable-secret");

        foreach (bool useMachineScope in new[] { false, true })
        {
            var protector = new MySerialKeyDpapiDataProtector(useMachineScope);
            byte[] protectedValue = protector.Protect(value, "Example.Product/license");
            byte[] recovered = protector.Unprotect(protectedValue, "Example.Product/license");

            CollectionAssert.AreEqual(value, recovered);
            Assert.ThrowsExactly<CryptographicException>(
                () => protector.Unprotect(protectedValue, "Another.Product/license"));
        }
    }

    [TestMethod]
    public void WindowsStoragePermissions_ApplyProtectedInheritedAcl()
    {
        string root = CreateTemporaryRoot();

        try
        {
            SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User
                ?? throw new InvalidOperationException("The current Windows user has no SID.");

            WindowsStoragePermissions.ApplyRestrictedMachineAcl(root, currentUser);

            DirectorySecurity security = new DirectoryInfo(root).GetAccessControl();
            AuthorizationRuleCollection rules = security.GetAccessRules(
                includeExplicit: true,
                includeInherited: false,
                typeof(SecurityIdentifier));
            bool currentUserCanModify = rules
                .OfType<FileSystemAccessRule>()
                .Any(rule =>
                    currentUser.Equals(rule.IdentityReference) &&
                    rule.AccessControlType == AccessControlType.Allow &&
                    (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify);

            Assert.IsTrue(security.AreAccessRulesProtected);
            Assert.IsTrue(currentUserCanModify);
            File.WriteAllText(Path.Combine(root, "access-check.txt"), "ok");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [TestMethod]
    public void FileStore_RoundTripsAndSeparatesProducts()
    {
        WithStore((store, unusedRoot) =>
        {
            byte[] value = Encoding.UTF8.GetBytes("license-a");

            Assert.AreEqual(
                ProtectedDataWriteStatus.Success,
                store.Write("Example.Product", "license", value).Status);

            ProtectedDataReadResult recovered = store.Read("Example.Product", "license");
            ProtectedDataReadResult otherProduct = store.Read("Another.Product", "license");

            Assert.AreEqual(ProtectedDataReadStatus.Success, recovered.Status);
            CollectionAssert.AreEqual(value, recovered.Value);
            Assert.AreEqual(ProtectedDataReadStatus.NotFound, otherProduct.Status);
        });
    }

    [TestMethod]
    public void FileStore_TryCreateDoesNotOverwriteExistingValue()
    {
        WithStore((store, unusedRoot) =>
        {
            byte[] first = Encoding.UTF8.GetBytes("first");
            byte[] second = Encoding.UTF8.GetBytes("second");

            Assert.AreEqual(
                ProtectedDataWriteStatus.Success,
                store.TryCreate("Example.Product", "entry", first).Status);
            Assert.AreEqual(
                ProtectedDataWriteStatus.AlreadyExists,
                store.TryCreate("Example.Product", "entry", second).Status);

            CollectionAssert.AreEqual(first, store.Read("Example.Product", "entry").Value);
        });
    }

    [TestMethod]
    public void FileStore_ReportsCorruptedFilesWithoutThrowing()
    {
        WithStore((store, root) =>
        {
            Assert.AreEqual(
                ProtectedDataWriteStatus.Success,
                store.Write("Example.Product", "entry", new byte[] { 1, 2, 3 }).Status);

            string path = Directory.GetFiles(root, "*.bin", SearchOption.AllDirectories).Single();
            File.WriteAllBytes(path, new byte[] { 0x00, 0x01 });

            ProtectedDataReadResult result = store.Read("Example.Product", "entry");

            Assert.AreEqual(ProtectedDataReadStatus.Corrupted, result.Status);
            Assert.IsNull(result.Value);
        });
    }

    [TestMethod]
    public void InstallationId_IsStableAndProductScoped()
    {
        WithStore((store, root) =>
        {
            var firstProvider = new PersistentInstallationIdProvider(store);

            string first = firstProvider.GetOrCreate("Example.Product");
            var reopenedStore = new FileProtectedDataStore(
                root,
                new MySerialKeyDpapiDataProtector());
            var secondProvider = new PersistentInstallationIdProvider(reopenedStore);
            string repeated = secondProvider.GetOrCreate("Example.Product");
            string otherProduct = secondProvider.GetOrCreate("Another.Product");

            Assert.AreEqual(first, repeated);
            Assert.AreNotEqual(first, otherProduct);
            Assert.IsTrue(Guid.TryParseExact(first, "N", out Guid parsed));
        });
    }

    [TestMethod]
    public void InstallationId_ConcurrentCreationReturnsSingleIdentifier()
    {
        WithStore((store, unusedRoot) =>
        {
            var provider = new PersistentInstallationIdProvider(store);
            Task<string>[] operations = Enumerable.Range(0, 12)
                .Select(_ => Task.Run(() => provider.GetOrCreate("Example.Product")))
                .ToArray();

            Task.WaitAll(operations);
            string[] identifiers = operations.Select(operation => operation.Result).ToArray();

            Assert.AreEqual(1, identifiers.Distinct(StringComparer.Ordinal).Count());
        });
    }

    [TestMethod]
    public void InstallationId_ConcurrentProcessesReturnSingleIdentifier()
    {
        string root = CreateTemporaryRoot();
        string gateFile = Path.Combine(root, "start.gate");
        Directory.CreateDirectory(root);
        var processes = new List<Process>();

        try
        {
            for (int index = 0; index < 8; index++)
            {
                processes.Add(StartProbe(root, gateFile));
            }

            File.WriteAllText(gateFile, "start");
            var identifiers = new List<string>();

            foreach (Process process in processes)
            {
                string output = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();

                Assert.IsTrue(process.WaitForExit(30000), "The process probe timed out.");
                Assert.AreEqual(0, process.ExitCode, error);
                identifiers.Add(output);
            }

            Assert.AreEqual(1, identifiers.Distinct(StringComparer.Ordinal).Count());
            Assert.IsTrue(Guid.TryParseExact(identifiers[0], "N", out Guid parsed));
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [TestMethod]
    public void ProtectedLicenseStore_RoundTripsPortableSignedLicense()
    {
        WithStore((store, unusedRoot) =>
        {
            var licenseStore = new ProtectedLicenseStore(store);
            using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var claims = new LicenseClaims(
                "license-storage-01",
                "Example.Product",
                new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
            SignedLicense license = new EcdsaLicenseSigner().Sign(claims, "key-01", key);

            Assert.IsTrue(licenseStore.Save("Example.Product", license));

            LicenseStoreReadResult loaded = licenseStore.Load("Example.Product");

            Assert.AreEqual(LicenseStoreReadStatus.Success, loaded.Status);
            Assert.AreEqual(license.ToString(), loaded.License!.ToString());
            Assert.AreEqual(
                LicenseStoreReadStatus.NotFound,
                licenseStore.Load("Another.Product").Status);
            Assert.IsTrue(licenseStore.Delete("Example.Product"));
            Assert.AreEqual(
                LicenseStoreReadStatus.NotFound,
                licenseStore.Load("Example.Product").Status);
        });
    }

    private static void WithStore(Action<FileProtectedDataStore, string> test)
    {
        string root = CreateTemporaryRoot();

        try
        {
            var store = new FileProtectedDataStore(root, new MySerialKeyDpapiDataProtector());
            test(store, root);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static string CreateTemporaryRoot()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "MySerialKey.GiqhDev.Tests",
            Guid.NewGuid().ToString("N"));
    }

    private static Process StartProbe(string storageRoot, string gateFile)
    {
        string probePath = GetProbePath();
        var startInfo = new ProcessStartInfo
        {
#if NET48
            FileName = probePath,
            Arguments = Quote(storageRoot) + " Example.Product " + Quote(gateFile),
#else
            FileName = "dotnet",
            Arguments = Quote(probePath) + " " + Quote(storageRoot) +
                " Example.Product " + Quote(gateFile),
#endif
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("The process probe could not be started.");
    }

    private static string GetProbePath()
    {
        var frameworkDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        string configuration = frameworkDirectory.Parent?.Name
            ?? throw new InvalidOperationException("The test configuration directory was not found.");
        DirectoryInfo testsDirectory = frameworkDirectory.Parent?.Parent?.Parent?.Parent
            ?? throw new InvalidOperationException("The tests directory was not found.");

#if NET48
        const string framework = "net48";
        const string executable = "MySerialKey.GiqhDev.ProcessProbe.exe";
#else
        const string framework = "net10.0";
        const string executable = "MySerialKey.GiqhDev.ProcessProbe.dll";
#endif

        string probePath = Path.Combine(
            testsDirectory.FullName,
            "MySerialKey.GiqhDev.ProcessProbe",
            "bin",
            configuration,
            framework,
            executable);

        if (!File.Exists(probePath))
        {
            throw new FileNotFoundException("The process probe was not built.", probePath);
        }

        return probePath;
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}

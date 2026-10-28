using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Activation.Offline;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Storage;
using MySerialKey.GiqhDev.Storage;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class OfflineActivationTests
{
    private const string ProductCode = "Example.Product";
    private const string InstallationId = "installation-01";
    private const string KeyId = "offline-production-2026";
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Request_RoundTripsTokenAndFileWithUniqueNonce()
    {
        ActivationRequest first = CreateRequest();
        ActivationRequest second = CreateRequest();
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "activation.msar");

        try
        {
            first.Save(path);
            ActivationRequest fromToken = ActivationRequest.Parse(first.ToString());
            ActivationRequest fromFile = ActivationRequest.Load(path);

            Assert.IsTrue(first.ToString().StartsWith("MSAR1.", StringComparison.Ordinal));
            Assert.AreEqual(first.RequestId, fromToken.RequestId);
            Assert.AreEqual(first.RequestId, fromFile.RequestId);
            Assert.AreEqual(ProductCode, fromFile.ProductCode);
            Assert.AreEqual(InstallationId, fromFile.InstallationId);
            Assert.AreEqual("2.0.0", fromFile.ApplicationVersion);
            CollectionAssert.AreEqual(first.Nonce, fromFile.Nonce);
            Assert.AreNotEqual(first.RequestId, second.RequestId);
            CollectionAssert.AreNotEqual(first.Nonce, second.Nonce);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Response_RoundTripsAndCarriesNormalMsl1License()
    {
        using ECDsa key = CreateKey();
        ActivationRequest request = CreateRequest();
        ActivationResponse response = Issue(request, key);
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "activation.msap");

        try
        {
            response.Save(path);
            ActivationResponse parsed = ActivationResponse.Load(path);

            Assert.IsTrue(response.ToString().StartsWith("MSAP1.ES256.", StringComparison.Ordinal));
            Assert.IsTrue(parsed.License.ToString().StartsWith("MSL1.ES256.", StringComparison.Ordinal));
            Assert.AreEqual(request.RequestId, parsed.RequestId);
            Assert.AreEqual(response.License.ToString(), parsed.License.ToString());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Import_ValidatesAndStoresExactEmbeddedLicense()
    {
        WithClient((client, licenseStore, key) =>
        {
            ActivationRequest request = CreateRequest();
            ActivationResponse response = Issue(request, key);

            OfflineActivationImportResult result = client.Import(
                request,
                response,
                IssuedAt.AddMinutes(1),
                Trust(key));
            LicenseStoreReadResult stored = licenseStore.Load(ProductCode);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(OfflineActivationImportStatus.Applied, result.Status);
            Assert.IsNotNull(result.LicenseValidation);
            Assert.IsTrue(result.LicenseValidation.IsValid);
            Assert.AreEqual(LicenseStoreReadStatus.Success, stored.Status);
            Assert.AreEqual(response.License.ToString(), stored.License!.ToString());
        });
    }

    [TestMethod]
    public void Import_RejectsTamperedResponse()
    {
        WithClient((client, unusedStore, key) =>
        {
            ActivationRequest request = CreateRequest();
            ActivationResponse response = Issue(request, key);
            string[] parts = response.ToString().Split('.');
            parts[4] = Mutate(parts[4]);

            OfflineActivationImportResult result = client.Import(
                request.ToString(),
                string.Join(".", parts),
                IssuedAt.AddMinutes(1),
                Trust(key));

            Assert.AreEqual(OfflineActivationImportStatus.InvalidSignature, result.Status);
        });
    }

    [TestMethod]
    public void Import_RejectsResponseForDifferentRequest()
    {
        WithClient((client, unusedStore, key) =>
        {
            ActivationRequest expected = CreateRequest();
            ActivationRequest another = CreateRequest();
            ActivationResponse response = Issue(another, key);

            OfflineActivationImportResult result = client.Import(
                expected,
                response,
                IssuedAt.AddMinutes(1),
                Trust(key));

            Assert.AreEqual(OfflineActivationImportStatus.WrongRequest, result.Status);
        });
    }

    [TestMethod]
    public void Import_RejectsWrongLocalProductAndInstallation()
    {
        WithStores((licenseStore, stateStore, key) =>
        {
            ActivationRequest request = CreateRequest();
            ActivationResponse response = Issue(request, key);
            var wrongProduct = new OfflineActivationClient(
                "Another.Product",
                InstallationId,
                licenseStore,
                stateStore);
            var wrongInstallation = new OfflineActivationClient(
                ProductCode,
                "installation-02",
                licenseStore,
                stateStore);

            Assert.AreEqual(
                OfflineActivationImportStatus.WrongProduct,
                wrongProduct.Import(request, response, IssuedAt, Trust(key)).Status);
            Assert.AreEqual(
                OfflineActivationImportStatus.WrongInstallation,
                wrongInstallation.Import(request, response, IssuedAt, Trust(key)).Status);
        });
    }

    [TestMethod]
    public void Import_RejectsExpiredRequestAndResponse()
    {
        WithClient((client, unusedStore, key) =>
        {
            ActivationRequest request = CreateRequest(TimeSpan.FromDays(2));
            ActivationResponse response = Issue(request, key, TimeSpan.FromHours(1));

            Assert.AreEqual(
                OfflineActivationImportStatus.ResponseExpired,
                client.Import(request, response, IssuedAt.AddHours(1), Trust(key)).Status);
            Assert.AreEqual(
                OfflineActivationImportStatus.RequestExpired,
                client.Import(request, response, IssuedAt.AddDays(2), Trust(key)).Status);
        });
    }

    [TestMethod]
    public void Import_RejectsUnknownSigningKey()
    {
        WithClient((client, unusedStore, key) =>
        {
            using ECDsa otherKey = CreateKey();
            ActivationRequest request = CreateRequest();
            ActivationResponse response = Issue(request, key);
            var trustedKeys = new[] { new LicenseVerificationKey("another-key", otherKey) };

            OfflineActivationImportResult result = client.Import(
                request,
                response,
                IssuedAt,
                trustedKeys);

            Assert.AreEqual(OfflineActivationImportStatus.UnknownSigningKey, result.Status);
        });
    }

    [TestMethod]
    public void Import_RejectsReplayAfterSuccessfulActivation()
    {
        WithClient((client, unusedStore, key) =>
        {
            ActivationRequest request = CreateRequest();
            ActivationResponse response = Issue(request, key);

            OfflineActivationImportResult first = client.Import(
                request,
                response,
                IssuedAt,
                Trust(key));
            OfflineActivationImportResult repeated = client.Import(
                request,
                response,
                IssuedAt,
                Trust(key));

            Assert.AreEqual(OfflineActivationImportStatus.Applied, first.Status);
            Assert.AreEqual(OfflineActivationImportStatus.ReplayDetected, repeated.Status);
        });
    }

    [TestMethod]
    public void Import_ReturnsTypedResultsForMalformedTokens()
    {
        WithClient((client, unusedStore, key) =>
        {
            Assert.AreEqual(
                OfflineActivationImportStatus.InvalidRequest,
                client.Import("invalid", "invalid", IssuedAt, Trust(key)).Status);

            ActivationRequest request = CreateRequest();
            Assert.AreEqual(
                OfflineActivationImportStatus.InvalidResponse,
                client.Import(request.ToString(), "invalid", IssuedAt, Trust(key)).Status);
        });
    }

    [TestMethod]
    public void Issuer_RejectsMismatchedClaimsAndExpiredRequest()
    {
        using ECDsa key = CreateKey();
        ActivationRequest request = CreateRequest(TimeSpan.FromHours(1));
        LicenseClaims wrongProduct = CreateClaims();
        wrongProduct.ProductCode = "Another.Product";
        var issuer = new OfflineActivationIssuer();

        Assert.ThrowsExactly<ArgumentException>(
            () => issuer.Issue(request, wrongProduct, KeyId, key, IssuedAt));
        Assert.ThrowsExactly<ArgumentException>(
            () => issuer.Issue(request, CreateClaims(), KeyId, key, IssuedAt.AddHours(1)));
    }

    private static ActivationRequest CreateRequest(TimeSpan? validity = null)
    {
        return ActivationRequest.Create(
            ProductCode,
            InstallationId,
            IssuedAt,
            validity ?? TimeSpan.FromDays(2),
            "2.0.0");
    }

    private static ActivationResponse Issue(
        ActivationRequest request,
        ECDsa key,
        TimeSpan? validity = null)
    {
        return new OfflineActivationIssuer().Issue(
            request,
            CreateClaims(),
            KeyId,
            key,
            IssuedAt,
            validity ?? TimeSpan.FromDays(1));
    }

    private static LicenseClaims CreateClaims()
    {
        return new LicenseClaims("license-offline-01", ProductCode, IssuedAt)
        {
            InstallationId = InstallationId,
            Edition = "Professional",
            Features = new[] { "feature-a", "feature-b" }
        };
    }

    private static LicenseVerificationKey[] Trust(ECDsa key)
    {
        return new[] { new LicenseVerificationKey(KeyId, key) };
    }

    private static ECDsa CreateKey()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static void WithClient(
        Action<OfflineActivationClient, ProtectedLicenseStore, ECDsa> test)
    {
        WithStores((licenseStore, stateStore, key) =>
        {
            var client = new OfflineActivationClient(
                ProductCode,
                InstallationId,
                licenseStore,
                stateStore);
            test(client, licenseStore, key);
        });
    }

    private static void WithStores(
        Action<ProtectedLicenseStore, FileProtectedDataStore, ECDsa> test)
    {
        string root = CreateTemporaryRoot();
        var protectedStore = new FileProtectedDataStore(root, new PassthroughProtector());
        var licenseStore = new ProtectedLicenseStore(protectedStore);
        using ECDsa key = CreateKey();

        try
        {
            test(licenseStore, protectedStore, key);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTemporaryRoot()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "MySerialKey.GiqhDev.OfflineActivationTests",
            Guid.NewGuid().ToString("N"));
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private static string Mutate(string value)
    {
        char replacement = value[0] == 'A' ? 'B' : 'A';
        return replacement + value.Substring(1);
    }

    private sealed class PassthroughProtector : IDataProtector
    {
        public byte[] Protect(byte[] plaintext, string purpose)
        {
            return (byte[])plaintext.Clone();
        }

        public byte[] Unprotect(byte[] protectedData, string purpose)
        {
            return (byte[])protectedData.Clone();
        }
    }
}

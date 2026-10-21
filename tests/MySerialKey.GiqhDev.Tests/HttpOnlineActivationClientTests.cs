using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Activation.Online;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;
using MySerialKey.GiqhDev.Licensing.Storage;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class HttpOnlineActivationClientTests
{
    private const string ProductCode = "Example.Product";
    private const string InstallationId = "installation-01";
    private const string KeyId = "online-production-2026";
    private static readonly DateTimeOffset Now =
        new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task ActivateAsync_ValidatesBeforeStoringExactMsl1License()
    {
        using ECDsa key = CreateKey();
        SignedLicense license = Sign(key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), license)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var activation = new OnlineActivationRequest(
            "ABCDE-123456-ABCDE-123456",
            ProductCode,
            "2.0.0",
            InstallationId);

        ActivationClientResult result = await client.ActivateAsync(activation);

        Assert.AreEqual(ActivationClientStatus.Success, result.Status);
        Assert.IsTrue(result.LicenseValidation!.IsValid);
        Assert.AreEqual(license.ToString(), store.SavedLicense!.ToString());
        Assert.AreEqual(ProductCode, store.SavedProductCode);
        StringAssert.Contains(handler.Bodies.Single(), "\"productCode\":\"Example.Product\"");
        StringAssert.Contains(handler.Bodies.Single(), "\"installationId\":\"installation-01\"");
    }

    [TestMethod]
    public async Task ActivateAsync_DoesNotStoreLicenseWithInvalidSignature()
    {
        using ECDsa trustedKey = CreateKey();
        using ECDsa untrustedKey = CreateKey();
        SignedLicense license = Sign(untrustedKey);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), license)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, trustedKey);

        ActivationClientResult result = await client.ActivateAsync(NewRequest());

        Assert.AreEqual(ActivationClientStatus.InvalidLicense, result.Status);
        Assert.AreEqual(LicenseValidationStatus.InvalidSignature, result.LicenseValidation!.Status);
        Assert.IsNull(store.SavedLicense);
    }

    [TestMethod]
    public async Task ActivateAsync_RetriesTransientFailureWithSameOperationIdentity()
    {
        using ECDsa key = CreateKey();
        SignedLicense license = Sign(key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, call) => call == 1
            ? Json(HttpStatusCode.ServiceUnavailable, "{}")
            : Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), license)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key, maxRetries: 1);
        OnlineActivationRequest activation = NewRequest();

        ActivationClientResult result = await client.ActivateAsync(activation);

        Assert.AreEqual(ActivationClientStatus.Success, result.Status);
        Assert.AreEqual(2, handler.Calls);
        Assert.AreEqual(1, handler.IdempotencyKeys.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(activation.OperationId, handler.IdempotencyKeys[0]);
        Assert.AreEqual(handler.Bodies[0], handler.Bodies[1]);
    }

    [TestMethod]
    public async Task ActivateAsync_MapsVersionedRateLimitError()
    {
        using ECDsa key = CreateKey();
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) => Json(
            (HttpStatusCode)429,
            "{\"schemaVersion\":1,\"operationId\":\"" + OperationId(request) +
            "\",\"errorCode\":\"rate_limited\",\"message\":\"Try later.\"," +
            "\"retryable\":true,\"retryAfterSeconds\":30}"));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key, maxRetries: 0);

        ActivationClientResult result = await client.ActivateAsync(NewRequest());

        Assert.AreEqual(ActivationClientStatus.RateLimited, result.Status);
        Assert.AreEqual("rate_limited", result.ErrorCode);
        Assert.AreEqual(TimeSpan.FromSeconds(30), result.RetryAfter);
        Assert.IsNull(store.SavedLicense);
    }

    [TestMethod]
    public async Task QueryAsync_UsesQueryEndpointAndImportsValidatedCurrentLicense()
    {
        using ECDsa key = CreateKey();
        SignedLicense license = Sign(key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), license)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var request = new OnlineLicenseQueryRequest(
            license,
            ProductCode,
            "2.0.0",
            InstallationId);

        ActivationClientResult result = await client.QueryAsync(request);

        Assert.AreEqual(ActivationClientStatus.Success, result.Status);
        Assert.AreEqual(
            "/" + ActivationServiceEndpoints.LicenseQuery,
            handler.RequestUris.Single().AbsolutePath);
        Assert.AreEqual(license.ToString(), store.SavedLicense!.ToString());
    }

    [TestMethod]
    public async Task QueryAsync_PersistsAuthenticRevocationInsteadOfKeepingActiveLicense()
    {
        using ECDsa key = CreateKey();
        SignedLicense activeLicense = Sign(key);
        var revokedClaims = new LicenseClaims("license-online-01", ProductCode, Now)
        {
            InstallationId = InstallationId,
            ExpiresAtUtc = Now.AddDays(30),
            EntitlementStatus = LicenseEntitlementStatus.Revoked
        };
        SignedLicense revokedLicense = new EcdsaLicenseSigner().Sign(
            revokedClaims,
            KeyId,
            key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), revokedLicense)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var request = new OnlineLicenseQueryRequest(
            activeLicense,
            ProductCode,
            "2.0.0",
            InstallationId);

        ActivationClientResult result = await client.QueryAsync(request);

        Assert.AreEqual(ActivationClientStatus.Revoked, result.Status);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(LicenseValidationStatus.Revoked, result.LicenseValidation!.Status);
        Assert.AreEqual(revokedLicense.ToString(), store.SavedLicense!.ToString());
    }

    [TestMethod]
    public async Task TransferAsync_ImportsOnlyLicenseBoundToTargetInstallation()
    {
        using ECDsa key = CreateKey();
        SignedLicense sourceLicense = Sign(key);
        var targetClaims = new LicenseClaims("license-online-01", ProductCode, Now)
        {
            InstallationId = "installation-02",
            ExpiresAtUtc = Now.AddDays(30)
        };
        SignedLicense targetLicense = new EcdsaLicenseSigner().Sign(targetClaims, KeyId, key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), targetLicense)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var request = new OnlineLicenseTransferRequest(
            sourceLicense,
            ProductCode,
            "2.0.0",
            InstallationId,
            "installation-02");

        ActivationClientResult result = await client.TransferAsync(request);

        Assert.AreEqual(ActivationClientStatus.Success, result.Status);
        Assert.AreEqual(targetLicense.ToString(), store.SavedLicense!.ToString());
        Assert.AreEqual(
            "/" + ActivationServiceEndpoints.Transfer,
            handler.RequestUris.Single().AbsolutePath);
        StringAssert.Contains(handler.Bodies.Single(), "\"sourceInstallationId\":\"installation-01\"");
        StringAssert.Contains(handler.Bodies.Single(), "\"targetInstallationId\":\"installation-02\"");
    }

    [TestMethod]
    public async Task TransferAsync_DoesNotReplaceLocalStateWithSourceBoundResponse()
    {
        using ECDsa key = CreateKey();
        SignedLicense sourceLicense = Sign(key);
        var store = new RecordingLicenseStore();
        var handler = new StubHandler((request, unusedCall) =>
            Json(HttpStatusCode.OK, LicenseResponse(OperationId(request), sourceLicense)));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var request = new OnlineLicenseTransferRequest(
            sourceLicense,
            ProductCode,
            "2.0.0",
            InstallationId,
            "installation-02");

        ActivationClientResult result = await client.TransferAsync(request);

        Assert.AreEqual(ActivationClientStatus.InvalidLicense, result.Status);
        Assert.AreEqual(LicenseValidationStatus.WrongInstallation, result.LicenseValidation!.Status);
        Assert.IsNull(store.SavedLicense);
    }

    [TestMethod]
    public async Task DeactivateAsync_RemovesLicenseOnlyAfterValidServiceResponse()
    {
        using ECDsa key = CreateKey();
        SignedLicense license = Sign(key);
        var store = new RecordingLicenseStore { DeleteResult = true };
        var handler = new StubHandler((request, unusedCall) => Json(
            HttpStatusCode.OK,
            "{\"schemaVersion\":1,\"operationId\":\"" + OperationId(request) +
            "\",\"trustedTimeUtc\":\"" + Now.ToString("O") + "\"}"));
        using HttpClient http = CreateHttpClient(handler);
        HttpOnlineActivationClient client = CreateClient(http, store, key);
        var request = new OnlineLicenseDeactivationRequest(
            license,
            ProductCode,
            "2.0.0",
            InstallationId);

        ActivationClientResult result = await client.DeactivateAsync(request);

        Assert.AreEqual(ActivationClientStatus.Success, result.Status);
        Assert.AreEqual(ProductCode, store.DeletedProductCode);
    }

    private static OnlineActivationRequest NewRequest()
    {
        return new OnlineActivationRequest(
            "ABCDE-123456-ABCDE-123456",
            ProductCode,
            "2.0.0",
            InstallationId);
    }

    private static HttpOnlineActivationClient CreateClient(
        HttpClient http,
        RecordingLicenseStore store,
        ECDsa key,
        int maxRetries = 0)
    {
        var options = new HttpOnlineActivationClientOptions
        {
            MaxRetries = maxRetries,
            RetryDelay = TimeSpan.Zero
        };
        return new HttpOnlineActivationClient(
            http,
            store,
            new[] { new LicenseVerificationKey(KeyId, key) },
            options,
            () => Now);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://licensing.example.test/")
        };
    }

    private static SignedLicense Sign(ECDsa key)
    {
        var claims = new LicenseClaims("license-online-01", ProductCode, Now)
        {
            InstallationId = InstallationId,
            ExpiresAtUtc = Now.AddDays(30)
        };
        return new EcdsaLicenseSigner().Sign(claims, KeyId, key);
    }

    private static ECDsa CreateKey()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static string LicenseResponse(string operationId, SignedLicense license)
    {
        return "{\"schemaVersion\":1,\"operationId\":\"" + operationId +
            "\",\"licenseToken\":\"" + license +
            "\",\"trustedTimeUtc\":\"" + Now.ToString("O") + "\"}";
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    private static string OperationId(HttpRequestMessage request)
    {
        return request.Headers.GetValues("Idempotency-Key").Single();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _response;

        public StubHandler(Func<HttpRequestMessage, int, HttpResponseMessage> response)
        {
            _response = response;
        }

        public int Calls { get; private set; }
        public List<string> Bodies { get; } = new List<string>();
        public List<string> IdempotencyKeys { get; } = new List<string>();
        public List<Uri> RequestUris { get; } = new List<Uri>();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Bodies.Add(await request.Content!.ReadAsStringAsync());
            IdempotencyKeys.Add(OperationId(request));
            RequestUris.Add(request.RequestUri!);
            return _response(request, Calls);
        }
    }

    private sealed class RecordingLicenseStore : ILicenseStore
    {
        public string? SavedProductCode { get; private set; }
        public SignedLicense? SavedLicense { get; private set; }
        public string? DeletedProductCode { get; private set; }
        public bool DeleteResult { get; set; }

        public LicenseStoreReadResult Load(string productCode)
        {
            throw new NotSupportedException();
        }

        public bool Save(string productCode, SignedLicense license)
        {
            SavedProductCode = productCode;
            SavedLicense = license;
            return true;
        }

        public bool Delete(string productCode)
        {
            DeletedProductCode = productCode;
            return DeleteResult;
        }
    }
}

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Activation.Online;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class OnlineActivationContractTests
{
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void ActivationRequest_IsVersionedAndContainsGenericConsumerContext()
    {
        var request = new OnlineActivationRequest(
            "ABCDE-123456-ABCDE-123456",
            "Example.Product",
            "2.0.0",
            "installation-01");

        Assert.AreEqual(1, request.SchemaVersion);
        Assert.IsTrue(Guid.TryParseExact(request.OperationId, "N", out _));
        Assert.AreEqual("Example.Product", request.ProductCode);
        Assert.AreEqual("2.0.0", request.ApplicationVersion);
        Assert.AreEqual("installation-01", request.InstallationId);
    }

    [TestMethod]
    public void ExplicitOperationId_IsPreservedAcrossRetryableRequests()
    {
        string operationId = Guid.NewGuid().ToString("N");
        var first = new OnlineActivationRequest(
            "ABCDE-123456-ABCDE-123456",
            "Example.Product",
            "2.0.0",
            "installation-01",
            operationId);
        var retry = new OnlineActivationRequest(
            first.Serial,
            first.ProductCode,
            first.ApplicationVersion,
            first.InstallationId,
            first.OperationId);

        Assert.AreEqual(operationId, retry.OperationId);
    }

    [TestMethod]
    public void RenewalAndDeactivation_CarryTheExactCurrentMsl1Token()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var claims = new LicenseClaims("license-01", "Example.Product", IssuedAt)
        {
            InstallationId = "installation-01"
        };
        SignedLicense license = new EcdsaLicenseSigner().Sign(claims, "key-01", key);

        var renewal = new OnlineLicenseRenewalRequest(
            license,
            "Example.Product",
            "2.0.0",
            "installation-01");
        var deactivation = new OnlineLicenseDeactivationRequest(
            license,
            "Example.Product",
            "2.0.0",
            "installation-01");
        var query = new OnlineLicenseQueryRequest(
            license,
            "Example.Product",
            "2.0.0",
            "installation-01");
        var transfer = new OnlineLicenseTransferRequest(
            license,
            "Example.Product",
            "2.0.0",
            "installation-01",
            "installation-02");

        Assert.AreEqual(license.ToString(), renewal.LicenseToken);
        Assert.AreEqual(license.ToString(), deactivation.LicenseToken);
        Assert.AreEqual(license.ToString(), query.LicenseToken);
        Assert.AreEqual(license.ToString(), transfer.LicenseToken);
        Assert.AreEqual("installation-01", transfer.SourceInstallationId);
        Assert.AreEqual("installation-02", transfer.TargetInstallationId);
        Assert.AreEqual(1, renewal.SchemaVersion);
        Assert.AreEqual(1, deactivation.SchemaVersion);
        Assert.AreEqual(1, query.SchemaVersion);
        Assert.AreEqual(1, transfer.SchemaVersion);
    }

    [TestMethod]
    public void Contracts_RejectMissingContextAndInvalidOperationIds()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new OnlineActivationRequest(
            "serial",
            " ",
            "2.0.0",
            "installation-01"));
        Assert.ThrowsExactly<ArgumentException>(() => new OnlineActivationRequest(
            "serial",
            "Example.Product",
            "2.0.0",
            "installation-01",
            "not-a-guid"));
        Assert.ThrowsExactly<ArgumentException>(() => new OnlineActivationResponse(
            Guid.NewGuid().ToString("N"),
            " ",
            IssuedAt));
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        SignedLicense license = new EcdsaLicenseSigner().Sign(
            new LicenseClaims("license-01", "Example.Product", IssuedAt),
            "key-01",
            key);
        Assert.ThrowsExactly<ArgumentException>(() => new OnlineLicenseTransferRequest(
            license,
            "Example.Product",
            "2.0.0",
            "same-installation",
            "same-installation"));
    }
}

using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class LicenseSigningTests
{
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void SignAndValidate_RoundTripsAuthenticatedClaims()
    {
        using ECDsa privateKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);
        SignedLicense parsed = SignedLicense.Parse(signedLicense.ToString());

        LicenseValidationResult result = Validate(
            parsed,
            privateKey,
            productCode: "Example.Product",
            installationId: "installation-01",
            nowUtc: IssuedAt.AddDays(1));

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(LicenseValidationStatus.Valid, result.Status);
        Assert.IsNotNull(result.Claims);
        Assert.AreEqual("license-01", result.Claims.LicenseId);
        CollectionAssert.AreEqual(
            new[] { "feature-a", "feature-b", "feature-c" },
            result.Claims.Features.ToArray());
    }

    [TestMethod]
    public void Validate_RejectsAlteredPayload()
    {
        using ECDsa privateKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);
        string[] parts = signedLicense.ToString().Split('.');
        parts[3] = Mutate(parts[3]);
        SignedLicense altered = SignedLicense.Parse(string.Join(".", parts));

        LicenseValidationResult result = Validate(
            altered,
            privateKey,
            "Example.Product",
            "installation-01",
            IssuedAt.AddDays(1));

        Assert.AreEqual(LicenseValidationStatus.InvalidSignature, result.Status);
    }

    [TestMethod]
    public void Validate_AuthenticatesKeyIdentifier()
    {
        using ECDsa privateKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "key-a", privateKey);
        string[] parts = signedLicense.ToString().Split('.');
        parts[2] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("key-b"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        SignedLicense altered = SignedLicense.Parse(string.Join(".", parts));
        var trustedKeys = new[]
        {
            new LicenseVerificationKey("key-a", privateKey),
            new LicenseVerificationKey("key-b", privateKey)
        };

        var validator = new EcdsaLicenseValidator();
        var context = new LicenseValidationContext(
            "Example.Product",
            IssuedAt.AddDays(1),
            "installation-01");
        LicenseValidationResult result = validator.Validate(altered, context, trustedKeys);

        Assert.AreEqual(LicenseValidationStatus.InvalidSignature, result.Status);
    }

    [TestMethod]
    public void Validate_RejectsUnknownSigningKey()
    {
        using ECDsa privateKey = CreateKey();
        using ECDsa otherKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);
        var validator = new EcdsaLicenseValidator();
        var context = new LicenseValidationContext(
            "Example.Product",
            IssuedAt.AddDays(1),
            "installation-01");
        var trustedKeys = new[]
        {
            new LicenseVerificationKey("another-key", otherKey)
        };

        LicenseValidationResult result = validator.Validate(signedLicense, context, trustedKeys);

        Assert.AreEqual(LicenseValidationStatus.UnknownSigningKey, result.Status);
    }

    [TestMethod]
    public void Validate_RejectsSignatureFromDifferentKey()
    {
        using ECDsa privateKey = CreateKey();
        using ECDsa otherKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);

        LicenseValidationResult result = Validate(
            signedLicense,
            otherKey,
            "Example.Product",
            "installation-01",
            IssuedAt.AddDays(1));

        Assert.AreEqual(LicenseValidationStatus.InvalidSignature, result.Status);
    }

    [TestMethod]
    public void Validate_SupportsKeyRotation()
    {
        using ECDsa oldKey = CreateKey();
        using ECDsa currentKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "current", currentKey);
        var trustedKeys = new[]
        {
            new LicenseVerificationKey("old", oldKey),
            new LicenseVerificationKey("current", currentKey)
        };
        var validator = new EcdsaLicenseValidator();
        var context = new LicenseValidationContext(
            "Example.Product",
            IssuedAt.AddDays(1),
            "installation-01");

        LicenseValidationResult result = validator.Validate(signedLicense, context, trustedKeys);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_RejectsWrongProduct()
    {
        using ECDsa privateKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);

        LicenseValidationResult result = Validate(
            signedLicense,
            privateKey,
            "Another.Product",
            "installation-01",
            IssuedAt.AddDays(1));

        Assert.AreEqual(LicenseValidationStatus.WrongProduct, result.Status);
    }

    [TestMethod]
    public void Validate_RejectsWrongInstallation()
    {
        using ECDsa privateKey = CreateKey();
        SignedLicense signedLicense = Sign(CreateClaims(), "production-2026-01", privateKey);

        LicenseValidationResult result = Validate(
            signedLicense,
            privateKey,
            "Example.Product",
            "another-installation",
            IssuedAt.AddDays(1));

        Assert.AreEqual(LicenseValidationStatus.WrongInstallation, result.Status);
    }

    [TestMethod]
    public void Validate_RejectsNotYetValidLicense()
    {
        using ECDsa privateKey = CreateKey();
        LicenseClaims claims = CreateClaims();
        claims.NotBeforeUtc = IssuedAt.AddDays(2);
        SignedLicense signedLicense = Sign(claims, "production-2026-01", privateKey);

        LicenseValidationResult result = Validate(
            signedLicense,
            privateKey,
            "Example.Product",
            "installation-01",
            IssuedAt.AddDays(1));

        Assert.AreEqual(LicenseValidationStatus.NotYetValid, result.Status);
    }

    [TestMethod]
    public void Validate_RejectsExpiredLicense()
    {
        using ECDsa privateKey = CreateKey();
        LicenseClaims claims = CreateClaims();
        claims.ExpiresAtUtc = IssuedAt.AddDays(2);
        SignedLicense signedLicense = Sign(claims, "production-2026-01", privateKey);

        LicenseValidationResult result = Validate(
            signedLicense,
            privateKey,
            "Example.Product",
            "installation-01",
            IssuedAt.AddDays(2));

        Assert.AreEqual(LicenseValidationStatus.Expired, result.Status);
    }

    [TestMethod]
    public void Parse_RejectsMalformedTokens()
    {
        Assert.IsFalse(SignedLicense.TryParse("not-a-license", out _));
        Assert.ThrowsExactly<FormatException>(() => SignedLicense.Parse("not-a-license"));
    }

    [TestMethod]
    public void Sign_RejectsNonP256Keys()
    {
        using ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var signer = new EcdsaLicenseSigner();

        Assert.ThrowsExactly<ArgumentException>(
            () => signer.Sign(CreateClaims(), "production-2026-01", privateKey));
    }

    private static ECDsa CreateKey()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static LicenseClaims CreateClaims()
    {
        return new LicenseClaims("license-01", "Example.Product", IssuedAt)
        {
            CustomerId = "customer-01",
            Edition = "Professional",
            InstallationId = "installation-01",
            MaxTerminals = 1,
            Features = new[] { "feature-c", "feature-a", "feature-b", "feature-a" }
        };
    }

    private static SignedLicense Sign(LicenseClaims claims, string keyId, ECDsa privateKey)
    {
        var signer = new EcdsaLicenseSigner();
        return signer.Sign(claims, keyId, privateKey);
    }

    private static LicenseValidationResult Validate(
        SignedLicense signedLicense,
        ECDsa publicKey,
        string productCode,
        string? installationId,
        DateTimeOffset nowUtc)
    {
        var validator = new EcdsaLicenseValidator();
        var context = new LicenseValidationContext(productCode, nowUtc, installationId);
        var trustedKeys = new[]
        {
            new LicenseVerificationKey(signedLicense.KeyId, publicKey)
        };
        return validator.Validate(signedLicense, context, trustedKeys);
    }

    private static string Mutate(string value)
    {
        char replacement = value[0] == 'A' ? 'B' : 'A';
        return replacement + value.Substring(1);
    }
}

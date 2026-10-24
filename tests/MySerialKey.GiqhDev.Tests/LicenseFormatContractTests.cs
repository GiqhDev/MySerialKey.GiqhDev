using System.Globalization;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class LicenseFormatContractTests
{
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void PublicKeyToken_RoundTripsAndProviderValidatesLicense()
    {
        using ECDsa signingKey = CreateKey();
        var originalKey = new LicenseVerificationKey("rotation-2026", signingKey);
        string publicKeyToken = originalKey.Export();

        LicenseVerificationKey importedKey = LicenseVerificationKey.Parse(publicKeyToken);
        var provider = new SingleKeyProvider(importedKey);
        SignedLicense license = new EcdsaLicenseSigner().Sign(
            CreateClaims(),
            originalKey.KeyId,
            signingKey);

        LicenseValidationResult result = new EcdsaLicenseValidator().Validate(
            license,
            new LicenseValidationContext("Example.Product", IssuedAt.AddHours(1), "installation-01"),
            provider);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("MSPK1", publicKeyToken.Split('.')[0]);
        Assert.AreEqual(originalKey.KeyId, importedKey.KeyId);
        Assert.AreEqual(LicenseAlgorithms.EcdsaP256Sha256, importedKey.Algorithm);
    }

    [TestMethod]
    public void PublicKeyToken_RejectsMalformedUnsupportedAndOversizedInputs()
    {
        using ECDsa key = CreateKey();
        string token = new LicenseVerificationKey("key-01", key).Export();
        string[] parts = token.Split('.');
        parts[1] = "UNSUPPORTED";

        Assert.IsFalse(LicenseVerificationKey.TryParse(string.Join(".", parts), out _));
        Assert.IsFalse(LicenseVerificationKey.TryParse(token + ".extra", out _));
        Assert.IsFalse(LicenseVerificationKey.TryParse(
            new string('A', LicenseFormatLimits.MaximumPublicKeyTokenCharacters + 1),
            out _));
        Assert.ThrowsExactly<FormatException>(() => LicenseVerificationKey.Parse("invalid"));
    }

    [TestMethod]
    public void Signing_EnforcesPublishedTextFeatureAndPayloadLimits()
    {
        using ECDsa key = CreateKey();
        var signer = new EcdsaLicenseSigner();
        LicenseClaims longText = CreateClaims();
        longText.CustomerId = new string('A', LicenseFormatLimits.MaximumClaimTextUtf8Bytes + 1);

        LicenseClaims tooManyFeatures = CreateClaims();
        tooManyFeatures.Features = Enumerable.Range(0, LicenseFormatLimits.MaximumFeatures + 1)
            .Select(index => "feature-" + index.ToString(CultureInfo.InvariantCulture))
            .ToArray();

        LicenseClaims largePayload = CreateClaims();
        largePayload.Features = Enumerable.Range(0, 7)
            .Select(index =>
                index.ToString(CultureInfo.InvariantCulture) +
                new string('F', LicenseFormatLimits.MaximumClaimTextUtf8Bytes - 1))
            .ToArray();

        Assert.ThrowsExactly<ArgumentException>(() => signer.Sign(longText, "key", key));
        Assert.ThrowsExactly<ArgumentException>(() => signer.Sign(tooManyFeatures, "key", key));
        Assert.ThrowsExactly<ArgumentException>(() => signer.Sign(largePayload, "key", key));
    }

    [TestMethod]
    public void CanonicalPayload_IsIndependentOfCultureOffsetAndFeatureOrder()
    {
        using ECDsa key = CreateKey();
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            LicenseClaims first = CreateClaims();
            first.CustomerId = "cliente-ñ-日本";
            first.Features = new[] { "zeta", "área", "zeta" };
            first.IssuedAtUtc = IssuedAt.ToOffset(TimeSpan.FromHours(-3));
            first.NotBeforeUtc = IssuedAt.ToOffset(TimeSpan.FromHours(-3));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-UY");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-UY");
            string firstPayload = Payload(new EcdsaLicenseSigner().Sign(first, "key", key));

            LicenseClaims second = CreateClaims();
            second.CustomerId = "cliente-ñ-日本";
            second.Features = new[] { "área", "zeta" };
            second.IssuedAtUtc = IssuedAt.ToOffset(TimeSpan.FromHours(9));
            second.NotBeforeUtc = IssuedAt.ToOffset(TimeSpan.FromHours(9));
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            string secondPayload = Payload(new EcdsaLicenseSigner().Sign(second, "key", key));

            Assert.AreEqual(firstPayload, secondPayload);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [TestMethod]
    public void Validation_RejectsAlterationOfEveryAuthenticatedClaim()
    {
        using ECDsa key = CreateKey();
        var signer = new EcdsaLicenseSigner();
        SignedLicense original = signer.Sign(CreateClaims(), "key", key);
        Action<LicenseClaims>[] alterations =
        {
            claims => claims.SchemaVersion = 1,
            claims => claims.LicenseId = "license-altered",
            claims => claims.ProductCode = "Altered.Product",
            claims => claims.CustomerId = "customer-altered",
            claims => claims.Edition = "Enterprise",
            claims => claims.IssuedAtUtc = claims.IssuedAtUtc.AddMinutes(1),
            claims => claims.NotBeforeUtc = claims.NotBeforeUtc.AddMinutes(1),
            claims => claims.ExpiresAtUtc = claims.IssuedAtUtc.AddDays(30),
            claims => claims.NextCheckInUtc = claims.IssuedAtUtc.AddDays(2),
            claims => claims.EntitlementStatus = LicenseEntitlementStatus.Revoked,
            claims => claims.InstallationId = "installation-altered",
            claims => claims.MaxTerminals = 5,
            claims => claims.Features = new[] { "feature-altered" }
        };

        foreach (Action<LicenseClaims> alter in alterations)
        {
            LicenseClaims changedClaims = CreateClaims();
            alter(changedClaims);
            SignedLicense changed = signer.Sign(changedClaims, "key", key);
            string[] originalParts = original.ToString().Split('.');
            originalParts[3] = Payload(changed);
            SignedLicense forged = SignedLicense.Parse(string.Join(".", originalParts));

            LicenseValidationResult result = Validate(forged, key);

            Assert.AreEqual(LicenseValidationStatus.InvalidSignature, result.Status);
        }
    }

    private static LicenseClaims CreateClaims()
    {
        return new LicenseClaims("license-01", "Example.Product", IssuedAt)
        {
            CustomerId = "customer-01",
            Edition = "Professional",
            InstallationId = "installation-01",
            Features = new[] { "feature-a", "feature-b" }
        };
    }

    private static ECDsa CreateKey()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static string Payload(SignedLicense license)
    {
        return license.ToString().Split('.')[3];
    }

    private static LicenseValidationResult Validate(SignedLicense license, ECDsa key)
    {
        return new EcdsaLicenseValidator().Validate(
            license,
            new LicenseValidationContext(
                "Example.Product",
                IssuedAt.AddHours(1),
                "installation-01"),
            new[] { new LicenseVerificationKey("key", key) });
    }

    private sealed class SingleKeyProvider : ILicenseVerificationKeyProvider
    {
        private readonly LicenseVerificationKey _key;

        public SingleKeyProvider(LicenseVerificationKey key)
        {
            _key = key;
        }

        public LicenseVerificationKey? Find(string keyId, string algorithm)
        {
            return string.Equals(_key.KeyId, keyId, StringComparison.Ordinal) &&
                string.Equals(_key.Algorithm, algorithm, StringComparison.Ordinal)
                ? _key
                : null;
        }
    }
}

using System.Security.Cryptography;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Revocation;
using MySerialKey.GiqhDev.Licensing.Revocation.Storage;
using MySerialKey.GiqhDev.Licensing.Signing;
using MySerialKey.GiqhDev.Storage;
using MySerialKeyDpapiDataProtector = MySerialKey.GiqhDev.Storage.DpapiDataProtector;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class RevocationListTests
{
    private const string ProductCode = "Example.Product";
    private const string KeyId = "revocation-2026-01";
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void SignAndValidate_RoundTripsSortedDeduplicatedIdentifiers()
    {
        using ECDsa key = CreateKey();
        RevocationListClaims claims = CreateClaims(7, IssuedAt, IssuedAt.AddDays(2));
        claims.RevokedLicenseIds = new[] { "license-c", "license-a", "license-a" };

        SignedRevocationList signed = new EcdsaRevocationListSigner().Sign(claims, KeyId, key);
        SignedRevocationList parsed = SignedRevocationList.Parse(signed.ToString());
        RevocationListValidationResult result = Validate(parsed, key, IssuedAt.AddHours(1));

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(7L, result.List!.SequenceNumber);
        CollectionAssert.AreEqual(
            new[] { "license-a", "license-c" },
            result.List.RevokedLicenseIds.ToArray());
        Assert.IsTrue(result.List.Contains("license-a"));
        Assert.IsFalse(result.List.Contains("license-b"));
    }

    [TestMethod]
    public void Validate_RejectsAlteredPayload()
    {
        using ECDsa key = CreateKey();
        SignedRevocationList signed = Sign(CreateClaims(1, IssuedAt, IssuedAt.AddDays(2)), key);
        string[] parts = signed.ToString().Split('.');
        parts[3] = Mutate(parts[3]);
        SignedRevocationList altered = SignedRevocationList.Parse(string.Join(".", parts));

        RevocationListValidationResult result = Validate(altered, key, IssuedAt.AddHours(1));

        Assert.AreEqual(RevocationListValidationStatus.InvalidSignature, result.Status);
        Assert.IsNull(result.List);
    }

    [TestMethod]
    public void Validate_EnforcesProductLifetimeAndSequenceFloor()
    {
        using ECDsa key = CreateKey();
        SignedRevocationList signed = Sign(CreateClaims(4, IssuedAt, IssuedAt.AddDays(2)), key);
        var validator = new EcdsaRevocationListValidator();
        LicenseVerificationKey[] trustedKeys = Trust(key);

        RevocationListValidationResult wrongProduct = validator.Validate(
            signed, "Another.Product", IssuedAt.AddHours(1), trustedKeys);
        RevocationListValidationResult notYetValid = validator.Validate(
            signed, ProductCode, IssuedAt.AddTicks(-1), trustedKeys);
        RevocationListValidationResult expired = validator.Validate(
            signed, ProductCode, IssuedAt.AddDays(2), trustedKeys);
        RevocationListValidationResult rollback = validator.Validate(
            signed, ProductCode, IssuedAt.AddHours(1), trustedKeys, 5);

        Assert.AreEqual(RevocationListValidationStatus.WrongProduct, wrongProduct.Status);
        Assert.AreEqual(RevocationListValidationStatus.NotYetValid, notYetValid.Status);
        Assert.AreEqual(RevocationListValidationStatus.Expired, expired.Status);
        Assert.AreEqual(RevocationListValidationStatus.RollbackDetected, rollback.Status);
    }

    [TestMethod]
    public void LicenseValidation_UsesAuthenticatedOfflineRevocationList()
    {
        using ECDsa key = CreateKey();
        var licenseClaims = new LicenseClaims("license-revoked", ProductCode, IssuedAt);
        SignedLicense license = new EcdsaLicenseSigner().Sign(licenseClaims, KeyId, key);
        RevocationListClaims listClaims = CreateClaims(1, IssuedAt, IssuedAt.AddDays(2));
        listClaims.RevokedLicenseIds = new[] { "license-revoked" };
        RevocationListValidationResult listResult = Validate(
            Sign(listClaims, key),
            key,
            IssuedAt.AddHours(1));
        var context = new LicenseValidationContext(
            ProductCode,
            IssuedAt.AddHours(1),
            revocationList: listResult.List);

        LicenseValidationResult result = new EcdsaLicenseValidator().Validate(
            license,
            context,
            Trust(key));

        Assert.AreEqual(LicenseValidationStatus.Revoked, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidationContext_RejectsListForAnotherProduct()
    {
        using ECDsa key = CreateKey();
        RevocationListClaims claims = new RevocationListClaims(
            "Another.Product", 1, IssuedAt, IssuedAt.AddDays(1));
        RevocationListValidationResult validated = new EcdsaRevocationListValidator().Validate(
            Sign(claims, key),
            "Another.Product",
            IssuedAt.AddHours(1),
            Trust(key));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new LicenseValidationContext(
                ProductCode,
                IssuedAt.AddHours(1),
                revocationList: validated.List));
    }

    [TestMethod]
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public void Importer_PersistsProtectedListAndRejectsSequenceRollback()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "MySerialKey.GiqhDev.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            using ECDsa key = CreateKey();
            var protectedDataStore = new FileProtectedDataStore(
                root,
                new MySerialKeyDpapiDataProtector());
            var store = new ProtectedRevocationListStore(protectedDataStore);
            var importer = new RevocationListImporter(
                store,
                new EcdsaRevocationListValidator());
            SignedRevocationList sequenceFive = Sign(
                CreateClaims(5, IssuedAt, IssuedAt.AddDays(1)),
                key);
            DateTimeOffset replacementIssuedAt = IssuedAt.AddDays(2);
            SignedRevocationList sequenceSix = Sign(
                CreateClaims(6, replacementIssuedAt, replacementIssuedAt.AddDays(2)),
                key);
            SignedRevocationList sequenceFour = Sign(
                CreateClaims(4, replacementIssuedAt, replacementIssuedAt.AddDays(2)),
                key);

            RevocationListImportResult first = importer.Import(
                ProductCode, sequenceFive, IssuedAt.AddHours(1), Trust(key));
            RevocationListImportResult replacement = importer.Import(
                ProductCode, sequenceSix, replacementIssuedAt.AddHours(1), Trust(key));
            RevocationListImportResult repeated = importer.Import(
                ProductCode, sequenceSix, replacementIssuedAt.AddHours(1), Trust(key));
            RevocationListImportResult rollback = importer.Import(
                ProductCode, sequenceFour, replacementIssuedAt.AddHours(1), Trust(key));

            Assert.AreEqual(RevocationListImportStatus.Imported, first.Status);
            Assert.AreEqual(RevocationListImportStatus.Imported, replacement.Status);
            Assert.AreEqual(RevocationListImportStatus.AlreadyCurrent, repeated.Status);
            Assert.AreEqual(RevocationListImportStatus.RollbackDetected, rollback.Status);
            Assert.AreEqual(
                sequenceSix.ToString(),
                store.Load(ProductCode).List!.ToString());
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
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public void Importer_ConcurrentCandidatesKeepHighestSequence()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "MySerialKey.GiqhDev.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            using ECDsa key = CreateKey();
            var store = new ProtectedRevocationListStore(
                new FileProtectedDataStore(root, new MySerialKeyDpapiDataProtector()));
            var validator = new EcdsaRevocationListValidator();
            var importer = new RevocationListImporter(store, validator);
            LicenseVerificationKey[] trustedKeys = Trust(key);
            SignedRevocationList baseline = Sign(
                CreateClaims(10, IssuedAt, IssuedAt.AddDays(3)),
                key);
            Assert.IsTrue(importer.Import(
                ProductCode,
                baseline,
                IssuedAt.AddHours(1),
                trustedKeys).IsSuccess);

            SignedRevocationList[] candidates = Enumerable.Range(11, 10)
                .Select(sequence => Sign(
                    CreateClaims(sequence, IssuedAt, IssuedAt.AddDays(3)),
                    key))
                .ToArray();
            Task[] imports = candidates
                .Select(candidate => Task.Run(() => importer.Import(
                    ProductCode,
                    candidate,
                    IssuedAt.AddHours(1),
                    trustedKeys)))
                .ToArray();

            Task.WaitAll(imports);
            RevocationListValidationResult final = validator.Validate(
                store.Load(ProductCode).List!,
                ProductCode,
                IssuedAt.AddHours(1),
                trustedKeys);

            Assert.AreEqual(20L, final.List!.SequenceNumber);
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
    public void Parse_RejectsMalformedTokenAndSignerRejectsWrongCurve()
    {
        Assert.IsFalse(SignedRevocationList.TryParse("not-a-list", out _));
        Assert.ThrowsExactly<FormatException>(
            () => SignedRevocationList.Parse("not-a-list"));

        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        Assert.ThrowsExactly<ArgumentException>(() =>
            new EcdsaRevocationListSigner().Sign(
                CreateClaims(1, IssuedAt, IssuedAt.AddDays(1)),
                KeyId,
                key));

        using ECDsa validKey = CreateKey();
        Assert.ThrowsExactly<ArgumentException>(() =>
            new EcdsaRevocationListSigner().Sign(
                CreateClaims(1, IssuedAt, IssuedAt.AddDays(1)),
                new string('k', 129),
                validKey));
    }

    private static RevocationListClaims CreateClaims(
        long sequence,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        return new RevocationListClaims(ProductCode, sequence, issuedAt, expiresAt)
        {
            RevokedLicenseIds = new[] { "license-01" }
        };
    }

    private static SignedRevocationList Sign(RevocationListClaims claims, ECDsa key)
    {
        return new EcdsaRevocationListSigner().Sign(claims, KeyId, key);
    }

    private static RevocationListValidationResult Validate(
        SignedRevocationList list,
        ECDsa key,
        DateTimeOffset nowUtc)
    {
        return new EcdsaRevocationListValidator().Validate(
            list,
            ProductCode,
            nowUtc,
            Trust(key));
    }

    private static LicenseVerificationKey[] Trust(ECDsa key)
    {
        return new[] { new LicenseVerificationKey(KeyId, key) };
    }

    private static ECDsa CreateKey()
    {
        return ECDsa.Create(ECCurve.NamedCurves.nistP256);
    }

    private static string Mutate(string value)
    {
        char replacement = value[0] == 'A' ? 'B' : 'A';
        return replacement + value.Substring(1);
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class Msl1InteroperabilityVectorTests
{
    [TestMethod]
    public void StableVector_ValidatesWithPortablePublicKey()
    {
        string vectorPath = Path.Combine(
            AppContext.BaseDirectory,
            "vectors",
            "msl1-es256-vector.txt");
        string[] lines = File.ReadAllLines(vectorPath);
        Assert.AreEqual(2, lines.Length);

        LicenseVerificationKey publicKey = LicenseVerificationKey.Parse(lines[0]);
        SignedLicense license = SignedLicense.Parse(lines[1]);
        LicenseValidationResult result = new EcdsaLicenseValidator().Validate(
            license,
            new LicenseValidationContext(
                "Example.Product",
                new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero)),
            new[] { publicKey });

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(LicenseValidationStatus.Valid, result.Status);
        Assert.AreEqual("example-customer", result.Claims!.CustomerId);
        Assert.AreEqual("Professional", result.Claims.Edition);
        CollectionAssert.AreEqual(
            new[] { "exports", "reports" },
            result.Claims.Features.ToArray());
        Assert.AreEqual(lines[1], license.ToString());
    }
}

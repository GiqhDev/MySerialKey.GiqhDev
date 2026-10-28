using System.Security.Cryptography;
using MySerialKey.GiqhDev.Generation;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;
using MySerialKey.GiqhDev.Validation;

string serial = SerialKeyGenerator.Generate();
if (!SerialKeyValidator.Validate(serial).IsValid)
{
    return 1;
}

using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
var claims = new LicenseClaims("package-consumer", "Package.Consumer", nowUtc)
{
    ExpiresAtUtc = nowUtc.AddMinutes(5)
};

SignedLicense license = new EcdsaLicenseSigner().Sign(claims, "package-test", signingKey);
string publicKeyToken = new LicenseVerificationKey("package-test", signingKey).Export();
var trustedKeys = new[] { LicenseVerificationKey.Parse(publicKeyToken) };
LicenseValidationResult result = new EcdsaLicenseValidator().Validate(
    license,
    new LicenseValidationContext("Package.Consumer", nowUtc),
    trustedKeys);

Console.WriteLine($"Package consumer validation: {result.Status}");
return result.IsValid ? 0 : 1;

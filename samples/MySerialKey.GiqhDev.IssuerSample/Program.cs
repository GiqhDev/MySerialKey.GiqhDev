using System.Security.Cryptography;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

const string ProductCode = "Example.Product";
const string KeyId = "example-2026";

string outputDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath("license-output");
Directory.CreateDirectory(outputDirectory);

using ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
var claims = new LicenseClaims(Guid.NewGuid().ToString("N"), ProductCode, nowUtc)
{
    CustomerId = "example-customer",
    Edition = "Professional",
    ExpiresAtUtc = nowUtc.AddYears(1),
    MaxTerminals = 3,
    Features = new[] { "reports", "exports" }
};

ILicenseSigner signer = new EcdsaLicenseSigner();
SignedLicense license = signer.Sign(claims, KeyId, privateKey);

string licensePath = Path.Combine(outputDirectory, "license.msl");
string publicKeyPath = Path.Combine(outputDirectory, "public-key.mspk");
File.WriteAllText(licensePath, license.ToString());
File.WriteAllText(publicKeyPath, new LicenseVerificationKey(KeyId, privateKey).Export());

Console.WriteLine($"License written to: {licensePath}");
Console.WriteLine($"Public verification key written to: {publicKeyPath}");
Console.WriteLine("The private signing key was not persisted.");

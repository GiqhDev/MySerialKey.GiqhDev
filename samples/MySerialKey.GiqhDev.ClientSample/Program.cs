using System.Security.Cryptography;
using MySerialKey.GiqhDev.Licensing;

const string ProductCode = "Example.Product";
const string KeyId = "example-2026";

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ClientSample <public-key.spki> <license.msl>");
    return 2;
}

try
{
    byte[] publicKeyBytes = Convert.FromBase64String(File.ReadAllText(args[0]).Trim());
    using ECDsa publicKey = ECDsa.Create();
    publicKey.ImportSubjectPublicKeyInfo(publicKeyBytes, out int bytesRead);
    if (bytesRead != publicKeyBytes.Length)
    {
        Console.Error.WriteLine("The public key file contains trailing data.");
        return 2;
    }

    SignedLicense license = SignedLicense.Parse(File.ReadAllText(args[1]).Trim());
    var trustedKeys = new[] { new LicenseVerificationKey(KeyId, publicKey) };
    var context = new LicenseValidationContext(ProductCode, DateTimeOffset.UtcNow);
    ILicenseValidator validator = new EcdsaLicenseValidator();
    LicenseValidationResult result = validator.Validate(license, context, trustedKeys);

    Console.WriteLine($"License status: {result.Status}");
    return result.IsValid ? 0 : 1;
}
catch (Exception exception) when (
    exception is IOException ||
    exception is UnauthorizedAccessException ||
    exception is FormatException ||
    exception is CryptographicException ||
    exception is ArgumentException)
{
    Console.Error.WriteLine($"The license inputs could not be read: {exception.Message}");
    return 2;
}

using MySerialKey.GiqhDev.Licensing;

const string ProductCode = "Example.Product";
const string KeyId = "example-2026";

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ClientSample <public-key.mspk> <license.msl>");
    return 2;
}

try
{
    LicenseVerificationKey publicKey = LicenseVerificationKey.Parse(File.ReadAllText(args[0]).Trim());
    SignedLicense license = SignedLicense.Parse(File.ReadAllText(args[1]).Trim());
    if (!string.Equals(publicKey.KeyId, KeyId, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("The public key identifier is not the expected example key.");
        return 2;
    }

    var trustedKeys = new[] { publicKey };
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
    exception is ArgumentException)
{
    Console.Error.WriteLine($"The license inputs could not be read: {exception.Message}");
    return 2;
}

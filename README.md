# MySerialKey.GiqhDev

`MySerialKey.GiqhDev` generates short serial keys and issues asymmetric signed licenses for .NET applications.

Default serial format:

```text
XXXXX-XXXXXX-XXXXX-XXXXXX
```

The package uses cryptographically secure randomness and has no third-party runtime dependencies.

## Status

Version `1.0.2` is published on NuGet.org. Version `2.0.0-alpha.1` is under development on the `dev` branch.

## Installation

```bash
dotnet add package MySerialKey.GiqhDev
```

## Quick Start

```csharp
using MySerialKey.GiqhDev.Formatting;
using MySerialKey.GiqhDev.Generation;
using MySerialKey.GiqhDev.Validation;

string serial = SerialKeyGenerator.Generate();
SerialValidationResult result = SerialKeyValidator.Validate(serial);

Console.WriteLine(serial);
Console.WriteLine(result.IsValid);
```

## Generation

```csharp
string serial = SerialKeyGenerator.Generate();
```

The default alphabet excludes visually ambiguous characters:

```text
ABCDEFGHJKMNPQRSTUVWXYZ23456789
```

## Validation

```csharp
SerialValidationResult result = SerialKeyValidator.ValidateFormat(serial);

if (!result.IsValid)
{
    Console.WriteLine(result.Error);
    Console.WriteLine(result.Message);
}
```

`Validate` is an alias for structural format validation. It does not prove authenticity.

## Normalization

```csharp
string normalized = SerialKeyNormalizer.Normalize("a7k9p x2m8q4 b6n3r z9t5w8");
```

Output:

```text
A7K9P-X2M8Q4-B6N3R-Z9T5W8
```

## Custom Configuration

```csharp
var options = new SerialKeyGenerationOptions
{
    Segments = new[] { 4, 4, 4, 4 },
    Separator = '-',
    Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
    UseUppercase = true
};

string serial = SerialKeyGenerator.Generate(options);
```

## Authenticated Serials

Authenticated serials reserve part of the 22 alphanumeric characters for a compact authentication tag:

```csharp
byte[] secretKey = Convert.FromBase64String(configuration.Secret);

string serial = SerialKeyGenerator.GenerateAuthenticated(secretKey);
SerialValidationResult result = SerialKeyValidator.ValidateAuthenticated(serial, secretKey);
```

This protects against basic tampering and random strings that merely match the visual format.

## Signed Licenses in 2.0

Signed licenses use ECDSA P-256 with SHA-256. The administrative tool or activation service signs with a private key. Client applications receive only public verification keys.

Issuer-side example:

```csharp
using System.Security.Cryptography;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

using ECDsa privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

var claims = new LicenseClaims(
    "license-01",
    "Example.Product",
    DateTimeOffset.UtcNow)
{
    CustomerId = "customer-01",
    Edition = "Professional",
    InstallationId = "installation-01",
    MaxTerminals = 1,
    Features = new[] { "feature-a", "feature-b", "feature-c" }
};

var signer = new EcdsaLicenseSigner();
SignedLicense license = signer.Sign(claims, "production-2026-01", privateKey);
string portableToken = license.ToString();
```

Client-side validation:

```csharp
SignedLicense license = SignedLicense.Parse(portableToken);
var trustedKeys = new[]
{
    new LicenseVerificationKey("production-2026-01", publicKey)
};
var context = new LicenseValidationContext(
    "Example.Product",
    DateTimeOffset.UtcNow,
    installationId);
var validator = new EcdsaLicenseValidator();

LicenseValidationResult result =
    validator.Validate(license, context, trustedKeys);
```

`keyId` and the algorithm are covered by the signature. Multiple public keys can be trusted simultaneously during rotation. Signing must remain in trusted server or administrative code; never deploy the private key with a client application.

## Security Notes

Authenticated serials use HMAC-SHA256 with a compact truncated tag. They remain available for legacy compatibility and are not full offline licenses.

Do not embed high-value secrets in client applications when an attacker can inspect the machine or executable. For stronger scenarios, prefer server-side activation or asymmetric license signatures where clients only receive a public key.

This package does not protect completely against reverse engineering, executable patching, secret extraction, validation bypasses, or attackers with full control of the client machine.

## Compatibility

The package targets:

- .NET Framework 4.8
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10

## Publishing

Publishing to NuGet.org is handled by GitHub Actions trusted publishing.

Create a trusted publishing policy on NuGet.org with these values:

```text
Package Owner: GiqhDev
Repository Owner: GiqhDev
Repository: MySerialKey.GiqhDev
Workflow File: publish-nuget.yml
Environment: leave empty
```

The workflow runs on pushes to `master` and can also be started manually from the GitHub Actions tab.

## Roadmap

- v2.0 alpha: signed-license core and key rotation.
- v2.0 next: protected installation identity and trial state.
- v2.0 next: offline activation requests and activation-service contracts.

## License

MIT.

## Author

Gustavo Quintana Hidalgo.

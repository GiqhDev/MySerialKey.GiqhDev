# MySerialKey.GiqhDev

`MySerialKey.GiqhDev` generates, normalizes, and validates short serial keys for .NET applications.

Default serial format:

```text
XXXXX-XXXXXX-XXXXX-XXXXXX
```

The package uses cryptographically secure randomness and has no third-party runtime dependencies.

## Status

Version `1.0.0` is prepared as a local NuGet package candidate.

## Installation

```bash
dotnet add package MySerialKey.GiqhDev
```

## Quick Start

```csharp
using MySerialKey.GiqhDev;

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

## Security Notes

Authenticated serials use HMAC-SHA256 with a compact truncated tag. This is a lightweight offline authenticity check, not DRM.

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

Publishing to NuGet.org is handled by GitHub Actions.

Create this repository secret before merging to `master`:

```text
NUGET_API_KEY
```

The workflow runs on pushes to `master` and can also be started manually from the GitHub Actions tab.

## Roadmap

- v1.1: additional pattern presets and optional checksum.
- v1.2: product metadata and product-specific serial namespaces.
- v2.0: evaluate asymmetric signed licenses.

## License

MIT.

## Author

Gustavo Quintana Hidalgo.

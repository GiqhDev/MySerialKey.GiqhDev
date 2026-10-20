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

## Protected Installation Storage in 2.0

The storage contracts are product-neutral and keep installation identity, protected persistence, and signed-license persistence separate. The included Windows implementation uses DPAPI; consumers on other platforms can provide their own `IDataProtector` and `IProtectedDataStore` implementations.

```csharp
using MySerialKey.GiqhDev.Installation;
using MySerialKey.GiqhDev.Licensing.Storage;
using MySerialKey.GiqhDev.Storage;

string storageRoot = WindowsStoragePaths.GetCurrentUserRoot();
var protectedStore = new FileProtectedDataStore(
    storageRoot,
    new DpapiDataProtector());

IInstallationIdProvider installations =
    new PersistentInstallationIdProvider(protectedStore);
string installationId = installations.GetOrCreate("Example.Product");

ILicenseStore licenses = new ProtectedLicenseStore(protectedStore);
licenses.Save("Example.Product", license);
LicenseStoreReadResult storedLicense = licenses.Load("Example.Product");
```

Use `WindowsStoragePaths.GetMachineRoot()` together with `new DpapiDataProtector(useMachineScope: true)` for machine-wide data. The installer or host application remains responsible for assigning suitable directory permissions. Stored entries are scoped by `productCode`, written atomically, protected with purpose-specific entropy, and return typed states for missing, corrupted, or unavailable data.

An elevated installer can replace inherited permissions with an explicit ACL. Pass the Windows identities that must run the application:

```csharp
using System.Security.Principal;

SecurityIdentifier applicationIdentity = /* resolve the account or group SID */;
WindowsStoragePermissions.ApplyRestrictedMachineAcl(
    WindowsStoragePaths.GetMachineRoot(),
    applicationIdentity);
```

The built-in installation provider persists only a random GUID scoped by `productCode`. It does not read MAC addresses, hardware serials, user names, or other personal data. Deleting the protected entry creates a new installation identity. Hardware fingerprinting is intentionally outside the default implementation; applications that require it must provide a custom `IInstallationIdProvider` and document their own collection and privacy policy.

## Configurable Trial in 2.0

Trial creation is explicit. The consuming application decides which business event calls `Start`; reading state never starts or resets the trial.

```csharp
using MySerialKey.GiqhDev.Trial;

var trial = new ProtectedTrialService(
    protectedStore,
    new TrialOptions(TimeSpan.FromDays(14)));

// Call only after the application-specific event that begins the trial.
TrialValidationResult started = trial.Start("Example.Product");

// Normal application launch: validates time and records a successful run.
TrialValidationResult current = trial.Validate("Example.Product");

if (current.IsActive)
{
    Console.WriteLine(current.Remaining);
}
```

`Get` is a read-only query. `Validate` detects clock rollback beyond the configured tolerance and can accept a trusted UTC time obtained by the host application. `MarkConvertedToLicense` preserves the original start, expiration, last successful run, and trusted-time history.

The protected state stores only UTC timestamps and a conversion marker. If the state is missing, the result is `NotStarted`; if it is malformed, the result is `Corrupted`. Neither condition starts a new trial automatically.

## Offline Activation in 2.0

Offline activation exchanges two portable, versioned tokens without placing an issuing secret in the client:

1. The client creates an `MSAR1` request containing a unique request ID, a cryptographic nonce, the product, the installation, and an expiration time.
2. A trusted administrative application imports that request and issues a signed `MSAP1` response with its private key.
3. The client verifies the response, imports the embedded standard `MSL1` license, and records the consumed request in protected storage.

Client-side request creation:

```csharp
using MySerialKey.GiqhDev.Activation.Offline;

ActivationRequest request = ActivationRequest.Create(
    "Example.Product",
    installationId,
    DateTimeOffset.UtcNow,
    validity: TimeSpan.FromDays(2),
    applicationVersion: "2.0.0");

request.Save("activation-request.msar");
```

The `.msar` file can be transferred to an administrative computer. The request contains no secret and is not itself trusted; its identity, nonce, product, and installation become authenticated when the response is signed.

Trusted administrative-side issuance:

```csharp
ActivationRequest request = ActivationRequest.Load("activation-request.msar");

var claims = new LicenseClaims(
    "license-01",
    request.ProductCode,
    DateTimeOffset.UtcNow)
{
    InstallationId = request.InstallationId,
    Edition = "Professional",
    Features = new[] { "feature-a", "feature-b" }
};

var issuer = new OfflineActivationIssuer();
ActivationResponse response = issuer.Issue(
    request,
    claims,
    "production-2026-01",
    privateKey,
    DateTimeOffset.UtcNow,
    responseValidity: TimeSpan.FromDays(1));

response.Save("activation-response.msap");
```

Keep `privateKey` only in trusted administrative or server-side code. The issuer rejects claims for a different product or installation and never changes the `MSL1` license format.

Client-side response import:

```csharp
ActivationRequest originalRequest = ActivationRequest.Load("activation-request.msar");
ActivationResponse response = ActivationResponse.Load("activation-response.msap");

var activation = new OfflineActivationClient(
    "Example.Product",
    installationId,
    licenses,
    protectedStore);

OfflineActivationImportResult import = activation.Import(
    originalRequest,
    response,
    DateTimeOffset.UtcNow,
    trustedKeys);

if (!import.IsSuccess)
{
    Console.WriteLine(import.Status);
}
```

Import verifies the `MSAP1` signature before trusting its payload, requires the original request and local product/installation, checks both deadlines, validates the embedded `MSL1` license normally, and stores a protected consumption receipt. Reusing the same request returns `ReplayDetected`. Keep the original `.msar` file until its response has been imported; a response cannot be applied against a newly generated request.

## Online Activation Contracts in 2.0

The online API is transport-independent through `IOnlineActivationClient`. Versioned request models are available for activation, renewal, and deactivation. They carry a stable `OperationId` so an HTTP implementation can retry the same logical operation idempotently.

```csharp
using MySerialKey.GiqhDev.Activation.Online;

var request = new OnlineActivationRequest(
    serial,
    "Example.Product",
    applicationVersion,
    installationId);

ActivationClientResult result =
    await activationClient.ActivateAsync(request, cancellationToken);
```

`ActivationClientResult` distinguishes network, timeout, authorization, rate-limit, service, response, license, and local-storage failures. `ActivationServiceError` provides versioned machine-readable server errors. The transport implementation remains under development; consumers can implement the interface without depending on a specific service vendor.

## Security Notes

Authenticated serials use HMAC-SHA256 with a compact truncated tag. They remain available for legacy compatibility and are not full offline licenses.

Do not embed high-value secrets in client applications when an attacker can inspect the machine or executable. For stronger scenarios, prefer server-side activation or asymmetric license signatures where clients only receive a public key.

This package does not protect completely against reverse engineering, executable patching, secret extraction, validation bypasses, or attackers with full control of the client machine.

A local administrator can delete protected trial state, replace application binaries, or bypass client-side validation. MySerialKey detects recoverable corruption and clock rollback, but cannot absolutely prevent trial reset on a machine controlled by an attacker. High-value enforcement requires a trusted activation service and server-maintained state.

DPAPI machine scope protects data for the computer but does not decide which local users may modify the file. Use a restrictive directory ACL for shared storage and grant access only to the identities that run the application.

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
- v2.0 alpha: protected installation identity and signed-license persistence.
- v2.0 alpha: configurable protected trial state.
- v2.0 alpha: signed offline activation requests and responses.
- v2.0 next: online activation-service contracts.

## License

MIT.

## Author

Gustavo Quintana Hidalgo.

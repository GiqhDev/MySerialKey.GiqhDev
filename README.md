# MySerialKey.GiqhDev

`MySerialKey.GiqhDev` generates short serial keys and issues asymmetric signed licenses for .NET applications.

Default serial format:

```text
XXXXX-XXXXXX-XXXXX-XXXXXX
```

The package uses cryptographically secure randomness and has no third-party runtime dependencies.

## Status

Version `1.0.2` is published on NuGet.org. Version `2.0.0-beta.1` is being prepared on the `dev` branch for external validation.

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

Public keys can be transported without platform-specific SPKI APIs by using the versioned `MSPK1` format:

```csharp
string publicKeyToken =
    new LicenseVerificationKey("production-2026-01", publicKey).Export();

LicenseVerificationKey importedKey =
    LicenseVerificationKey.Parse(publicKeyToken);
```

`MSPK1` contains only the key identifier, the explicit `ES256` algorithm, and public P-256 coordinates. It never contains private key material.

The public `LicenseFormatLimits` contract defines the enforced MSL1 limits: 131,072 token characters, 97,280 authenticated payload bytes, 16,384 UTF-8 bytes per text claim, 256 supplied features, 128 characters per `keyId`, and a 64-byte P1363 signature.

Payload schema 2 keeps the same `MSL1` envelope and adds optional periodic refresh and signed entitlement state:

```csharp
claims.NextCheckInUtc = DateTimeOffset.UtcNow.AddDays(7);
claims.EntitlementStatus = LicenseEntitlementStatus.Active;

var policy = new LicenseValidationPolicy(
    gracePeriod: TimeSpan.FromDays(2),
    isOnlineRefreshUnavailable: refreshAttemptFailed);
var context = new LicenseValidationContext(
    "Example.Product",
    DateTimeOffset.UtcNow,
    installationId,
    policy);
```

After `NextCheckInUtc`, validation returns `RefreshRequired`. `GracePeriod` is returned only when the consumer confirms that an online refresh was attempted but the service is unavailable, and only inside the configured grace duration. `GracePeriod` remains usable through `IsValid`; `RefreshRequired` and `Revoked` do not. An authentic revoked response from the online client is stored locally and returned as `ActivationClientStatus.Revoked`, preventing a previously active token from remaining effective. Existing signed payload schema 1 licenses remain supported.

## Signed Offline Revocation Lists in 2.0

An issuer can publish a short-lived `MSRL1` artifact containing revoked license identifiers. The list is signed with ES256, scoped to one product, and carries a monotonically increasing sequence number to prevent an older valid list from replacing a newer one.

Issuer-side example:

```csharp
using MySerialKey.GiqhDev.Licensing.Revocation;

DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
var claims = new RevocationListClaims(
    "Example.Product",
    sequenceNumber: 42,
    issuedAtUtc: issuedAt,
    expiresAtUtc: issuedAt.AddDays(7))
{
    RevokedLicenseIds = new[] { "license-01", "license-17" }
};

SignedRevocationList signedList =
    new EcdsaRevocationListSigner().Sign(claims, "production-2026-01", privateKey);
```

Client-side import and use:

```csharp
using MySerialKey.GiqhDev.Licensing.Revocation;
using MySerialKey.GiqhDev.Licensing.Revocation.Storage;

var revocationStore = new ProtectedRevocationListStore(protectedStore);
var listValidator = new EcdsaRevocationListValidator();
var importer = new RevocationListImporter(revocationStore, listValidator);

RevocationListImportResult imported = importer.Import(
    "Example.Product",
    SignedRevocationList.Parse(downloadedToken),
    DateTimeOffset.UtcNow,
    trustedKeys);

RevocationListStoreReadResult stored = revocationStore.Load("Example.Product");
RevocationListValidationResult validated = listValidator.Validate(
    stored.List!,
    "Example.Product",
    DateTimeOffset.UtcNow,
    trustedKeys);

var context = new LicenseValidationContext(
    "Example.Product",
    DateTimeOffset.UtcNow,
    installationId,
    revocationList: validated.List);
```

Only a successfully validated `ValidatedRevocationList` can enter the license-validation context. The importer authenticates the existing protected list even when it has expired and requires a strictly newer sequence before replacing it. Re-importing the exact current token is idempotent. Clients contain only public verification keys; private signing keys remain outside the application.

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

The online API is transport-independent through `IOnlineActivationClient`. Versioned request models are available for activation, query, renewal, transfer, and deactivation. They carry a stable `OperationId` so an HTTP implementation can retry the same logical operation idempotently.

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

`ActivationClientResult` distinguishes network, timeout, authorization, rate-limit, service, response, license, and local-storage failures. `ActivationServiceError` provides versioned machine-readable server errors.

The built-in HTTP implementation receives a caller-owned `HttpClient`, so the application controls its base address, authentication handler, proxy, TLS policy, and lifetime:

```csharp
var http = new HttpClient(authenticationHandler)
{
    BaseAddress = new Uri("https://licenses.example.com/")
};

var online = new HttpOnlineActivationClient(
    http,
    licenses,
    trustedKeys,
    new HttpOnlineActivationClientOptions
    {
        MaxRetries = 2,
        RetryDelay = TimeSpan.FromMilliseconds(250)
    });

ActivationClientResult result =
    await online.ActivateAsync(request, cancellationToken);
```

The default relative endpoints are:

- `POST api/v1/activations`
- `POST api/v1/activations/query`
- `POST api/v1/activations/renewals`
- `POST api/v1/activations/transfers`
- `POST api/v1/activations/deactivations`

All endpoints can be replaced through `HttpOnlineActivationClientOptions`. Every attempt sends the same `OperationId` in the body and `Idempotency-Key` header. Retries are limited to timeouts, transport errors, HTTP 408, HTTP 429, and HTTP 5xx responses. Successful activation, query, renewal, and transfer responses must contain a matching operation ID, schema version 1, trusted-time field, and a valid `MSL1` token for the requested product and installation. Transfer validates that the returned license is bound to the target installation before replacing local state. The returned token can then be delivered to that installation through a secure channel chosen by the consumer. Network or service failures never delete an existing local license.

The complete server-side contract, including signed trusted time, administrative authentication, rate limiting, and audit requirements, is documented in [Activation Service Contract v1](docs/ACTIVATION_SERVICE_V1.md).

## Security Notes

Authenticated serials use HMAC-SHA256 with a compact truncated tag. They remain available for legacy compatibility and are not full offline licenses.

Do not embed high-value secrets in client applications when an attacker can inspect the machine or executable. For stronger scenarios, prefer server-side activation or asymmetric license signatures where clients only receive a public key.

This package does not protect completely against reverse engineering, executable patching, secret extraction, validation bypasses, or attackers with full control of the client machine.

A local administrator can delete protected trial state, replace application binaries, or bypass client-side validation. MySerialKey detects recoverable corruption and clock rollback, but cannot absolutely prevent trial reset on a machine controlled by an attacker. High-value enforcement requires a trusted activation service and server-maintained state.

DPAPI machine scope protects data for the computer but does not decide which local users may modify the file. Use a restrictive directory ACL for shared storage and grant access only to the identities that run the application.

MySerialKey does not log tokens, serials, secrets, claims, HTTP bodies, or protected values. Consuming applications should preserve that boundary and log only minimal operation identifiers, typed statuses, stable error codes, schema versions, and non-secret `keyId` values. The console sample generates its legacy HMAC secret at runtime, clears it after use, and never prints generated serial values.

Repository builds enable current .NET analyzers and code-style checks. NuGet advisories `NU1901` through `NU1904` are treated as errors, and the security workflow runs restore/audit, Release build, and tests on pushes, pull requests, weekly schedule, and manual execution.

The repository includes a stable public `MSL1`/`MSPK1` interoperability vector. The .NET test suite validates it, while the security workflow independently verifies the ES256 signature with Node/OpenSSL. The vector contains no private key.

## Complete Issuer and Client Examples

The repository keeps issuance and validation in separate applications so the private key boundary is explicit:

```bash
dotnet run --project samples/MySerialKey.GiqhDev.IssuerSample -- artifacts/example-license
dotnet run --project samples/MySerialKey.GiqhDev.ClientSample -- artifacts/example-license/public-key.mspk artifacts/example-license/license.msl
```

The issuer creates a temporary P-256 key for demonstration, writes only its public key and the signed license, and does not persist the private key. A production issuer must load its private key from a protected administrative environment, HSM, or managed key service. The client imports only the public key and reports the typed validation status.

## Compatibility

The package targets:

- .NET Framework 4.8
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10

Version 2.0 keeps these direct targets to preserve the compatibility already exercised by the alpha. The .NET 6, 7, and 9 targets are compatibility targets; applications should still run on a Microsoft-supported runtime. `netstandard2.0` is intentionally not advertised because it would hide platform and cryptographic requirements behind a broader target than the implementation actually guarantees.

The 2.0 release remains a single NuGet package. Issuance, validation, storage, and platform concerns are separated by namespaces and interfaces, while a physical package split is deferred until it provides a concrete dependency or deployment boundary.

Applications migrating from 1.x can update the package without replacing their serial or HMAC flows. Signed licenses are opt-in: add an immutable product code, provision one or more public verification keys, choose protected storage, and map typed validation statuses to application policy. Existing HMAC serials are not automatically converted into signed licenses, and the legacy APIs are not obsolete in 2.0.

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
- v2.0 alpha: online activation-service contracts, refresh, transfer, and deactivation.
- v2.0 alpha: signed offline revocation lists with protected rollback prevention.
- v2.0 alpha: threat-model documentation, dependency auditing, analyzers, and parser fuzzing.
- v2.0 alpha: public API baseline, separate issuer/client samples, package-consumer validation, and documented compatibility policy.
- v2.0 next: close remaining acceptance criteria and prepare the beta.

## License

MIT.

## Author

Gustavo Quintana Hidalgo.

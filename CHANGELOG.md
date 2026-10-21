# Changelog

## 2.0.0-alpha.1

- Added versioned `MSL1` signed-license tokens.
- Added ECDSA P-256/SHA-256 issuance and public-key validation.
- Added typed validation for product, installation, validity period, schema, and signing key.
- Added simultaneous trusted-key support for signing-key rotation.
- Added deterministic binary claim encoding and canonical feature ordering.
- Added a random, stable, product-scoped installation identifier provider.
- Added protected storage and signed-license store abstractions for custom platforms.
- Added Windows DPAPI protection with current-user and machine scopes.
- Added atomic file persistence with typed missing, corruption, and availability states.
- Added conventional `%LocalAppData%` and `%ProgramData%` storage roots for Windows.
- Added explicit Windows ACL setup for machine-wide storage directories.
- Documented the random installation identifier's data and privacy boundaries.
- Added cross-process race coverage for stable installation identity creation.
- Added explicit, configurable trial start with a seven-day suggested duration.
- Added read-only trial queries, typed validation states, and remaining time.
- Added an injectable clock, rollback tolerance, and optional trusted UTC time.
- Added versioned protected trial persistence with corruption and deletion handling.
- Added idempotent trial-to-license conversion while preserving trial history.
- Added portable `MSAR1` offline activation requests with unique identifiers, cryptographic nonces, product and installation scope, and expiration.
- Added signed `MSAP1` activation responses carrying the same standard `MSL1` license used by other activation channels.
- Added administrative offline issuance without distributing private signing keys to client applications.
- Added token and UTF-8 file import/export for offline requests and responses.
- Added typed client-side validation for response signatures, trusted keys, request linkage, local scope, and validity windows.
- Added protected consumption receipts and replay detection for imported offline requests.
- Added versioned, product-neutral online activation, renewal, and deactivation contracts.
- Added stable operation identifiers for idempotent service requests.
- Added a transport-independent online activation client abstraction and typed client/service results.
- Added an injectable `HttpClient` implementation for activation, renewal, and deactivation.
- Added bounded versioned JSON responses, configurable endpoints, and typed HTTP/service failure mapping.
- Added idempotent transient retries using a stable operation body and `Idempotency-Key` header.
- Added mandatory `MSL1` signature, product, installation, and validity checks before online license persistence.
- Added a current-license query contract and configurable HTTP endpoint.
- Documented activation-service v1 authentication, rate limiting, administrative audit, and nonce-bound signed trusted time.
- Added backward-compatible `MSL1` payload schema 2 with `NextCheckInUtc` and signed entitlement state.
- Added typed `RefreshRequired`, `GracePeriod`, and `Revoked` validation outcomes.
- Persist authentic signed revocation responses so a stale active license cannot remain locally effective.
- Added a consumer-controlled grace policy that activates only after an unavailable online refresh.
- Added an idempotent online transfer contract that validates the returned target installation before replacing local state.
- Fixed atomic temporary paths for concurrent storage on .NET Framework `MAX_PATH` environments.
- Preserved the existing serial and HMAC APIs for legacy compatibility.
- Made the default serial segment collection immutable.

## 1.0.2

- Disabled symbols package publishing to keep NuGet.org release uploads clean.

## 1.0.1

- Added package icon for NuGet.org.

## 1.0.0

- Initial package structure for `MySerialKey.GiqhDev`.
- Added cryptographically secure serial generation.
- Added format validation and normalization.
- Added compact HMAC-SHA256 authenticated serials.
- Added NuGet package metadata, README, sample, and tests.

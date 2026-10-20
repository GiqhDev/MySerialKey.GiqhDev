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

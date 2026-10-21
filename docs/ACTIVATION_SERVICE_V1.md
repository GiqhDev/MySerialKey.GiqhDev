# Activation Service Contract v1

This document defines a product-neutral HTTP contract for services consumed by `MySerialKey.GiqhDev`. It is a protocol specification, not a required server implementation.

## Transport and common rules

- Production endpoints require HTTPS.
- Request and response media type is `application/json; charset=utf-8`.
- Every mutable or state-reading licensing operation carries a GUID `operationId` in `N` format and the same value in the `Idempotency-Key` header.
- Repeating an operation with the same identifier and body returns the original semantic result. Reusing the identifier with a different body returns HTTP `409` and `idempotency_conflict`.
- Servers retain idempotency records for at least 24 hours. Deployments with longer client retry windows must increase that duration.
- Request and response schema version 1 is represented by `"schemaVersion": 1`.
- Unknown schema versions return HTTP `400` and `unsupported_schema`.
- Responses should set `Cache-Control: no-store` except where an explicitly signed artifact defines its own lifetime.
- Raw serials, license tokens, private keys, access tokens, and full request bodies must not be logged.

## Client endpoints

### Activate

`POST /api/v1/activations`

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "serial": "consumer-supplied-serial",
  "productCode": "Example.Product",
  "applicationVersion": "2.0.0",
  "installationId": "stable-installation-id"
}
```

The service validates the serial and entitlement, enforces the allowed installation count, records the activation idempotently, and returns a signed `MSL1` license.

### Query current license

`POST /api/v1/activations/query`

### Renew

`POST /api/v1/activations/renewals`

### Transfer

`POST /api/v1/activations/transfers`

### Deactivate

`POST /api/v1/activations/deactivations`

Query, renewal, and deactivation use this body:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "licenseToken": "MSL1.ES256...",
  "productCode": "Example.Product",
  "applicationVersion": "2.0.0",
  "installationId": "stable-installation-id"
}
```

The service validates `MSL1` before using its claims and requires its product and installation to match the request. Query and renewal return the current signed license. Deactivation returns success only after the server-side installation state has been updated idempotently.

Transfer uses a distinct body so the source and destination are explicit:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "licenseToken": "MSL1.ES256...",
  "productCode": "Example.Product",
  "applicationVersion": "2.0.0",
  "sourceInstallationId": "current-stable-installation-id",
  "targetInstallationId": "new-stable-installation-id"
}
```

The service verifies that the supplied license belongs to the source, atomically deactivates the source activation, activates the target within the entitlement limits, and returns an `MSL1` bound to the target. Repeating the same operation ID cannot consume another activation. If any step fails, the source remains active. The client validates the returned target binding before replacing its local token; the returned token can then be moved to the target application through a consumer-defined secure channel.

### License response

HTTP `200`:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "licenseToken": "MSL1.ES256...",
  "trustedTimeUtc": "2026-10-21T12:00:00.0000000+00:00"
}
```

The client must not persist `licenseToken` until its signature, signing key, product, installation, `NotBeforeUtc`, and `ExpiresAtUtc` have been validated locally. `trustedTimeUtc` is informational in this response and must not override local license validity by itself.

### Deactivation response

HTTP `200`:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "trustedTimeUtc": "2026-10-21T12:00:00.0000000+00:00"
}
```

## Trusted time

`POST /api/v1/time`

A plain timestamp over HTTP is not sufficient for rollback protection. The client sends a fresh 32-byte cryptographic nonce:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "nonce": "base64url-encoded-32-byte-value"
}
```

The service returns a short-lived token using this envelope:

```text
MST1.ES256.<base64url-key-id>.<base64url-payload>.<base64url-signature>
```

The version-one payload authenticates:

- schema version;
- operation ID;
- SHA-256 hash of the request nonce;
- `issuedAtUtc`;
- `expiresAtUtc`.

The client accepts the time only after verifying ES256 with a trusted public key, matching the operation and nonce, and confirming that the token is within its short validity window. Responses are single-operation artifacts and must not be accepted for another nonce. A recommended maximum lifetime is five minutes.

The `MST1` parser and client operation are intentionally separate from the current HTTP client until stable test vectors are added.

## Error response

Non-success responses use a stable machine-readable body:

```json
{
  "schemaVersion": 1,
  "operationId": "32-character-guid-n-format",
  "errorCode": "rate_limited",
  "message": "Try again later.",
  "retryable": true,
  "retryAfterSeconds": 30
}
```

Recommended HTTP mappings:

- `400`: malformed request or unsupported schema;
- `401`/`403`: missing or insufficient authentication;
- `404`: unknown entitlement without disclosing sensitive details;
- `409`: installation limit, state conflict, or idempotency conflict;
- `429`: rate limited, with `Retry-After` when possible;
- `5xx`: transient or internal service failure.

Error messages are diagnostic and must not reveal whether a guessed serial exists, customer data, key material, or internal exception details.

## Authentication and authorization

Client applications must never contain administrative credentials or private signing keys. `HttpClient` injection allows each deployment to add its own client authentication handler without changing the protocol models.

Administrative operations use a separate route group such as `/api/v1/admin/*` and require a short-lived identity-provider token. Recommended controls are:

- OAuth 2.0/OIDC access tokens with audience restricted to the licensing service;
- distinct scopes such as `licenses.issue`, `licenses.revoke`, `licenses.transfer`, and `keys.rotate`;
- optional mutual TLS for automation or high-value deployments;
- multi-factor authentication for interactive administrators;
- no permanent administrative API key embedded in client software;
- signing keys held in an HSM or managed key service and never returned by an API.

## Rate limiting and brute-force protection

Rate limits are enforced before expensive signing work and combined across several dimensions:

- source network or authenticated client identity;
- product code;
- keyed hash of the normalized serial, never the raw serial in telemetry;
- installation identifier hash;
- administrative actor and scope.

Activation failures use generic outward responses, progressive delays, and bounded attempt windows. HTTP `429` communicates a safe retry delay. A successful request does not erase abuse history immediately. Limits and thresholds are deployment policy and are not hard-coded into the reusable client library.

## Administrative audit

Every administrative mutation and every successful activation-state transition records:

- UTC timestamp and correlation ID;
- operation ID and action;
- authenticated actor or service principal;
- product code;
- hashed license, serial, customer, and installation references where needed;
- result and stable error code;
- signing `keyId`, never private key material;
- previous and new state identifiers for revocation or transfer.

Audit records are append-only, access-controlled, retained by policy, and excluded from normal application logs. Secrets, complete tokens, raw serials, and unnecessary personal data are prohibited.

## Security boundary

The service owns entitlement state, activation limits, revocation state, idempotency records, administrative authorization, and private signing keys. The client owns local validation and protected persistence. Neither side treats an unsigned claim or caller-supplied timestamp as authoritative.

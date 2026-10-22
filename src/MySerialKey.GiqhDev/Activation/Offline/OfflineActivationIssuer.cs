using System.Security.Cryptography;
using MySerialKey.GiqhDev.Activation.Offline.Internal;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Signing;

namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>
/// Issues signed offline activation responses in trusted administrative code.
/// </summary>
public sealed class OfflineActivationIssuer
{
    /// <summary>Issues an MSAP1 response containing a standard MSL1 license.</summary>
    public ActivationResponse Issue(
        ActivationRequest request,
        LicenseClaims licenseClaims,
        string keyId,
        ECDsa privateKey,
        DateTimeOffset nowUtc,
        TimeSpan? responseValidity = null)
    {
        Guard.NotNull(request, nameof(request));
        Guard.NotNull(licenseClaims, nameof(licenseClaims));
        Guard.NotNull(keyId, nameof(keyId));
        Guard.NotNull(privateKey, nameof(privateKey));

        DateTimeOffset issuedAtUtc = nowUtc.ToUniversalTime();
        TimeSpan validity = responseValidity ?? TimeSpan.FromDays(7);
        if (validity <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(responseValidity),
                "Response validity must be greater than zero.");
        }

        if (issuedAtUtc < request.CreatedAtUtc || issuedAtUtc >= request.ExpiresAtUtc)
        {
            throw new ArgumentException("The activation request is not valid at the issue time.", nameof(request));
        }

        if (!string.Equals(
                licenseClaims.ProductCode,
                request.ProductCode,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("License product does not match the request.", nameof(licenseClaims));
        }

        if (!string.Equals(
                licenseClaims.InstallationId,
                request.InstallationId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "License installation does not match the request.",
                nameof(licenseClaims));
        }

        SignedLicense license = new EcdsaLicenseSigner().Sign(
            licenseClaims,
            keyId,
            privateKey);
        DateTimeOffset expiresAtUtc = issuedAtUtc.Add(validity);
        if (expiresAtUtc > request.ExpiresAtUtc)
        {
            expiresAtUtc = request.ExpiresAtUtc;
        }

        var values = new ActivationResponsePayload(
            request.RequestId,
            HashNonce(request.NonceBytes),
            request.ProductCode,
            request.InstallationId,
            issuedAtUtc,
            expiresAtUtc,
            license);
        ActivationResponse unsigned = ActivationResponseEnvelope.Create(
            keyId,
            values,
            Array.Empty<byte>());
        byte[] signature = privateKey.SignData(
            ActivationResponseEnvelope.BuildSigningInput(unsigned),
            HashAlgorithmName.SHA256);
        if (signature.Length != 64)
        {
            throw new CryptographicException("ES256 must produce a 64-byte IEEE P1363 signature.");
        }

        return ActivationResponseEnvelope.Create(keyId, values, signature);
    }

    /// <summary>Hashes the request nonce so the signed response binds to it without copying it directly.</summary>
    internal static byte[] HashNonce(byte[] nonce)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(nonce);
        }
    }
}

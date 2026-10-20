using System.Security.Cryptography;
using System.Text;
using MySerialKey.GiqhDev.Activation.Offline.Internal;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Storage;
using MySerialKey.GiqhDev.Storage;

namespace MySerialKey.GiqhDev.Activation.Offline;

/// <summary>
/// Validates and imports offline activation responses for one product installation.
/// </summary>
public sealed class OfflineActivationClient
{
    private const string ConsumptionEntryPrefix = "offline-activation-consumed-";
    private readonly string _productCode;
    private readonly string _installationId;
    private readonly ILicenseStore _licenseStore;
    private readonly IProtectedDataStore _stateStore;

    /// <summary>Initializes an offline activation client bound to a local installation.</summary>
    public OfflineActivationClient(
        string productCode,
        string installationId,
        ILicenseStore licenseStore,
        IProtectedDataStore stateStore)
    {
        Guard.NotNull(productCode, nameof(productCode));
        Guard.NotNull(installationId, nameof(installationId));
        Guard.NotNull(licenseStore, nameof(licenseStore));
        Guard.NotNull(stateStore, nameof(stateStore));
        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            throw new ArgumentException("Installation identifier cannot be empty.", nameof(installationId));
        }

        _productCode = productCode;
        _installationId = installationId;
        _licenseStore = licenseStore;
        _stateStore = stateStore;
    }

    /// <summary>Parses, validates, and imports portable request and response tokens.</summary>
    public OfflineActivationImportResult Import(
        string requestToken,
        string responseToken,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        Guard.NotNull(trustedKeys, nameof(trustedKeys));
        if (!ActivationRequest.TryParse(requestToken, out ActivationRequest? request))
        {
            return Failure(
                OfflineActivationImportStatus.InvalidRequest,
                "The activation-request token is malformed.");
        }

        if (!ActivationResponse.TryParse(responseToken, out ActivationResponse? response))
        {
            return Failure(
                OfflineActivationImportStatus.InvalidResponse,
                "The activation-response token is malformed.");
        }

        return Import(request!, response!, nowUtc, trustedKeys);
    }

    /// <summary>Validates and imports an already parsed request and response.</summary>
    public OfflineActivationImportResult Import(
        ActivationRequest request,
        ActivationResponse response,
        DateTimeOffset nowUtc,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        Guard.NotNull(request, nameof(request));
        Guard.NotNull(response, nameof(response));
        Guard.NotNull(trustedKeys, nameof(trustedKeys));

        DateTimeOffset currentUtc = nowUtc.ToUniversalTime();
        if (currentUtc < request.CreatedAtUtc)
        {
            return Failure(
                OfflineActivationImportStatus.RequestNotYetValid,
                "The activation request is not valid yet.");
        }

        if (currentUtc >= request.ExpiresAtUtc)
        {
            return Failure(
                OfflineActivationImportStatus.RequestExpired,
                "The activation request has expired.");
        }

        if (!string.Equals(request.ProductCode, _productCode, StringComparison.Ordinal))
        {
            return Failure(
                OfflineActivationImportStatus.WrongProduct,
                "The activation request belongs to another product.");
        }

        if (!string.Equals(request.InstallationId, _installationId, StringComparison.Ordinal))
        {
            return Failure(
                OfflineActivationImportStatus.WrongInstallation,
                "The activation request belongs to another installation.");
        }

        string consumptionEntry = ConsumptionEntryPrefix + request.RequestId;
        ProtectedDataReadResult consumed = _stateStore.Read(_productCode, consumptionEntry);
        if (consumed.Status == ProtectedDataReadStatus.Success)
        {
            return Failure(
                OfflineActivationImportStatus.ReplayDetected,
                "The activation request was already consumed.");
        }

        if (consumed.Status != ProtectedDataReadStatus.NotFound)
        {
            return Failure(
                OfflineActivationImportStatus.StorageUnavailable,
                consumed.Message ?? "The protected activation state could not be read.");
        }

        if (!string.Equals(
                response.Algorithm,
                LicenseAlgorithms.EcdsaP256Sha256,
                StringComparison.Ordinal))
        {
            return Failure(
                OfflineActivationImportStatus.UnsupportedAlgorithm,
                "The activation-response signature algorithm is not supported.");
        }

        LicenseVerificationKey? verificationKey = FindKey(response, trustedKeys);
        if (verificationKey is null)
        {
            return Failure(
                OfflineActivationImportStatus.UnknownSigningKey,
                "No trusted public key matches the activation response.");
        }

        bool signatureIsValid;
        using (ECDsa publicKey = verificationKey.CreatePublicKey())
        {
            signatureIsValid = publicKey.VerifyData(
                ActivationResponseEnvelope.BuildSigningInput(response),
                response.Signature,
                HashAlgorithmName.SHA256);
        }

        if (!signatureIsValid)
        {
            return Failure(
                OfflineActivationImportStatus.InvalidSignature,
                "The activation-response signature is invalid.");
        }

        if (!string.Equals(response.RequestId, request.RequestId, StringComparison.Ordinal) ||
            !FixedTimeEquals(response.RequestNonceHash, OfflineActivationIssuer.HashNonce(request.NonceBytes)))
        {
            return Failure(
                OfflineActivationImportStatus.WrongRequest,
                "The activation response belongs to another request.");
        }

        if (!string.Equals(response.ProductCode, _productCode, StringComparison.Ordinal))
        {
            return Failure(
                OfflineActivationImportStatus.WrongProduct,
                "The activation response belongs to another product.");
        }

        if (!string.Equals(response.InstallationId, _installationId, StringComparison.Ordinal))
        {
            return Failure(
                OfflineActivationImportStatus.WrongInstallation,
                "The activation response belongs to another installation.");
        }

        if (currentUtc < response.IssuedAtUtc)
        {
            return Failure(
                OfflineActivationImportStatus.ResponseNotYetValid,
                "The activation response is not valid yet.");
        }

        if (currentUtc >= response.ExpiresAtUtc || response.ExpiresAtUtc > request.ExpiresAtUtc)
        {
            return Failure(
                OfflineActivationImportStatus.ResponseExpired,
                "The activation response has expired or exceeds its request deadline.");
        }

        var context = new LicenseValidationContext(
            _productCode,
            currentUtc,
            _installationId);
        LicenseValidationResult licenseValidation = new EcdsaLicenseValidator().Validate(
            response.License,
            context,
            trustedKeys);
        if (!licenseValidation.IsValid)
        {
            return Failure(
                OfflineActivationImportStatus.InvalidLicense,
                "The embedded signed license is invalid.",
                licenseValidation);
        }

        if (!_licenseStore.Save(_productCode, response.License))
        {
            return Failure(
                OfflineActivationImportStatus.StorageUnavailable,
                "The signed license could not be stored.");
        }

        byte[] receipt = ComputeReceipt(response);
        ProtectedDataWriteResult marked = _stateStore.TryCreate(
            _productCode,
            consumptionEntry,
            receipt);
        if (marked.Status == ProtectedDataWriteStatus.AlreadyExists)
        {
            return Failure(
                OfflineActivationImportStatus.ReplayDetected,
                "The activation request was already consumed.");
        }

        if (marked.Status != ProtectedDataWriteStatus.Success)
        {
            return Failure(
                OfflineActivationImportStatus.StorageUnavailable,
                marked.Message ?? "The activation consumption receipt could not be stored.");
        }

        return OfflineActivationImportResult.Success(response.License, licenseValidation);
    }

    private static LicenseVerificationKey? FindKey(
        ActivationResponse response,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys)
    {
        foreach (LicenseVerificationKey key in trustedKeys)
        {
            if (string.Equals(key.KeyId, response.KeyId, StringComparison.Ordinal) &&
                string.Equals(key.Algorithm, response.Algorithm, StringComparison.Ordinal))
            {
                return key;
            }
        }

        return null;
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        int difference = left.Length ^ right.Length;
        int length = left.Length < right.Length ? left.Length : right.Length;
        for (int index = 0; index < length; index++)
        {
            difference |= left[index] ^ right[index];
        }

        return difference == 0;
    }

    private static byte[] ComputeReceipt(ActivationResponse response)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(response.ToString()));
        }
    }

    private static OfflineActivationImportResult Failure(
        OfflineActivationImportStatus status,
        string message,
        LicenseValidationResult? licenseValidation = null)
    {
        return OfflineActivationImportResult.Failure(status, message, licenseValidation);
    }
}

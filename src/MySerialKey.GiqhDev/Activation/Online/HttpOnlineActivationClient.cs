using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using MySerialKey.GiqhDev.Activation.Online.Internal;
using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;
using MySerialKey.GiqhDev.Licensing.Storage;

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Implements version-one online licensing operations over HTTP.</summary>
public sealed class HttpOnlineActivationClient : IOnlineActivationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILicenseStore _licenseStore;
    private readonly IReadOnlyCollection<LicenseVerificationKey> _trustedKeys;
    private readonly HttpOnlineActivationClientOptions _options;
    private readonly Func<DateTimeOffset> _utcNow;

    /// <summary>Initializes an HTTP activation client using caller-owned dependencies.</summary>
    public HttpOnlineActivationClient(
        HttpClient httpClient,
        ILicenseStore licenseStore,
        IReadOnlyCollection<LicenseVerificationKey> trustedKeys,
        HttpOnlineActivationClientOptions? options = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        Guard.NotNull(httpClient, nameof(httpClient));
        Guard.NotNull(licenseStore, nameof(licenseStore));
        Guard.NotNull(trustedKeys, nameof(trustedKeys));
        _options = options ?? new HttpOnlineActivationClientOptions();
        _options.Validate();
        _httpClient = httpClient;
        _licenseStore = licenseStore;
        _trustedKeys = trustedKeys;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public Task<ActivationClientResult> ActivateAsync(
        OnlineActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request, nameof(request));
        var wire = new OnlineActivationWireRequest
        {
            SchemaVersion = request.SchemaVersion,
            OperationId = request.OperationId,
            Serial = request.Serial,
            ProductCode = request.ProductCode,
            ApplicationVersion = request.ApplicationVersion,
            InstallationId = request.InstallationId
        };
        return ExecuteLicenseOperationAsync(
            _options.ActivationEndpoint,
            OnlineJsonSerializer.Serialize(wire),
            request.OperationId,
            request.ProductCode,
            request.InstallationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ActivationClientResult> RenewAsync(
        OnlineLicenseRenewalRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request, nameof(request));
        return ExecuteLicenseOperationAsync(
            _options.RenewalEndpoint,
            SerializeLicenseRequest(
                request.SchemaVersion,
                request.OperationId,
                request.LicenseToken,
                request.ProductCode,
                request.ApplicationVersion,
                request.InstallationId),
            request.OperationId,
            request.ProductCode,
            request.InstallationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ActivationClientResult> QueryAsync(
        OnlineLicenseQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request, nameof(request));
        return ExecuteLicenseOperationAsync(
            _options.QueryEndpoint,
            SerializeLicenseRequest(
                request.SchemaVersion,
                request.OperationId,
                request.LicenseToken,
                request.ProductCode,
                request.ApplicationVersion,
                request.InstallationId),
            request.OperationId,
            request.ProductCode,
            request.InstallationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ActivationClientResult> TransferAsync(
        OnlineLicenseTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request, nameof(request));
        var wire = new OnlineLicenseTransferWireRequest
        {
            SchemaVersion = request.SchemaVersion,
            OperationId = request.OperationId,
            LicenseToken = request.LicenseToken,
            ProductCode = request.ProductCode,
            ApplicationVersion = request.ApplicationVersion,
            SourceInstallationId = request.SourceInstallationId,
            TargetInstallationId = request.TargetInstallationId
        };
        return ExecuteLicenseOperationAsync(
            _options.TransferEndpoint,
            OnlineJsonSerializer.Serialize(wire),
            request.OperationId,
            request.ProductCode,
            request.TargetInstallationId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivationClientResult> DeactivateAsync(
        OnlineLicenseDeactivationRequest request,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request, nameof(request));
        string body = SerializeLicenseRequest(
            request.SchemaVersion,
            request.OperationId,
            request.LicenseToken,
            request.ProductCode,
            request.ApplicationVersion,
            request.InstallationId);
        HttpSendResult sent = await SendWithRetriesAsync(
            _options.DeactivationEndpoint,
            body,
            request.OperationId,
            cancellationToken).ConfigureAwait(false);
        if (sent.Failure is not null)
        {
            return sent.Failure;
        }

        using (HttpResponseMessage response = sent.Response!)
        {
            byte[]? responseBody = await ReadBodyAsync(response).ConfigureAwait(false);
            if (responseBody is null)
            {
                return InvalidResponse(request.OperationId, "The service response is too large.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return MapServiceFailure(response.StatusCode, responseBody, request.OperationId);
            }

            if (!OnlineJsonSerializer.TryDeserialize(
                    responseBody,
                    out OnlineDeactivationWireResponse? wire) ||
                wire is null ||
                wire.SchemaVersion != ActivationContractVersions.Current ||
                !string.Equals(wire.OperationId, request.OperationId, StringComparison.Ordinal) ||
                !TryReadUtc(wire.TrustedTimeUtc, out _))
            {
                return InvalidResponse(request.OperationId, "The deactivation response is invalid.");
            }

            if (!_licenseStore.Delete(request.ProductCode))
            {
                return new ActivationClientResult(
                    ActivationClientStatus.StorageUnavailable,
                    request.OperationId,
                    message: "The local signed license could not be removed.");
            }

            return new ActivationClientResult(ActivationClientStatus.Success, request.OperationId);
        }
    }

    /// <summary>Sends a license-producing operation, validates the returned MSL1 token, and persists it safely.</summary>
    private async Task<ActivationClientResult> ExecuteLicenseOperationAsync(
        Uri endpoint,
        string body,
        string operationId,
        string productCode,
        string installationId,
        CancellationToken cancellationToken)
    {
        HttpSendResult sent = await SendWithRetriesAsync(
            endpoint,
            body,
            operationId,
            cancellationToken).ConfigureAwait(false);
        if (sent.Failure is not null)
        {
            return sent.Failure;
        }

        using (HttpResponseMessage response = sent.Response!)
        {
            byte[]? responseBody = await ReadBodyAsync(response).ConfigureAwait(false);
            if (responseBody is null)
            {
                return InvalidResponse(operationId, "The service response is too large.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return MapServiceFailure(response.StatusCode, responseBody, operationId);
            }

            if (!OnlineJsonSerializer.TryDeserialize(
                    responseBody,
                    out OnlineLicenseWireResponse? wire) ||
                wire is null ||
                wire.SchemaVersion != ActivationContractVersions.Current ||
                !string.Equals(wire.OperationId, operationId, StringComparison.Ordinal) ||
                !TryReadUtc(wire.TrustedTimeUtc, out _) ||
                !SignedLicense.TryParse(wire.LicenseToken, out SignedLicense? license))
            {
                return InvalidResponse(operationId, "The activation response is invalid.");
            }

            var context = new LicenseValidationContext(
                productCode,
                _utcNow().ToUniversalTime(),
                installationId);
            LicenseValidationResult validation = new EcdsaLicenseValidator().Validate(
                license!,
                context,
                _trustedKeys);
            bool isRevoked = validation.Status == LicenseValidationStatus.Revoked;
            if (!validation.IsValid && !isRevoked)
            {
                return new ActivationClientResult(
                    ActivationClientStatus.InvalidLicense,
                    operationId,
                    licenseValidation: validation,
                    message: "The service returned an invalid signed license.");
            }

            if (!_licenseStore.Save(productCode, license!))
            {
                return new ActivationClientResult(
                    ActivationClientStatus.StorageUnavailable,
                    operationId,
                    licenseValidation: validation,
                    message: "The validated signed license could not be stored.");
            }

            return new ActivationClientResult(
                isRevoked
                    ? ActivationClientStatus.Revoked
                    : ActivationClientStatus.Success,
                operationId,
                license,
                validation,
                message: isRevoked
                    ? "The signed license records a revoked entitlement."
                    : null);
        }
    }

    /// <summary>Sends one logical request with bounded retries while preserving its idempotency identity.</summary>
    private async Task<HttpSendResult> SendWithRetriesAsync(
        Uri endpoint,
        string body,
        string operationId,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.TryAddWithoutValidation("Idempotency-Key", operationId);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false);
                    if (IsTransient(response.StatusCode) && attempt < _options.MaxRetries)
                    {
                        response.Dispose();
                        await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return HttpSendResult.Success(response);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < _options.MaxRetries)
                {
                    await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return HttpSendResult.Failed(new ActivationClientResult(
                    ActivationClientStatus.Timeout,
                    operationId,
                    message: "The activation service request timed out."));
            }
            catch (HttpRequestException exception)
            {
                if (attempt < _options.MaxRetries)
                {
                    await DelayAsync(attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return HttpSendResult.Failed(new ActivationClientResult(
                    ActivationClientStatus.NetworkUnavailable,
                    operationId,
                    message: exception.Message));
            }
        }

        throw new InvalidOperationException("The retry loop ended unexpectedly.");
    }

    /// <summary>Reads a response body only when it remains within the configured memory limit.</summary>
    private async Task<byte[]?> ReadBodyAsync(HttpResponseMessage response)
    {
        long? length = response.Content.Headers.ContentLength;
        if (length.HasValue && length.Value > _options.MaximumResponseBytes)
        {
            return null;
        }

        byte[] body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        return body.Length <= _options.MaximumResponseBytes ? body : null;
    }

    /// <summary>Maps HTTP and versioned service errors to transport-independent client statuses.</summary>
    private ActivationClientResult MapServiceFailure(
        HttpStatusCode statusCode,
        byte[] body,
        string operationId)
    {
        OnlineJsonSerializer.TryDeserialize(body, out OnlineErrorWireResponse? error);
        string? errorCode = error?.SchemaVersion == ActivationContractVersions.Current &&
            string.Equals(error.OperationId, operationId, StringComparison.Ordinal)
            ? error.ErrorCode
            : null;
        string? message = errorCode is null ? null : error!.Message;
        TimeSpan? retryAfter = errorCode is null || !error!.RetryAfterSeconds.HasValue
            ? null
            : TimeSpan.FromSeconds(Math.Max(0, error.RetryAfterSeconds.Value));
        ActivationClientStatus status;
        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        {
            status = ActivationClientStatus.Unauthorized;
        }
        else if ((int)statusCode == 429)
        {
            status = ActivationClientStatus.RateLimited;
        }
        else if (statusCode == HttpStatusCode.RequestTimeout)
        {
            status = ActivationClientStatus.Timeout;
        }
        else
        {
            status = ActivationClientStatus.ServiceRejected;
        }

        return new ActivationClientResult(
            status,
            operationId,
            errorCode: errorCode,
            message: message ?? "The activation service rejected the request.",
            retryAfter: retryAfter);
    }

    /// <summary>Applies capped exponential backoff before the next retry attempt.</summary>
    private Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        if (_options.RetryDelay == TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        double multiplier = Math.Pow(2, attempt);
        return Task.Delay(
            TimeSpan.FromMilliseconds(Math.Min(
                _options.RetryDelay.TotalMilliseconds * multiplier,
                TimeSpan.FromMinutes(1).TotalMilliseconds)),
            cancellationToken);
    }

    /// <summary>Serializes the common query, renewal, or deactivation request contract.</summary>
    private static string SerializeLicenseRequest(
        int schemaVersion,
        string operationId,
        string licenseToken,
        string productCode,
        string applicationVersion,
        string installationId)
    {
        return OnlineJsonSerializer.Serialize(new OnlineLicenseWireRequest
        {
            SchemaVersion = schemaVersion,
            OperationId = operationId,
            LicenseToken = licenseToken,
            ProductCode = productCode,
            ApplicationVersion = applicationVersion,
            InstallationId = installationId
        });
    }

    /// <summary>Determines whether an HTTP status is eligible for an idempotent retry.</summary>
    private static bool IsTransient(HttpStatusCode statusCode)
    {
        int value = (int)statusCode;
        return statusCode == HttpStatusCode.RequestTimeout || value == 429 || value >= 500;
    }

    /// <summary>Parses a round-trip timestamp without accepting alternate ambiguous formats.</summary>
    private static bool TryReadUtc(string? value, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out result);
    }

    /// <summary>Creates a consistent result for malformed or oversized service responses.</summary>
    private static ActivationClientResult InvalidResponse(string operationId, string message)
    {
        return new ActivationClientResult(
            ActivationClientStatus.InvalidResponse,
            operationId,
            message: message);
    }

    private sealed class HttpSendResult
    {
        private HttpSendResult(HttpResponseMessage? response, ActivationClientResult? failure)
        {
            Response = response;
            Failure = failure;
        }

        public HttpResponseMessage? Response { get; }
        public ActivationClientResult? Failure { get; }

        /// <summary>Wraps a received HTTP response for the caller that owns its disposal.</summary>
        public static HttpSendResult Success(HttpResponseMessage response)
        {
            return new HttpSendResult(response, null);
        }

        /// <summary>Wraps a typed transport failure when no HTTP response is available.</summary>
        public static HttpSendResult Failed(ActivationClientResult failure)
        {
            return new HttpSendResult(null, failure);
        }
    }
}

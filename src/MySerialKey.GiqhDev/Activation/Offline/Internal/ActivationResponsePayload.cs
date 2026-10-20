using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Offline.Internal;

internal sealed class ActivationResponsePayload
{
    public ActivationResponsePayload(
        string requestId,
        byte[] requestNonceHash,
        string productCode,
        string installationId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        SignedLicense license)
    {
        RequestId = requestId;
        RequestNonceHash = (byte[])requestNonceHash.Clone();
        ProductCode = productCode;
        InstallationId = installationId;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        License = license;
    }

    public string RequestId { get; }
    public byte[] RequestNonceHash { get; }
    public string ProductCode { get; }
    public string InstallationId { get; }
    public DateTimeOffset IssuedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public SignedLicense License { get; }
}

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MySerialKey.GiqhDev.Activation.Online.Internal;

internal static class OnlineJsonSerializer
{
    public static string Serialize<T>(T value)
    {
        using (var stream = new MemoryStream())
        {
            new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    public static bool TryDeserialize<T>(byte[] value, out T? result)
        where T : class
    {
        result = null;
        try
        {
            using (var stream = new MemoryStream(value, false))
            {
                result = new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
                return result is not null;
            }
        }
        catch (Exception exception) when (
            exception is SerializationException ||
            exception is FormatException ||
            exception is ArgumentException)
        {
            return false;
        }
    }
}

[DataContract]
internal sealed class OnlineActivationWireRequest
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string OperationId { get; set; } = string.Empty;
    [DataMember(Name = "serial", Order = 3)] public string Serial { get; set; } = string.Empty;
    [DataMember(Name = "productCode", Order = 4)] public string ProductCode { get; set; } = string.Empty;
    [DataMember(Name = "applicationVersion", Order = 5)] public string ApplicationVersion { get; set; } = string.Empty;
    [DataMember(Name = "installationId", Order = 6)] public string InstallationId { get; set; } = string.Empty;
}

[DataContract]
internal sealed class OnlineLicenseWireRequest
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string OperationId { get; set; } = string.Empty;
    [DataMember(Name = "licenseToken", Order = 3)] public string LicenseToken { get; set; } = string.Empty;
    [DataMember(Name = "productCode", Order = 4)] public string ProductCode { get; set; } = string.Empty;
    [DataMember(Name = "applicationVersion", Order = 5)] public string ApplicationVersion { get; set; } = string.Empty;
    [DataMember(Name = "installationId", Order = 6)] public string InstallationId { get; set; } = string.Empty;
}

[DataContract]
internal sealed class OnlineLicenseTransferWireRequest
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string OperationId { get; set; } = string.Empty;
    [DataMember(Name = "licenseToken", Order = 3)] public string LicenseToken { get; set; } = string.Empty;
    [DataMember(Name = "productCode", Order = 4)] public string ProductCode { get; set; } = string.Empty;
    [DataMember(Name = "applicationVersion", Order = 5)] public string ApplicationVersion { get; set; } = string.Empty;
    [DataMember(Name = "sourceInstallationId", Order = 6)] public string SourceInstallationId { get; set; } = string.Empty;
    [DataMember(Name = "targetInstallationId", Order = 7)] public string TargetInstallationId { get; set; } = string.Empty;
}

[DataContract]
internal sealed class OnlineLicenseWireResponse
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string? OperationId { get; set; }
    [DataMember(Name = "licenseToken", Order = 3)] public string? LicenseToken { get; set; }
    [DataMember(Name = "trustedTimeUtc", Order = 4)] public string? TrustedTimeUtc { get; set; }
}

[DataContract]
internal sealed class OnlineDeactivationWireResponse
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string? OperationId { get; set; }
    [DataMember(Name = "trustedTimeUtc", Order = 3)] public string? TrustedTimeUtc { get; set; }
}

[DataContract]
internal sealed class OnlineErrorWireResponse
{
    [DataMember(Name = "schemaVersion", Order = 1)] public int SchemaVersion { get; set; }
    [DataMember(Name = "operationId", Order = 2)] public string? OperationId { get; set; }
    [DataMember(Name = "errorCode", Order = 3)] public string? ErrorCode { get; set; }
    [DataMember(Name = "message", Order = 4)] public string? Message { get; set; }
    [DataMember(Name = "retryable", Order = 5)] public bool Retryable { get; set; }
    [DataMember(Name = "retryAfterSeconds", Order = 6)] public int? RetryAfterSeconds { get; set; }
}

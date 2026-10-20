using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Online.Internal;

internal static class OnlineContractValidation
{
    private const int MaximumTextLength = 4096;

    public static string Required(string value, string parameterName)
    {
        Guard.NotNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumTextLength)
        {
            throw new ArgumentException(
                "The value cannot be empty or longer than 4096 characters.",
                parameterName);
        }

        return value;
    }

    public static string? Optional(string? value, string parameterName)
    {
        if (value is not null &&
            (string.IsNullOrWhiteSpace(value) || value.Length > MaximumTextLength))
        {
            throw new ArgumentException(
                "The value must be null or contain at most 4096 characters.",
                parameterName);
        }

        return value;
    }

    public static string OperationId(string? value, string parameterName)
    {
        string operationId = value ?? Guid.NewGuid().ToString("N");
        if (!Guid.TryParseExact(operationId, "N", out _))
        {
            throw new ArgumentException(
                "The operation identifier must be a GUID in N format.",
                parameterName);
        }

        return operationId;
    }

    public static string LicenseToken(SignedLicense license, string parameterName)
    {
        Guard.NotNull(license, parameterName);
        return license.ToString();
    }

    public static string PortableLicenseToken(string value, string parameterName)
    {
        Guard.NotNull(value, parameterName);
        if (string.IsNullOrWhiteSpace(value) || value.Length > 131072)
        {
            throw new ArgumentException(
                "The license token is empty or exceeds the supported size.",
                parameterName);
        }

        return value;
    }
}

using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Licensing;

namespace MySerialKey.GiqhDev.Activation.Online.Internal;

internal static class OnlineContractValidation
{
    private const int MaximumTextLength = 4096;

    /// <summary>Validates and returns a required bounded contract string.</summary>
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

    /// <summary>Validates and returns an optional bounded contract string.</summary>
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

    /// <summary>Creates or validates the GUID-N identifier used for idempotent operations.</summary>
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

    /// <summary>Converts a parsed signed license into its portable contract token.</summary>
    public static string LicenseToken(SignedLicense license, string parameterName)
    {
        Guard.NotNull(license, parameterName);
        return license.ToString();
    }

    /// <summary>Validates a portable token''s presence and maximum accepted contract size.</summary>
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

using MySerialKey.GiqhDev.Licensing.Internal;

namespace MySerialKey.GiqhDev.Activation.Offline.Internal;

internal static class ActivationRequestEnvelope
{
    public const string CurrentFormat = "MSAR1";
    private const int MaximumTokenLength = 131072;

    /// <summary>Builds the portable MSAR1 token from a validated activation request.</summary>
    public static string BuildToken(ActivationRequest request)
    {
        return CurrentFormat + "." + Base64Url.Encode(ActivationRequestSerializer.Serialize(request));
    }

    /// <summary>Parses an MSAR1 envelope and decodes its request payload without throwing for malformed input.</summary>
    public static bool TryParse(string? token, out ActivationRequest? request)
    {
        request = null;
        if (token is null || string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        string[] parts = token.Split('.');
        if (parts.Length != 2 ||
            !string.Equals(parts[0], CurrentFormat, StringComparison.Ordinal) ||
            !Base64Url.TryDecode(parts[1], out byte[] payload))
        {
            return false;
        }

        try
        {
            request = ActivationRequestSerializer.Deserialize(payload);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

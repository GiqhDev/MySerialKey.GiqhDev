using System;

namespace MySerialKey.GiqhDev.Licensing.Internal;

internal static class Base64Url
{
    /// <summary>Encodes bytes with the unpadded URL-safe Base64 alphabet used by portable tokens.</summary>
    public static string Encode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Decodes strict unpadded Base64url text without throwing for malformed input.</summary>
    public static bool TryDecode(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrEmpty(value) || value.IndexOf('=') >= 0)
        {
            return false;
        }

        string base64 = value.Replace('-', '+').Replace('_', '/');
        int remainder = base64.Length % 4;
        if (remainder == 1)
        {
            return false;
        }

        if (remainder > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - remainder, '=');
        }

        try
        {
            bytes = Convert.FromBase64String(base64);
            return string.Equals(Encode(bytes), value, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

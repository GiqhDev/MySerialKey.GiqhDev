using System.Security.Cryptography;
using System.Text;

namespace MySerialKey.GiqhDev.Internal;

internal static class SerialAuthenticator
{
    public static string CreateAuthenticationTag(
        string payload,
        byte[] secretKey,
        string alphabet,
        SerialFormatVersion version)
    {
        byte[] payloadBytes = Encoding.ASCII.GetBytes(((int)version).ToString() + ":" + payload);

        using (var hmac = new HMACSHA256(secretKey))
        {
            byte[] hash = hmac.ComputeHash(payloadBytes);
            return BaseAlphabetEncoder.Encode(hash, alphabet, SerialKeyDefaults.AuthenticationTagLength);
        }
    }
}

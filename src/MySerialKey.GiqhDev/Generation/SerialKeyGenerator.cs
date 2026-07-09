using System.Collections.Generic;
using System.Text;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Generates cryptographically secure serial keys.
/// </summary>
public static class SerialKeyGenerator
{
    /// <summary>
    /// Generates a cryptographically secure serial key using the default 5-6-5-6 pattern.
    /// </summary>
    /// <returns>A formatted serial key.</returns>
    public static string Generate()
    {
        return Generate(null);
    }

    /// <summary>
    /// Generates a cryptographically secure serial key using the supplied options.
    /// </summary>
    /// <param name="options">The generation options. If null, defaults are used.</param>
    /// <returns>A formatted serial key.</returns>
    public static string Generate(SerialKeyGenerationOptions? options)
    {
        SerialKeyOptionsValidator.Validate(options);
        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();
        string alphabet = SerialKeyOptionsValidator.GetEffectiveAlphabet(effectiveOptions);

        var rawCharacters = new char[SerialKeyOptionsValidator.GetTotalCharacterCount(effectiveOptions.Segments)];

        for (int i = 0; i < rawCharacters.Length; i++)
        {
            rawCharacters[i] = alphabet[SecureRandom.GetInt32(alphabet.Length)];
        }

        return SerialKeyFormatter.Format(new string(rawCharacters), effectiveOptions);
    }

    /// <summary>
    /// Generates a serial key containing a compact HMAC-SHA256 authentication tag.
    /// </summary>
    /// <param name="secretKey">The secret key used to calculate the authentication tag.</param>
    /// <returns>A formatted authenticated serial key.</returns>
    public static string GenerateAuthenticated(byte[] secretKey)
    {
        return GenerateAuthenticated(secretKey, null);
    }

    /// <summary>
    /// Generates a serial key containing a compact HMAC-SHA256 authentication tag.
    /// </summary>
    /// <param name="secretKey">The secret key used to calculate the authentication tag.</param>
    /// <param name="options">The generation options. If null, defaults are used.</param>
    /// <returns>A formatted authenticated serial key.</returns>
    public static string GenerateAuthenticated(byte[] secretKey, SerialKeyGenerationOptions? options)
    {
        Guard.NotNull(secretKey, nameof(secretKey));
        if (secretKey.Length == 0)
        {
            throw new ArgumentException("Secret key cannot be empty.", nameof(secretKey));
        }

        SerialKeyOptionsValidator.Validate(options);
        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();
        string alphabet = SerialKeyOptionsValidator.GetEffectiveAlphabet(effectiveOptions);
        int totalCharacterCount = SerialKeyOptionsValidator.GetTotalCharacterCount(effectiveOptions.Segments);

        if (totalCharacterCount <= SerialKeyDefaults.AuthenticationTagLength)
        {
            throw new ArgumentException("The configured serial pattern is too short for authenticated serials.", nameof(options));
        }

        int payloadLength = totalCharacterCount - SerialKeyDefaults.AuthenticationTagLength;
        string payload = GenerateRawPayload(payloadLength, alphabet);
        string tag = SerialAuthenticator.CreateAuthenticationTag(payload, secretKey, alphabet, effectiveOptions.FormatVersion);

        return SerialKeyFormatter.Format(payload + tag, effectiveOptions);
    }

    private static string GenerateRawPayload(int length, string alphabet)
    {
        var builder = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            builder.Append(alphabet[SecureRandom.GetInt32(alphabet.Length)]);
        }

        return builder.ToString();
    }
}

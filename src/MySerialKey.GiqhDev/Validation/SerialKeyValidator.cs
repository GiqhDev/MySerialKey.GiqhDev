using System;
using System.Linq;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Validates serial key format and authenticity.
/// </summary>
public static class SerialKeyValidator
{
    /// <summary>
    /// Validates serial key structure using default options.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult Validate(string? serial)
    {
        return ValidateFormat(serial, null);
    }

    /// <summary>
    /// Validates serial key structure using supplied options.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <param name="options">The validation options. If null, defaults are used.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult Validate(string? serial, SerialKeyGenerationOptions? options)
    {
        return ValidateFormat(serial, options);
    }

    /// <summary>
    /// Validates serial key structure using default options.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult ValidateFormat(string? serial)
    {
        return ValidateFormat(serial, null);
    }

    /// <summary>
    /// Validates serial key structure using supplied options.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <param name="options">The validation options. If null, defaults are used.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult ValidateFormat(string? serial, SerialKeyGenerationOptions? options)
    {
        SerialKeyOptionsValidator.Validate(options);
        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();

        if (string.IsNullOrWhiteSpace(serial))
        {
            return SerialValidationResult.Failure(SerialValidationError.NullOrEmpty, "Serial key cannot be null or empty.");
        }

        string normalized = SerialKeyNormalizer.Normalize(serial!, effectiveOptions);
        int visibleLength = SerialKeyOptionsValidator.GetVisibleCharacterCount(effectiveOptions.Segments);

        if (normalized.Length != visibleLength)
        {
            return SerialValidationResult.Failure(SerialValidationError.InvalidLength, "Serial key length does not match the configured pattern.");
        }

        string[] segments = normalized.Split(effectiveOptions.Separator);

        if (segments.Length != effectiveOptions.Segments.Count)
        {
            return SerialValidationResult.Failure(SerialValidationError.InvalidSegmentCount, "Serial key segment count does not match the configured pattern.");
        }

        for (int i = 0; i < normalized.Length; i++)
        {
            bool separatorExpected = IsSeparatorPosition(i, effectiveOptions);

            if (separatorExpected && normalized[i] != effectiveOptions.Separator)
            {
                return SerialValidationResult.Failure(SerialValidationError.InvalidSeparator, "Serial key separator is invalid.");
            }
        }

        string alphabet = SerialKeyOptionsValidator.GetEffectiveAlphabet(effectiveOptions);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length != effectiveOptions.Segments[i])
            {
                return SerialValidationResult.Failure(SerialValidationError.InvalidSegmentLength, "Serial key segment length does not match the configured pattern.");
            }

            if (segments[i].Any(character => alphabet.IndexOf(character) < 0))
            {
                return SerialValidationResult.Failure(SerialValidationError.InvalidCharacter, "Serial key contains characters outside the configured alphabet.");
            }
        }

        return SerialValidationResult.Success(normalized);
    }

    /// <summary>
    /// Returns true when the serial key has a valid structure.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <returns>True when the serial key is structurally valid.</returns>
    public static bool IsValid(string? serial)
    {
        return ValidateFormat(serial).IsValid;
    }

    /// <summary>
    /// Validates serial key structure and compact HMAC-SHA256 authenticity.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <param name="secretKey">The secret key used to validate authenticity.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult ValidateAuthenticated(string? serial, byte[] secretKey)
    {
        return ValidateAuthenticated(serial, secretKey, null);
    }

    /// <summary>
    /// Validates serial key structure and compact HMAC-SHA256 authenticity.
    /// </summary>
    /// <param name="serial">The serial key to validate.</param>
    /// <param name="secretKey">The secret key used to validate authenticity.</param>
    /// <param name="options">The validation options. If null, defaults are used.</param>
    /// <returns>The validation result.</returns>
    public static SerialValidationResult ValidateAuthenticated(string? serial, byte[] secretKey, SerialKeyGenerationOptions? options)
    {
        Guard.NotNull(secretKey, nameof(secretKey));
        if (secretKey.Length == 0)
        {
            throw new ArgumentException("Secret key cannot be empty.", nameof(secretKey));
        }

        SerialValidationResult formatResult = ValidateFormat(serial, options);
        if (!formatResult.IsValid || formatResult.NormalizedSerial is null)
        {
            return formatResult;
        }

        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();
        string alphabet = SerialKeyOptionsValidator.GetEffectiveAlphabet(effectiveOptions);
        string rawSerial = SerialKeyNormalizer.GetRawSerial(formatResult.NormalizedSerial, effectiveOptions);

        if (rawSerial.Length <= SerialKeyDefaults.AuthenticationTagLength)
        {
            return SerialValidationResult.Failure(SerialValidationError.AuthenticationFailed, "Serial key is too short to contain an authentication tag.");
        }

        int payloadLength = rawSerial.Length - SerialKeyDefaults.AuthenticationTagLength;
        string payload = rawSerial.Substring(0, payloadLength);
        string providedTag = rawSerial.Substring(payloadLength, SerialKeyDefaults.AuthenticationTagLength);
        string expectedTag = SerialAuthenticator.CreateAuthenticationTag(payload, secretKey, alphabet, effectiveOptions.FormatVersion);

        if (!FixedTimeComparer.Equals(providedTag, expectedTag))
        {
            return SerialValidationResult.Failure(SerialValidationError.AuthenticationFailed, "Serial key authentication failed.");
        }

        return SerialValidationResult.Success(formatResult.NormalizedSerial);
    }

    private static bool IsSeparatorPosition(int index, SerialKeyGenerationOptions options)
    {
        int cursor = 0;
        for (int i = 0; i < options.Segments.Count - 1; i++)
        {
            cursor += options.Segments[i];
            if (index == cursor)
            {
                return true;
            }

            cursor++;
        }

        return false;
    }
}

using MySerialKey.GiqhDev.Generation;

namespace MySerialKey.GiqhDev.Internal;

internal static class SerialKeyOptionsValidator
{
    /// <summary>Validates all serial-format options and their combined size constraints.</summary>
    public static void Validate(SerialKeyGenerationOptions? options)
    {
        if (options is null)
        {
            return;
        }

        if (options.Segments is null)
        {
            throw new ArgumentException("Segments cannot be null.", nameof(options));
        }

        if (options.Segments.Count == 0)
        {
            throw new ArgumentException("At least one segment is required.", nameof(options));
        }

        if (options.Segments.Any(segment => segment <= 0))
        {
            throw new ArgumentException("All segments must be greater than zero.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Alphabet))
        {
            throw new ArgumentException("Alphabet cannot be null or empty.", nameof(options));
        }

        string effectiveAlphabet = GetEffectiveAlphabet(options);
        if (effectiveAlphabet.Length < 2)
        {
            throw new ArgumentException("Alphabet must contain at least two distinct characters.", nameof(options));
        }

        if (effectiveAlphabet.Distinct().Count() != effectiveAlphabet.Length)
        {
            throw new ArgumentException("Alphabet cannot contain duplicate characters.", nameof(options));
        }

        if (effectiveAlphabet.IndexOf(options.Separator) >= 0)
        {
            throw new ArgumentException("Separator cannot be part of the alphabet.", nameof(options));
        }
    }

    /// <summary>Returns the configured alphabet in the casing used for generation and validation.</summary>
    public static string GetEffectiveAlphabet(SerialKeyGenerationOptions options)
    {
        return options.UseUppercase ? options.Alphabet.ToUpperInvariant() : options.Alphabet;
    }

    /// <summary>Calculates the number of non-separator characters across all serial segments.</summary>
    public static int GetTotalCharacterCount(IReadOnlyList<int> segments)
    {
        int total = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            total += segments[i];
        }

        return total;
    }

    /// <summary>Calculates total displayed length including separators between segments.</summary>
    public static int GetVisibleCharacterCount(IReadOnlyList<int> segments)
    {
        return GetTotalCharacterCount(segments) + segments.Count - 1;
    }
}

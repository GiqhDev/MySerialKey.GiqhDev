using System.Text;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Normalizes serial key input for validation and display.
/// </summary>
public static class SerialKeyNormalizer
{
    /// <summary>
    /// Normalizes a serial key using the default pattern.
    /// </summary>
    /// <param name="serial">The serial key input.</param>
    /// <returns>The normalized serial key.</returns>
    public static string Normalize(string serial)
    {
        return Normalize(serial, null);
    }

    /// <summary>
    /// Normalizes a serial key using the supplied options.
    /// </summary>
    /// <param name="serial">The serial key input.</param>
    /// <param name="options">The normalization options. If null, defaults are used.</param>
    /// <returns>The normalized serial key.</returns>
    public static string Normalize(string serial, SerialKeyGenerationOptions? options)
    {
        Guard.NotNull(serial, nameof(serial));
        SerialKeyOptionsValidator.Validate(options);

        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();
        var rawBuilder = new StringBuilder(serial.Length);

        foreach (char character in serial)
        {
            if (char.IsWhiteSpace(character) || character == effectiveOptions.Separator)
            {
                continue;
            }

            rawBuilder.Append(effectiveOptions.UseUppercase ? char.ToUpperInvariant(character) : character);
        }

        string raw = rawBuilder.ToString();
        int expectedLength = SerialKeyOptionsValidator.GetTotalCharacterCount(effectiveOptions.Segments);

        return raw.Length == expectedLength
            ? SerialKeyFormatter.Format(raw, effectiveOptions)
            : raw;
    }

    internal static string GetRawSerial(string serial, SerialKeyGenerationOptions options)
    {
        var rawBuilder = new StringBuilder(serial.Length);

        foreach (char character in serial)
        {
            if (character != options.Separator)
            {
                rawBuilder.Append(character);
            }
        }

        return rawBuilder.ToString();
    }
}

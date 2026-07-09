using System.Text;
using MySerialKey.GiqhDev.Internal;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Formats raw serial key characters into segmented serial keys.
/// </summary>
public static class SerialKeyFormatter
{
    /// <summary>
    /// Formats raw serial key characters according to the default 5-6-5-6 pattern.
    /// </summary>
    /// <param name="rawSerial">The raw serial key characters without separators.</param>
    /// <returns>A formatted serial key.</returns>
    public static string Format(string rawSerial)
    {
        return Format(rawSerial, null);
    }

    /// <summary>
    /// Formats raw serial key characters according to the supplied options.
    /// </summary>
    /// <param name="rawSerial">The raw serial key characters without separators.</param>
    /// <param name="options">The formatting options. If null, defaults are used.</param>
    /// <returns>A formatted serial key.</returns>
    public static string Format(string rawSerial, SerialKeyGenerationOptions? options)
    {
        Guard.NotNull(rawSerial, nameof(rawSerial));
        SerialKeyOptionsValidator.Validate(options);

        SerialKeyGenerationOptions effectiveOptions = options ?? new SerialKeyGenerationOptions();
        int expectedLength = SerialKeyOptionsValidator.GetTotalCharacterCount(effectiveOptions.Segments);

        if (rawSerial.Length != expectedLength)
        {
            throw new ArgumentException("Raw serial length does not match the configured pattern.", nameof(rawSerial));
        }

        var builder = new StringBuilder(rawSerial.Length + effectiveOptions.Segments.Count - 1);
        int rawIndex = 0;

        for (int segmentIndex = 0; segmentIndex < effectiveOptions.Segments.Count; segmentIndex++)
        {
            if (segmentIndex > 0)
            {
                builder.Append(effectiveOptions.Separator);
            }

            int segmentLength = effectiveOptions.Segments[segmentIndex];
            builder.Append(rawSerial, rawIndex, segmentLength);
            rawIndex += segmentLength;
        }

        return effectiveOptions.UseUppercase
            ? builder.ToString().ToUpperInvariant()
            : builder.ToString();
    }
}

using System.Collections.Generic;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Defines the generation settings used to create serial keys.
/// </summary>
public sealed class SerialKeyGenerationOptions
{
    /// <summary>
    /// Gets or sets the segment lengths used to format the serial key.
    /// </summary>
    public IReadOnlyList<int> Segments { get; set; } = SerialKeyDefaults.DefaultSegments;

    /// <summary>
    /// Gets or sets the separator placed between serial key segments.
    /// </summary>
    public char Separator { get; set; } = SerialKeyDefaults.DefaultSeparator;

    /// <summary>
    /// Gets or sets the allowed alphabet used for generated serial keys.
    /// </summary>
    public string Alphabet { get; set; } = SerialKeyDefaults.DefaultAlphabet;

    /// <summary>
    /// Gets or sets a value indicating whether generated and normalized serial keys use uppercase characters.
    /// </summary>
    public bool UseUppercase { get; set; } = true;

    /// <summary>
    /// Gets or sets the authenticated serial format version.
    /// </summary>
    public SerialFormatVersion FormatVersion { get; set; } = SerialFormatVersion.V1;
}

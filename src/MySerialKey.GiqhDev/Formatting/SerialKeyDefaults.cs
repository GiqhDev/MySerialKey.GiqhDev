using System.Collections.Generic;

namespace MySerialKey.GiqhDev;

/// <summary>
/// Provides default serial key settings.
/// </summary>
public static class SerialKeyDefaults
{
    /// <summary>
    /// Gets the default segment distribution.
    /// </summary>
    public static IReadOnlyList<int> DefaultSegments { get; } = new[] { 5, 6, 5, 6 };

    /// <summary>
    /// Gets the default separator.
    /// </summary>
    public const char DefaultSeparator = '-';

    /// <summary>
    /// Gets the default alphabet without visually ambiguous characters.
    /// </summary>
    public const string DefaultAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    internal const int AuthenticationTagLength = 8;
}

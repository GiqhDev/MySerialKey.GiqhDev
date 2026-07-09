namespace MySerialKey.GiqhDev;

/// <summary>
/// Describes serial validation failures.
/// </summary>
public enum SerialValidationError
{
    /// <summary>
    /// No validation error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The serial key was null or empty.
    /// </summary>
    NullOrEmpty,

    /// <summary>
    /// The serial key length was invalid.
    /// </summary>
    InvalidLength,

    /// <summary>
    /// The serial key has an invalid segment count.
    /// </summary>
    InvalidSegmentCount,

    /// <summary>
    /// A serial key segment length was invalid.
    /// </summary>
    InvalidSegmentLength,

    /// <summary>
    /// A serial key separator was invalid.
    /// </summary>
    InvalidSeparator,

    /// <summary>
    /// A serial key character was invalid.
    /// </summary>
    InvalidCharacter,

    /// <summary>
    /// The serial key format was invalid.
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// The serial key authentication check failed.
    /// </summary>
    AuthenticationFailed
}

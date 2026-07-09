namespace MySerialKey.GiqhDev;

/// <summary>
/// Represents the result of serial key validation.
/// </summary>
public sealed class SerialValidationResult
{
    private SerialValidationResult(
        bool isValid,
        SerialValidationError error,
        string? message,
        string? normalizedSerial)
    {
        IsValid = isValid;
        Error = error;
        Message = message;
        NormalizedSerial = normalizedSerial;
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation error.
    /// </summary>
    public SerialValidationError Error { get; }

    /// <summary>
    /// Gets a human-readable validation message.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the normalized serial key when validation succeeds.
    /// </summary>
    public string? NormalizedSerial { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="normalizedSerial">The normalized serial key.</param>
    /// <returns>A successful validation result.</returns>
    public static SerialValidationResult Success(string normalizedSerial)
    {
        return new SerialValidationResult(true, SerialValidationError.None, null, normalizedSerial);
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <param name="message">The validation message.</param>
    /// <returns>A failed validation result.</returns>
    public static SerialValidationResult Failure(SerialValidationError error, string message)
    {
        return new SerialValidationResult(false, error, message, null);
    }
}

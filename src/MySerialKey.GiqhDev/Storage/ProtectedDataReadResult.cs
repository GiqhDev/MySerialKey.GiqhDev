namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Represents a protected-store read result.
/// </summary>
public sealed class ProtectedDataReadResult
{
    private ProtectedDataReadResult(
        ProtectedDataReadStatus status,
        byte[]? value,
        string? message)
    {
        Status = status;
        Value = value;
        Message = message;
    }

    /// <summary>Gets the operation status.</summary>
    public ProtectedDataReadStatus Status { get; }

    /// <summary>Gets a copy of the recovered value when successful.</summary>
    public byte[]? Value { get; }

    /// <summary>Gets a diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>Creates a successful read result while taking ownership of the recovered bytes.</summary>
    internal static ProtectedDataReadResult Success(byte[] value)
    {
        return new ProtectedDataReadResult(
            ProtectedDataReadStatus.Success,
            (byte[])value.Clone(),
            null);
    }

    /// <summary>Creates a typed read failure without exposing partial or unauthenticated data.</summary>
    internal static ProtectedDataReadResult Failure(
        ProtectedDataReadStatus status,
        string message)
    {
        return new ProtectedDataReadResult(status, null, message);
    }
}

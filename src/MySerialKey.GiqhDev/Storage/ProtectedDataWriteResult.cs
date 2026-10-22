namespace MySerialKey.GiqhDev.Storage;

/// <summary>
/// Represents a protected-store write result.
/// </summary>
public sealed class ProtectedDataWriteResult
{
    private ProtectedDataWriteResult(ProtectedDataWriteStatus status, string? message)
    {
        Status = status;
        Message = message;
    }

    /// <summary>Gets the operation status.</summary>
    public ProtectedDataWriteStatus Status { get; }

    /// <summary>Gets a diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>Creates a result indicating that protected data was written successfully.</summary>
    internal static ProtectedDataWriteResult Success()
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.Success, null);
    }

    /// <summary>Creates the expected result for a failed create-only operation on an existing entry.</summary>
    internal static ProtectedDataWriteResult AlreadyExists()
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.AlreadyExists, null);
    }

    /// <summary>Creates a result describing an inaccessible or failed backing store.</summary>
    internal static ProtectedDataWriteResult Unavailable(string message)
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.Unavailable, message);
    }
}

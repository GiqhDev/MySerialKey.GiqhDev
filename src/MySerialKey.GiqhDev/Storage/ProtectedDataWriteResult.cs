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

    internal static ProtectedDataWriteResult Success()
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.Success, null);
    }

    internal static ProtectedDataWriteResult AlreadyExists()
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.AlreadyExists, null);
    }

    internal static ProtectedDataWriteResult Unavailable(string message)
    {
        return new ProtectedDataWriteResult(ProtectedDataWriteStatus.Unavailable, message);
    }
}

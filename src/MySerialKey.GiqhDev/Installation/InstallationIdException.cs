namespace MySerialKey.GiqhDev.Installation;

/// <summary>
/// Represents an installation identity storage failure.
/// </summary>
public sealed class InstallationIdException : Exception
{
    /// <summary>
    /// Initializes the exception.
    /// </summary>
    public InstallationIdException(string message)
        : base(message)
    {
    }
}

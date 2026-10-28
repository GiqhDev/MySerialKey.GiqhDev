namespace MySerialKey.GiqhDev.Licensing.Revocation;

/// <summary>Represents a typed signed revocation-list validation result.</summary>
public sealed class RevocationListValidationResult
{
    internal RevocationListValidationResult(
        RevocationListValidationStatus status,
        ValidatedRevocationList? list,
        string? message)
    {
        Status = status;
        List = list;
        Message = message;
    }

    /// <summary>Gets the typed validation outcome.</summary>
    public RevocationListValidationStatus Status { get; }

    /// <summary>Gets the authenticated list when validation succeeds.</summary>
    public ValidatedRevocationList? List { get; }

    /// <summary>Gets a diagnostic message intended for logs or support tooling.</summary>
    public string? Message { get; }

    /// <summary>Gets whether the list can be used for offline revocation checks.</summary>
    public bool IsValid => Status == RevocationListValidationStatus.Valid;
}

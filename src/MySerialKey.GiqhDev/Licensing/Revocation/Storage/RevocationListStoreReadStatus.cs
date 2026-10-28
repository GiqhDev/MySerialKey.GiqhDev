namespace MySerialKey.GiqhDev.Licensing.Revocation.Storage;

/// <summary>Describes the outcome of reading a stored signed revocation list.</summary>
public enum RevocationListStoreReadStatus
{
    /// <summary>A parsed token was loaded successfully.</summary>
    Success = 0,
    /// <summary>No token has been stored for the product.</summary>
    NotFound,
    /// <summary>The protected value or portable token is malformed.</summary>
    Corrupted,
    /// <summary>The protected storage provider cannot complete the operation.</summary>
    Unavailable
}

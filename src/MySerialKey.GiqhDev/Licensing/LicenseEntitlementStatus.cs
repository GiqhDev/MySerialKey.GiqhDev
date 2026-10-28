namespace MySerialKey.GiqhDev.Licensing;

/// <summary>Describes the signed entitlement state of a license.</summary>
public enum LicenseEntitlementStatus
{
    /// <summary>The entitlement is active.</summary>
    Active = 0,
    /// <summary>The entitlement has been explicitly revoked by its issuer.</summary>
    Revoked = 1
}

namespace MySerialKey.GiqhDev.Activation.Online;

/// <summary>Defines transport-independent online licensing operations.</summary>
public interface IOnlineActivationClient
{
    /// <summary>Activates a serial and imports a validated signed license.</summary>
    Task<ActivationClientResult> ActivateAsync(
        OnlineActivationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Queries and imports the current validated installation license.</summary>
    Task<ActivationClientResult> QueryAsync(
        OnlineLicenseQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Renews and imports a validated signed license.</summary>
    Task<ActivationClientResult> RenewAsync(
        OnlineLicenseRenewalRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Transfers a license and imports the validated target-bound license.</summary>
    Task<ActivationClientResult> TransferAsync(
        OnlineLicenseTransferRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Deactivates an installation and removes its local license.</summary>
    Task<ActivationClientResult> DeactivateAsync(
        OnlineLicenseDeactivationRequest request,
        CancellationToken cancellationToken = default);
}

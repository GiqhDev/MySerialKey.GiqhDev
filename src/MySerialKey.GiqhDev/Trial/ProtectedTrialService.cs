using MySerialKey.GiqhDev.Internal;
using MySerialKey.GiqhDev.Storage;
using MySerialKey.GiqhDev.Trial.Internal;

namespace MySerialKey.GiqhDev.Trial;

/// <summary>
/// Persists and evaluates product-scoped trial state in a protected store.
/// </summary>
public sealed class ProtectedTrialService : ITrialService
{
    private const string EntryName = "trial-state";
    private readonly IProtectedDataStore _store;
    private readonly TrialOptions _options;
    private readonly IClock _clock;

    /// <summary>Initializes a protected trial service.</summary>
    public ProtectedTrialService(
        IProtectedDataStore store,
        TrialOptions? options = null,
        IClock? clock = null)
    {
        Guard.NotNull(store, nameof(store));
        _store = store;
        _options = options ?? new TrialOptions();
        _clock = clock ?? SystemClock.Instance;
    }

    /// <inheritdoc />
    public TrialValidationResult Start(string productCode)
    {
        DateTimeOffset nowUtc = GetUtcNow();
        DateTimeOffset expiresAtUtc;
        try
        {
            expiresAtUtc = nowUtc.Add(_options.Duration);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Result(
                TrialValidationStatus.StorageUnavailable,
                null,
                nowUtc,
                exception.Message);
        }

        var candidate = new TrialState(nowUtc, expiresAtUtc, nowUtc, null, null);
        ProtectedDataWriteResult create = _store.TryCreate(
            productCode,
            EntryName,
            TrialStateSerializer.Serialize(candidate));

        if (create.Status == ProtectedDataWriteStatus.Success)
        {
            return Result(TrialValidationStatus.Active, candidate, nowUtc, null);
        }

        if (create.Status == ProtectedDataWriteStatus.AlreadyExists)
        {
            TrialValidationResult existing = Load(productCode, nowUtc);
            return existing.State is null
                ? existing
                : Result(
                    TrialValidationStatus.AlreadyStarted,
                    existing.State,
                    nowUtc,
                    "The trial was already started.");
        }

        return Result(
            TrialValidationStatus.StorageUnavailable,
            null,
            nowUtc,
            create.Message);
    }

    /// <inheritdoc />
    public TrialValidationResult Get(string productCode)
    {
        return Load(productCode, GetUtcNow());
    }

    /// <inheritdoc />
    public TrialValidationResult Validate(
        string productCode,
        DateTimeOffset? trustedTimeUtc = null)
    {
        DateTimeOffset localNowUtc = GetUtcNow();
        TrialValidationResult loaded = Load(productCode, localNowUtc);
        TrialState? state = loaded.State;
        if (state is null ||
            loaded.Status == TrialValidationStatus.Corrupted ||
            loaded.Status == TrialValidationStatus.StorageUnavailable)
        {
            return loaded;
        }

        if (state.ConvertedToLicenseAtUtc.HasValue)
        {
            return Result(
                TrialValidationStatus.ConvertedToLicense,
                state,
                localNowUtc,
                null);
        }

        DateTimeOffset? normalizedTrustedTime = trustedTimeUtc?.ToUniversalTime();
        DateTimeOffset effectiveNowUtc = normalizedTrustedTime ?? localNowUtc;

        if (IsRollback(effectiveNowUtc, state.LastSuccessfulRunUtc) ||
            IsTrustedRollback(normalizedTrustedTime, state.LastTrustedTimeUtc))
        {
            return Result(
                TrialValidationStatus.ClockRollbackDetected,
                state,
                effectiveNowUtc,
                "The clock moved backward beyond the configured tolerance.");
        }

        if (effectiveNowUtc >= state.ExpiresAtUtc)
        {
            return Result(TrialValidationStatus.Expired, state, effectiveNowUtc, null);
        }

        DateTimeOffset successfulRunUtc = Max(
            state.LastSuccessfulRunUtc,
            effectiveNowUtc);
        DateTimeOffset? lastTrustedTimeUtc = Max(
            state.LastTrustedTimeUtc,
            normalizedTrustedTime);
        TrialState updated = state.WithSuccessfulRun(
            successfulRunUtc,
            lastTrustedTimeUtc);

        ProtectedDataWriteResult write = _store.Write(
            productCode,
            EntryName,
            TrialStateSerializer.Serialize(updated));
        if (write.Status != ProtectedDataWriteStatus.Success)
        {
            return Result(
                TrialValidationStatus.StorageUnavailable,
                state,
                effectiveNowUtc,
                write.Message);
        }

        return Result(TrialValidationStatus.Active, updated, effectiveNowUtc, null);
    }

    /// <inheritdoc />
    public TrialValidationResult MarkConvertedToLicense(
        string productCode,
        DateTimeOffset? trustedTimeUtc = null)
    {
        DateTimeOffset nowUtc = (trustedTimeUtc ?? GetUtcNow()).ToUniversalTime();
        TrialValidationResult loaded = Load(productCode, nowUtc);
        TrialState? state = loaded.State;
        if (state is null ||
            loaded.Status == TrialValidationStatus.Corrupted ||
            loaded.Status == TrialValidationStatus.StorageUnavailable)
        {
            return loaded;
        }

        if (state.ConvertedToLicenseAtUtc.HasValue)
        {
            return Result(
                TrialValidationStatus.ConvertedToLicense,
                state,
                nowUtc,
                null);
        }

        if (IsRollback(nowUtc, state.LastSuccessfulRunUtc))
        {
            return Result(
                TrialValidationStatus.ClockRollbackDetected,
                state,
                nowUtc,
                "The conversion time moved backward beyond the configured tolerance.");
        }

        DateTimeOffset conversionTimeUtc = Max(nowUtc, state.LastSuccessfulRunUtc);
        TrialState updated = state.WithConversion(conversionTimeUtc);
        ProtectedDataWriteResult write = _store.Write(
            productCode,
            EntryName,
            TrialStateSerializer.Serialize(updated));
        if (write.Status != ProtectedDataWriteStatus.Success)
        {
            return Result(
                TrialValidationStatus.StorageUnavailable,
                state,
                nowUtc,
                write.Message);
        }

        return Result(
            TrialValidationStatus.ConvertedToLicense,
            updated,
            nowUtc,
            null);
    }

    private TrialValidationResult Load(string productCode, DateTimeOffset nowUtc)
    {
        ProtectedDataReadResult read = _store.Read(productCode, EntryName);
        switch (read.Status)
        {
            case ProtectedDataReadStatus.NotFound:
                return Result(TrialValidationStatus.NotStarted, null, nowUtc, read.Message);
            case ProtectedDataReadStatus.Corrupted:
                return Result(TrialValidationStatus.Corrupted, null, nowUtc, read.Message);
            case ProtectedDataReadStatus.Unavailable:
                return Result(
                    TrialValidationStatus.StorageUnavailable,
                    null,
                    nowUtc,
                    read.Message);
        }

        try
        {
            TrialState state = TrialStateSerializer.Deserialize(read.Value!);
            TrialValidationStatus status = state.ConvertedToLicenseAtUtc.HasValue
                ? TrialValidationStatus.ConvertedToLicense
                : nowUtc >= state.ExpiresAtUtc
                    ? TrialValidationStatus.Expired
                    : TrialValidationStatus.Active;
            return Result(status, state, nowUtc, null);
        }
        catch (FormatException exception)
        {
            return Result(
                TrialValidationStatus.Corrupted,
                null,
                nowUtc,
                exception.Message);
        }
    }

    private bool IsRollback(DateTimeOffset current, DateTimeOffset previous)
    {
        return current < previous &&
            previous - current > _options.ClockRollbackTolerance;
    }

    private bool IsTrustedRollback(
        DateTimeOffset? current,
        DateTimeOffset? previous)
    {
        return current.HasValue &&
            previous.HasValue &&
            IsRollback(current.Value, previous.Value);
    }

    private DateTimeOffset GetUtcNow()
    {
        return _clock.UtcNow.ToUniversalTime();
    }

    private static TrialValidationResult Result(
        TrialValidationStatus status,
        TrialState? state,
        DateTimeOffset nowUtc,
        string? message)
    {
        TimeSpan remaining = state is null || nowUtc >= state.ExpiresAtUtc
            ? TimeSpan.Zero
            : state.ExpiresAtUtc - nowUtc;
        return new TrialValidationResult(status, state, remaining, message);
    }

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
    {
        return left >= right ? left : right;
    }

    private static DateTimeOffset? Max(
        DateTimeOffset? left,
        DateTimeOffset? right)
    {
        if (!left.HasValue)
        {
            return right;
        }

        if (!right.HasValue)
        {
            return left;
        }

        return Max(left.Value, right.Value);
    }
}

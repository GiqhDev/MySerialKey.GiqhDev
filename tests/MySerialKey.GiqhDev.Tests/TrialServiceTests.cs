using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySerialKey.GiqhDev.Storage;
using MySerialKey.GiqhDev.Trial;

namespace MySerialKey.GiqhDev.Tests;

[TestClass]
public sealed class TrialServiceTests
{
    private static readonly DateTimeOffset InitialTime =
        new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Get_DoesNotStartOrPersistTrial()
    {
        WithTrial((service, store, clock, root) =>
        {
            TrialValidationResult first = service.Get("Example.Product");
            TrialValidationResult second = service.Get("Example.Product");

            Assert.AreEqual(TrialValidationStatus.NotStarted, first.Status);
            Assert.AreEqual(TrialValidationStatus.NotStarted, second.Status);
            Assert.IsFalse(Directory.Exists(root));
        });
    }

    [TestMethod]
    public void Start_IsExplicitUniqueAndUsesSevenDayDefault()
    {
        WithTrial((service, store, clock, root) =>
        {
            TrialValidationResult started = service.Start("Example.Product");
            TrialValidationResult repeated = service.Start("Example.Product");

            Assert.AreEqual(TrialValidationStatus.Active, started.Status);
            Assert.AreEqual(TrialValidationStatus.AlreadyStarted, repeated.Status);
            Assert.AreEqual(InitialTime, started.State!.StartedAtUtc);
            Assert.AreEqual(InitialTime.AddDays(7), started.State.ExpiresAtUtc);
            Assert.AreEqual(TimeSpan.FromDays(7), started.Remaining);
            Assert.AreEqual(started.State.StartedAtUtc, repeated.State!.StartedAtUtc);
        });
    }

    [TestMethod]
    public void CustomDuration_UsesExactTwentyFourHourPeriodsAcrossDstOffsets()
    {
        string root = CreateTemporaryRoot();
        var clock = new FakeClock(
            new DateTimeOffset(2026, 3, 8, 1, 30, 0, TimeSpan.FromHours(-5)));
        var options = new TrialOptions(TimeSpan.FromHours(24));
        var service = CreateService(root, clock, options);

        try
        {
            TrialValidationResult started = service.Start("Example.Product");
            clock.UtcNow = new DateTimeOffset(
                2026,
                3,
                9,
                2,
                30,
                0,
                TimeSpan.FromHours(-4));

            TrialValidationResult expired = service.Validate("Example.Product");

            Assert.AreEqual(TimeSpan.FromHours(24),
                started.State!.ExpiresAtUtc - started.State.StartedAtUtc);
            Assert.AreEqual(TrialValidationStatus.Expired, expired.Status);
            Assert.AreEqual(TimeSpan.Zero, expired.Remaining);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void Validate_ToleratesSmallRollbackWithoutMovingWatermark()
    {
        WithTrial(
            (service, store, clock, root) =>
            {
                service.Start("Example.Product");
                clock.UtcNow = InitialTime.AddHours(1);
                TrialValidationResult forward = service.Validate("Example.Product");

                clock.UtcNow = InitialTime.AddMinutes(56);
                TrialValidationResult tolerated = service.Validate("Example.Product");

                clock.UtcNow = InitialTime.AddMinutes(54);
                TrialValidationResult rejected = service.Validate("Example.Product");

                Assert.AreEqual(TrialValidationStatus.Active, forward.Status);
                Assert.AreEqual(TrialValidationStatus.Active, tolerated.Status);
                Assert.AreEqual(
                    InitialTime.AddHours(1),
                    tolerated.State!.LastSuccessfulRunUtc);
                Assert.AreEqual(
                    TrialValidationStatus.ClockRollbackDetected,
                    rejected.Status);
            },
            new TrialOptions(TimeSpan.FromDays(7), TimeSpan.FromMinutes(5)));
    }

    [TestMethod]
    public void Validate_PersistsAndChecksTrustedTime()
    {
        WithTrial((service, store, clock, root) =>
        {
            service.Start("Example.Product");
            DateTimeOffset trusted = InitialTime.AddHours(2);

            TrialValidationResult accepted = service.Validate("Example.Product", trusted);
            TrialValidationResult rejected = service.Validate(
                "Example.Product",
                trusted.AddMinutes(-6));

            Assert.AreEqual(trusted, accepted.State!.LastTrustedTimeUtc);
            Assert.AreEqual(trusted, accepted.State.LastSuccessfulRunUtc);
            Assert.AreEqual(
                TrialValidationStatus.ClockRollbackDetected,
                rejected.Status);
        });
    }

    [TestMethod]
    public void State_SurvivesNewServiceAndStoreInstances()
    {
        string root = CreateTemporaryRoot();
        var clock = new FakeClock(InitialTime);

        try
        {
            ProtectedTrialService first = CreateService(root, clock);
            TrialValidationResult started = first.Start("Example.Product");

            clock.UtcNow = InitialTime.AddDays(1);
            ProtectedTrialService reopened = CreateService(root, clock);
            TrialValidationResult loaded = reopened.Get("Example.Product");

            Assert.AreEqual(TrialValidationStatus.Active, loaded.Status);
            Assert.AreEqual(started.State!.StartedAtUtc, loaded.State!.StartedAtUtc);
            Assert.AreEqual(TimeSpan.FromDays(6), loaded.Remaining);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void MissingAndCorruptedState_ReturnTypedRecoverableResults()
    {
        WithTrial((service, store, clock, root) =>
        {
            service.Start("Example.Product");
            Assert.IsTrue(store.Delete("Example.Product", "trial-state"));
            Assert.AreEqual(
                TrialValidationStatus.NotStarted,
                service.Get("Example.Product").Status);

            Assert.AreEqual(
                ProtectedDataWriteStatus.Success,
                store.Write(
                    "Example.Product",
                    "trial-state",
                    new byte[] { 0x01, 0x02, 0x03 }).Status);
            Assert.AreEqual(
                TrialValidationStatus.Corrupted,
                service.Get("Example.Product").Status);
        });
    }

    [TestMethod]
    public void Conversion_PreservesTrialHistoryAndIsIdempotent()
    {
        WithTrial((service, store, clock, root) =>
        {
            TrialValidationResult started = service.Start("Example.Product");
            clock.UtcNow = InitialTime.AddDays(2);
            TrialValidationResult validated = service.Validate("Example.Product");
            clock.UtcNow = InitialTime.AddDays(3);

            TrialValidationResult converted =
                service.MarkConvertedToLicense("Example.Product");
            TrialValidationResult repeated =
                service.MarkConvertedToLicense("Example.Product");
            TrialValidationResult afterConversion = service.Validate("Example.Product");

            Assert.AreEqual(
                TrialValidationStatus.ConvertedToLicense,
                converted.Status);
            Assert.AreEqual(
                TrialValidationStatus.ConvertedToLicense,
                repeated.Status);
            Assert.AreEqual(
                TrialValidationStatus.ConvertedToLicense,
                afterConversion.Status);
            Assert.AreEqual(started.State!.StartedAtUtc, converted.State!.StartedAtUtc);
            Assert.AreEqual(started.State.ExpiresAtUtc, converted.State.ExpiresAtUtc);
            Assert.AreEqual(
                validated.State!.LastSuccessfulRunUtc,
                converted.State.LastSuccessfulRunUtc);
            Assert.AreEqual(InitialTime.AddDays(3), converted.State.ConvertedToLicenseAtUtc);
        });
    }

    [TestMethod]
    public void ConcurrentStart_CreatesOnlyOneTrial()
    {
        WithTrial((service, store, clock, root) =>
        {
            Task<TrialValidationResult>[] starts = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(() => service.Start("Example.Product")))
                .ToArray();

            Task.WaitAll(starts);
            TrialValidationResult[] results = starts.Select(item => item.Result).ToArray();

            Assert.AreEqual(
                1,
                results.Count(result => result.Status == TrialValidationStatus.Active));
            Assert.AreEqual(
                7,
                results.Count(result =>
                    result.Status == TrialValidationStatus.AlreadyStarted));
            Assert.AreEqual(
                1,
                results.Select(result => result.State!.StartedAtUtc).Distinct().Count());
        });
    }

    [TestMethod]
    public void Start_ReportsUnavailableStorage()
    {
        string root = CreateTemporaryRoot();
        Directory.CreateDirectory(Path.GetDirectoryName(root)!);
        File.WriteAllText(root, "not-a-directory");

        try
        {
            var clock = new FakeClock(InitialTime);
            ProtectedTrialService service = CreateService(root, clock);

            TrialValidationResult result = service.Start("Example.Product");

            Assert.AreEqual(TrialValidationStatus.StorageUnavailable, result.Status);
        }
        finally
        {
            if (File.Exists(root))
            {
                File.Delete(root);
            }
        }
    }

    [TestMethod]
    public void Options_RejectInvalidDurationAndTolerance()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new TrialOptions(TimeSpan.Zero));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new TrialOptions(TimeSpan.FromDays(1), TimeSpan.FromSeconds(-1)));
    }

    private static ProtectedTrialService CreateService(
        string root,
        FakeClock clock,
        TrialOptions? options = null)
    {
        var store = new FileProtectedDataStore(root, new PassthroughProtector());
        return new ProtectedTrialService(store, options, clock);
    }

    private static void WithTrial(
        Action<ProtectedTrialService, FileProtectedDataStore, FakeClock, string> test,
        TrialOptions? options = null)
    {
        string root = CreateTemporaryRoot();
        var clock = new FakeClock(InitialTime);
        var store = new FileProtectedDataStore(root, new PassthroughProtector());
        var service = new ProtectedTrialService(store, options, clock);

        try
        {
            test(service, store, clock, root);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static string CreateTemporaryRoot()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "MySerialKey.GiqhDev.TrialTests",
            Guid.NewGuid().ToString("N"));
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class PassthroughProtector : IDataProtector
    {
        public byte[] Protect(byte[] plaintext, string purpose)
        {
            return (byte[])plaintext.Clone();
        }

        public byte[] Unprotect(byte[] protectedData, string purpose)
        {
            return (byte[])protectedData.Clone();
        }
    }
}

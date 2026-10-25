using System.Diagnostics;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] ReserveCrashVisibleTables =
        ["campaigns", "accepted_steps", "reserve_rounds", "cycle_rounds", "owner_actions"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReserveCrashAtFallbackBoundaryRecoversAtomically(bool afterCommit)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        var (request, _) = PrepareThirdCycleReserve(journal, clock, original.Path);
        var campaignId = request.CampaignId;
        var before = journal.ReadVerified(campaignId);
        var version = CampaignExercises.QueryCheckpoint(before).StateVersion;
        var (_, deadline) = FreshProcessRoundClock(original.Path, "reserve_rounds", campaignId);
        var saves = new List<JournalFixture>();
        try
        {
            var expectedDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var trace = new ReserveCrashTraceClock(deadline);
            var expected = expectedDisk.Open(trace);
            trace.Arm();
            Assert.True(expected.TickReserveRound(campaignId));
            var (cycleOpened, _) = FreshProcessRoundClock(expectedDisk.Path, "cycle_rounds", campaignId);
            // The unique persisted Cycle entry time identifies the exact clock
            // read after Reserve event + timed-out row writes, before Commit.
            var cycleRead = Assert.Single(trace.Reads, item => item.Observed == cycleOpened);
            Assert.True(cycleRead.Index > 1);
            using var expectedTimeout = ReserveTimeoutEvent(expectedDisk.Path, campaignId);
            Assert.Equal(deadline, expectedTimeout.RootElement.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            Assert.Equal(3, expectedTimeout.RootElement.GetProperty("cycleOrdinal").GetInt32());
            Assert.Empty(expectedTimeout.RootElement.GetProperty("convertedElementIds").EnumerateArray());
            Assert.Equal("axis-infantry", Assert.Single(expectedTimeout.RootElement.GetProperty("retainedElementIds").EnumerateArray()).GetString());

            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            await KillReserveCrashChild(disk.Path, campaignId, deadline, cycleRead.Index, cycleOpened, afterCommit,
                () =>
                {
                    // Read-only SQL avoids contending for the paused writer's lock.
                    foreach (var table in ReserveCrashVisibleTables)
                        Assert.Equal(IntegratedCampaignRows(afterCommit ? expectedDisk.Path : original.Path, table),
                            IntegratedCampaignRows(disk.Path, table));
                });
            // Kill returned only after abrupt OS termination, not managed cleanup.
            IntegratedCampaignCompare(afterCommit ? expected : journal,
                afterCommit ? expectedDisk.Path : original.Path, disk.Open(clock), disk.Path, campaignId);
            var recovered = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-reserve-crash-probe");
            Assert.NotEqual(Environment.ProcessId, recovered.ProcessId);
            Assert.True(recovered.IsVerified);
            Assert.Equal(afterCommit ? version + 1 : version, recovered.BeforeStateVersion);
            Assert.Equal(version + 1, recovered.AfterStateVersion);
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
            var replay = CampaignExercises.Reconstruct(expected.ReadVerified(campaignId));
            Assert.Equal(replay.EventStreamHash, recovered.EventStreamHash);
            Assert.Equal(replay.ReconstructedSnapshotHash, recovered.ReconstructedSnapshotHash);
            var rows = IntegratedCampaignRows(disk.Path, "accepted_steps");
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline, "--native-reserve-crash-probe");
            Assert.True(retry.IsVerified);
            Assert.Equal(recovered.AfterStateVersion, retry.BeforeStateVersion);
            Assert.Equal(recovered.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(recovered.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(rows, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            Assert.Equal(ThirdCycleOwnerChoicesExpected, ThirdCycleOwnerChoices(disk.Path));
            IntegratedCampaignCompare(expected, expectedDisk.Path, disk.Open(clock), disk.Path, campaignId);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }

    private sealed class ReserveCrashTraceClock(long now) : TimeProvider
    {
        private bool armed;
        public List<(int Index, long Observed)> Reads { get; } = [];
        public void Arm() { Reads.Clear(); armed = true; }
        public override DateTimeOffset GetUtcNow()
        {
            if (!armed) return DateTimeOffset.FromUnixTimeMilliseconds(now);
            if (Reads.Count >= 32) throw new InvalidOperationException("Reserve trace exceeded bound.");
            var observed = checked(now + Reads.Count);
            Reads.Add((Reads.Count + 1, observed));
            return DateTimeOffset.FromUnixTimeMilliseconds(observed);
        }
    }

    private static async Task KillReserveCrashChild(string path, string campaignId, long now,
        int cycleRead, long cycleOpened, bool afterCommit, Action inspectWhilePaused)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child));
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add("--native-reserve-crash-probe");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start crash child.");
        var errors = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new
            {
                Path = path,
                CampaignId = campaignId,
                NowUnixMilliseconds = now,
                StopAtRead = afterCommit ? 0 : cycleRead,
                StopAfterCommit = afterCommit
            }));
            await process.StandardInput.FlushAsync();
            var line = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(120));
            Assert.NotNull(line);
            using var signal = JsonDocument.Parse(line);
            Assert.Equal(afterCommit ? "after-commit" : "clock-read", signal.RootElement.GetProperty("Phase").GetString());
            if (!afterCommit)
            {
                Assert.Equal(cycleRead, signal.RootElement.GetProperty("ReadIndex").GetInt32());
                Assert.Equal(cycleOpened, signal.RootElement.GetProperty("Observed").GetInt64());
            }
            Assert.False(process.HasExited);
            inspectWhilePaused();
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEqual(0, process.ExitCode);
            Assert.Empty(await process.StandardOutput.ReadToEndAsync());
            Assert.Empty(await errors);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
        }
    }
}

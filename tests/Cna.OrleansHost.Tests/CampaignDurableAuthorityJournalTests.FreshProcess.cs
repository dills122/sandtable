using System.Diagnostics;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData("retreat", "retreat_rounds", CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind)]
    [InlineData("selection", "selection_rounds", CampaignHostAcceptedStep.NativeCombatSelectionTimeoutKind)]
    [InlineData("rba", "rba_rounds", CampaignHostAcceptedStep.NativeCombatRbaCancellationKind)]
    [InlineData("force", "force_rounds", CampaignHostAcceptedStep.NativeCombatForceCancellationKind)]
    public async Task FreshProcessNativeDeadlineRecoveryMatchesInProcessAndIsIdempotent(
        string cut, string table, string timeoutKind)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var control = original.Create(clock);
        var campaignId = FreshProcessPreparePendingCut(control, clock, cut);
        var saves = new List<JournalFixture>();
        try
        {
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var initial = control.ReadVerified(campaignId);
            var initialVersion = CampaignExercises.QueryCheckpoint(initial).StateVersion;
            var initialReplay = CampaignExercises.Reconstruct(initial);
            var (opened, deadline) = FreshProcessRoundClock(original.Path, table, campaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            SweepControl();
            var early = await FreshProcessRunDeadlineChild(disk.Path, campaignId, clock.GetUtcNow().ToUnixTimeMilliseconds());
            Assert.Equal(initialVersion, early.BeforeStateVersion);
            Assert.Equal(initialVersion, early.AfterStateVersion);
            Assert.Equal(initialReplay.EventStreamHash, early.EventStreamHash);
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, table, campaignId));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, timeoutKind));
            CompareChild(early);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            SweepControl();
            var expired = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(initialVersion, expired.BeforeStateVersion);
            Assert.True(expired.AfterStateVersion > initialVersion);
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, timeoutKind));
            CompareChild(expired);

            var beforeRetry = IntegratedCampaignRows(disk.Path, "accepted_steps");
            SweepControl();
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(expired.AfterStateVersion, retry.BeforeStateVersion);
            Assert.Equal(expired.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(expired.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(beforeRetry, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, timeoutKind));
            CompareChild(retry);

            void SweepControl() => new CampaignCombatTimeoutService(control, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);

            void CompareChild(NativeChildResult result)
            {
                Assert.True(result.IsVerified);
                Assert.NotEqual(Environment.ProcessId, result.ProcessId);
                var resumed = disk.Open(clock);
                IntegratedCampaignCompare(control, original.Path, resumed, disk.Path, campaignId);
                var replay = CampaignExercises.Reconstruct(control.ReadVerified(campaignId));
                Assert.Equal(replay.EventStreamHash, result.EventStreamHash);
                Assert.Equal(replay.ReconstructedSnapshotHash, result.ReconstructedSnapshotHash);
                Assert.Equal(CampaignExercises.QueryCheckpoint(control.ReadVerified(campaignId)).ContractVersion,
                    result.ContractVersion);
            }
        }
        finally
        {
            foreach (var save in saves) save.Dispose();
        }
    }

    private static string FreshProcessPreparePendingCut(CampaignDurableAuthorityJournal journal,
        ManualClock clock, string cut)
    {
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 6,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(journal.ReadVerified(request.CampaignId)).ContractVersion);
        if (cut == "retreat") return request.CampaignId;
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickRetreatRound(request.CampaignId));
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
        if (cut == "selection") return request.CampaignId;
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-second");
        using var visible = JsonDocument.Parse(selection.VisibleBytes);
        var offers = visible.RootElement.GetProperty("assaultOffers");
        var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
            offers[index].GetProperty("apparentLocationId").GetString() == "west");
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second",
            selection.Revision, selection.PublicActionRefs[west]).IsAccepted);
        if (cut == "rba") return request.CampaignId;
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender, rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        var force = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second", force.Revision, force.PublicActionRefs[0]).IsAccepted);
        return request.CampaignId;
    }

    private static (long Opened, long Deadline) FreshProcessRoundClock(string path, string table, string campaignId)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = $"SELECT opened_ms,deadline_ms FROM {table} WHERE campaign_id=$id AND status='collecting'";
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        Assert.True(reader.Read());
        var result = (reader.GetInt64(0), reader.GetInt64(1));
        Assert.False(reader.Read());
        return result;
    }

    private static async Task<NativeChildResult> FreshProcessRunDeadlineChild(string path, string campaignId, long now,
        string mode = "--native-campaign-deadline")
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Name;
        var child = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..",
            "Cna.OrleansHost.FoundationChild", configuration, "Cna.OrleansHost.FoundationChild.dll"));
        Assert.True(File.Exists(child), $"Built native child missing: {child}");
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(child);
        start.ArgumentList.Add(mode);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start native recovery child.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new { Path = path, CampaignId = campaignId, NowUnixMilliseconds = now }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                using var killTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await process.WaitForExitAsync(killTimeout.Token);
                throw new TimeoutException($"Native recovery child exceeded 120 seconds. stdout={await stdout}; stderr={await stderr}");
            }
            var output = await stdout;
            var errors = await stderr;
            Assert.True(process.ExitCode == 0, $"Child exit {process.ExitCode}; stdout={output}; stderr={errors}");
            var result = JsonSerializer.Deserialize<NativeChildResult>(output)
                ?? throw new InvalidOperationException("Native recovery child returned no result.");
            Assert.Equal(process.Id, result.ProcessId);
            return result;
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    private sealed record NativeChildResult(int ProcessId, long BeforeStateVersion, long AfterStateVersion,
        int ContractVersion, bool IsVerified, string EventStreamHash, string ReconstructedSnapshotHash);
}

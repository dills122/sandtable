using System.Globalization;
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
    [InlineData("paid-assault")]
    [InlineData("selection-timeout")]
    [InlineData("rba-timeout")]
    [InlineData("force-timeout")]
    public void IntegratedCampaignDurableSaveResumeMatchesUninterruptedAuthority(string secondCycle)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var uninterrupted = original.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(uninterrupted, positive: true, seed: 6,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(uninterrupted.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(uninterrupted.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(uninterrupted.ReadVerified(request.CampaignId)).ContractVersion);
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId,
            CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId,
            CampaignHostAcceptedStep.NativeCombatAssaultResultKind));

        var saved = new List<JournalFixture>();
        try
        {
            var resumedFixture = IntegratedCampaignSave(original.Path, clock, saved);
            var resumed = resumedFixture.Open(clock);
            var saveCount = 1;
            Compare();
            clock.Advance(TimeSpan.FromSeconds(30));
            InBoth(journal => new CampaignCombatTimeoutService(journal, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None));
            SaveAndResume(); // Reserve choice remains pending in copied authority.
            InBoth(journal =>
            {
                var opening = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
                Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
                    opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
            });
            SaveAndResume(); // Cycle control entry retains its original deadline.
            InBoth(journal =>
            {
                var opening = journal.QueryCycleControlOwnerOpening(attacker);
                Assert.True(journal.SubmitOwnerCycleControl(attacker,
                    opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
                var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
                Assert.True(journal.SubmitOwnerRepeatMovement(attacker,
                    movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
                Assert.Contains(request.CampaignId, journal.CollectingSelectionCampaigns("", 16));
            });
            SaveAndResume(); // Genuine repeated Breakdown has entered Selection.
            if (secondCycle == "selection-timeout")
            {
                clock.Advance(TimeSpan.FromSeconds(30));
                InBoth(journal => Assert.True(journal.TickSelectionRound(request.CampaignId)));
            }
            else
            {
                InBoth(journal =>
                {
                    var opening = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-second");
                    using var visible = JsonDocument.Parse(opening.VisibleBytes);
                    var offers = visible.RootElement.GetProperty("assaultOffers");
                    var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                        offers[index].GetProperty("apparentLocationId").GetString() == "west");
                    Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second",
                        opening.Revision, opening.PublicActionRefs[west]).IsAccepted);
                });
                SaveAndResume(); // Repeated RBA precedes any new Force payment.
                if (secondCycle == "rba-timeout")
                {
                    clock.Advance(TimeSpan.FromSeconds(30));
                    InBoth(journal => Assert.True(journal.TickRbaRound(request.CampaignId)));
                }
                else
                {
                    var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
                        uninterrupted.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
                    InBoth(journal =>
                    {
                        var opening = journal.QueryRbaOwnerOpening(defender, westDefender);
                        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender,
                            opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
                        var force = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
                        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second",
                            force.Revision, force.PublicActionRefs[0]).IsAccepted);
                    });
                    SaveAndResume(); // A single retained Force seal must survive backup.
                    if (secondCycle == "force-timeout")
                    {
                        clock.Advance(TimeSpan.FromSeconds(30));
                        InBoth(journal => Assert.True(journal.TickForceRound(request.CampaignId)));
                    }
                    else
                    {
                        InBoth(journal =>
                        {
                            var force = journal.QueryForceOwnerOpening(defender, westDefender);
                            Assert.True(journal.SubmitOwnerForceSeal(defender, westDefender,
                                force.Revision, force.PublicActionRefs[0]).IsAccepted);
                        });
                        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId,
                            CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
                        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId,
                            CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
                    }
                }
            }

            SaveAndResume(); // No branch invents a third-cycle eligibility claim.
            Assert.Equal(27, CampaignExercises.QueryCheckpoint(resumed.ReadVerified(request.CampaignId)).ContractVersion);
            using var reserveBeforeFinish = IntegratedCampaignRecord(original.Path,
                CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind, "snapshot_checkpoint");
            var worldBeforeFinish = reserveBeforeFinish.RootElement.GetProperty("worldHash").GetString();
            clock.Advance(TimeSpan.FromSeconds(30));
            InBoth(journal => new CampaignCombatTimeoutService(journal, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None));
            SaveAndResume();
            Assert.Equal(34, CampaignExercises.QueryCheckpoint(resumed.ReadVerified(request.CampaignId)).ContractVersion);
            Assert.False(resumed.TickCycleRound(request.CampaignId));
            using var cycleFinish = IntegratedCampaignRecord(resumedFixture.Path,
                CampaignHostAcceptedStep.NativeCycleControlTimeoutKind, "canonical_event");
            Assert.Equal(worldBeforeFinish, cycleFinish.RootElement.GetProperty("resultWorldHash").GetString());
            var expectedPaid = secondCycle == "paid-assault" ? 2 : 1;
            Assert.Equal(expectedPaid, CountAcceptedStep(resumedFixture.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
            Assert.Equal(expectedPaid, CountAcceptedStep(resumedFixture.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            using var paidResult = IntegratedCampaignRecord(resumedFixture.Path,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            using var successor = IntegratedCampaignRecord(resumedFixture.Path,
                CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.True(paidResult.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64()
                > paidResult.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(paidResult.RootElement.GetProperty("afterRandomState").GetRawText(),
                successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.True(saveCount >= 6);
            Assert.Equal(1, CountAcceptedStep(resumedFixture.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind));
            Assert.Equal(1, CountAcceptedStep(resumedFixture.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCycleControlTimeoutKind));
            Compare();

            void InBoth(Action<CampaignDurableAuthorityJournal> action)
            {
                action(uninterrupted);
                action(resumed);
                Compare();
            }

            void SaveAndResume()
            {
                Compare();
                resumedFixture = IntegratedCampaignSave(resumedFixture.Path, clock, saved);
                resumed = resumedFixture.Open(clock);
                saveCount++;
                Compare();
            }

            void Compare() => IntegratedCampaignCompare(uninterrupted, original.Path,
                resumed, resumedFixture.Path, request.CampaignId);
        }
        finally
        {
            foreach (var fixture in saved) fixture.Dispose();
        }
    }

    private static JournalFixture IntegratedCampaignSave(string sourcePath, TimeProvider clock,
        List<JournalFixture> retained)
    {
        var destination = new JournalFixture();
        retained.Add(destination);
        _ = destination.Create(clock);
        // Journal methods have disposed their connections. SQLite backup copies a
        // coherent database including committed WAL pages; then both handles close
        // before the saved copy is opened as resumed authority. Same process only.
        using (var source = new SqliteConnection($"Data Source={sourcePath};Pooling=False"))
        using (var target = new SqliteConnection($"Data Source={destination.Path};Pooling=False"))
        {
            source.Open();
            target.Open();
            source.BackupDatabase(target);
        }
        return destination;
    }

    private static void IntegratedCampaignCompare(CampaignDurableAuthorityJournal left, string leftPath,
        CampaignDurableAuthorityJournal right, string rightPath, string campaignId)
    {
        var leftSession = left.ReadVerified(campaignId);
        var rightSession = right.ReadVerified(campaignId);
        var leftCheckpoint = CampaignExercises.QueryCheckpoint(leftSession);
        var rightCheckpoint = CampaignExercises.QueryCheckpoint(rightSession);
        Assert.Equal(leftCheckpoint.ContractVersion, rightCheckpoint.ContractVersion);
        Assert.Equal(leftCheckpoint.StateVersion, rightCheckpoint.StateVersion);
        Assert.Equal(leftCheckpoint.PositionId, rightCheckpoint.PositionId);
        var leftReplay = CampaignExercises.Reconstruct(leftSession);
        var rightReplay = CampaignExercises.Reconstruct(rightSession);
        Assert.True(leftReplay.IsVerified);
        Assert.True(rightReplay.IsVerified);
        Assert.Equal(leftReplay.EventStreamHash, rightReplay.EventStreamHash);
        Assert.Equal(leftReplay.ExpectedSnapshotHash, rightReplay.ExpectedSnapshotHash);
        Assert.Equal(leftReplay.ReconstructedSnapshotHash, rightReplay.ReconstructedSnapshotHash);
        // Terminal canonical snapshots can be compact events. Full accepted history
        // below also binds World/resource evidence and paid RNG state.
        Assert.Equal(leftReplay.ReconstructedSnapshotBytes, rightReplay.ReconstructedSnapshotBytes);
        var leftContacts = CampaignHostAdapter.QueryContactRecords(leftSession);
        var rightContacts = CampaignHostAdapter.QueryContactRecords(rightSession);
        Assert.Equal(leftContacts.Count, rightContacts.Count);
        for (var index = 0; index < leftContacts.Count; index++)
            Assert.Equal(leftContacts[index], rightContacts[index]);
        foreach (var table in new[] { "campaigns", "accepted_steps", "owner_openings", "owner_actions",
            "seats", "reference_key", "operator_capability",
            "selection_rounds", "rba_rounds", "force_rounds", "retreat_rounds",
            "custody_rounds", "reserve_rounds", "cycle_rounds" })
            Assert.Equal(IntegratedCampaignRows(leftPath, table), IntegratedCampaignRows(rightPath, table));
    }

    private static JsonDocument IntegratedCampaignRecord(string path, string kind, string column)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM accepted_steps WHERE action_kind=$kind ORDER BY sequence DESC LIMIT 1";
        command.Parameters.AddWithValue("$kind", kind);
        return JsonDocument.Parse((byte[])command.ExecuteScalar()!);
    }

    private static string[] IntegratedCampaignRows(string path, string table)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            var values = new string[reader.FieldCount];
            for (var index = 0; index < values.Length; index++)
                values[index] = reader.GetValue(index) switch
                {
                    DBNull => "null",
                    byte[] bytes => "blob:" + Convert.ToHexString(bytes),
                    string text => "text:" + JsonSerializer.Serialize(text),
                    var value => "number:" + Convert.ToString(value, CultureInfo.InvariantCulture)
                };
            rows.Add(JsonSerializer.Serialize(values));
        }
        return rows.ToArray();
    }
}

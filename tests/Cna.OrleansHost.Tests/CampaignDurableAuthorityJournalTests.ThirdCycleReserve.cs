using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] ThirdCycleRollbackTables =
        ["campaigns", "accepted_steps", "reserve_rounds", "cycle_rounds"];

    private static readonly string[] ThirdCycleOwnerChoicesExpected =
        ["axis-infantry:ConvertToII", "axis-infantry-second:ReleaseI",
            "axis-infantry:RetainII", "axis-infantry-third:ReleaseII"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThirdCycleReserveTimeoutRetainsConvertedReserveAfterActualHistory(bool injectCycleFailure)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        var (request, owner) = PrepareThirdCycleReserve(journal, clock, original.Path);
        var before = journal.ReadVerified(request.CampaignId);
        using var pending = JsonDocument.Parse(CampaignExercises.Reconstruct(before).ReconstructedSnapshotBytes!);
        Assert.Equal(3, pending.RootElement.GetProperty("cycleOrdinal").GetInt32());
        Assert.Contains(request.CampaignId, journal.CollectingReserveCampaigns("", 16));
        var beforeWorld = pending.RootElement.GetProperty("worldHash").GetString();
        var ownerActionsBefore = IntegratedCampaignRows(original.Path, "owner_actions");
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeRepeatBreakdownKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        using var successor = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
        var paidRandom = paid.RootElement.GetProperty("afterRandomState").GetRawText();
        Assert.Equal(paidRandom, successor.RootElement.GetProperty("randomState").GetRawText());

        var saves = new List<JournalFixture>();
        try
        {
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var cold = disk.Open(clock);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "reserve_rounds", request.CampaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            Assert.False(cold.TickReserveRound(request.CampaignId));
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "reserve_rounds", request.CampaignId));
            clock.Advance(TimeSpan.FromMilliseconds(1));
            if (injectCycleFailure)
            {
                var retained = ThirdCycleRollbackTables
                    .ToDictionary(table => table, table => IntegratedCampaignRows(disk.Path, table));
                ThirdCycleReserveSql(disk.Path, "CREATE TRIGGER fail_third_cycle BEFORE INSERT ON cycle_rounds BEGIN SELECT RAISE(ABORT,'injected third cycle failure'); END");
                Assert.Throws<SqliteException>(() => cold.TickReserveRound(request.CampaignId));
                foreach (var (table, rows) in retained)
                    Assert.Equal(rows, IntegratedCampaignRows(disk.Path, table));
                Assert.True(CampaignExercises.Reconstruct(disk.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
                ThirdCycleReserveSql(disk.Path, "DROP TRIGGER fail_third_cycle");
            }
            new CampaignCombatTimeoutService(cold, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            var after = disk.Open(clock).ReadVerified(request.CampaignId);
            Assert.Equal(CampaignExercises.QueryCheckpoint(before).StateVersion + 1,
                CampaignExercises.QueryCheckpoint(after).StateVersion);
            Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
            Assert.False(cold.TickReserveRound(request.CampaignId));
            Assert.Contains(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
            using var timeout = ReserveTimeoutEvent(disk.Path, request.CampaignId);
            var root = timeout.RootElement;
            Assert.Equal(3, root.GetProperty("cycleOrdinal").GetInt32());
            Assert.Empty(root.GetProperty("convertedElementIds").EnumerateArray());
            Assert.Equal("axis-infantry", Assert.Single(root.GetProperty("retainedElementIds").EnumerateArray()).GetString());
            Assert.Equal(beforeWorld, root.GetProperty("resultWorldHash").GetString());
            Assert.Equal(opened, root.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, root.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, root.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            using var afterSnapshot = IntegratedCampaignRecord(disk.Path,
                CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind, "snapshot_checkpoint");
            Assert.True(afterSnapshot.RootElement.GetProperty("completed").GetBoolean());
            Assert.Equal(3, afterSnapshot.RootElement.GetProperty("cycleOrdinal").GetInt32());
            Assert.Equal(beforeWorld, afterSnapshot.RootElement.GetProperty("worldHash").GetString());
            Assert.Empty(afterSnapshot.RootElement.GetProperty("nextMovement").EnumerateArray());
            Assert.Equal(ownerActionsBefore, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ThirdCycleOwnerChoicesExpected, ThirdCycleOwnerChoices(disk.Path));
            Assert.Equal(4, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind));
            // Native terminal checkpoints do not export a new RNG object. The
            // unchanged paid event plus verified full replay is the no-extra-roll witness.
            var cycle = cold.QueryCycleControlOwnerOpening(owner);
            Assert.Single(cycle.PublicActionRefs);
            Assert.True(cold.SubmitOwnerCycleControl(owner, cycle.Revision, cycle.PublicActionRefs[^1]).IsAccepted);
            Assert.Equal(28, CampaignExercises.QueryCheckpoint(disk.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
            Assert.True(CampaignExercises.Reconstruct(disk.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        }
        finally
        {
            foreach (var save in saves) save.Dispose();
        }
    }

    private static (CampaignCreationRequest Request, CampaignSeatCredential Owner) PrepareThirdCycleReserve(
        CampaignDurableAuthorityJournal journal, ManualClock clock, string path)
    {
        var (request, attacker, defender, defenderId, attack, defend) = PrepareInfantryForceRound(
            journal, positive: true, seed: 12345, tripleContact: true,
            reserveElementId: "axis-infantry-second", secondReserveElementId: "axis-infantry-third",
            thirdReserveElementId: "axis-infantry");
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry", attack.Revision, attack.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId, defend.Revision, defend.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);
        var initialCut = journal.ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.QueryCheckpoint(initialCut).ContractVersion == 26,
            System.Text.Encoding.UTF8.GetString(CampaignExercises.Reconstruct(initialCut).ReconstructedSnapshotBytes!));
        // Owner offers are ordered. Convert the earlier member before releasing
        // the second; the deadline then converts the unresolved third member.
        var firstReserve = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            firstReserve.Revision, firstReserve.PublicActionRefs[^1]).IsAccepted);
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        var releasedSecond = journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second", release.Revision, release.PublicActionRefs[0]);
        Assert.True(releasedSecond.IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickReserveRound(request.CampaignId));
        using (var initial = ReserveTimeoutEvent(path, request.CampaignId))
        {
            Assert.Equal(1, initial.RootElement.GetProperty("cycleOrdinal").GetInt32());
            Assert.Equal("axis-infantry-third", Assert.Single(initial.RootElement
                .GetProperty("convertedElementIds").EnumerateArray()).GetString());
            Assert.Empty(initial.RootElement.GetProperty("retainedElementIds").EnumerateArray());
        }
        var retry = journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second", release.Revision, release.PublicActionRefs[0]);
        Assert.Equal(releasedSecond.CanonicalReceipt, retry.CanonicalReceipt);
        Repeat(1);
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-second");
        using (var visible = JsonDocument.Parse(selection.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "west");
            Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second", selection.Revision, selection.PublicActionRefs[west]).IsAccepted);
        }
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender, rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        attack = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        defend = journal.QueryForceOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second", attack.Revision, attack.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, westDefender, defend.Revision, defend.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, westDefender);
        var retainedFirst = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry",
            retainedFirst.Revision, retainedFirst.PublicActionRefs[^1]).IsAccepted);
        release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-third");
        var releasedThird = journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-third", release.Revision, release.PublicActionRefs[0]);
        Assert.True(releasedThird.IsAccepted);
        using (var second = IntegratedCampaignRecord(path,
            CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind, "snapshot_checkpoint"))
        {
            Assert.Equal(2, second.RootElement.GetProperty("cycleOrdinal").GetInt32());
            Assert.True(second.RootElement.GetProperty("completed").GetBoolean());
        }
        retry = journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-third", release.Revision, release.PublicActionRefs[0]);
        Assert.Equal(releasedThird.CanonicalReceipt, retry.CanonicalReceipt);
        Repeat(0);
        var third = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-third");
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-third", third.Revision, third.PublicActionRefs[^1]).IsAccepted);
        // No owner Reserve query occurs in the actual third window.
        return (request, attacker);

        void Repeat(int movementChoice)
        {
            var control = journal.QueryCycleControlOwnerOpening(attacker);
            Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision, control.PublicActionRefs[0]).IsAccepted);
            var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
            Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision, movement.PublicActionRefs[movementChoice]).IsAccepted);
        }
    }

    private static string[] ThirdCycleOwnerChoices(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        connection.Open();
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT canonical_event FROM accepted_steps WHERE action_kind='contact-reserve-release-choice' ORDER BY sequence";
        using var reader = query.ExecuteReader();
        var choices = new List<string>();
        while (reader.Read())
        {
            using var item = JsonDocument.Parse((byte[])reader.GetValue(0));
            choices.Add(item.RootElement.GetProperty("elementId").GetString() + ":"
                + item.RootElement.GetProperty("choice").GetString());
        }
        return choices.ToArray();
    }

    private static void ThirdCycleReserveSql(string path, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

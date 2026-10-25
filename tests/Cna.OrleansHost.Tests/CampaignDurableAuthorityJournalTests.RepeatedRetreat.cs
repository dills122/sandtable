using System.Text;
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
    private static readonly int[] RepeatedRetreatFirstDice = [6, 3, 4, 5, 2, 5, 4, 5];
    private static readonly int[] RepeatedRetreatSecondDice = [5, 3, 4, 5, 5, 1, 4, 1];
    private static readonly string[] RepeatedRetreatRollbackTables =
        ["campaigns", "accepted_steps", "retreat_rounds", "custody_rounds", "reserve_rounds", "cycle_rounds", "owner_actions"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedRetreatTimeoutRefusesActualSecondRequiredRetreat(bool injectSuccessorFailure)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        // Fixed native regression fixture chosen by bounded offline RNG/table
        // calculation; actual accepted results below independently bind its draws.
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 4,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);
        Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)));
        using (var first = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event"))
        {
            Assert.Equal(2UL, first.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(10UL, first.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(RepeatedRetreatFirstDice, first.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        }
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker, control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker, movement.Revision, movement.PublicActionRefs[1]).IsAccepted);
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry-second");
        using (var visible = JsonDocument.Parse(selection.VisibleBytes))
        {
            var offers = visible.RootElement.GetProperty("assaultOffers");
            var west = Enumerable.Range(0, offers.GetArrayLength()).Single(index =>
                offers[index].GetProperty("apparentLocationId").GetString() == "west");
            Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry-second",
                selection.Revision, selection.PublicActionRefs[west]).IsAccepted);
        }
        var westDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, westDefender, rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);
        var attack = journal.QueryForceOwnerOpening(attacker, "axis-infantry-second");
        var defend = journal.QueryForceOwnerOpening(defender, westDefender);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry-second", attack.Revision, attack.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, westDefender, defend.Revision, defend.PublicActionRefs[0]).IsAccepted);
        var pending = journal.ReadVerified(request.CampaignId);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(pending).ContractVersion);
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeRepeatBreakdownKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(10UL, paid.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(18UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(RepeatedRetreatSecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal(1, paid.RootElement.GetProperty("facts").GetProperty("requiredRetreat").GetInt32());
        Assert.Equal(5, paid.RootElement.GetProperty("facts").GetProperty("defenderPercent").GetInt32());
        Assert.Equal(JsonValueKind.Null, paid.RootElement.GetProperty("facts").GetProperty("capturedRole").ValueKind);
        Assert.Equal("commonwealth-infantry-west", paid.RootElement.GetProperty("defenderElementId").GetString());
        var resultHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(paid.RootElement.GetRawText()));
        Assert.Equal(resultHash, CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(pending).ResultHash);
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var saves = new List<JournalFixture>();
        try
        {
            // An independent saved branch exercises actual legal owner refusal.
            // Only this branch queries the Retreat owner; expiry branch never does.
            var ownerDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var ownerJournal = ownerDisk.Open(clock);
            var refusal = ownerJournal.QueryPostAssaultRetreatOwnerOpening(defender, westDefender);
            Assert.True(ownerJournal.SubmitOwnerPostAssaultRetreat(defender, westDefender,
                refusal.Revision, refusal.PublicActionRefs[^1]).IsAccepted);
            using var ownerSettlement = IntegratedCampaignRecord(ownerDisk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var cold = disk.Open(clock);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "retreat_rounds", request.CampaignId);
            var acceptedBefore = IntegratedCampaignRows(disk.Path, "accepted_steps");
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            Assert.False(cold.TickRetreatRound(request.CampaignId));
            Assert.Equal(acceptedBefore, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "retreat_rounds", request.CampaignId));
            clock.Advance(TimeSpan.FromMilliseconds(1));
            if (injectSuccessorFailure)
            {
                var retained = RepeatedRetreatRollbackTables.ToDictionary(table => table,
                    table => IntegratedCampaignRows(disk.Path, table));
                ThirdCycleReserveSql(disk.Path, "CREATE TRIGGER fail_repeated_retreat BEFORE INSERT ON cycle_rounds BEGIN SELECT RAISE(ABORT,'injected repeated retreat successor failure'); END");
                Assert.Throws<SqliteException>(() => cold.TickRetreatRound(request.CampaignId));
                foreach (var (table, rows) in retained) Assert.Equal(rows, IntegratedCampaignRows(disk.Path, table));
                Assert.Equal(22, CampaignExercises.QueryCheckpoint(disk.Open(clock).ReadVerified(request.CampaignId)).ContractVersion);
                ThirdCycleReserveSql(disk.Path, "DROP TRIGGER fail_repeated_retreat");
            }
            new CampaignCombatTimeoutService(cold, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            var after = disk.Open(clock).ReadVerified(request.CampaignId);
            Assert.NotEqual(22, CampaignExercises.QueryCheckpoint(after).ContractVersion);
            Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
            Assert.False(cold.TickRetreatRound(request.CampaignId));
            Assert.Contains(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
            using var timeout = RetreatTimeoutEvent(disk.Path, request.CampaignId);
            var result = timeout.RootElement;
            Assert.Equal(resultHash, result.GetProperty("resultHash").GetString());
            Assert.Equal("commonwealth-infantry-west", result.GetProperty("defenderElementId").GetString());
            Assert.Equal("Commonwealth", result.GetProperty("defenderSide").GetString());
            Assert.Equal(paid.RootElement.GetProperty("decisionActionId").GetString(), result.GetProperty("decisionActionId").GetString());
            Assert.Equal(paid.RootElement.GetProperty("paidWorldHash").GetString(), result.GetProperty("paidWorldHash").GetString());
            Assert.Equal("refuse-retreat", result.GetProperty("fallback").GetString());
            Assert.False(result.TryGetProperty("actionId", out _));
            Assert.False(result.TryGetProperty("destinationLocationId", out _));
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            using var settlement = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            Assert.Equal(HashRepeatedCustody(Encoding.UTF8.GetBytes(result.GetRawText())), settlement.RootElement.GetProperty("retreatHash").GetString());
            Assert.NotEqual(paid.RootElement.GetProperty("paidWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            Assert.Equal(ownerSettlement.RootElement.GetProperty("settledWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            Assert.Empty(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());
            // V24 exposes a World hash, not numeric TOE/distance. Its equality
            // with legal owner refusal plus verified replay binds source refusal
            // effects. Numeric loss/distance remain source-derived expectations,
            // not exported Host fields.
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(0, CountAcceptedStep(disk.Path, request.CampaignId, "contact-post-assault-retreat"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            var rowsAfter = IntegratedCampaignRows(disk.Path, "accepted_steps");
            new CampaignCombatTimeoutService(disk.Open(clock), clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            Assert.Equal(rowsAfter, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.True(CampaignExercises.Reconstruct(disk.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}

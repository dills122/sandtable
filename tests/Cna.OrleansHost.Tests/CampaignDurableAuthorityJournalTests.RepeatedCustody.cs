using System.Security.Cryptography;
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
    private static readonly int[] RepeatedCustodyFirstDice = [1, 6, 4, 5, 2, 2, 4, 4];
    private static readonly int[] RepeatedCustodySecondDice = [2, 5, 5, 2, 3, 4, 1, 1, 4];
    private static readonly string[] RepeatedCustodyRollbackTables =
        ["campaigns", "accepted_steps", "custody_rounds", "reserve_rounds", "cycle_rounds", "owner_actions"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedCustodyTimeoutLeavesActualSecondCapturedLotUnguarded(bool injectSuccessorFailure)
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var journal = original.Create(clock);
        // Seed50 is a fixed native regression fixture, selected by bounded offline
        // RNG/table calculation. No authority/RNG injection or public admission claim.
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 50,
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
            Assert.Equal(RepeatedCustodyFirstDice, first.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
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
        CompleteRetreatIfPending(journal, request.CampaignId, defender, westDefender);
        var pending = journal.ReadVerified(request.CampaignId);
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(pending));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, request.CampaignId, CampaignHostAcceptedStep.NativeRepeatBreakdownKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(10UL, paid.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(19UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(RepeatedCustodySecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal("defender", paid.RootElement.GetProperty("facts").GetProperty("capturedRole").GetString());
        Assert.Equal(50, paid.RootElement.GetProperty("facts").GetProperty("captureShare").GetInt32());
        using var settlement = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
        var lot = Assert.Single(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());
        Assert.Equal("axis-infantry-second", lot.GetProperty("captorElementId").GetString());
        Assert.Equal("commonwealth-infantry-west", lot.GetProperty("victimElementId").GetString());
        Assert.Equal(1, lot.GetProperty("quantity").GetInt32());
        var settlementHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(settlement.RootElement.GetRawText()));
        var lotId = "combat.captive." + settlementHash[7..];
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var saves = new List<JournalFixture>();
        try
        {
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var cold = disk.Open(clock);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "custody_rounds", request.CampaignId);
            var acceptedBefore = IntegratedCampaignRows(disk.Path, "accepted_steps");
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            Assert.False(cold.TickCustodyRound(request.CampaignId));
            Assert.Equal(acceptedBefore, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "custody_rounds", request.CampaignId));
            clock.Advance(TimeSpan.FromMilliseconds(1));
            if (injectSuccessorFailure)
            {
                var retained = RepeatedCustodyRollbackTables.ToDictionary(table => table,
                    table => IntegratedCampaignRows(disk.Path, table));
                ThirdCycleReserveSql(disk.Path, "CREATE TRIGGER fail_repeated_custody BEFORE INSERT ON cycle_rounds BEGIN SELECT RAISE(ABORT,'injected repeated custody successor failure'); END");
                Assert.Throws<SqliteException>(() => cold.TickCustodyRound(request.CampaignId));
                foreach (var (table, rows) in retained) Assert.Equal(rows, IntegratedCampaignRows(disk.Path, table));
                Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(disk.Open(clock).ReadVerified(request.CampaignId)));
                ThirdCycleReserveSql(disk.Path, "DROP TRIGGER fail_repeated_custody");
            }
            new CampaignCombatTimeoutService(cold, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            var after = disk.Open(clock).ReadVerified(request.CampaignId);
            Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(after));
            Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
            Assert.False(cold.TickCustodyRound(request.CampaignId));
            Assert.Contains(request.CampaignId, cold.CollectingCycleCampaigns("", 16));
            using var timeout = CustodyTimeoutEvent(disk.Path, request.CampaignId);
            var result = timeout.RootElement;
            Assert.Equal(settlementHash, result.GetProperty("settlementHash").GetString());
            Assert.Equal(lotId, result.GetProperty("lotId").GetString());
            Assert.Equal("leave-unguarded", result.GetProperty("fallback").GetString());
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), result.GetProperty("worldHash").GetString());
            // Explicit escaped-state hash proves leave-unguarded created no guard,
            // with delayed replacement entitlement; World equality excludes guard TOE debit.
            var custody = JsonSerializer.SerializeToUtf8Bytes(new
            {
                lotId,
                victimElementId = "commonwealth-infantry-west",
                captorElementId = "axis-infantry-second",
                quantity = 1,
                originLocationId = "west",
                currentLocationId = (string?)null,
                status = "escaped",
                guard = (object?)null,
                replacement = new
                {
                    entitlementId = lotId + ".replacement",
                    victimElementId = "commonwealth-infantry-west",
                    victimComponentId = lot.GetProperty("victimComponentId").GetString(),
                    reunionLocationId = result.GetProperty("route").EnumerateArray().Last().GetString(),
                    quantity = 1,
                    eligibleGameTurn = 5,
                    eligibleOperationStage = 1,
                    trainingPending = true,
                },
            });
            Assert.Equal(HashRepeatedCustody(custody), result.GetProperty("custodyStateHash").GetString());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(0, CountAcceptedStep(disk.Path, request.CampaignId, "contact-combat-custody"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            Assert.Equal(2, CountAcceptedStep(disk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(result.GetProperty("worldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            Assert.Equal(result.GetProperty("custodyStateHash").GetString(), successor.RootElement.GetProperty("custodyStateHash").GetString());
            var rowsAfter = IntegratedCampaignRows(disk.Path, "accepted_steps");
            new CampaignCombatTimeoutService(disk.Open(clock), clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            Assert.Equal(rowsAfter, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.True(CampaignExercises.Reconstruct(disk.Open(clock).ReadVerified(request.CampaignId)).IsVerified);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }

    private static string HashRepeatedCustody(byte[] bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}

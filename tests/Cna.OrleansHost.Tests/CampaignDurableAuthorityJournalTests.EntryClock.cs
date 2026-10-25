using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void EntryClockFailureAtCustodyOrRetreatEntryKeepsPaidHistory(
        bool custody, bool repeated)
    {
        using var original = new JournalFixture();
        var baselineClock = new EntryClock();
        var journal = original.Create(baselineClock);
        var (request, defender, defenderId, opening) = PrepareEntryClockFinalSeal(journal, custody, repeated);
        var lane = custody ? "custody" : "retreat";
        var table = lane + "_rounds";
        var timeoutKind = custody ? CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind
            : CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind;
        var precursorKind = custody ? CampaignHostAcceptedStep.NativeCombatSettlementKind
            : CampaignHostAcceptedStep.NativeCombatAssaultResultKind;
        var saves = new List<JournalFixture>();
        try
        {
            // Calibrate entry against persisted opened_ms in an identical healthy
            // saved branch. Distinct read timestamps make the entry read observable
            // without stack inspection, production hooks or a guessed call count.
            var healthyClock = new EntryClock();
            var healthyDisk = IntegratedCampaignSave(original.Path, healthyClock, saves);
            var healthy = healthyDisk.Open(healthyClock);
            healthyClock.Arm();
            Assert.True(healthy.SubmitOwnerForceSeal(defender, defenderId,
                opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
            var (opened, deadline) = FreshProcessRoundClock(healthyDisk.Path, table, request.CampaignId);
            var entryRead = Assert.Single(healthyClock.Reads, read => read.Timestamp == opened);
            Assert.True(entryRead.Index > 1); // Prior Force gate succeeded first.
            Assert.Equal(30_000, deadline - opened);
            Assert.False(entryRead.Failed);
            using var healthyPaid = IntegratedCampaignRecord(healthyDisk.Path,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            using var healthyPrecursor = IntegratedCampaignRecord(healthyDisk.Path, precursorKind, "canonical_event");
            Assert.Equal(repeated ? 2 : 1, CountAcceptedStep(healthyDisk.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind));

            var faultClock = new EntryClock();
            var faultDisk = IntegratedCampaignSave(original.Path, faultClock, saves);
            var fault = faultDisk.Open(faultClock);
            faultClock.Arm(entryRead.Index);
            var accepted = fault.SubmitOwnerForceSeal(defender, defenderId,
                opening.Revision, opening.PublicActionRefs[0]);
            Assert.True(accepted.IsAccepted);
            var failedRead = Assert.Single(faultClock.Reads, read => read.Failed);
            Assert.Equal(entryRead.Index, failedRead.Index);
            Assert.Equal(opened, failedRead.Timestamp);
            Assert.Equal(healthyClock.Reads.Take(entryRead.Index - 1), faultClock.Reads.Take(entryRead.Index - 1));
            Assert.Contains(faultClock.Reads, read => read.Index > entryRead.Index && !read.Failed);
            using var paid = IntegratedCampaignRecord(faultDisk.Path,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(healthyPaid.RootElement.GetRawText(), paid.RootElement.GetRawText());
            Assert.Equal(repeated ? 2 : 1, CountAcceptedStep(faultDisk.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            Assert.Equal(repeated ? 2 : 1, CountAcceptedStep(faultDisk.Path, request.CampaignId,
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
            Assert.Equal(0, CountAcceptedStep(faultDisk.Path, request.CampaignId, CampaignHostAcceptedStep.NativeCombatForceCancellationKind));
            using var timeout = IntegratedCampaignRecord(faultDisk.Path, timeoutKind, "canonical_event");
            var root = timeout.RootElement;
            Assert.Equal(lane + "-unavailable", root.GetProperty("reason").GetString());
            Assert.Equal(custody ? "leave-unguarded" : "refuse-retreat", root.GetProperty("fallback").GetString());
            Assert.Equal(Timing(request).Hash, root.GetProperty("configurationHash").GetString());
            Assert.Equal(-1, root.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(-1, root.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(-1, root.GetProperty("highWaterUnixMilliseconds").GetInt64());
            Assert.False(root.TryGetProperty("cancelledAtUnixMilliseconds", out _));
            Assert.Equal(HashRepeatedCustody(Encoding.UTF8.GetBytes(healthyPrecursor.RootElement.GetRawText())),
                root.GetProperty(custody ? "settlementHash" : "resultHash").GetString());
            if (custody)
                Assert.Equal(healthyPrecursor.RootElement.GetProperty("settledWorldHash").GetString(), root.GetProperty("worldHash").GetString());
            else
                Assert.Equal(defenderId, root.GetProperty("defenderElementId").GetString());
            Assert.Equal(0, CountAcceptedStep(faultDisk.Path, request.CampaignId,
                custody ? "contact-combat-custody" : "contact-post-assault-retreat"));
            Assert.Equal(1, CountAcceptedStep(faultDisk.Path, request.CampaignId, timeoutKind));
            Assert.Empty(IntegratedCampaignRows(faultDisk.Path, table));
            using var successor = IntegratedCampaignRecord(faultDisk.Path,
                CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.True(CampaignExercises.Reconstruct(faultDisk.Open(faultClock).ReadVerified(request.CampaignId)).IsVerified);
            var retainedSteps = IntegratedCampaignRows(faultDisk.Path, "accepted_steps");
            var retry = fault.SubmitOwnerForceSeal(defender, defenderId,
                opening.Revision, opening.PublicActionRefs[0]);
            Assert.True(retry.IsAccepted);
            Assert.Equal(accepted.CanonicalReceipt, retry.CanonicalReceipt);
            new CampaignCombatTimeoutService(faultDisk.Open(faultClock), faultClock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
            Assert.Equal(retainedSteps, IntegratedCampaignRows(faultDisk.Path, "accepted_steps"));
            Assert.True(CampaignExercises.Reconstruct(faultDisk.Open(faultClock).ReadVerified(request.CampaignId)).IsVerified);
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }

    private sealed class EntryClock : TimeProvider
    {
        private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private int? failedIndex;
        private bool armed;
        public List<(int Index, long Timestamp, bool Failed)> Reads { get; } = [];
        public void Arm(int? failAt = null)
        {
            Reads.Clear();
            failedIndex = failAt;
            armed = true;
        }
        public override DateTimeOffset GetUtcNow()
        {
            if (!armed) return Epoch;
            if (Reads.Count >= 32) throw new InvalidOperationException("Entry clock trace exceeded test bound.");
            var index = Reads.Count + 1;
            var now = Epoch.AddMilliseconds(100 + index);
            var fail = index == failedIndex;
            Reads.Add((index, now.ToUnixTimeMilliseconds(), fail));
            if (fail) throw new InvalidOperationException("Injected precise decision-entry clock failure.");
            return now;
        }
    }

    private static (CampaignCreationRequest Request, CampaignSeatCredential Defender,
        string DefenderId, CampaignOwnerOpening Opening) PrepareEntryClockFinalSeal(
        CampaignDurableAuthorityJournal journal, bool custody, bool repeated)
    {
        if (!repeated)
        {
            var first = PrepareInfantryForceRound(journal, positive: true, seed: custody ? 5UL : 6UL);
            Assert.True(journal.SubmitOwnerForceSeal(first.Attacker, "axis-infantry",
                first.AttackerForce.Revision, first.AttackerForce.PublicActionRefs[0]).IsAccepted);
            return (first.Request, first.Defender, first.DefenderId, first.DefenderForce);
        }
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: custody ? 50UL : 4UL,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        CompleteRetreatIfPending(journal, request.CampaignId, defender, defenderId);
        Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(journal.ReadVerified(request.CampaignId)));
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
        return (request, defender, westDefender, defend);
    }
}

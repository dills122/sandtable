using System.Text;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task FreshProcessRepeatedRequiredRetreatRecoveryAppliesSourceRefusal()
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var control = original.Create(clock);
        // Seed4 traverses real first combat and accepted repeat; finish the second
        // paid assault normally, without manufacturing its required Retreat.
        var (request, defender, defenderId, opening) = PrepareEntryClockFinalSeal(control, custody: false, repeated: true);
        Assert.True(control.SubmitOwnerForceSeal(defender, defenderId, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var campaignId = request.CampaignId;
        var initial = control.ReadVerified(campaignId);
        Assert.Equal(22, CampaignExercises.QueryCheckpoint(initial).ContractVersion);
        var version = CampaignExercises.QueryCheckpoint(initial).StateVersion;
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(RepeatedRetreatSecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal(10UL, paid.RootElement.GetProperty("beforeRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(18UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        Assert.Equal(1, paid.RootElement.GetProperty("facts").GetProperty("requiredRetreat").GetInt32());
        Assert.Equal(5, paid.RootElement.GetProperty("facts").GetProperty("defenderPercent").GetInt32());
        Assert.Equal(JsonValueKind.Null, paid.RootElement.GetProperty("facts").GetProperty("capturedRole").ValueKind);
        Assert.Equal("commonwealth-infantry-west", defenderId);
        var resultHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(paid.RootElement.GetRawText()));
        Assert.Equal(resultHash, CampaignHostAdapter.QueryNativeCombatRetreatRoundBinding(initial).ResultHash);
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var ownerOpenings = IntegratedCampaignRows(original.Path, "owner_openings");
        var saves = new List<JournalFixture>();
        try
        {
            // Only this separate branch queries/submits legal owner refusal.
            // Neither expiry branch queries its Retreat owner.
            var ownerDisk = IntegratedCampaignSave(original.Path, clock, saves);
            var ownerJournal = ownerDisk.Open(clock);
            var refusal = ownerJournal.QueryPostAssaultRetreatOwnerOpening(defender, defenderId);
            Assert.True(ownerJournal.SubmitOwnerPostAssaultRetreat(defender, defenderId,
                refusal.Revision, refusal.PublicActionRefs[^1]).IsAccepted);
            using var ownerSettlement = IntegratedCampaignRecord(ownerDisk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            Assert.True(CampaignExercises.Reconstruct(ownerJournal.ReadVerified(campaignId)).IsVerified);

            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "retreat_rounds", campaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            SweepControl();
            var early = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline - 1);
            Assert.Equal(version, early.BeforeStateVersion);
            Assert.Equal(version, early.AfterStateVersion);
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "retreat_rounds", campaignId));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind));
            Compare(early);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            SweepControl();
            var expired = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(version, expired.BeforeStateVersion);
            Assert.True(expired.AfterStateVersion > version);
            Assert.NotEqual(22, expired.ContractVersion);
            Compare(expired);
            using var timeout = RetreatTimeoutEvent(disk.Path, campaignId);
            var result = timeout.RootElement;
            Assert.Equal(resultHash, result.GetProperty("resultHash").GetString());
            Assert.Equal(defenderId, result.GetProperty("defenderElementId").GetString());
            Assert.Equal("Commonwealth", result.GetProperty("defenderSide").GetString());
            Assert.Equal(paid.RootElement.GetProperty("decisionActionId").GetString(), result.GetProperty("decisionActionId").GetString());
            Assert.Equal(paid.RootElement.GetProperty("paidWorldHash").GetString(), result.GetProperty("paidWorldHash").GetString());
            Assert.Equal("refuse-retreat", result.GetProperty("fallback").GetString());
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.False(result.TryGetProperty("actionId", out _));
            Assert.False(result.TryGetProperty("destinationLocationId", out _));
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            using var settlement = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
            Assert.Equal(HashRepeatedCustody(Encoding.UTF8.GetBytes(result.GetRawText())), settlement.RootElement.GetProperty("retreatHash").GetString());
            Assert.NotEqual(paid.RootElement.GetProperty("paidWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            // V24 exports World hashes, not numeric TOE/distance: compare to the
            // actual legal owner-refusal branch, not a fabricated expected World.
            Assert.Equal(ownerSettlement.RootElement.GetProperty("settledWorldHash").GetString(), settlement.RootElement.GetProperty("settledWorldHash").GetString());
            Assert.Empty(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ownerOpenings, IntegratedCampaignRows(disk.Path, "owner_openings"));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, "contact-post-assault-retreat"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            Assert.Contains(campaignId, disk.Open(clock).CollectingCycleCampaigns("", 16));

            var accepted = IntegratedCampaignRows(disk.Path, "accepted_steps");
            SweepControl();
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(expired.AfterStateVersion, retry.BeforeStateVersion);
            Assert.Equal(expired.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(expired.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(accepted, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatRetreatTimeoutKind));
            Compare(retry);

            void SweepControl() => new CampaignCombatTimeoutService(control, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);

            void Compare(NativeChildResult child)
            {
                Assert.NotEqual(Environment.ProcessId, child.ProcessId);
                Assert.True(child.IsVerified);
                IntegratedCampaignCompare(control, original.Path, disk.Open(clock), disk.Path, campaignId);
                var replay = CampaignExercises.Reconstruct(control.ReadVerified(campaignId));
                Assert.Equal(replay.EventStreamHash, child.EventStreamHash);
                Assert.Equal(replay.ReconstructedSnapshotHash, child.ReconstructedSnapshotHash);
                Assert.Equal(CampaignExercises.QueryCheckpoint(control.ReadVerified(campaignId)).ContractVersion, child.ContractVersion);
            }
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}

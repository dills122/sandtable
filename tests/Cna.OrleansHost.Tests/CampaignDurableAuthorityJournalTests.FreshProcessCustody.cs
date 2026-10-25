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
    public async Task FreshProcessRepeatedCustodyRecoveryLeavesActualCapturedLotUnguarded()
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var control = original.Create(clock);
        // Existing seed50 fixture traverses actual first combat and accepted repeat.
        // Complete its second paid assault normally, without clock/state injection.
        var (request, defender, defenderId, opening) = PrepareEntryClockFinalSeal(control, custody: true, repeated: true);
        Assert.True(control.SubmitOwnerForceSeal(defender, defenderId, opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        var campaignId = request.CampaignId;
        var initial = control.ReadVerified(campaignId);
        Assert.True(CampaignHostAdapter.IsNativeCombatCustodyPending(initial));
        var version = CampaignExercises.QueryCheckpoint(initial).StateVersion;
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatForceCompletionKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        Assert.Equal(1, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactRepeatMovementKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        Assert.Equal(RepeatedCustodySecondDice, paid.RootElement.GetProperty("draws").EnumerateArray().Select(draw => draw.GetProperty("die").GetInt32()));
        Assert.Equal(19UL, paid.RootElement.GetProperty("afterRandomState").GetProperty("nextByteCursor").GetUInt64());
        using var settlement = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatSettlementKind, "canonical_event");
        var lot = Assert.Single(settlement.RootElement.GetProperty("pendingCaptives").EnumerateArray());
        Assert.Equal("axis-infantry-second", lot.GetProperty("captorElementId").GetString());
        Assert.Equal("commonwealth-infantry-west", lot.GetProperty("victimElementId").GetString());
        Assert.Equal(1, lot.GetProperty("quantity").GetInt32());
        var settlementHash = HashRepeatedCustody(Encoding.UTF8.GetBytes(settlement.RootElement.GetRawText()));
        var lotId = "combat.captive." + settlementHash[7..];
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var ownerOpenings = IntegratedCampaignRows(original.Path, "owner_openings");
        var saves = new List<JournalFixture>();
        try
        {
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "custody_rounds", campaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            SweepControl();
            var early = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline - 1);
            Assert.Equal(version, early.BeforeStateVersion);
            Assert.Equal(version, early.AfterStateVersion);
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "custody_rounds", campaignId));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind));
            Compare(early);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            SweepControl();
            var expired = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(version, expired.BeforeStateVersion);
            Assert.True(expired.AfterStateVersion > version);
            Compare(expired);
            Assert.False(CampaignHostAdapter.IsNativeCombatCustodyPending(disk.Open(clock).ReadVerified(campaignId)));
            using var timeout = CustodyTimeoutEvent(disk.Path, campaignId);
            var result = timeout.RootElement;
            Assert.Equal(settlementHash, result.GetProperty("settlementHash").GetString());
            Assert.Equal(lotId, result.GetProperty("lotId").GetString());
            Assert.Equal("leave-unguarded", result.GetProperty("fallback").GetString());
            Assert.Equal("deadline", result.GetProperty("reason").GetString());
            Assert.Equal(opened, result.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, result.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            Assert.Equal(settlement.RootElement.GetProperty("settledWorldHash").GetString(), result.GetProperty("worldHash").GetString());
            // Bind escaped state, absent guard and delayed replacement explicitly.
            var escaped = JsonSerializer.SerializeToUtf8Bytes(new
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
            Assert.Equal(HashRepeatedCustody(escaped), result.GetProperty("custodyStateHash").GetString());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ownerOpenings, IntegratedCampaignRows(disk.Path, "owner_openings"));
            Assert.Equal(0, CountAcceptedStep(disk.Path, campaignId, "contact-combat-custody"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            using var successor = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatSuccessorKind, "snapshot_checkpoint");
            Assert.Equal(paid.RootElement.GetProperty("afterRandomState").GetRawText(), successor.RootElement.GetProperty("randomState").GetRawText());
            Assert.Equal(result.GetProperty("worldHash").GetString(), successor.RootElement.GetProperty("worldHash").GetString());
            Assert.Equal(result.GetProperty("custodyStateHash").GetString(), successor.RootElement.GetProperty("custodyStateHash").GetString());
            Assert.Contains(campaignId, disk.Open(clock).CollectingCycleCampaigns("", 16));

            var accepted = IntegratedCampaignRows(disk.Path, "accepted_steps");
            SweepControl();
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(expired.AfterStateVersion, retry.BeforeStateVersion);
            Assert.Equal(expired.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(expired.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(accepted, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind));
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

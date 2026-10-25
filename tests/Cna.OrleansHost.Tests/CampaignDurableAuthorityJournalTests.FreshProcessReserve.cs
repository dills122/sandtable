using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task FreshProcessThirdCycleReserveRecoveryRetainsConvertedReserveAndIsIdempotent()
    {
        using var original = new JournalFixture();
        var clock = new ManualClock();
        var control = original.Create(clock);
        var (request, owner) = PrepareThirdCycleReserve(control, clock, original.Path);
        var campaignId = request.CampaignId;
        var initial = control.ReadVerified(campaignId);
        var version = CampaignExercises.QueryCheckpoint(initial).StateVersion;
        using var pending = JsonDocument.Parse(CampaignExercises.Reconstruct(initial).ReconstructedSnapshotBytes!);
        Assert.Equal(3, pending.RootElement.GetProperty("cycleOrdinal").GetInt32());
        var world = pending.RootElement.GetProperty("worldHash").GetString();
        var ownerActions = IntegratedCampaignRows(original.Path, "owner_actions");
        var ownerOpenings = IntegratedCampaignRows(original.Path, "owner_openings");
        Assert.Equal(ThirdCycleOwnerChoicesExpected, ThirdCycleOwnerChoices(original.Path));
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));
        Assert.Equal(2, CountAcceptedStep(original.Path, campaignId, CampaignHostAcceptedStep.ContactCycleControlKind));
        using var paid = IntegratedCampaignRecord(original.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
        using var converted = ReserveTimeoutEvent(original.Path, campaignId);
        Assert.Equal(1, converted.RootElement.GetProperty("cycleOrdinal").GetInt32());
        Assert.Equal("axis-infantry-third", Assert.Single(converted.RootElement.GetProperty("convertedElementIds").EnumerateArray()).GetString());
        var saves = new List<JournalFixture>();
        try
        {
            // Copy committed authority, then let separate OS processes own recovery.
            // No parent journal object or owner credential is passed to the child.
            var disk = IntegratedCampaignSave(original.Path, clock, saves);
            var (opened, deadline) = FreshProcessRoundClock(disk.Path, "reserve_rounds", campaignId);
            clock.Advance(TimeSpan.FromMilliseconds(deadline - clock.GetUtcNow().ToUnixTimeMilliseconds() - 1));
            SweepControl();
            var early = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline - 1);
            Assert.Equal(version, early.BeforeStateVersion);
            Assert.Equal(version, early.AfterStateVersion);
            Assert.Equal((opened, deadline), FreshProcessRoundClock(disk.Path, "reserve_rounds", campaignId));
            Assert.Equal(1, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind));
            Compare(early);

            clock.Advance(TimeSpan.FromMilliseconds(1));
            SweepControl();
            var expired = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(version, expired.BeforeStateVersion);
            Assert.Equal(version + 1, expired.AfterStateVersion);
            Compare(expired);
            using var timeout = ReserveTimeoutEvent(disk.Path, campaignId);
            var root = timeout.RootElement;
            Assert.Equal(3, root.GetProperty("cycleOrdinal").GetInt32());
            Assert.Empty(root.GetProperty("convertedElementIds").EnumerateArray());
            Assert.Equal("axis-infantry", Assert.Single(root.GetProperty("retainedElementIds").EnumerateArray()).GetString());
            Assert.Equal(world, root.GetProperty("resultWorldHash").GetString());
            Assert.Equal(opened, root.GetProperty("openedAtUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, root.GetProperty("deadlineUnixMilliseconds").GetInt64());
            Assert.Equal(deadline, root.GetProperty("cancelledAtUnixMilliseconds").GetInt64());
            using var completed = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind, "snapshot_checkpoint");
            Assert.True(completed.RootElement.GetProperty("completed").GetBoolean());
            Assert.Empty(completed.RootElement.GetProperty("nextMovement").EnumerateArray());
            Assert.Equal(ownerActions, IntegratedCampaignRows(disk.Path, "owner_actions"));
            Assert.Equal(ownerOpenings, IntegratedCampaignRows(disk.Path, "owner_openings"));
            Assert.Equal(ThirdCycleOwnerChoicesExpected, ThirdCycleOwnerChoices(disk.Path));
            using var unchangedPaid = IntegratedCampaignRecord(disk.Path, CampaignHostAcceptedStep.NativeCombatAssaultResultKind, "canonical_event");
            Assert.Equal(paid.RootElement.GetRawText(), unchangedPaid.RootElement.GetRawText());
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeCombatAssaultResultKind));

            var accepted = IntegratedCampaignRows(disk.Path, "accepted_steps");
            SweepControl();
            var retry = await FreshProcessRunDeadlineChild(disk.Path, campaignId, deadline);
            Assert.Equal(expired.AfterStateVersion, retry.BeforeStateVersion);
            Assert.Equal(expired.AfterStateVersion, retry.AfterStateVersion);
            Assert.Equal(expired.EventStreamHash, retry.EventStreamHash);
            Assert.Equal(accepted, IntegratedCampaignRows(disk.Path, "accepted_steps"));
            Assert.Equal(2, CountAcceptedStep(disk.Path, campaignId, CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind));
            Compare(retry);

            // The recovered third cycle remains Finish-only and can continue.
            var resumed = disk.Open(clock);
            foreach (var journal in new[] { control, resumed })
            {
                var finish = journal.QueryCycleControlOwnerOpening(owner);
                Assert.Single(finish.PublicActionRefs);
                Assert.True(journal.SubmitOwnerCycleControl(owner, finish.Revision, finish.PublicActionRefs[0]).IsAccepted);
            }
            IntegratedCampaignCompare(control, original.Path, disk.Open(clock), disk.Path, campaignId);

            void SweepControl() => new CampaignCombatTimeoutService(control, clock,
                NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);

            void Compare(NativeChildResult result)
            {
                Assert.NotEqual(Environment.ProcessId, result.ProcessId);
                Assert.True(result.IsVerified);
                IntegratedCampaignCompare(control, original.Path, disk.Open(clock), disk.Path, campaignId);
                var replay = CampaignExercises.Reconstruct(control.ReadVerified(campaignId));
                Assert.Equal(replay.EventStreamHash, result.EventStreamHash);
                Assert.Equal(replay.ReconstructedSnapshotHash, result.ReconstructedSnapshotHash);
                Assert.Equal(CampaignExercises.QueryCheckpoint(control.ReadVerified(campaignId)).ContractVersion, result.ContractVersion);
            }
        }
        finally { foreach (var save in saves) save.Dispose(); }
    }
}

using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Exercises;

public sealed class CampaignStageEntryPreambleAuthorityTests
{
    [Fact]
    public void ForwardPreambleMatchesHandBuiltEventsAndReachesFirstOwnerDecision()
    {
        // PendingFixtures drives both sides through Stage 1 and constructs the
        // Stage 2 preamble events independently using Create and With*.
        var session = Fixture<StageEntryWeatherPending>();
        var steps = new (Func<ExerciseSession, ExerciseStepResult> Complete, byte[] Expected)[]
        {
            (CampaignStageEntryPreambleAuthority.CompleteStageEntryOrganization,
                Fixture<NoObligationOrganizationPending>().CurrentNoObligationOrganization!.SerializeCanonical()),
            (CampaignStageEntryPreambleAuthority.CompleteStageEntryNavalConvoyArrival,
                Fixture<NoObligationNavalConvoyArrivalPending>().CurrentNoObligationNavalConvoyArrival!.SerializeCanonical()),
            (CampaignStageEntryPreambleAuthority.CompleteStageEntryFleetAssignment,
                Fixture<NoObligationFleetAssignmentPending>().CurrentNoObligationFleetAssignment!.SerializeCanonical()),
            (CampaignStageEntryPreambleAuthority.CompleteStageEntryFleetRepair,
                Fixture<NoObligationFleetRepairPending>().CurrentNoObligationFleetRepair!.SerializeCanonical()),
        };

        for (var index = 0; index < steps.Length; index++)
        {
            var (complete, expected) = steps[index];
            // Only the next step is legal at this cut, not an earlier or later step.
            for (var other = 0; other < steps.Length; other++)
                if (other != index)
                    AssertRejected(steps[other].Complete(session));

            var before = CampaignExercises.QueryCheckpoint(session);
            var accepted = complete(session);
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            var evidence = accepted.Evidence!;
            Assert.Equal(expected, Assert.Single(evidence.EventRecords));
            Assert.Equal(expected, evidence.SnapshotCheckpoint);
            session = accepted.SuccessorSession!;
            var after = CampaignExercises.QueryCheckpoint(session);
            Assert.Equal(before.CampaignId, evidence.Receipt.CampaignId);
            Assert.Equal(before.StateVersion, evidence.Receipt.PriorStateVersion);
            Assert.Equal(before.StateVersion + 1, after.StateVersion);
            Assert.Equal(after.StateVersion, evidence.Receipt.CommittedStateVersion);
            Assert.Equal(after.PositionId, evidence.Receipt.ResultingPositionId);
            Assert.Equal(CampaignActionAudience.System, evidence.Receipt.Audience);
            Assert.Equal("sha256:" + Convert.ToHexStringLower(SHA256.HashData(expected)), evidence.Receipt.ActionId);
            Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
            AssertRejected(complete(session));
        }

        Assert.Equal("land.position.operation-2.first-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(session).PositionId);
        var firstSide = session.CurrentNoObligationFleetRepair!.ToOrder().FirstSide;
        var audience = firstSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var reserve = CampaignExercises.SubmitStageEntryReserve(session, audience,
            CampaignStageEntryCursorContractTests.Hash("stage-entry-reserve"), elementId: null);
        Assert.True(reserve.IsAccepted, reserve.RejectionReason.ToString());
        Assert.IsType<StageEntryReservePending>(reserve.SuccessorSession!.Pending);
    }

    private static ExerciseSession Fixture<T>() where T : PendingDecision => PendingFixtures.All[typeof(T)]();

    private static void AssertRejected(ExerciseStepResult result)
    {
        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, result.RejectionReason);
        Assert.Null(result.SuccessorSession);
        Assert.Null(result.Evidence);
    }
}

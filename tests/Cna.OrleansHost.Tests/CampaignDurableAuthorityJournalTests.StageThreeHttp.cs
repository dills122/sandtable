using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public Task TwoHttpSeatsCompleteStageThreeWithDiscoveryAndColdReplay() => RunStagesHttp(includeStageThree: true);

    // HTTP drains the five preamble events as one transaction. Replay every prefix from
    // the cold declaration head as well, then compare the final prefix with the actual
    // durable result of the next HTTP opening.
    private static ExerciseSession ReplayStageThreePreamblePrefixes(ExerciseSession seed)
    {
        var kinds = new[]
        {
            CampaignHostAcceptedStep.NativeStageThreeWeatherKind,
            CampaignHostAcceptedStep.NativeStageThreeOrganizationKind,
            CampaignHostAcceptedStep.NativeStageThreeNavalConvoyArrivalKind,
            CampaignHostAcceptedStep.NativeStageThreeFleetAssignmentKind,
            CampaignHostAcceptedStep.NativeStageThreeFleetRepairKind,
        };
        var steps = new List<CampaignHostAcceptedStep>();
        var live = seed;
        for (var index = 0; index < kinds.Length; index++)
        {
            var native = CampaignHostAdapter.TryCompleteStageEntrySystemStep(live);
            Assert.NotNull(native);
            Assert.Equal(kinds[index], native.Value.Kind);
            var result = native.Value.Result;
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            var evidence = result.Evidence!;
            var command = CampaignHostAdapter.StageEntrySystemCommand(live, result);
            var step = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(command),
                evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
                evidence.SnapshotCheckpoint, native.Value.Kind);
            Assert.Equal(135 + index, step.ContractVersion);
            steps.Add(step);
            live = result.SuccessorSession!;
            var replay = CampaignHostAdapter.ImportResumed(seed, steps);
            Assert.True(replay.IsVerified, replay.RejectionReason.ToString());
            Assert.Equal(CampaignExercises.QueryCheckpoint(live).PositionId,
                CampaignExercises.QueryCheckpoint(replay.Session!).PositionId);
            Assert.Equal(CampaignExercises.Reconstruct(live).EventStreamHash,
                CampaignExercises.Reconstruct(replay.Session!).EventStreamHash);
        }
        return live;
    }
}

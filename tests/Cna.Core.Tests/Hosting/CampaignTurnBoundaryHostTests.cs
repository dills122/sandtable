using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Tests.Exercises;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Hosting;

public sealed class CampaignTurnBoundaryHostTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void BoundarySystemTransitionsReplayFromTheirPredecessors(int initialTurn)
    {
        var samples = SequencingScenarios.DriveTurnBoundary(initialTurn, 12345).Samples;
        var boundary = samples.Where(sample => sample.Label.StartsWith("turn.", StringComparison.Ordinal)).ToArray();
        var seed = samples.Single(sample => sample.Label == "op3.second.post-combat-pass-3").Session;
        var live = seed;
        var steps = new List<CampaignHostAcceptedStep>();
        int[] versions = [142, 143, 144, 144, CampaignHostAcceptedStep.ContactStageEntryInitiativeDeclarationContractVersion,
            145, 146, 147, 148, 149];
        foreach (var expected in boundary)
        {
            if (expected.Label == "turn.declaration")
            {
                var holder = CampaignGameTurn.LatestInitiativeHolder(live.CurrentHistory, live.CurrentSnapshot.InitiativeHolder!.Value);
                var audience = holder == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                var draft = CampaignHostAdapter.QueryStageEntryInitiativeDeclarationOwnerDraft(live, audience);
                Assert.Equal(2, draft.PrivateActionIds.Count);
                var checkpoint = CampaignExercises.QueryCheckpoint(live);
                var command = new CampaignActionSubmission(1, checkpoint.CampaignId, checkpoint.StateVersion,
                    checkpoint.PositionId, audience, new ActLastAction(1).ActionId);
                var result = CampaignHostAdapter.SubmitStageEntryInitiativeDeclaration(live, command);
                Append(CampaignHostAcceptedStep.ContactStageEntryInitiativeDeclarationKind, command, result);
            }
            else
            {
                var step = CampaignHostAdapter.TryCompleteStageEntrySystemStep(live);
                Assert.NotNull(step);
                Assert.True(step.Value.Result.IsAccepted, step.Value.Result.RejectionReason.ToString());
                Append(step.Value.Kind, CampaignHostAdapter.StageEntrySystemCommand(live, step.Value.Result), step.Value.Result);
            }
            Assert.Equal(versions[steps.Count - 1], steps[^1].ContractVersion);
            Assert.Equal(CampaignExercises.Reconstruct(expected.Session).EventStreamHash,
                CampaignExercises.Reconstruct(live).EventStreamHash);
            var (tampered, _) = SessionTamper.TamperOwnRecord(live);
            Assert.NotNull(tampered);
            Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(tampered).FailureReason);
            var replay = CampaignHostAdapter.ImportResumed(seed, steps);
            Assert.True(replay.IsVerified, replay.RejectionReason.ToString());
            Assert.Equal(CampaignExercises.Reconstruct(live).EventStreamHash,
                CampaignExercises.Reconstruct(replay.Session!).EventStreamHash);
        }

        void Append(string kind, CampaignActionSubmission command, ExerciseStepResult result)
        {
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            var evidence = result.Evidence!;
            steps.Add(new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(command),
                evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
                evidence.SnapshotCheckpoint, kind,
                kind == CampaignHostAcceptedStep.ContactStageEntryInitiativeDeclarationKind ? "land.stage-entry-initiative-declaration" : null));
            live = result.SuccessorSession!;
        }
    }
}

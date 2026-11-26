using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public Task HttpTurnBoundaryReachesLaterTurnReserveOpening(int initialTurn) =>
        RunStagesHttp(includeStageThree: true, boundaryTurn: initialTurn);

    private static CampaignCreationRequest TurnBoundaryRequest(int initialTurn) => new(1,
        $"campaign-turn-boundary-gt{initialTurn}", Cna1979Ruleset.Manifest.Hash, 12345,
        $"rules-lab.breakdown.turn-boundary.gt{initialTurn}.v1",
        initialTurn == 1 ? "sha256:e6f9d8de65a7f01fec9089d3f58e8f94871214a7d628c52508c9aecadca68dc6"
            : "sha256:faba55d645dc442d1fb0a163c0a74357800c0029e6a5c5a9e86704ebb455fab7",
        $"rules-lab.content.breakdown-turn-boundary.gt{initialTurn}.v1",
        initialTurn == 1 ? "sha256:b0ac6f247766a4fbf45f11dae2d69c2893af0128f834a3c5683e6642522563aa"
            : "sha256:0998845c6cf3d317b391a50a22b94a13046e3cf81ee01a64b2caee6e5803b74c",
        $"breakdown-turn-boundary-gt{initialTurn}");

    private static ExerciseSession ReplayTurnBoundaryPrefixes(ExerciseSession seed, int initialTurn, bool beforeDeclaration)
    {
        var kinds = beforeDeclaration
            ? new[] { CampaignHostAcceptedStep.NativeEndOfTurnKind, CampaignHostAcceptedStep.NativeTurnInitiativeKind,
                CampaignHostAcceptedStep.NativeNoObligationTurnPhaseKind, CampaignHostAcceptedStep.NativeNoObligationTurnPhaseKind }
            : new[] { CampaignHostAcceptedStep.NativeTurnStartWeatherKind, CampaignHostAcceptedStep.NativeTurnStartOrganizationKind,
                CampaignHostAcceptedStep.NativeTurnStartConvoyArrivalKind, CampaignHostAcceptedStep.NativeTurnStartFleetAssignmentKind,
                CampaignHostAcceptedStep.NativeTurnStartFleetRepairKind };
        var steps = new List<CampaignHostAcceptedStep>();
        var live = seed;
        foreach (var kind in kinds)
        {
            var native = CampaignHostAdapter.TryCompleteStageEntrySystemStep(live);
            Assert.NotNull(native);
            Assert.Equal(kind, native.Value.Kind);
            var result = native.Value.Result;
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            var evidence = result.Evidence!;
            var command = CampaignHostAdapter.StageEntrySystemCommand(live, result);
            var step = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(command),
                evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
                evidence.SnapshotCheckpoint, kind);
            Assert.InRange(step.ContractVersion, 142, 149);
            if (kind == CampaignHostAcceptedStep.NativeTurnStartWeatherKind)
            {
                using var weather = JsonDocument.Parse(evidence.EventRecords[0]);
                var turn = initialTurn + 1;
                Assert.Equal(turn, weather.RootElement.GetProperty("gameTurn").GetInt32());
                Assert.Equal(turn == 13 ? "winter" : "fall", weather.RootElement.GetProperty("season").GetString(), ignoreCase: true);
            }
            steps.Add(step);
            live = result.SuccessorSession!;
            var replay = CampaignHostAdapter.ImportResumed(seed, steps);
            Assert.True(replay.IsVerified, replay.RejectionReason.ToString());
            Assert.Equal(CampaignExercises.QueryCheckpoint(live).PositionId,
                CampaignExercises.QueryCheckpoint(replay.Session!).PositionId);
            var reconstructed = CampaignExercises.Reconstruct(replay.Session!);
            Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
            Assert.Equal(CampaignExercises.Reconstruct(live).EventStreamHash, reconstructed.EventStreamHash);

            // A one-byte change cannot be accepted by the cold Host importer.
            var altered = evidence.EventRecords[0].ToArray();
            altered[^2] ^= 1;
            var tampered = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(command),
                [altered], CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt), evidence.SnapshotCheckpoint, kind);
            Assert.False(CampaignHostAdapter.ImportResumed(seed, [.. steps.Take(steps.Count - 1), tampered]).IsVerified);
        }
        return live;
    }
}

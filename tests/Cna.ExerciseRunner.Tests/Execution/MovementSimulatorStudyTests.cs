using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class MovementSimulatorStudyTests
{
    private const string BreakdownBoundary =
        "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";

    private static readonly ulong[] StudySeeds =
        [0, 1, ulong.MaxValue / 2, ulong.MaxValue];

    private static readonly (
        ExerciseControllerPolicy Policy,
        int Reserves,
        int Moves,
        int Steps)[] Policies =
    [
        (ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete, 0, 2, 17),
        (ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete, 1, 1, 15),
        (ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete, 1, 1, 15),
        (ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete, 0, 2, 17),
        (ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete, 1, 1, 15),
        (ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete, 1, 1, 15),
    ];

    [Fact]
    public void FourSeedSixControllerStudyRepeatsExactEvidenceAndStableMovementRoutes()
    {
        var signatures = Policies.ToDictionary(
            value => value.Policy,
            _ => new HashSet<string>(StringComparer.Ordinal));

        foreach (var seed in StudySeeds)
        {
            foreach (var (policy, expectedReserves, expectedMoves, expectedSteps) in Policies)
            {
                var manifest = Manifest(seed, policy);
                var first = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);
                var second = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);

                AssertRun(first, manifest, expectedReserves, expectedMoves, expectedSteps);
                AssertRun(second, manifest, expectedReserves, expectedMoves, expectedSteps);
                Assert.Equal(
                    ExerciseEvidenceWriter.WriteAcceptedActions(first),
                    ExerciseEvidenceWriter.WriteAcceptedActions(second));
                Assert.Equal(
                    ExerciseEvidenceWriter.WriteCanonicalEvents(first),
                    ExerciseEvidenceWriter.WriteCanonicalEvents(second));
                Assert.Equal(
                    ExerciseEvidenceWriter.WriteStepEvidence(first),
                    ExerciseEvidenceWriter.WriteStepEvidence(second));
                Assert.Equal(first.InitialSnapshot, second.InitialSnapshot);
                Assert.Equal(first.FinalSnapshot, second.FinalSnapshot);
                Assert.Equal(
                    ReplayProofCodec.Serialize(first.Reconstruction!),
                    ReplayProofCodec.Serialize(second.Reconstruction!));
                var firstReadjudication = ReadjudicationVerifier.Verify(manifest, first);
                var secondReadjudication = ReadjudicationVerifier.Verify(manifest, second);
                Assert.True(firstReadjudication.IsVerified);
                Assert.Equal(ReplayProofCodec.Serialize(firstReadjudication),
                    ReplayProofCodec.Serialize(secondReadjudication));
                signatures[policy].Add(MovementSignature(first));
            }
        }

        Assert.All(signatures.Values, values => Assert.Single(values));
    }

    private static void AssertRun(
        ExerciseExecutionResult result,
        ExerciseManifest manifest,
        int expectedReserves,
        int expectedMoves,
        int expectedSteps)
    {
        Assert.True(result.IsSucceeded);
        Assert.Equal(expectedSteps, result.Steps.Count);
        Assert.Equal(BreakdownBoundary, result.BoundaryPositionId);
        Assert.True(result.Reconstruction!.IsVerified);
        var events = result.Steps.SelectMany(value => value.EventRecords)
            .Select(value => JsonDocument.Parse(value)).ToArray();
        try
        {
            Assert.Equal(expectedReserves, events.Count(value => EventType(value) ==
                "reserve-element-designated"));
            var moved = events.Where(value => EventType(value) == "element-moved").ToArray();
            Assert.Equal(expectedMoves, moved.Length);
            Assert.Equal(expectedMoves, moved.Select(value => value.RootElement
                .GetProperty("elementId").GetString()).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(
                1,
                events.Count(value => EventType(value) == "movement-segment-completed"));
        }
        finally
        {
            foreach (var document in events) document.Dispose();
        }

        Assert.Equal(manifest.RootSeed, result.SeedLedger.Identity.RootSeed);
    }

    private static string MovementSignature(ExerciseExecutionResult result) => string.Join(
        '|',
        result.Steps.SelectMany(value => value.EventRecords).Select(value =>
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            if (!string.Equals(
                    root.GetProperty("eventType").GetString(),
                    "element-moved",
                    StringComparison.Ordinal))
                return null;
            var after = root.GetProperty("capabilityPointsExpendedAfter");
            return $"{root.GetProperty("elementId").GetString()}:"
                + $"{root.GetProperty("originLocationId").GetString()}>"
                + $"{root.GetProperty("destinationLocationId").GetString()}:"
                + $"{after.GetProperty("numerator").GetInt64()}/"
                + $"{after.GetProperty("denominator").GetInt32()}";
        }).Where(value => value is not null));

    private static string EventType(JsonDocument document) =>
        document.RootElement.GetProperty("eventType").GetString()!;

    private static ExerciseManifest Manifest(
        ulong rootSeed,
        ExerciseControllerPolicy policy) => new(
        ExerciseManifest.CurrentContractVersion,
        "movement-study",
        "rules-lab.breakdown.truck.v1",
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
        "rules-lab.content.breakdown-truck.v1",
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee",
        "breakdown-truck-lab",
        Cna1979Ruleset.Manifest.Hash,
        BreakdownBoundary,
        30,
        rootSeed,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Compact,
        new ExerciseControllerManifest(policy, policy, policy),
        null);
}

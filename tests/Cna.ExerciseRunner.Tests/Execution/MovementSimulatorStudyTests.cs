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

    private static readonly (ExerciseControllerPolicy Policy, int Reserves, int Moves)[] Policies =
    [
        (ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete, 0, 2),
        (ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete, 1, 1),
        (ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete, 2, 0),
        (ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete, 0, 2),
        (ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete, 1, 1),
        (ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete, 2, 0),
    ];

    [Fact]
    public void FourSeedSixControllerStudyRepeatsExactEvidenceAndStableMovementRoutes()
    {
        var signatures = Policies.ToDictionary(
            value => value.Policy,
            _ => new HashSet<string>(StringComparer.Ordinal));

        foreach (var seed in StudySeeds)
        {
            foreach (var (policy, expectedReserves, expectedMoves) in Policies)
            {
                var manifest = Manifest(seed, policy);
                var first = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);
                var second = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);

                AssertRun(first, manifest, expectedReserves, expectedMoves);
                AssertRun(second, manifest, expectedReserves, expectedMoves);
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
                Assert.Equal(
                    ReplayProofCodec.Serialize(firstReadjudication),
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
        int expectedMoves)
    {
        Assert.True(result.IsSucceeded);
        Assert.Equal(13, result.Steps.Count);
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
            Assert.Single(events, value => EventType(value) == "movement-segment-completed");
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
        "rules-lab.initiative.predetermined",
        "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
        "rules-lab.content.movement-contact.v1",
        "sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79",
        "movement-contact-lab",
        Cna1979Ruleset.Manifest.Hash,
        BreakdownBoundary,
        13,
        rootSeed,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Compact,
        new ExerciseControllerManifest(policy, policy, policy),
        null);
}

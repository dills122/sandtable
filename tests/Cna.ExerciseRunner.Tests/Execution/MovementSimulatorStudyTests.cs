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
        bool OpensReaction)[] Policies =
    [
        (ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete, 0, 1, true),
        (ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete, 1, 1, false),
        (ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete, 2, 0, false),
        (ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete, 0, 1, true),
        (ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete, 1, 1, false),
        (ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete, 2, 0, false),
    ];

    [Fact]
    public void FourSeedSixControllerStudyRepeatsExactEvidenceAndStableMovementRoutes()
    {
        var signatures = Policies.ToDictionary(
            value => value.Policy,
            _ => new HashSet<string>(StringComparer.Ordinal));

        foreach (var seed in StudySeeds)
        {
            foreach (var (policy, expectedReserves, expectedMoves, opensReaction) in Policies)
            {
                var manifest = Manifest(seed, policy);
                var first = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);
                var second = ExerciseExecutor.Execute(
                    manifest,
                    TestContext.Current.CancellationToken);

                AssertRun(first, manifest, expectedReserves, expectedMoves, opensReaction);
                AssertRun(second, manifest, expectedReserves, expectedMoves, opensReaction);
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
                if (!opensReaction)
                {
                    Assert.Equal(
                        ReplayProofCodec.Serialize(first.Reconstruction!),
                        ReplayProofCodec.Serialize(second.Reconstruction!));

                    var firstReadjudication = ReadjudicationVerifier.Verify(manifest, first);
                    var secondReadjudication = ReadjudicationVerifier.Verify(manifest, second);
                    Assert.True(firstReadjudication.IsVerified);
                    Assert.Equal(
                        ReplayProofCodec.Serialize(firstReadjudication),
                        ReplayProofCodec.Serialize(secondReadjudication));
                }
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
        bool opensReaction)
    {
        if (opensReaction)
        {
            Assert.False(result.IsSucceeded);
            Assert.Equal(ExerciseFailureCategory.InvariantFailed, result.FailureCategory);
            Assert.Equal(11, result.Steps.Count);
            Assert.Null(result.BoundaryPositionId);
            Assert.Null(result.Reconstruction);
        }
        else
        {
            Assert.True(result.IsSucceeded);
            Assert.Equal(13, result.Steps.Count);
            Assert.Equal(BreakdownBoundary, result.BoundaryPositionId);
            Assert.True(result.Reconstruction!.IsVerified);
        }
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
                opensReaction ? 0 : 1,
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
        "rules-lab.initiative.predetermined",
        "sha256:48ad98fd232f7c7c50d4f925dd83e3de97f2eb48cc6929a17aa1fb172cdbd394",
        "rules-lab.content.movement-contact.v1",
        "sha256:20cf54f25d752253105877c6139d8db86549759f9dbb80fad873686498f26f5f",
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

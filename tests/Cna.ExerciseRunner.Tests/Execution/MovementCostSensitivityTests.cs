using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class MovementCostSensitivityTests
{
    private const string BreakdownBoundary =
        "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";
    private const string ActionA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ActionB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ActionC =
        "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    [Fact]
    public void VersionThreeMovementCandidateRequiresExactPositivePublicCost()
    {
        var candidate = MovementCandidate(
            ActionA,
            "unit.alpha",
            "west",
            "north",
            new CapabilityPointAmount(1, 2));

        Assert.Equal(3, ExerciseControllerCandidate.CurrentContractVersion);
        Assert.Equal(new CapabilityPointAmount(1, 2), candidate.MovementTotalCost);
        Assert.ThrowsAny<ArgumentException>(() => new ExerciseControllerCandidate(
            ExerciseControllerCandidate.CurrentContractVersion,
            ActionA,
            "move-element",
            "unit.alpha",
            "west",
            "north"));
        Assert.ThrowsAny<ArgumentException>(() => MovementCandidate(
            ActionA,
            "unit.alpha",
            "west",
            "north",
            CapabilityPointAmount.Zero));
        Assert.ThrowsAny<ArgumentException>(() => new ExerciseControllerCandidate(
            ExerciseControllerCandidate.CurrentContractVersion,
            ActionA,
            "complete-movement-segment",
            null,
            movementTotalCost: new CapabilityPointAmount(1, 1)));
        foreach (var version in new[] { 2, 4 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExerciseControllerCandidate(
                version,
                ActionA,
                "resolve-weather",
                null));
        }
    }

    [Fact]
    public void LowestCostPolicyChoosesCheapestRouteForFirstEligibleElementWithStableTies()
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(
            "ActFirstReserveNoneMoveEachOnceByLowestCostThenComplete");
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var candidates = new[]
        {
            MovementCandidate(ActionA, "unit.alpha", "west", "south",
                new CapabilityPointAmount(1, 2)),
            MovementCandidate(ActionB, "unit.alpha", "west", "north",
                new CapabilityPointAmount(1, 2)),
            MovementCandidate(ActionC, "unit.alpha", "west", "east",
                new CapabilityPointAmount(8, 1)),
            MovementCandidate(Sha('d'), "unit.zulu", "west", "north",
                new CapabilityPointAmount(1, 4)),
            Candidate(Sha('e'), "complete-movement-segment"),
        };

        var first = ExerciseController.Select(controllers, ActionSets(candidates, []));
        var second = ExerciseController.Select(
            controllers,
            ActionSets(candidates, ["unit.alpha"]));
        var completion = ExerciseController.Select(
            controllers,
            ActionSets(candidates, ["unit.alpha", "unit.zulu"]));

        Assert.Equal(ActionB, first.ActionId);
        Assert.Equal(Sha('d'), second.ActionId);
        Assert.Equal(Sha('e'), completion.ActionId);
    }

    [Fact]
    public void LowestCostPolicyChangesOnlyTheMovementTrajectoryAndRepeatsExactly()
    {
        var baselineManifest = Manifest(
            ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete);
        var lowestCostPolicy = Enum.Parse<ExerciseControllerPolicy>(
            "ActFirstReserveNoneMoveEachOnceByLowestCostThenComplete");
        var candidateManifest = Manifest(lowestCostPolicy);

        var baseline = ExerciseExecutor.Execute(
            baselineManifest,
            TestContext.Current.CancellationToken);
        var first = ExerciseExecutor.Execute(
            candidateManifest,
            TestContext.Current.CancellationToken);
        var second = ExerciseExecutor.Execute(
            candidateManifest,
            TestContext.Current.CancellationToken);

        AssertRun(baseline, baselineManifest);
        AssertRun(first, candidateManifest);
        AssertRun(second, candidateManifest);
        Assert.Equal(new CapabilityPointAmount(9, 1), TotalMovementCost(baseline));
        Assert.Equal(new CapabilityPointAmount(3, 2), TotalMovementCost(first));
        Assert.Equal("center", MovementDestinations(baseline)[0]);
        Assert.Equal("north-west", MovementDestinations(first)[0]);
        Assert.Equal(
            ExerciseEvidenceWriter.WriteAcceptedActions(first),
            ExerciseEvidenceWriter.WriteAcceptedActions(second));
        Assert.Equal(
            ExerciseEvidenceWriter.WriteCanonicalEvents(first),
            ExerciseEvidenceWriter.WriteCanonicalEvents(second));
        Assert.Equal(first.FinalSnapshot, second.FinalSnapshot);
    }

    private static void AssertRun(
        ExerciseExecutionResult result,
        ExerciseManifest manifest)
    {
        Assert.True(result.IsSucceeded);
        Assert.Equal(BreakdownBoundary, result.BoundaryPositionId);
        Assert.Equal(13, result.Steps.Count);
        Assert.True(result.Reconstruction!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(manifest, result).IsVerified);
        Assert.Equal(2, MovementDestinations(result).Length);
    }

    private static CapabilityPointAmount TotalMovementCost(ExerciseExecutionResult result) =>
        MovementEvents(result).Aggregate(
            CapabilityPointAmount.Zero,
            (total, movement) => total + new CapabilityPointAmount(
                movement.GetProperty("cost").GetProperty("totalCost")
                    .GetProperty("numerator").GetInt64(),
                movement.GetProperty("cost").GetProperty("totalCost")
                    .GetProperty("denominator").GetInt32()));

    private static string[] MovementDestinations(ExerciseExecutionResult result) =>
        MovementEvents(result).Select(value =>
            value.GetProperty("destinationLocationId").GetString()!).ToArray();

    private static JsonElement[] MovementEvents(ExerciseExecutionResult result) =>
        result.Steps.SelectMany(step => step.EventRecords)
            .Select(CloneEvent)
            .Where(root => string.Equals(
                root.GetProperty("eventType").GetString(),
                "element-moved",
                StringComparison.Ordinal))
            .ToArray();

    private static JsonElement CloneEvent(byte[] record)
    {
        using var document = JsonDocument.Parse(record);
        return document.RootElement.Clone();
    }

    private static ExerciseManifest Manifest(ExerciseControllerPolicy policy) =>
        ExerciseManifestCodecTests.Create(
            maximumSteps: 13,
            terminalBoundary: BreakdownBoundary,
            controllerPolicy: policy);

    private static ExerciseControllerCandidate Candidate(
        string actionId,
        string kind) => new(
            ExerciseControllerCandidate.CurrentContractVersion,
            actionId,
            kind,
            null);

    private static ExerciseControllerCandidate MovementCandidate(
        string actionId,
        string elementId,
        string originLocationId,
        string destinationLocationId,
        CapabilityPointAmount totalCost) => new(
            ExerciseControllerCandidate.CurrentContractVersion,
            actionId,
            "move-element",
            elementId,
            originLocationId,
            destinationLocationId,
            totalCost);

    private static IReadOnlyList<ExerciseControllerActionSet> ActionSets(
        IEnumerable<ExerciseControllerCandidate> candidates,
        IEnumerable<string> priorMovedElementIds) =>
        [
            new ExerciseControllerActionSet(CampaignActionAudience.System, []),
            new ExerciseControllerActionSet(
                CampaignActionAudience.Axis,
                candidates,
                priorMovedElementIds: priorMovedElementIds),
            new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
        ];

    private static string Sha(char value) => $"sha256:{new string(value, 64)}";
}

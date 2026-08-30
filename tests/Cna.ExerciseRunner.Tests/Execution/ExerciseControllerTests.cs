using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseControllerTests
{
    private const string ActionA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ActionB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ActionC =
        "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    [Fact]
    public void FixedAudienceOrderSelectsTheUniqueActiveAudienceAndFirstActionId()
    {
        var result = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(
                    CampaignActionAudience.Axis,
                    [Candidate(ActionB, "resolve-weather"),
                        Candidate(ActionA, "resolve-weather")]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.True(result.IsSelected);
        Assert.Equal(CampaignActionAudience.Axis, result.Audience);
        Assert.Equal(ActionA, result.ActionId);
        Assert.Equal(ExerciseControllerSelectionFailure.None, result.FailureReason);
    }

    [Fact]
    public void ZeroAndMultipleActiveAudiencesFailClosed()
    {
        var zero = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);
        var multiple = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System,
                    [Candidate(ActionA, "resolve-weather")]),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis,
                    [Candidate(ActionB, "resolve-weather")]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.False(zero.IsSelected);
        Assert.Equal(ExerciseControllerSelectionFailure.NoActiveAudience, zero.FailureReason);
        Assert.False(multiple.IsSelected);
        Assert.Equal(
            ExerciseControllerSelectionFailure.MultipleActiveAudiences,
            multiple.FailureReason);
    }

    [Fact]
    public void SemanticPolicyDesignatesByElementThenActionAndCompletesLast()
    {
        var designation = ExerciseController.Select(
            SemanticControllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis,
                [
                    Candidate(ActionA, "complete-reserve-designation"),
                    Candidate(ActionB, "designate-reserve", "unit.zulu"),
                    Candidate(ActionC, "designate-reserve", "unit.alpha"),
                ]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);
        var completion = ExerciseController.Select(
            SemanticControllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis,
                    [Candidate(ActionA, "complete-reserve-designation")]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.Equal(ActionC, designation.ActionId);
        Assert.Equal(ActionA, completion.ActionId);
    }

    [Fact]
    public void SemanticPolicyFallsBackOutsideReserveAndRejectsMalformedReserveSets()
    {
        var ordinary = ExerciseController.Select(
            SemanticControllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System,
                [
                    Candidate(ActionB, "act-last"),
                    Candidate(ActionA, "act-first"),
                ]),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);
        var missingCompletion = ExerciseController.Select(
            SemanticControllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis,
                    [Candidate(ActionB, "designate-reserve", "unit.alpha")]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);
        var mixed = ExerciseController.Select(
            SemanticControllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis,
                [
                    Candidate(ActionA, "complete-reserve-designation"),
                    Candidate(ActionB, "resolve-weather"),
                ]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.Equal(ActionA, ordinary.ActionId);
        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed,
            missingCompletion.FailureReason);
        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed,
            mixed.FailureReason);
    }

    [Theory]
    [InlineData("ActFirstReserveNoneThenFirstByActionId", "act-first", 0, false)]
    [InlineData("ActFirstReserveOneThenFirstByActionId", "act-first", 0, true)]
    [InlineData("ActFirstReserveAllThenFirstByActionId", "act-first", 0, true)]
    [InlineData("ActLastReserveNoneThenFirstByActionId", "act-last", 0, false)]
    [InlineData("ActLastReserveOneThenFirstByActionId", "act-last", 1, false)]
    [InlineData("ActLastReserveAllThenFirstByActionId", "act-last", 1, true)]
    public void MatrixPolicySelectsDeclaredInitiativeAndReserveCardinality(
        string policyName,
        string expectedInitiativeKind,
        int priorReserveDesignationCount,
        bool expectsDesignation)
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(policyName);
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var initiativeCandidates = new[]
        {
            Candidate(ActionA, "act-last"),
            Candidate(ActionB, "act-first"),
        };
        var reserveCandidates = new[]
        {
            Candidate(ActionA, "complete-reserve-designation"),
            Candidate(ActionB, "designate-reserve", "unit.zulu"),
            Candidate(ActionC, "designate-reserve", "unit.alpha"),
        };

        var initiative = ExerciseController.Select(
            controllers,
            ActionSets(CampaignActionAudience.Axis, initiativeCandidates));
        var reserve = ExerciseController.Select(
            controllers,
            ActionSets(
                CampaignActionAudience.Commonwealth,
                reserveCandidates,
                priorReserveDesignationCount));

        Assert.Equal(
            initiativeCandidates.Single(candidate => candidate.Kind == expectedInitiativeKind)
                .ActionId,
            initiative.ActionId);
        Assert.Equal(expectsDesignation ? ActionC : ActionA, reserve.ActionId);
    }

    [Fact]
    public void ReserveOnePolicyDesignatesOnceThenCompletesAndRejectsImpossibleHistory()
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(
            "ActFirstReserveOneThenFirstByActionId");
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var candidates = new[]
        {
            Candidate(ActionA, "complete-reserve-designation"),
            Candidate(ActionB, "designate-reserve", "unit.zulu"),
            Candidate(ActionC, "designate-reserve", "unit.alpha"),
        };

        var first = ExerciseController.Select(
            controllers,
            ActionSets(CampaignActionAudience.Axis, candidates, 0));
        var completed = ExerciseController.Select(
            controllers,
            ActionSets(CampaignActionAudience.Axis, candidates[0..2], 1));
        var impossible = ExerciseController.Select(
            controllers,
            ActionSets(CampaignActionAudience.Axis, candidates, 2));

        Assert.Equal(ActionC, first.ActionId);
        Assert.Equal(ActionA, completed.ActionId);
        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed,
            impossible.FailureReason);
    }

    [Fact]
    public void MatrixPolicyFailsClosedOnMalformedInitiativeCandidates()
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(
            "ActLastReserveAllThenFirstByActionId");
        var controllers = new ExerciseControllerManifest(policy, policy, policy);

        var result = ExerciseController.Select(
            controllers,
            ActionSets(
                CampaignActionAudience.Axis,
                [Candidate(ActionA, "act-first")]));

        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed,
            result.FailureReason);
    }

    [Fact]
    public void BoundedMovementPolicyMovesEachElementOnceBySemanticRouteThenCompletes()
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(
            "ActFirstReserveNoneMoveEachOnceThenComplete");
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var candidates = new[]
        {
            MovementCandidate(ActionA, "unit.zulu", "west", "north"),
            MovementCandidate(ActionB, "unit.alpha", "west", "south"),
            MovementCandidate(ActionC, "unit.alpha", "west", "east"),
            Candidate(Sha('d'), "complete-movement-segment"),
        };

        var first = ExerciseController.Select(
            controllers,
            MovementActionSets(candidates, []));
        var second = ExerciseController.Select(
            controllers,
            MovementActionSets(candidates, ["unit.alpha"]));
        var completion = ExerciseController.Select(
            controllers,
            MovementActionSets(candidates, ["unit.alpha", "unit.zulu"]));

        Assert.Equal(ActionC, first.ActionId);
        Assert.Equal(ActionA, second.ActionId);
        Assert.Equal(Sha('d'), completion.ActionId);
    }

    [Theory]
    [InlineData("missing-completion")]
    [InlineData("multiple-completions")]
    [InlineData("mixed-kind")]
    public void BoundedMovementPolicyFailsClosedOnMalformedMovementSets(string mutation)
    {
        var policy = Enum.Parse<ExerciseControllerPolicy>(
            "ActLastReserveAllMoveEachOnceThenComplete");
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var moves = new List<ExerciseControllerCandidate>
        {
            MovementCandidate(ActionA, "unit.alpha", "west", "east"),
        };
        if (mutation != "missing-completion")
            moves.Add(Candidate(ActionB, "complete-movement-segment"));
        if (mutation == "multiple-completions")
            moves.Add(Candidate(ActionC, "complete-movement-segment"));
        if (mutation == "mixed-kind")
            moves.Add(Candidate(ActionC, "resolve-weather"));

        var result = ExerciseController.Select(
            controllers,
            MovementActionSets(moves, []));

        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed,
            result.FailureReason);
    }

    [Fact]
    public void ExistingMatrixPolicyKeepsFirstByActionIdMovementBehavior()
    {
        var policy = ExerciseControllerPolicy.ActFirstReserveNoneThenFirstByActionId;
        var controllers = new ExerciseControllerManifest(policy, policy, policy);
        var candidates = new[]
        {
            MovementCandidate(ActionB, "unit.alpha", "west", "east"),
            Candidate(ActionA, "complete-movement-segment"),
        };

        var result = ExerciseController.Select(
            controllers,
            MovementActionSets(candidates, []));

        Assert.Equal(ActionA, result.ActionId);
    }

    [Fact]
    public void SemanticCandidateRequiresExactIdsForDesignationAndMovement()
    {
        var designation = Candidate(ActionA, "designate-reserve", "unit.alpha");

        Assert.Equal(ExerciseControllerCandidate.CurrentContractVersion,
            designation.ContractVersion);
        Assert.Equal("unit.alpha", designation.ElementId);
        Assert.ThrowsAny<ArgumentException>(() =>
            Candidate(ActionA, "designate-reserve"));
        Assert.Throws<ArgumentException>(() =>
            Candidate(ActionA, "resolve-weather", "unit.alpha"));
        Assert.ThrowsAny<ArgumentException>(() => new ExerciseControllerCandidate(
            ExerciseControllerCandidate.CurrentContractVersion,
            ActionA,
            "move-element",
            "unit.alpha"));
        Assert.ThrowsAny<ArgumentException>(() => new ExerciseControllerCandidate(
            ExerciseControllerCandidate.CurrentContractVersion,
            ActionA,
            "move-element",
            "unit.alpha",
            "west",
            "west"));
        foreach (var version in new[] { 1, 3 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ExerciseControllerCandidate(
                    version,
                    ActionA,
                    "resolve-weather",
                    null));
        }
    }

    private static ExerciseControllerManifest Controllers() => new(
        ExerciseControllerPolicy.FirstByActionId,
        ExerciseControllerPolicy.FirstByActionId,
        ExerciseControllerPolicy.FirstByActionId);

    private static ExerciseControllerManifest SemanticControllers() => new(
        ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
        ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
        ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId);

    private static ExerciseControllerCandidate Candidate(
        string actionId,
        string kind,
        string? elementId = null) => new(
            ExerciseControllerCandidate.CurrentContractVersion,
            actionId,
            kind,
            elementId);

    private static ExerciseControllerCandidate MovementCandidate(
        string actionId,
        string elementId,
        string originLocationId,
        string destinationLocationId) => new(
            ExerciseControllerCandidate.CurrentContractVersion,
            actionId,
            "move-element",
            elementId,
            originLocationId,
            destinationLocationId);

    private static IReadOnlyList<ExerciseControllerActionSet> MovementActionSets(
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

    private static IReadOnlyList<ExerciseControllerActionSet> ActionSets(
        CampaignActionAudience activeAudience,
        IEnumerable<ExerciseControllerCandidate> candidates,
        int priorReserveDesignationCount = 0)
    {
        return
        [
            CreateActionSet(
                CampaignActionAudience.System,
                activeAudience == CampaignActionAudience.System ? candidates : [],
                priorReserveDesignationCount),
            CreateActionSet(
                CampaignActionAudience.Axis,
                activeAudience == CampaignActionAudience.Axis ? candidates : [],
                priorReserveDesignationCount),
            CreateActionSet(
                CampaignActionAudience.Commonwealth,
                activeAudience == CampaignActionAudience.Commonwealth ? candidates : [],
                priorReserveDesignationCount),
        ];
    }

    private static ExerciseControllerActionSet CreateActionSet(
        CampaignActionAudience audience,
        IEnumerable<ExerciseControllerCandidate> candidates,
        int priorReserveDesignationCount) =>
        new(audience, candidates, priorReserveDesignationCount);
}

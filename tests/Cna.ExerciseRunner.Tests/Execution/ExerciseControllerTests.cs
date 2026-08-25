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

    [Fact]
    public void SemanticCandidateRequiresAnElementExactlyForDesignation()
    {
        var designation = Candidate(ActionA, "designate-reserve", "unit.alpha");

        Assert.Equal(ExerciseControllerCandidate.CurrentContractVersion,
            designation.ContractVersion);
        Assert.Equal("unit.alpha", designation.ElementId);
        Assert.ThrowsAny<ArgumentException>(() =>
            Candidate(ActionA, "designate-reserve"));
        Assert.Throws<ArgumentException>(() =>
            Candidate(ActionA, "resolve-weather", "unit.alpha"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExerciseControllerCandidate(
            2,
            ActionA,
            "resolve-weather",
            null));
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
}

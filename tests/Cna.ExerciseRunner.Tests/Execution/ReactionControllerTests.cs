using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ReactionControllerTests
{
    [Theory]
    [InlineData(ExerciseControllerPolicy.ReactionAllByActionId, 0, 0, false, "b", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionAllByDescendingActionId, 0, 0, false, "c", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionDecline, 0, 0, false, "a", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionOneThenDecline, 0, 1, false, "a", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionAllByActionId, 1, 0, true, "a", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionTwoSteps, 1, 0, true, "b", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionTwoSteps, 2, 0, true, "a", CampaignActionAudience.Axis)]
    [InlineData(ExerciseControllerPolicy.ReactionUnavailable, 0, 0, false, "u", CampaignActionAudience.System)]
    [InlineData(ExerciseControllerPolicy.ReactionTimeout, 0, 0, false, "t", CampaignActionAudience.System)]
    [InlineData(ExerciseControllerPolicy.ReactionActiveUnavailable, 1, 0, true, "u", CampaignActionAudience.System)]
    [InlineData(ExerciseControllerPolicy.ReactionActiveTimeout, 1, 0, true, "t", CampaignActionAudience.System)]
    public void ExplicitPoliciesArbitrateReactionAndBoundEpisodes(
        ExerciseControllerPolicy policy, int moves, int completions, bool active,
        string expectedId, CampaignActionAudience expectedAudience)
    {
        var selection = ExerciseController.Select(new(policy, policy, policy),
        [
            new(CampaignActionAudience.System,
            [Candidate("u", "close-reaction-window-scripted-unavailable"),
                Candidate("t", "close-reaction-window-timeout")]),
            new(CampaignActionAudience.Axis,
            [Candidate("c", "move-reacting-element"), Candidate("b", "move-reacting-element"),
                Candidate("a", active ? "complete-reaction-participant" : "decline-reaction-window")],
                priorReactionEpisodeMoveCount: moves,
                priorReactionWindowCompletionCount: completions),
            new(CampaignActionAudience.Commonwealth, []),
        ]);

        Assert.True(selection.IsSelected);
        Assert.Equal(expectedId, selection.ActionId);
        Assert.Equal(expectedAudience, selection.Audience);
    }

    [Theory]
    [InlineData("resolve-weather")]
    [InlineData("close-reaction-window-timeout")]
    public void ReactionPolicyDoesNotHideMalformedOrOrdinaryAudienceConflicts(string systemKind)
    {
        var policy = ExerciseControllerPolicy.ReactionAllByActionId;
        var selection = ExerciseController.Select(new(policy, policy, policy),
        [
            new(CampaignActionAudience.System, [Candidate("s", systemKind)]),
            new(CampaignActionAudience.Axis, [Candidate("a", "decline-reaction-window")]),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
        Assert.False(selection.IsSelected);
    }

    [Fact]
    public void ReactionPolicyRejectsIncompleteSingleAudienceReactionShapes()
    {
        var policy = ExerciseControllerPolicy.ReactionAllByActionId;
        var playerOnly = ExerciseController.Select(new(policy, policy, policy),
        [
            new(CampaignActionAudience.System, []),
            new(CampaignActionAudience.Axis, [Candidate("a", "decline-reaction-window")]),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
        var systemOnly = ExerciseController.Select(new(policy, policy, policy),
        [
            new(CampaignActionAudience.System, [Candidate("t", "close-reaction-window-timeout")]),
            new(CampaignActionAudience.Axis, []),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
        Assert.False(playerOnly.IsSelected);
        Assert.False(systemOnly.IsSelected);
    }

    [Fact]
    public void EveryNewPolicyRoundTripsAndCompletesPublicReactionTrajectory()
    {
        foreach (var policy in Enum.GetValues<ExerciseControllerPolicy>()
            .Where(value => value.ToString().StartsWith("Reaction", StringComparison.Ordinal)))
        {
            var manifest = ReactionRunnerTestData.Manifest(policy);
            Assert.Equal(manifest, ExerciseManifestCodec.Deserialize(ExerciseManifestCodec.Serialize(manifest)));
            var result = Cna.ExerciseRunner.Execution.ExerciseExecutor.Execute(
                manifest, TestContext.Current.CancellationToken);
            Assert.True(result.IsSucceeded, $"{policy}: {result.FailureCategory}; steps={result.Steps.Count}; "
                + string.Join(";", result.FailedDecisions.Select(value =>
                    $"{value.SelectedActionId}/{value.SubmissionRejectionReason}")));
            Assert.True(result.Reconstruction!.IsVerified);
        }
    }

    private static ExerciseControllerCandidate Candidate(string id, string kind) =>
        new(ExerciseControllerCandidate.CurrentContractVersion, id, kind, null);
}

internal static class ReactionRunnerTestData
{
    internal static ExerciseManifest Manifest(ExerciseControllerPolicy policy) => new(
        ExerciseManifest.CurrentContractVersion, "reaction-study",
        "rules-lab.breakdown.reaction.v1",
        "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e",
        "rules-lab.content.breakdown-reaction.v1",
        "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd",
        "breakdown-reaction-lab", Cna.Core.Rules.Cna1979Ruleset.Manifest.Hash,
        "land.position.operation-1.first-player.movement-and-combat.breakdown-determination",
        50, 0, ExerciseBuildMode.Exploratory, ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic, new(policy, policy, policy), null);
}

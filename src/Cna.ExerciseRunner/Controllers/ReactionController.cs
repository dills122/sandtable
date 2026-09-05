using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Controllers;

public static partial class ExerciseController
{
    private static bool IsReactionPolicy(ExerciseControllerPolicy policy) => policy is
        ExerciseControllerPolicy.ReactionAllByActionId
        or ExerciseControllerPolicy.ReactionAllByDescendingActionId
        or ExerciseControllerPolicy.ReactionTwoSteps
        or ExerciseControllerPolicy.ReactionDecline
        or ExerciseControllerPolicy.ReactionOneThenDecline
        or ExerciseControllerPolicy.ReactionUnavailable
        or ExerciseControllerPolicy.ReactionTimeout
        or ExerciseControllerPolicy.ReactionActiveUnavailable
        or ExerciseControllerPolicy.ReactionActiveTimeout;

    private static ExerciseControllerSelection? SelectReaction(
        ExerciseControllerManifest policies,
        IReadOnlyList<ExerciseControllerActionSet> actionSets)
    {
        var system = actionSets[0];
        var players = actionSets.Skip(1).Where(set => set.Candidates.Count > 0).ToArray();
        if (!IsReactionPolicy(policies.System)) return null;

        if (system.Candidates.Count == 1
            && system.Candidates[0].Kind == "close-reaction-window-no-eligible-reactor"
            && players.Length == 0)
            return ExerciseControllerSelection.Selected(system.Audience, system.Candidates[0].ActionId);

        if (players.Length != 1 || system.Candidates.Count != 2
            || system.Candidates.Count(value => value.Kind == "close-reaction-window-scripted-unavailable") != 1
            || system.Candidates.Count(value => value.Kind == "close-reaction-window-timeout") != 1)
            return null;

        var player = players[0];
        var policy = player.Audience == CampaignActionAudience.Axis ? policies.Axis : policies.Commonwealth;
        if (!IsReactionPolicy(policy)) return null;
        var completions = player.Candidates.Where(value => value.Kind == "complete-reaction-participant").ToArray();
        var declines = player.Candidates.Where(value => value.Kind == "decline-reaction-window").ToArray();
        var moves = player.Candidates.Where(value => value.Kind == "move-reacting-element").ToArray();
        if (completions.Length + declines.Length != 1
            || moves.Length + completions.Length + declines.Length != player.Candidates.Count)
            return null;
        var active = completions.Length == 1;

        var systemCloseKind = policies.System switch
        {
            ExerciseControllerPolicy.ReactionUnavailable => "close-reaction-window-scripted-unavailable",
            ExerciseControllerPolicy.ReactionTimeout => "close-reaction-window-timeout",
            ExerciseControllerPolicy.ReactionActiveUnavailable when active => "close-reaction-window-scripted-unavailable",
            ExerciseControllerPolicy.ReactionActiveTimeout when active => "close-reaction-window-timeout",
            _ => null,
        };
        if (systemCloseKind is not null)
            return ExerciseControllerSelection.Selected(system.Audience,
                system.Candidates.Single(value => value.Kind == systemCloseKind).ActionId);

        ExerciseControllerCandidate? chosen;
        if (active && (moves.Length == 0 || player.PriorReactionEpisodeMoveCount >=
            (policy == ExerciseControllerPolicy.ReactionTwoSteps ? 2 : 1)))
            chosen = completions[0];
        else if (!active && (moves.Length == 0 || policy == ExerciseControllerPolicy.ReactionDecline
            || (policy == ExerciseControllerPolicy.ReactionOneThenDecline
                && player.PriorReactionWindowCompletionCount > 0)))
            chosen = declines[0];
        else
            chosen = policy == ExerciseControllerPolicy.ReactionAllByDescendingActionId
                ? moves.LastOrDefault() : moves.FirstOrDefault();

        return chosen is null
            ? ExerciseControllerSelection.Failed(ExerciseControllerSelectionFailure.PolicyFailed)
            : ExerciseControllerSelection.Selected(player.Audience, chosen.ActionId);
    }
}

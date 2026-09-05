using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;

namespace Cna.Core.Campaigns;

internal static class CampaignReactionWindowClosedFactory
{
    public static ReactionWindowClosed Create(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CloseReactionWindowIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var window = prior?.ReactionWindow
            ?? throw Unsupported("Reaction close authority requires an open window.");
        return Create(
            prior,
            artifact,
            scenario,
            new ReactionWindowClosedReplayInput(
                prior.CampaignId,
                intent.ExpectedStateVersion,
                intent.ExpectedPositionId,
                intent.ActingSide,
                intent.ActionId,
                intent.WindowId,
                window.WindowId,
                ToReason(intent.CloseKind)));
    }

    public static ReactionWindowClosed Create(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ReactionWindowClosedReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(input);
        ValidateAuthority(prior, artifact, scenario, input);

        var window = prior.ReactionWindow!;
        var resolved = window.ResolvedOpportunityIds
            .Select(value => value.Value)
            .ToHashSet(StringComparer.Ordinal);
        var closedOpportunityIds = window.FrozenOpportunities
            .Select(value => value.OpportunityId)
            .Where(value => !resolved.Contains(value.Value))
            .ToArray();
        return new ReactionWindowClosed(
            prior.CampaignId,
            checked(prior.StateVersion + 1),
            prior.StateVersion,
            input.FromPositionId,
            input.ActingSide,
            input.ActionId,
            input.SubmittedWindowId,
            window.WindowId,
            input.Reason,
            closedOpportunityIds,
            window.ReactingPosition.SuspendedMovementPosition);
    }

    private static void ValidateAuthority(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ReactionWindowClosedReplayInput input)
    {
        var window = prior.ReactionWindow;
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || window is null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Reaction
            || prior.StateVersion != input.PriorStateVersion
            || !string.Equals(prior.CampaignId, input.CampaignId, StringComparison.Ordinal)
            || !string.Equals(
                window.ReactingPosition.SuspendedMovementPosition.PositionId,
                input.FromPositionId,
                StringComparison.Ordinal)
            || window.WindowId != input.WindowId)
        {
            throw Unsupported("Reaction close authority is not admitted.");
        }

        var controlled = CampaignElementMovedV2Factory.DeriveControlledLocationIds(
            prior.World,
            artifact,
            scenario,
            window.PhasingSide);
        var observation = CampaignObservationV6Projector.Project(
            prior,
            artifact,
            scenario,
            window.ReactingSide,
            new CampaignObservationV6AuthorityFacts(controlled, []));
        var publicWindowId = CampaignObservationV6DisclosureIdentity.CreateWindow(
            prior.CampaignId,
            prior.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide);
        var isPlayer = input.Reason == CampaignReactionWindowCloseReason.PlayerDecline;
        if (!string.Equals(input.SubmittedWindowId, publicWindowId, StringComparison.Ordinal)
            || (isPlayer
                ? input.ActingSide != window.ReactingSide
                : input.ActingSide is not null))
        {
            throw Unsupported("Reaction close audience or window authority is invalid.");
        }

        var actions = isPlayer
            ? CampaignObservationV6ActionDerivation.DerivePlayer(observation)
            : CampaignObservationV6ActionDerivation.DeriveSystem(observation);
        var expected = actions.Candidates.SingleOrDefault(candidate =>
            IsReasonCandidate(candidate, input.Reason));
        if (expected is not ReactionWindowAction close
            || !string.Equals(close.WindowId, input.SubmittedWindowId, StringComparison.Ordinal)
            || !string.Equals(expected.ActionId, input.ActionId, StringComparison.Ordinal))
        {
            throw Unsupported("The requested Reaction close is not a current exact action.");
        }
    }

    private static bool IsReasonCandidate(
        CampaignActionCandidate candidate,
        CampaignReactionWindowCloseReason reason) => reason switch
        {
            CampaignReactionWindowCloseReason.PlayerDecline =>
                candidate is DeclineReactionWindowAction,
            CampaignReactionWindowCloseReason.ScriptedUnavailable =>
                candidate is CloseReactionWindowUnavailableAction,
            CampaignReactionWindowCloseReason.Timeout =>
                candidate is CloseReactionWindowTimeoutAction,
            CampaignReactionWindowCloseReason.NoEligibleReactor =>
                candidate is CloseReactionWindowNoEligibleAction,
            _ => false,
        };

    private static CampaignReactionWindowCloseReason ToReason(
        CampaignReactionCloseIntentKind kind) => kind switch
        {
            CampaignReactionCloseIntentKind.PlayerDecline =>
                CampaignReactionWindowCloseReason.PlayerDecline,
            CampaignReactionCloseIntentKind.ScriptedUnavailable =>
                CampaignReactionWindowCloseReason.ScriptedUnavailable,
            CampaignReactionCloseIntentKind.Timeout => CampaignReactionWindowCloseReason.Timeout,
            CampaignReactionCloseIntentKind.NoEligibleReactor =>
                CampaignReactionWindowCloseReason.NoEligibleReactor,
            _ => throw Unsupported("The Reaction close reason is unsupported."),
        };

    private static InvalidOperationException Unsupported(string message) => new(message);
}

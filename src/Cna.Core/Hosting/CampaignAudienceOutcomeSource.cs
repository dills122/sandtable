using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;

namespace Cna.Core.Hosting;

/// <summary>
/// Host-private accepted-outcome source. It is deliberately disconnected from HTTP and diary
/// presentation: proposed copy keys remain unavailable until a catalog is approved.
/// </summary>
public static class CampaignAudienceOutcomeSource
{
    public static CampaignAudienceOutcomePair Decide(ExerciseSession predecessor,
        CampaignHostAcceptedStep acceptedStep)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(acceptedStep);
        try
        {
            if (acceptedStep.ContractVersion != CampaignHostAcceptedStep.CurrentContractVersion
                || acceptedStep.EventRecords.Count != 1)
                return CampaignAudienceOutcomePair.Unavailable();

            var commandBytes = acceptedStep.CanonicalCommand;
            var command = CampaignActionSubmissionSerializer.DeserializeCanonical(commandBytes);
            if (!commandBytes.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(command)))
                return CampaignAudienceOutcomePair.Unavailable();

            var prior = predecessor.CurrentSnapshot;
            var set = CampaignExercises.Query(predecessor, command.Audience).ActionSet;
            var candidate = set?.Candidates.SingleOrDefault(action => action.ActionId == command.ActionId);
            if (candidate is null || command.CampaignId != prior.CampaignId
                || command.ExpectedStateVersion != prior.StateVersion
                || command.ExpectedPositionId != set!.PositionId)
                return CampaignAudienceOutcomePair.Unavailable();

            // Re-adjudication verifies the full typed event, source pins, receipt and committed
            // successor head. A supplied event/receipt alone is never an audience grant.
            var replay = CampaignExercises.Submit(predecessor, command);
            if (!replay.IsAccepted || !acceptedStep.Matches(replay.Evidence!))
                return CampaignAudienceOutcomePair.Unavailable();
            var successor = replay.SuccessorSession!;
            var successorSnapshot = successor.CurrentSnapshot;
            var receipt = replay.Evidence!.Receipt;
            if (receipt.CampaignId != prior.CampaignId
                || receipt.PriorStateVersion != prior.StateVersion
                || receipt.CommittedStateVersion != successorSnapshot.StateVersion
                || receipt.Audience != command.Audience
                || receipt.ActionId != command.ActionId
                || prior.RulesetHash != successorSnapshot.RulesetHash
                || prior.Setup.SetupHash != successorSnapshot.Setup.SetupHash)
                return CampaignAudienceOutcomePair.Unavailable();

            var axisBefore = CampaignHostAdapter.QueryPrivateSideCut(predecessor, CampaignActionAudience.Axis);
            var commonwealthBefore = CampaignHostAdapter.QueryPrivateSideCut(predecessor, CampaignActionAudience.Commonwealth);
            var axisAfter = CampaignHostAdapter.QueryPrivateSideCut(successor, CampaignActionAudience.Axis);
            var commonwealthAfter = CampaignHostAdapter.QueryPrivateSideCut(successor, CampaignActionAudience.Commonwealth);
            if (!Coherent(axisBefore, axisAfter, prior, successorSnapshot, CampaignActionAudience.Axis)
                || !Coherent(commonwealthBefore, commonwealthAfter, prior, successorSnapshot,
                    CampaignActionAudience.Commonwealth))
                return CampaignAudienceOutcomePair.Unavailable();

            var eventBytes = acceptedStep.EventRecords[0];
            var currentEvent = CampaignCurrentEventSerializer.Deserialize(eventBytes);
            if (!eventBytes.AsSpan().SequenceEqual(CampaignCurrentEventSerializer.Serialize(currentEvent)))
                return CampaignAudienceOutcomePair.Unavailable();
            using var parsed = JsonDocument.Parse(eventBytes);
            var eventKind = parsed.RootElement.GetProperty("eventType").GetString();
            var eventVersion = parsed.RootElement.GetProperty("contractVersion").GetInt32();
            if (eventKind is null || eventVersion < 1
                || currentEvent is not (CampaignEvent or CampaignSuccessorEvent)
                || currentEvent is CampaignCreatedV10)
                return CampaignAudienceOutcomePair.Unavailable();

            var classification = Classify(currentEvent, candidate, command.Audience);
            if (classification is null) return CampaignAudienceOutcomePair.Unavailable();

            var source = prior.Setup;
            var sourceBinding = Hash(string.Join('\n', prior.RulesetHash, source.SetupHash,
                source.Content.Pack.Hash, source.Content.ScenarioId));
            var priorHead = Hash(CampaignCurrentSnapshotSerializer.Serialize(prior));
            var successorHead = Hash(replay.Evidence.SnapshotCheckpoint);
            var campaignBinding = Hash(prior.CampaignId);
            var commandBinding = Hash(commandBytes);
            var eventBinding = Hash(eventBytes);
            CampaignAudienceOutcomeDecision Make(CampaignActionAudience audience,
                CampaignHostPrivateSideCut before, CampaignHostPrivateSideCut after)
            {
                var own = audience == command.Audience;
                var key = own ? classification.Value.CopyKey : null;
                var elementId = own ? classification.Value.ElementId : null;
                int? ownIndex = null;
                if (elementId is not null)
                {
                    var index = FindOwnIndex(before, after, elementId);
                    if (index < 0) key = null;
                    else ownIndex = index;
                }
                var disposition = key is not null
                    ? CampaignAudienceOutcomeDisposition.Unavailable
                    : CampaignAudienceOutcomeDisposition.Silent;
                var reason = key is not null ? "copy-catalog-unapproved" : "known-silent";
                if (elementId is not null && ownIndex is null)
                {
                    disposition = CampaignAudienceOutcomeDisposition.Unavailable;
                    reason = "own-element-unavailable";
                }
                return new CampaignAudienceOutcomeDecision(1, audience, campaignBinding,
                    sourceBinding, priorHead, successorHead, prior.StateVersion,
                    successorSnapshot.StateVersion, commandBinding, command.Audience,
                    candidate.Kind, eventBinding, 0, eventKind, eventVersion,
                    disposition, reason, key, ownIndex);
            }
            return new CampaignAudienceOutcomePair(Make(CampaignActionAudience.Axis, axisBefore!, axisAfter!),
                Make(CampaignActionAudience.Commonwealth, commonwealthBefore!, commonwealthAfter!));
        }
        catch (Exception error) when (error is JsonException or ArgumentException
            or InvalidOperationException or InvalidCampaignHistoryException
            or OverflowException or KeyNotFoundException)
        {
            return CampaignAudienceOutcomePair.Unavailable();
        }
    }

    private static bool Coherent(CampaignHostPrivateSideCut? before, CampaignHostPrivateSideCut? after,
        CampaignSnapshotV11 prior, CampaignSnapshotV11 successor, CampaignActionAudience audience) =>
        before is not null && after is not null && before.Audience == audience && after.Audience == audience
        && before.CampaignId == prior.CampaignId && after.CampaignId == successor.CampaignId
        && before.StateVersion == prior.StateVersion && after.StateVersion == successor.StateVersion
        && before.RulesetHash == prior.RulesetHash && after.RulesetHash == successor.RulesetHash
        && before.ScenarioId == prior.Setup.Content.ScenarioId
        && after.ScenarioId == successor.Setup.Content.ScenarioId;

    private static int FindOwnIndex(CampaignHostPrivateSideCut before, CampaignHostPrivateSideCut after,
        string elementId)
    {
        // The index is only an internal reference into the same side's coherent cut.
        var beforeIndex = Array.FindIndex(before.OwnElements.ToArray(), element => element.ElementId == elementId);
        var afterIndex = Array.FindIndex(after.OwnElements.ToArray(), element => element.ElementId == elementId);
        return beforeIndex >= 0 && afterIndex >= 0 ? afterIndex : -1;
    }

    private static (string? CopyKey, string? ElementId)? Classify(object value,
        CampaignActionCandidate candidate, CampaignActionAudience actor)
    {
        static bool Human(CampaignActionAudience actor, LandSide side) =>
            actor is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth
            && actor == (side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth);
        static bool System(CampaignActionAudience actor) => actor == CampaignActionAudience.System;
        // The replay above proves exact action/event bytes and immutable source. These typed
        // pairings additionally ensure a System action cannot acquire a human entry.
        return (value, candidate) switch
        {
            (InitiativeDetermined, ResolveInitiativeAction) when System(actor) => (null, null),
            (NoObligationNavalConvoyScheduleResolved, ResolveNoObligationNavalConvoyScheduleAction) when System(actor) => (null, null),
            (NoObligationTacticalShippingResolved, ResolveNoObligationTacticalShippingAction) when System(actor) => (null, null),
            (InitiativeOrderDeclared e, ActFirstAction) when Human(actor, e.DeclaringHolder)
                && e.FirstSide == e.DeclaringHolder => ("diary.initiativeOrder", null),
            (InitiativeOrderDeclared e, ActLastAction) when Human(actor, e.DeclaringHolder)
                && e.SecondSide == e.DeclaringHolder => ("diary.initiativeOrder", null),
            (WeatherDetermined, ResolveWeatherAction) when System(actor) => (null, null),
            (NoObligationOrganizationResolved, ResolveNoObligationOrganizationAction) when System(actor) => (null, null),
            (NoObligationNavalConvoyArrivalResolved, ResolveNoObligationNavalConvoyArrivalAction) when System(actor) => (null, null),
            (NoObligationFleetAssignmentResolved, ResolveNoObligationFleetAssignmentAction) when System(actor) => (null, null),
            (NoObligationFleetRepairResolved, ResolveNoObligationFleetRepairAction) when System(actor) => (null, null),
            (ReserveElementDesignated e, DesignateReserveAction) when Human(actor, e.ActingSide) =>
                ("diary.reserveDesignated", e.ElementId),
            (ReserveDesignationCompleted e, CompleteReserveDesignationAction) when Human(actor, e.ActingSide) =>
                ("diary.reserveCompleted", null),
            (ElementMovedV3 e, MoveElementAction) when Human(actor, e.ActingSide) =>
                ("diary.elementMoved", e.ElementId),
            (ElementMovementStopped e, StopElementMovementAction) when Human(actor, e.ActingSide) =>
                ("diary.movementStopped", null),
            (BreakdownStopResolved, ResolveBreakdownStopAction) when System(actor) => (null, null),
            (ReactingElementMovedV2 e, MoveReactingElementAction) when Human(actor, e.ActingSide) =>
                ("diary.reactionMoved", e.ElementId),
            (ReactionParticipantCompletedV2 e, CompleteReactionParticipantAction) when Human(actor, e.ActingSide) =>
                ("diary.reactionCompleted", null),
            (ReactionWindowClosedV2 e, DeclineReactionWindowAction) when e.Reason == CampaignReactionWindowCloseReason.PlayerDecline
                && e.ActingSide is { } side && Human(actor, side) => ("diary.reactionDeclined", null),
            (ReactionWindowClosedV2 e, CloseReactionWindowUnavailableAction) when System(actor)
                && e.ActingSide is null && e.Reason == CampaignReactionWindowCloseReason.ScriptedUnavailable => (null, null),
            (ReactionWindowClosedV2 e, CloseReactionWindowTimeoutAction) when System(actor)
                && e.ActingSide is null && e.Reason == CampaignReactionWindowCloseReason.Timeout => (null, null),
            (ReactionWindowClosedV2 e, CloseReactionWindowNoEligibleAction) when System(actor)
                && e.ActingSide is null && e.Reason == CampaignReactionWindowCloseReason.NoEligibleReactor => (null, null),
            (MovementSegmentCompletedV2 e, CompleteMovementSegmentAction) when Human(actor, e.ActingSide) =>
                ("diary.movementCompleted", null),
            (BreakdownSegmentCompleted, CompleteBreakdownSegmentAction) when System(actor) => (null, null),
            _ => null,
        };
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";
    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
}

public enum CampaignAudienceOutcomeDisposition { Unavailable, Silent }

/// <summary>Never serialize this host-private proof as a browser response.</summary>
public sealed record CampaignAudienceOutcomeDecision(int ContractVersion,
    CampaignActionAudience Audience, string CampaignBindingHash, string SourceBindingHash,
    string PriorHeadHash, string SuccessorHeadHash, long PriorStateVersion,
    long SuccessorStateVersion, string CommandBindingHash, CampaignActionAudience ActingAudience,
    string ActionKind, string EventHash, int EventIndex, string EventKind, int EventContractVersion,
    CampaignAudienceOutcomeDisposition Disposition, string ReasonCode, string? ProposedCopyKey,
    int? OwnElementIndex);

public sealed record CampaignAudienceOutcomePair(CampaignAudienceOutcomeDecision? Axis,
    CampaignAudienceOutcomeDecision? Commonwealth, string? UnavailableReason = null)
{
    public bool HasTypedProof => Axis is not null && Commonwealth is not null && UnavailableReason is null;
    internal static CampaignAudienceOutcomePair Unavailable() => new(null, null, "unavailable-source");
}

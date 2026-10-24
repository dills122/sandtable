using System.Text.Json.Serialization;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Hosting;

/// <summary>Trusted, in-process boundary. None of these private records are browser DTOs.</summary>
public static class CampaignHostAdapter
{
    public static ExerciseStartResult Create(CampaignCreationRequest request) =>
        CampaignExercises.Begin(request);

    /// <summary>
    /// Trusted successor input for the normal campaign host. The visible bytes contain no
    /// private authority version or Core action ID; Host must durably bind an authenticated
    /// owner opening and its private action map before any client can receive them.
    /// </summary>
    public static CampaignHostContactOwnerDraft QueryContactOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactOwnerMovementCut(session, side, actingElementId);
        return new CampaignHostContactOwnerDraft(cut.AuthorityVersion,
            CampaignContactOwnerVisibleCutV1.SerializeCanonical(cut), cut.MovementOffers);
    }

    /// <summary>Trusted Combat owner draft; only visible contact values enter outward bytes.</summary>
    public static CampaignHostContactCombatOwnerDraft QueryContactCombatOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactOwnerCombatCut(session, side, actingElementId);
        return new CampaignHostContactCombatOwnerDraft(cut.AuthorityVersion,
            CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(cut),
            cut.AssaultOffers.OrderBy(value => value.ContactHandle, StringComparer.Ordinal)
                .ThenBy(value => value.ApparentLocationId, StringComparer.Ordinal)
                .Select(value => value.ActionId)
                .Concat(cut.FinishOffer is { } finish ? [finish.ActionId] : [])
                .ToArray());
    }

    /// <summary>Trusted defender draft after accepted Barrage; action IDs stay private.</summary>
    public static CampaignHostContactRbaOwnerDraft QueryContactRbaOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience || defenderElementId != cut.DefenderElementId)
            throw new InvalidOperationException("Only the bound defender may receive this retreat cut.");
        return new CampaignHostContactRbaOwnerDraft(cut.AuthorityVersion,
            CampaignContactRetreatBeforeAssaultOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.MoveOffers.OrderBy(value => value.ElementId, StringComparer.Ordinal)
                .ThenBy(value => value.DestinationLocationId, StringComparer.Ordinal)
                .Select(value => value.ActionId).Append(cut.DeclineOffer.ActionId).ToArray());
    }

    /// <summary>Trusted post-result defender draft; Core action IDs stay in Host custody.</summary>
    public static CampaignHostContactPostAssaultRetreatOwnerDraft
        QueryContactPostAssaultRetreatOwnerDraft(ExerciseSession session,
            CampaignActionAudience audience, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience || defenderElementId != cut.DefenderElementId)
            throw new InvalidOperationException("Only the bound defender may receive this retreat cut.");
        return new CampaignHostContactPostAssaultRetreatOwnerDraft(cut.AuthorityVersion,
            CampaignContactRetreatAfterAssaultOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.MoveOffers.OrderBy(value => value.DestinationLocationId,
                StringComparer.Ordinal).Select(value => value.ActionId)
                .Append(cut.RefuseOffer.ActionId).ToArray());
    }

    /// <summary>Trusted captor draft; private authority and Core action IDs stay in Host custody.</summary>
    public static CampaignHostContactCustodyOwnerDraft QueryContactCustodyOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience,
        string captorElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)
            ?? throw new InvalidOperationException("No captive lot awaits an owner.");
        var sideId = session.Context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == cut.CaptorElementId).SideId;
        var captorAudience = sideId == "axis" ? CampaignActionAudience.Axis
            : sideId == "commonwealth" ? CampaignActionAudience.Commonwealth
            : throw new InvalidOperationException("Unknown captor side.");
        if (audience != captorAudience || captorElementId != cut.CaptorElementId)
            throw new InvalidOperationException("Only the bound captor may receive this cut.");
        return new CampaignHostContactCustodyOwnerDraft(cut.AuthorityVersion,
            CampaignContactCustodyOwnerVisibleCutV1.SerializeCanonical(cut),
            cut.Choices.Select(value => value.ActionId).ToArray());
    }

    /// <summary>Trusted Force Assignment draft for one authenticated owner.</summary>
    public static CampaignHostContactForceOwnerDraft QueryContactForceOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            throw new ArgumentOutOfRangeException(nameof(audience));
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var cut = CampaignExercises.QueryContactForceAssignmentCut(session, side);
        if (cut.SealOffer.ElementId != elementId)
            throw new InvalidOperationException("Only the bound owner may receive this seal cut.");
        return new CampaignHostContactForceOwnerDraft(
            session.CurrentContacts.AuthorityVersion,
            CampaignContactForceAssignmentOwnerVisibleCutV1.SerializeCanonical(cut),
            [cut.SealOffer.ActionId]);
    }

    /// <summary>Trusted seat routing for the pending defender decision.</summary>
    public static string QueryContactRbaDefenderId(ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        var defenderAudience = cut.DefenderSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        if (audience != defenderAudience)
            throw new InvalidOperationException("This seat does not own the pending defender decision.");
        return cut.DefenderElementId;
    }

    /// <summary>Trusted canonical contact evidence for durable campaign custody.</summary>
    public static IReadOnlyList<byte[]> QueryContactRecords(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentContacts.Records;
    }

    public static CampaignHostAudienceCut QueryAudience(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidAudience);

        var handle = new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context);
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var observation = CampaignObservations.Query(handle, side);
        var legal = CampaignLegalActions.Query(handle, audience);
        if (!observation.IsProjected || !legal.IsSuccessful)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);

        var view = observation.Observation!;
        var actions = legal.ActionSet!;
        if (view.CampaignId != actions.CampaignId
            || view.StateVersion != actions.StateVersion
            || view.RulesetHash != actions.RulesetHash
            || view.Position.PositionId != actions.PositionId)
            return CampaignHostAudienceCut.Rejected(CampaignHostQueryRejectionReason.InvalidState);

        return CampaignHostAudienceCut.Projected(view, actions);
    }

    /// <summary>One typed, trusted side cut. The representation keys and Core action IDs
    /// remain private inputs to the host's continuity and action trackers.</summary>
    public static CampaignHostPrivateSideCut? QueryPrivateSideCut(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var cut = QueryAudience(session, audience);
        if (!cut.IsProjected) return null;
        var view = cut.Observation!;
        var actions = cut.LegalActions!;
        var decisionKind = view.DecisionState switch
        {
            CampaignObservationV7NormalDecisionState => "normal",
            CampaignObservationV7BreakdownWaitingDecisionState => "breakdown-waiting",
            CampaignObservationV7PhasingWaitingDecisionState => "phasing-waiting",
            CampaignObservationV7ReactingDecisionState => "reacting",
            _ => null,
        };
        if (decisionKind is null) return null;
        return new CampaignHostPrivateSideCut(
            view.CampaignId, view.StateVersion, view.RulesetHash, view.ScenarioId,
            view.Position.PositionId, view.Position.GameTurn, view.Position.PhaseId,
            view.Position.StageId, audience,
            view.Locations.Select(location => new CampaignHostPrivateLocation(
                location.LocationId, location.TerrainId)).ToArray(),
            view.OwnElements.Select(element => new CampaignHostPrivateOwnElement(
                element.ElementId, element.ParentFormationId, element.CurrentLocationId,
                element.ReserveStatus.ToString())).ToArray(),
            view.ApparentOpposingPresences.Select(presence => new CampaignHostPrivateContact(
                presence.RepresentationId, presence.CurrentLocationId, presence.ExertsZoc)).ToArray(),
            actions.Candidates.Select(candidate => ProjectPrivateAction(view, candidate)).ToArray(),
            decisionKind);
    }

    private static CampaignHostPrivateAction ProjectPrivateAction(
        CampaignObservationV7 view, CampaignActionCandidate candidate)
    {
        var elementId = candidate switch
        {
            DesignateReserveAction reserve => reserve.ElementId,
            MoveElementAction move => move.ElementId,
            StopElementMovementAction stop
                when view.DecisionState is CampaignObservationV7NormalDecisionState
                { ActiveMovement: { } active }
                    && active.RouteId == stop.RouteId => active.ElementId,
            _ => null,
        };
        var targetLocationId = candidate switch
        {
            MoveElementAction move => move.DestinationLocationId,
            MoveReactingElementAction move => move.DestinationLocationId,
            _ => null,
        };
        var opportunityId = candidate switch
        {
            MoveReactingElementAction move => move.OpportunityId,
            CompleteReactionParticipantAction complete => complete.OpportunityId,
            _ => null,
        };
        return new CampaignHostPrivateAction(candidate.ActionId, candidate.Kind,
            elementId, targetLocationId, opportunityId);
    }

    public static CampaignLegalActionQueryResult QuerySystem(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignExercises.Query(session, CampaignActionAudience.System);
    }

    public static ExerciseStepResult Submit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.Submit(session, submission);

    /// <summary>Trusted Core submission against the contact-derived owner cut, never V7 actions.</summary>
    public static ExerciseStepResult SubmitContactOwnerMovement(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId) =>
        CampaignExercises.SubmitContactOwnerMovement(session, submission, actingElementId);

    /// <summary>Trusted native Combat selection against the same contact authority cut.</summary>
    public static ExerciseStepResult SubmitContactOwnerCombatSelection(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId) =>
        CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId);

    /// <summary>Trusted explicit defender decline at the retained RBA contact cut.</summary>
    public static ExerciseStepResult SubmitContactDefenderRbaDecline(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId) =>
        CampaignExercises.SubmitContactDefenderRbaDecline(session, submission, defenderElementId);

    public static ExerciseStepResult SubmitContactDefenderRbaMove(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId) =>
        CampaignExercises.SubmitContactDefenderRbaMove(session, submission, defenderElementId);

    public static ExerciseStepResult SubmitContactOwnerForceSeal(
        ExerciseSession session, CampaignActionSubmission submission, string elementId) =>
        CampaignExercises.SubmitContactOwnerForceSeal(session, submission, elementId);

    /// <summary>System-only structural continuation of an accepted voluntary finish.</summary>
    public static ExerciseStepResult CompleteNativeCombatNoAttack(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatNoAttack(session);

    public static ExerciseStepResult CompleteNativeRepeatCombatNoAttack(
        ExerciseSession session) =>
        CampaignExercises.CompleteNativeRepeatCombatNoAttack(session);

    /// <summary>System-only no-effect Barrage for a selected infantry assault.</summary>
    public static ExerciseStepResult CompleteNativeCombatBarrage(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatBarrage(session);

    public static ExerciseStepResult CompleteNativeCombatForceAssignment(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatForceAssignment(session);

    public static ExerciseStepResult CancelNativeCombatForceAssignment(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.CancelNativeCombatForceAssignment(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static string NoTimingForceCancellationHash =>
        CampaignNativeCombatForceCancellationV19.NoTimingConfigurationHash;

    public static ExerciseStepResult CancelNativeCombatRba(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.CancelNativeCombatRba(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static string NoTimingRbaCancellationHash =>
        CampaignNativeCombatRbaCancellationV33.NoTimingConfigurationHash;

    public static (string DecisionActionId, string BarrageEventHash)
        QueryNativeCombatRbaRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (session.CurrentRepeatAssault is
            { Barrage: { } repeatedBarrage, Decline: null, RbaCancellation: null } repeated
            && checkpoint.StateVersion == ((repeated.RetreatMoves.Count > 0 ? repeated.RetreatMoves[^1].StateVersion : (long?)null) ?? repeatedBarrage.StateVersion))
            return (repeated.Selection.Decision.ActionId,
                "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(repeatedBarrage.SerializeCanonical())));
        if (session.CurrentRepeatAssault is not null
            || session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentCombatRbaCancellation is not null
            || checkpoint.StateVersion != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            throw new InvalidOperationException("No collecting RBA round is available.");
        return (selected.Decision.ActionId,
            "sha256:" + Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(barrage.SerializeCanonical())));
    }

    public static bool CanCompleteNativeCombatForceAssignment(ExerciseSession session) =>
        CampaignExercises.CanCompleteNativeCombatForceAssignment(session);

    public static bool IsInitialNativeCombatForceRound(ExerciseSession session) =>
        session.CurrentRepeatAssault is null && session.CurrentPostRepeat is null
        && session.CurrentCombatRetreatDecline is not null
        && session.CurrentCombatForceCompletion is null
        && session.CurrentCombatForceCancellation is null;

    public static ExerciseStepResult CompleteNativeCombatAntiArmor(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatAntiArmor(session);

    public static ExerciseStepResult CompleteNativeCombatCancelledAssault(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatCancelledAssault(session);

    public static ExerciseStepResult ResolveNativeCombatAssault(ExerciseSession session) =>
        CampaignExercises.ResolveNativeCombatAssault(session);

    public static ExerciseStepResult SubmitContactPostAssaultRetreat(
        ExerciseSession session, CampaignActionSubmission submission,
        string defenderElementId) => CampaignExercises.SubmitContactPostResultRetreat(
            session, submission, defenderElementId);

    public static ExerciseStepResult CompleteNoPostAssaultRetreat(ExerciseSession session) =>
        CampaignExercises.CompleteNoPostResultRetreat(session);

    public static bool IsNativeCombatNoPostResultRetreat(ExerciseSession session) =>
        (session.CurrentCombatAssaultResult is { Facts.RequiredRetreat: 0 }
            && session.CurrentCombatPostResultRetreat is null)
        || session.CurrentRepeatAssault is
        {
            Result.Facts.RequiredRetreat: 0, PostResultRetreat: null
        };

    public static bool IsNativeCombatForcePrepared(ExerciseSession session) =>
        (session.CurrentCombatForceSeals.Count == 2
            && session.CurrentCombatForceCompletion is null)
        || session.CurrentRepeatAssault is
        {
            Seals.Count: 2,
            ForceCompletion: null
        };

    public static bool IsNativeCombatVoluntaryFinish(ExerciseSession session) =>
        session.CurrentCombatDecision?.Decision.Choice == "finish-combat-without-attack";

    public static bool IsNativeCombatSelectedAssault(ExerciseSession session) =>
        session.CurrentCombatDecision?.Decision.Choice == "select-contact-assault";

    public static bool IsNativeRepeatCombatVoluntaryFinish(ExerciseSession session) =>
        (session.CurrentPostRepeat?.Selection
            ?? session.CurrentRepeatCombatSelection)?.Choice
            == "finish-combat-without-attack";

    public static bool IsNativeRepeatCombatSelectedAssault(ExerciseSession session) =>
        (session.CurrentPostRepeat?.Selection
            ?? session.CurrentRepeatCombatSelection)?.Choice
            == "select-contact-assault";

    public static (string DecisionActionId, string DeclineEventHash)
        QueryNativeCombatForceRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentRepeatAssault is
            {
                Decline: { } repeatedDecline,
                ForceCompletion: null,
                ForceCancellation: null
            } repeated)
            return (repeated.Selection.Decision.ActionId,
                "sha256:" + Convert.ToHexStringLower(
                    System.Security.Cryptography.SHA256.HashData(
                        repeatedDecline.SerializeCanonical())));
        if (session.CurrentRepeatAssault is not null
            || session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceCancellation is not null)
            throw new InvalidOperationException("No collecting Force round is available.");
        return (selected.Decision.ActionId,
            "sha256:" + Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(decline.SerializeCanonical())));
    }

    /// <summary>Deterministic System command retained alongside the no-attack event.</summary>
    public static CampaignActionSubmission NativeCombatNoAttackCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || predecessor.CurrentCombatDecision is null
            || accepted.SuccessorSession?.CurrentNoAttackCompletion is null)
            throw new ArgumentException("Accepted no-attack completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeRepeatCombatNoAttackCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentPostRepeat?.Selection is null
                ? predecessor.CurrentRepeatCombatSelection is null
                    || accepted.SuccessorSession?.CurrentRepeatNoAttack is null
                : accepted.SuccessorSession?.CurrentPostRepeatEvents
                    is not [.., CampaignNativeRepeatCombatNoAttackV32]))
            throw new ArgumentException(
                "Accepted repeated no-attack completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion,
            checkpoint.PositionId, CampaignActionAudience.System,
            accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatBarrageCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Barrage: null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Barrage is null
                : predecessor.CurrentCombatDecision is null
                    || accepted.SuccessorSession?.CurrentCombatBarrage is null))
            throw new ArgumentException("Accepted Barrage completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatForceCompletionCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { Seals.Count: 2 }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.ForceCompletion is null
                : predecessor.CurrentCombatForceSeals.Count != 2
                    || accepted.SuccessorSession?.CurrentCombatForceCompletion is null))
            throw new ArgumentException("Accepted Force completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatForceCancellationCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { } repeated
                ? repeated.Seals.Count > 1
                    || accepted.SuccessorSession?.CurrentRepeatAssault?.ForceCancellation is null
                : predecessor.CurrentCombatForceSeals.Count > 1
                    || accepted.SuccessorSession?.CurrentCombatForceCancellation is null))
            throw new ArgumentException("Accepted Force cancellation required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatRbaCancellationCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence
            || (predecessor.CurrentRepeatAssault is not null
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.RbaCancellation is null
                : accepted.SuccessorSession?.CurrentCombatRbaCancellation is null))
            throw new ArgumentException("Accepted RBA cancellation required.", nameof(accepted));
        var cancellation = CampaignNativeCombatRbaCancellationV33.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCombatRbaRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (cancellation.CampaignId != checkpoint.CampaignId
            || cancellation.PriorStateVersion != checkpoint.StateVersion
            || cancellation.PositionId != checkpoint.PositionId
            || cancellation.DecisionActionId != binding.DecisionActionId
            || cancellation.BarrageEventHash != binding.BarrageEventHash)
            throw new ArgumentException("RBA cancellation differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatAntiArmorCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is
        { ForceCompletion: not null } or { ForceCancellation: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.AntiArmor is null
                : accepted.SuccessorSession?.CurrentCombatAntiArmor is null))
            throw new ArgumentException("Accepted Anti Armor completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatCancelledAssaultCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { ForceCancellation: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.CancelledAssault is null
                : accepted.SuccessorSession?.CurrentCombatCancelledAssault is null))
            throw new ArgumentException("Accepted cancelled assault completion required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatAssaultResultCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { AntiArmor: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Result is null
                : accepted.SuccessorSession?.CurrentCombatAssaultResult is null))
            throw new ArgumentException("Accepted native assault result required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatNoPostAssaultRetreatCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Result: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.PostResultRetreat
                    is not { Disposition: "not-required" }
                : accepted.SuccessorSession?.CurrentCombatPostResultRetreat
                    is not { Disposition: "not-required" }))
            throw new ArgumentException("Accepted no-retreat completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static ExerciseStepResult SettleNativeCombatWorld(ExerciseSession session) =>
        CampaignExercises.SettleNativeCombatWorld(session);

    public static ExerciseStepResult CompleteNoNativeCombatCustody(ExerciseSession session) =>
        CampaignExercises.CompleteNoNativeCombatCustody(session);

    public static ExerciseStepResult CompleteNativeCombatSuccessor(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatSuccessor(session);

    public static bool IsNativeReserveReleaseEmpty(ExerciseSession session)
    {
        try
        {
            var state = CampaignExercises.QueryNativeReserveReleaseState(session);
            return !state.Completed && state.Pending(session.Context).Count == 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static CampaignHostReserveReleaseOwnerDraft QueryNativeReserveReleaseOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience, string elementId)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Reserve Release needs an owner side."),
        };
        var state = CampaignExercises.QueryNativeReserveReleaseState(session);
        var cut = CampaignReserveReleaseOwnerCutV27.Project(state, session.Context,
            side, elementId);
        return new CampaignHostReserveReleaseOwnerDraft(state.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeReserveReleaseChoice(
        ExerciseSession session, CampaignActionSubmission submission,
        string elementId)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission);
        CampaignNativeReserveReleaseStateV27 state;
        CampaignReserveReleaseOwnerCutV27 cut;
        try
        {
            state = CampaignExercises.QueryNativeReserveReleaseState(session);
            var side = submission.Audience == CampaignActionAudience.Axis
                ? LandSide.Axis : submission.Audience == CampaignActionAudience.Commonwealth
                    ? LandSide.Commonwealth : (LandSide?)null;
            if (side is null)
                return ExerciseStepResult.Rejected(
                    CampaignActionSubmissionRejectionReason.ActionNotLegal);
            cut = CampaignReserveReleaseOwnerCutV27.Project(state, session.Context,
                side.Value, elementId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (submission.CampaignId != state.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != state.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != state.Position.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var ordinal = Array.FindIndex(cut.ActionIds.ToArray(), value =>
            value == submission.ActionId);
        if (ordinal < 0)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeReserveReleaseChoice(session,
            submission.Audience, elementId, cut.Member.Choices[ordinal],
            submission.ActionId);
    }

    public static (string SettlementHash, long StateVersion) QueryNativeCombatCustodyRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)
            ?? throw new InvalidOperationException("No pending custody decision.");
        if (!cut.Choices.Any(value => value.Kind == "leave-unguarded"))
            throw new InvalidOperationException("Custody fallback is outside supported source scope.");
        return (cut.SettlementHash, cut.AuthorityVersion);
    }

    public static string NoTimingCustodyTimeoutHash => CampaignNativeCombatCustodyTimeoutV36.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCombatCustody(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCombatCustody(session, configuration, openedAtUnixMilliseconds,
            deadlineUnixMilliseconds, highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCombatCustodyTimeoutCommand(ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted custody timeout is required.", nameof(accepted));
        var timeout = CampaignNativeCombatCustodyTimeoutV36.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCombatCustodyRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId || timeout.PriorStateVersion != binding.StateVersion
            || timeout.SettlementHash != binding.SettlementHash || timeout.PositionId != checkpoint.PositionId
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Custody timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static ExerciseStepResult CompleteNativeReserveRelease(ExerciseSession session) =>
        CampaignExercises.CompleteNativeReserveRelease(session);

    public static (string ReleaseEventHash, long StateVersion, int CycleOrdinal)
        QueryNativeReserveReleaseRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var release = CampaignExercises.QueryPendingNativeReserveRelease(session);
        return (release.ReleaseEventHash, release.StateVersion, release.CycleOrdinal);
    }

    public static string NoTimingReserveReleaseTimeoutHash =>
        CampaignNativeReserveReleaseTimeoutV35.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeReserveRelease(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeReserveRelease(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
            cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeReserveReleaseTimeoutCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted Reserve Release timeout is required.", nameof(accepted));
        var timeout = CampaignNativeReserveReleaseTimeoutV35.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeReserveReleaseRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId
            || timeout.PriorStateVersion != binding.StateVersion
            || timeout.ReleaseEventHash != binding.ReleaseEventHash
            || timeout.PriorEventHash != CampaignExercises.QueryPendingNativeReserveRelease(predecessor).HeadEventHash
            || timeout.CycleOrdinal != binding.CycleOrdinal
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Reserve Release timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static (string ReleaseEventHash, long StateVersion, int CycleOrdinal)
        QueryNativeCycleControlRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var release = CampaignExercises.QueryPendingNativeCycleControl(session);
        return (release.HeadEventHash, release.StateVersion, release.CycleOrdinal);
    }

    public static string NoTimingCycleControlTimeoutHash =>
        CampaignNativeCycleControlTimeoutV34.NoTimingConfigurationHash;

    public static ExerciseStepResult TimeoutNativeCycleControl(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1? configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.TimeoutNativeCycleControl(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds, highWaterUnixMilliseconds,
            cancelledAtUnixMilliseconds, reason);

    public static CampaignActionSubmission NativeCycleControlTimeoutCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(accepted);
        if (!accepted.IsAccepted || accepted.Evidence is not { EventRecords.Count: 1 } evidence)
            throw new ArgumentException("Accepted cycle timeout is required.", nameof(accepted));
        var timeout = CampaignNativeCycleControlTimeoutV34.Deserialize(evidence.EventRecords[0]);
        var binding = QueryNativeCycleControlRoundBinding(predecessor);
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        if (timeout.CampaignId != checkpoint.CampaignId
            || timeout.PriorStateVersion != binding.StateVersion
            || timeout.PriorEventHash != binding.ReleaseEventHash
            || timeout.PriorCycleOrdinal != binding.CycleOrdinal
            || evidence.Receipt.Audience != CampaignActionAudience.System)
            throw new ArgumentException("Cycle timeout differs from predecessor.", nameof(accepted));
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, evidence.Receipt.ActionId);
    }

    public static CampaignHostCycleControlOwnerDraft QueryNativeCycleControlOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Cycle control needs an owner side."),
        };
        if ((session.CurrentRepeatAssault?.Release is not null
                ? session.CurrentRepeatAssault.Control
                : session.CurrentPostRepeat is not null
                    ? session.CurrentPostRepeat.Control
                    : session.CurrentCycleControl) is not null)
            throw new InvalidOperationException("Cycle control is already committed.");
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        var cut = CampaignCycleControlOwnerCutV28.Project(release,
            session.Context, session.CurrentContacts, side);
        return new CampaignHostCycleControlOwnerDraft(release.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeCycleControl(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        CampaignNativeReserveReleaseStateV27 release;
        CampaignCycleControlOwnerCutV28 cut;
        try
        {
            release = CampaignExercises.QueryNativeReserveReleaseState(session);
            var side = submission.Audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Cycle control needs an owner side."),
            };
            if ((session.CurrentRepeatAssault?.Release is not null
                    ? session.CurrentRepeatAssault.Control
                    : session.CurrentPostRepeat is not null
                        ? session.CurrentPostRepeat.Control
                        : session.CurrentCycleControl) is not null)
                throw new InvalidOperationException("Cycle control is already committed.");
            cut = CampaignCycleControlOwnerCutV28.Project(release,
                session.Context, session.CurrentContacts, side);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (submission.CampaignId != release.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != release.StateVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != release.Position.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var ordinal = Array.FindIndex(cut.ActionIds.ToArray(), value =>
            value == submission.ActionId);
        if (ordinal < 0)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeCycleControl(session,
            submission.Audience, cut.Choices[ordinal], submission.ActionId);
    }

    public static CampaignHostRepeatMovementOwnerDraft QueryNativeRepeatMovementOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Repeated Movement needs an owner side."),
        };
        if ((session.CurrentPostRepeat?.Movement is not null
                || session.CurrentPostRepeat is null
                    && session.CurrentRepeatAssault?.Control is null
                    && session.CurrentRepeatMove is not null)
            || (session.CurrentPostRepeat is not null
                ? session.CurrentPostRepeat.Control
                : session.CurrentRepeatAssault?.Control ?? session.CurrentCycleControl) is not { } control)
            throw new InvalidOperationException("Repeated Movement is unavailable.");
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        var cut = CampaignRepeatMovementOwnerCutV29.Project(release, control,
            session.Context, session.CurrentContacts, side);
        return new CampaignHostRepeatMovementOwnerDraft(control.StateVersion,
            cut.VisibleBytes, cut.ActionIds);
    }

    public static ExerciseStepResult SubmitNativeRepeatMovement(
        ExerciseSession session, CampaignActionSubmission submission)
    {
        CampaignHostRepeatMovementOwnerDraft draft;
        try
        {
            draft = QueryNativeRepeatMovementOwnerDraft(session,
                submission.Audience);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != draft.AuthorityVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateActionIds.Contains(submission.ActionId, StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeRepeatMovement(session,
            submission.Audience, submission.ActionId);
    }

    public static CampaignHostContactCombatOwnerDraft QueryNativeRepeatCombatOwnerDraft(
        ExerciseSession session, CampaignActionAudience audience,
        string actingElementId)
    {
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new InvalidOperationException(
                "Repeated Combat needs an owner side."),
        };
        var cut = CampaignExercises.QueryNativeRepeatCombatCut(session,
            side, actingElementId);
        return new CampaignHostContactCombatOwnerDraft(cut.AuthorityVersion,
            cut.VisibleBytes,
            cut.AssaultOffers.OrderBy(value => value.ContactHandle,
                    StringComparer.Ordinal)
                .ThenBy(value => value.ApparentLocationId,
                    StringComparer.Ordinal)
                .Select(value => value.ActionId)
                .Append(cut.FinishOffer.ActionId).ToArray());
    }

    public static ExerciseStepResult SubmitNativeRepeatCombat(
        ExerciseSession session, CampaignActionSubmission submission,
        string actingElementId)
    {
        CampaignHostContactCombatOwnerDraft draft;
        try
        {
            draft = QueryNativeRepeatCombatOwnerDraft(session,
                submission.Audience, actingElementId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException or InvalidCampaignHistoryException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != draft.AuthorityVersion)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        if (!draft.PrivateCombatActionIds.Contains(submission.ActionId,
                StringComparer.Ordinal))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return CampaignExercises.SubmitNativeRepeatCombat(session,
            submission.Audience, actingElementId, submission.ActionId);
    }

    public static CampaignActionSubmission NativeReserveReleaseCompletionCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || accepted.SuccessorSession?.CurrentReserveRelease is not { Completed: true })
            throw new ArgumentException("Accepted Reserve Release completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static ExerciseStepResult CompleteNativeRepeatBreakdown(
        ExerciseSession session) =>
        CampaignExercises.CompleteNativeRepeatBreakdown(session);

    public static CampaignActionSubmission NativeRepeatBreakdownCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || accepted.SuccessorSession?.CurrentRepeatBreakdown is null)
            throw new ArgumentException("Accepted repeated Breakdown required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatSuccessorCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Custody: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Successor is null
                : accepted.SuccessorSession?.CurrentCombatSuccessor is null))
            throw new ArgumentException("Accepted Combat successor required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static bool IsNativeCombatNoCustody(ExerciseSession session) =>
        (session.CurrentCombatSettlement is { PendingCaptives.Count: 0 }
            && session.CurrentCombatCustodyDisposition is null)
        || session.CurrentRepeatAssault is
        {
            Settlement.PendingCaptives.Count: 0,
            Custody: null
        };

    public static bool IsNativeCombatCustodyPending(ExerciseSession session) =>
        (session.CurrentCombatSettlement is { PendingCaptives.Count: > 0 }
            && session.CurrentCombatCustodyDisposition is null)
        || session.CurrentRepeatAssault is
        {
            Settlement.PendingCaptives.Count: > 0,
            Custody: null
        };

    public static ExerciseStepResult SubmitNativeCombatCustody(
        ExerciseSession session, CampaignActionSubmission submission,
        string captorElementId)
    {
        if (string.IsNullOrWhiteSpace(captorElementId))
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        CampaignNativeCombatCustodyCutV25? cut;
        try { cut = CampaignExercises.QueryNativeCombatCustodyCut(session); }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
        if (cut is null || cut.CaptorElementId != captorElementId)
            return ExerciseStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        return CampaignExercises.SubmitNativeCombatCustody(session, submission);
    }

    public static CampaignActionSubmission NativeCombatSettlementCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted ||
            (predecessor.CurrentRepeatAssault is { PostResultRetreat: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Settlement is null
                : accepted.SuccessorSession?.CurrentCombatSettlement is null))
            throw new ArgumentException("Accepted native settlement required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatNoCustodyCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted
            || (predecessor.CurrentRepeatAssault is { Settlement: not null }
                ? accepted.SuccessorSession?.CurrentRepeatAssault?.Custody
                    is not { ChoiceKind: "not-required" }
                : accepted.SuccessorSession?.CurrentCombatCustodyDisposition
                    is not { ChoiceKind: "not-required" }))
            throw new ArgumentException("Accepted no-custody completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    /// <summary>Private typed decision proof for one accepted step. It neither publishes a
    /// second transition nor supplies browser diary content.</summary>
    public static CampaignAudienceOutcomePair DecideAcceptedOutcomes(
        ExerciseSession predecessor, CampaignHostAcceptedStep acceptedStep) =>
        CampaignAudienceOutcomeSource.Decide(predecessor, acceptedStep);

    /// <summary>
    /// Re-adjudicates a trusted private transcript from its pinned creation request. The caller
    /// must verify journal order, head/fence, request indexes and checkpoint binding separately.
    /// </summary>
    public static CampaignHostImportResult Import(
        CampaignCreationRequest request,
        ReadOnlyMemory<byte> creationEvent,
        ReadOnlyMemory<byte> initialSnapshot,
        IReadOnlyList<CampaignHostAcceptedStep> steps,
        IReadOnlyList<byte[]>? initialContactRecords = null,
        IReadOnlyList<IReadOnlyList<byte[]>>? stepContactRecords = null,
        CampaignDecisionTimingConfigurationV1? timing = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);
        if ((initialContactRecords is null) != (stepContactRecords is null)
            || (stepContactRecords is not null && stepContactRecords.Count != steps.Count))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
        var started = Create(request);
        if (!started.IsStarted
            || !creationEvent.Span.SequenceEqual(started.CreationEventBytes)
            || !initialSnapshot.Span.SequenceEqual(started.InitialSnapshotBytes))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation);

        var session = started.Session!;
        if (initialContactRecords is not null
            && !ContactRecordsMatch(session.CurrentContacts.Records, initialContactRecords))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation);
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            if (step is null)
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            CampaignActionSubmission submission;
            try
            {
                submission = CampaignActionSubmissionSerializer.DeserializeCanonical(
                    step.CanonicalCommand);
            }
            catch (System.Text.Json.JsonException)
            {
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }

            if (step.Kind is not (CampaignHostAcceptedStep.LegacyKind
                or CampaignHostAcceptedStep.ContactMovementKind
                or CampaignHostAcceptedStep.ContactCombatSelectionKind
                or CampaignHostAcceptedStep.NativeCombatNoAttackKind
                or CampaignHostAcceptedStep.NativeCombatBarrageKind
                or CampaignHostAcceptedStep.ContactRbaDeclineKind
                or CampaignHostAcceptedStep.ContactRbaMoveKind
                or CampaignHostAcceptedStep.ContactForceSealKind
                or CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                or CampaignHostAcceptedStep.NativeCombatForceCancellationKind
                or CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
                or CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind
                or CampaignHostAcceptedStep.NativeCycleControlTimeoutKind
                or CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind
                or CampaignHostAcceptedStep.NativeCombatAntiArmorKind
                or CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                or CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                or CampaignHostAcceptedStep.ContactPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatSettlementKind
                or CampaignHostAcceptedStep.NativeCombatNoCustodyKind
                or CampaignHostAcceptedStep.NativeCombatSuccessorKind
                or CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                or CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind
                or CampaignHostAcceptedStep.ContactCycleControlKind
                or CampaignHostAcceptedStep.ContactRepeatMovementKind
                or CampaignHostAcceptedStep.NativeRepeatBreakdownKind
                or CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind
                or CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                or CampaignHostAcceptedStep.ContactCombatCustodyKind))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            CampaignNativeCombatForceCancellationV19? forceCancellation = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try
                {
                    forceCancellation = CampaignNativeCombatForceCancellationV19.Deserialize(
                        step.EventRecords[0]);
                    if (timing is null && (forceCancellation.Reason != "force-unavailable"
                        || forceCancellation.ConfigurationHash
                            != NoTimingForceCancellationHash))
                        return CampaignHostImportResult.Rejected(
                            CampaignHostImportRejectionReason.InvalidStep);
                }
                catch (System.Text.Json.JsonException)
                {
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
            }
            CampaignNativeCombatRbaCancellationV33? rbaCancellation = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRbaCancellationKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try
                {
                    rbaCancellation = CampaignNativeCombatRbaCancellationV33.Deserialize(step.EventRecords[0]);
                    if (timing is null && (rbaCancellation.Reason != "rba-unavailable"
                        || rbaCancellation.ConfigurationHash != NoTimingRbaCancellationHash))
                        return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
                catch (System.Text.Json.JsonException)
                {
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
            }
            CampaignNativeCombatCustodyTimeoutV36? custodyTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind)
            {
                if (step.EventRecords.Count != 1) return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { custodyTimeout = CampaignNativeCombatCustodyTimeoutV36.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException) { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (custodyTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingCustodyTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeReserveReleaseTimeoutV35? reserveTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { reserveTimeout = CampaignNativeReserveReleaseTimeoutV35.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException)
                { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (reserveTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingReserveReleaseTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            CampaignNativeCycleControlTimeoutV34? cycleTimeout = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCycleControlTimeoutKind)
            {
                if (step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try { cycleTimeout = CampaignNativeCycleControlTimeoutV34.Deserialize(step.EventRecords[0]); }
                catch (System.Text.Json.JsonException)
                { return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep); }
                if (cycleTimeout.ConfigurationHash != (timing?.Hash ?? NoTimingCycleControlTimeoutHash))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            var accepted = step.Kind switch
            {
                CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind => TimeoutNativeCombatCustody(session,
                    timing, custodyTimeout!.OpenedAtUnixMilliseconds, custodyTimeout.DeadlineUnixMilliseconds,
                    custodyTimeout.HighWaterUnixMilliseconds, custodyTimeout.CancelledAtUnixMilliseconds, custodyTimeout.Reason),
                CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind => TimeoutNativeReserveRelease(session,
                    timing, reserveTimeout!.OpenedAtUnixMilliseconds, reserveTimeout.DeadlineUnixMilliseconds,
                    reserveTimeout.HighWaterUnixMilliseconds, reserveTimeout.CancelledAtUnixMilliseconds, reserveTimeout.Reason),
                CampaignHostAcceptedStep.NativeCycleControlTimeoutKind => TimeoutNativeCycleControl(session,
                    timing, cycleTimeout!.OpenedAtUnixMilliseconds, cycleTimeout.DeadlineUnixMilliseconds,
                    cycleTimeout.HighWaterUnixMilliseconds, cycleTimeout.CancelledAtUnixMilliseconds, cycleTimeout.Reason),
                CampaignHostAcceptedStep.LegacyKind => Submit(session, submission),
                CampaignHostAcceptedStep.ContactMovementKind => SubmitContactOwnerMovement(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.NativeCombatNoAttackKind => CompleteNativeCombatNoAttack(session),
                CampaignHostAcceptedStep.NativeCombatBarrageKind => CompleteNativeCombatBarrage(session),
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind =>
                    CompleteNativeCombatForceAssignment(session),
                CampaignHostAcceptedStep.NativeCombatForceCancellationKind =>
                    CancelNativeCombatForceAssignment(session, timing,
                        forceCancellation!.OpenedAtUnixMilliseconds,
                        forceCancellation.DeadlineUnixMilliseconds,
                        forceCancellation.HighWaterUnixMilliseconds,
                        forceCancellation.CancelledAtUnixMilliseconds,
                        forceCancellation.Reason),
                CampaignHostAcceptedStep.NativeCombatRbaCancellationKind =>
                    CancelNativeCombatRba(session, timing,
                        rbaCancellation!.OpenedAtUnixMilliseconds,
                        rbaCancellation.DeadlineUnixMilliseconds,
                        rbaCancellation.HighWaterUnixMilliseconds,
                        rbaCancellation.CancelledAtUnixMilliseconds,
                        rbaCancellation.Reason),
                CampaignHostAcceptedStep.NativeCombatAntiArmorKind =>
                    CompleteNativeCombatAntiArmor(session),
                CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind =>
                    CompleteNativeCombatCancelledAssault(session),
                CampaignHostAcceptedStep.NativeCombatAssaultResultKind =>
                    ResolveNativeCombatAssault(session),
                CampaignHostAcceptedStep.ContactPostAssaultRetreatKind =>
                    SubmitContactPostAssaultRetreat(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind =>
                    CompleteNoPostAssaultRetreat(session),
                CampaignHostAcceptedStep.NativeCombatSettlementKind =>
                    SettleNativeCombatWorld(session),
                CampaignHostAcceptedStep.NativeCombatNoCustodyKind =>
                    CompleteNoNativeCombatCustody(session),
                CampaignHostAcceptedStep.NativeCombatSuccessorKind =>
                    CompleteNativeCombatSuccessor(session),
                CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind =>
                    CompleteNativeReserveRelease(session),
                CampaignHostAcceptedStep.ContactReserveReleaseChoiceKind =>
                    SubmitNativeReserveReleaseChoice(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.ContactCycleControlKind =>
                    SubmitNativeCycleControl(session, submission),
                CampaignHostAcceptedStep.ContactRepeatMovementKind =>
                    SubmitNativeRepeatMovement(session, submission),
                CampaignHostAcceptedStep.NativeRepeatBreakdownKind =>
                    CompleteNativeRepeatBreakdown(session),
                CampaignHostAcceptedStep.ContactRepeatCombatSelectionKind =>
                    SubmitNativeRepeatCombat(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind =>
                    CompleteNativeRepeatCombatNoAttack(session),
                CampaignHostAcceptedStep.ContactCombatCustodyKind =>
                    SubmitNativeCombatCustody(session, submission,
                        step.ActingElementId!),
                CampaignHostAcceptedStep.ContactRbaDeclineKind => SubmitContactDefenderRbaDecline(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactRbaMoveKind => SubmitContactDefenderRbaMove(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.ContactForceSealKind => SubmitContactOwnerForceSeal(
                    session, submission, step.ActingElementId!),
                _ => SubmitContactOwnerCombatSelection(session, submission, step.ActingElementId!),
            };
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoAttackKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoAttackCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeRepeatCombatNoAttackKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeRepeatCombatNoAttackCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(
                    CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatBarrageKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatBarrageCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCompletionKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatForceCompletionCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatForceCancellationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatRbaCancellationKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatRbaCancellationCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCustodyTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(NativeCombatCustodyTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeReserveReleaseTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCycleControlTimeoutKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCycleControlTimeoutCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatAntiArmorKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatAntiArmorCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatCancelledAssaultCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatAssaultResultCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoPostAssaultRetreatCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSettlementKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatSettlementCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatNoCustodyKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatNoCustodyCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatSuccessorKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeCombatSuccessorCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (step.Kind == CampaignHostAcceptedStep.NativeReserveReleaseCompletionKind
                && (!accepted.IsAccepted || !step.CanonicalCommand.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(
                        NativeReserveReleaseCompletionCommand(session, accepted)))))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (!accepted.IsAccepted
                || !step.Matches(accepted.Evidence!))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            if (stepContactRecords is not null)
            {
                var before = session.CurrentContacts.Records;
                var after = accepted.SuccessorSession!.CurrentContacts.Records;
                if (after.Count < before.Count
                    || !ContactRecordsMatch(after.Take(before.Count).ToArray(), before)
                    || !ContactRecordsMatch(after.Skip(before.Count).ToArray(), stepContactRecords[index]))
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            }
            session = accepted.SuccessorSession!;
        }

        return CampaignHostImportResult.Verified(session);
    }

    private static bool ContactRecordsMatch(IReadOnlyList<byte[]> actual,
        IReadOnlyList<byte[]> retained) =>
        actual.Count == retained.Count
        && actual.Zip(retained).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));
}

/// <summary>Host-private authority fence and action map paired with one side-safe Core cut.</summary>
public sealed class CampaignHostContactOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly MoveElementAction[] privateMovementActions;

    internal CampaignHostContactOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<MoveElementAction> privateMovementActions)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateMovementActions = privateMovementActions.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<MoveElementAction> PrivateMovementActions =>
        Array.AsReadOnly(privateMovementActions.ToArray());
}

/// <summary>Host-private Combat action map paired with canonical side-safe owner bytes.</summary>
public sealed class CampaignHostContactCombatOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateCombatActionIds;

    internal CampaignHostContactCombatOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateCombatActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateCombatActionIds = privateCombatActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateCombatActionIds =>
        Array.AsReadOnly(privateCombatActionIds.ToArray());
}

/// <summary>Host-private defender action map paired with canonical contact-only bytes.</summary>
public sealed class CampaignHostContactRbaOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactRbaOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private post-result defender action map and side-safe bytes.</summary>
public sealed class CampaignHostContactPostAssaultRetreatOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactPostAssaultRetreatOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private captor action map and side-safe visible bytes.</summary>
public sealed class CampaignHostContactCustodyOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactCustodyOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private Reserve Release action map paired with own-member bytes.</summary>
public sealed class CampaignHostReserveReleaseOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostReserveReleaseOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private same-slot cycle choice map paired with side-safe bytes.</summary>
public sealed class CampaignHostCycleControlOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostCycleControlOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private repeated Movement action map and side-safe bytes.</summary>
public sealed class CampaignHostRepeatMovementOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostRepeatMovementOwnerDraft(long authorityVersion,
        byte[] visibleBytes, IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds =>
        Array.AsReadOnly(privateActionIds.ToArray());
}

/// <summary>Host-private Force Assignment action map and side-safe bytes.</summary>
public sealed class CampaignHostContactForceOwnerDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignHostContactForceOwnerDraft(long authorityVersion, byte[] visibleBytes,
        IReadOnlyList<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }

    [JsonIgnore]
    public long AuthorityVersion { get; }
    [JsonIgnore]
    public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore]
    public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}

public enum CampaignHostQueryRejectionReason { None, InvalidAudience, InvalidState }

// Host-private source data. These records must never be returned from an HTTP endpoint.
public sealed record CampaignHostPrivateSideCut(
    string CampaignId, long StateVersion, string RulesetHash, string ScenarioId,
    string PositionId, int GameTurn, string PhaseId, string StageId,
    CampaignActionAudience Audience,
    IReadOnlyList<CampaignHostPrivateLocation> Locations,
    IReadOnlyList<CampaignHostPrivateOwnElement> OwnElements,
    IReadOnlyList<CampaignHostPrivateContact> Contacts,
    IReadOnlyList<CampaignHostPrivateAction> Actions,
    string DecisionStateKind);
public sealed record CampaignHostPrivateLocation(string LocationId, string TerrainId);
public sealed record CampaignHostPrivateOwnElement(string ElementId, string FormationId,
    string LocationId, string ReserveStatus);
public sealed record CampaignHostPrivateContact(string RepresentationId, string LocationId,
    bool ExertsZoc);
public sealed record CampaignHostPrivateAction(string CoreActionId, string Kind,
    string? ElementId = null, string? TargetLocationId = null,
    string? OpportunityId = null);

public sealed class CampaignHostAudienceCut
{
    private CampaignHostAudienceCut(CampaignObservationV7? observation,
        CampaignLegalActionSet? legalActions, CampaignHostQueryRejectionReason rejectionReason)
    {
        Observation = observation;
        LegalActions = legalActions;
        RejectionReason = rejectionReason;
    }

    public bool IsProjected => Observation is not null;
    public CampaignObservationV7? Observation { get; }
    public CampaignLegalActionSet? LegalActions { get; }
    public CampaignHostQueryRejectionReason RejectionReason { get; }

    internal static CampaignHostAudienceCut Projected(CampaignObservationV7 observation,
        CampaignLegalActionSet legalActions) => new(observation, legalActions,
        CampaignHostQueryRejectionReason.None);
    internal static CampaignHostAudienceCut Rejected(CampaignHostQueryRejectionReason reason) =>
        new(null, null, reason);
}

/// <summary>Private replay evidence; bytes are copied on both ingress and egress.</summary>
public sealed class CampaignHostAcceptedStep
{
    public const int CurrentContractVersion = 1;
    public const int ContactMovementContractVersion = 2;
    public const int ContactCombatSelectionContractVersion = 3;
    public const int NativeCombatNoAttackContractVersion = 4;
    public const int NativeCombatBarrageContractVersion = 5;
    public const int ContactRbaDeclineContractVersion = 6;
    public const int ContactRbaMoveContractVersion = 7;
    public const int ContactForceSealContractVersion = 8;
    public const int NativeCombatForceCompletionContractVersion = 9;
    public const int NativeCombatForceCancellationContractVersion = 10;
    public const int NativeCombatAntiArmorContractVersion = 11;
    public const int NativeCombatCancelledAssaultContractVersion = 12;
    public const int NativeCombatAssaultResultContractVersion = 13;
    public const int ContactPostAssaultRetreatContractVersion = 14;
    public const int NativeCombatNoPostAssaultRetreatContractVersion = 15;
    public const int NativeCombatSettlementContractVersion = 16;
    public const int NativeCombatNoCustodyContractVersion = 17;
    public const int ContactCombatCustodyContractVersion = 18;
    public const int NativeCombatSuccessorContractVersion = 19;
    public const int NativeReserveReleaseCompletionContractVersion = 20;
    public const int ContactReserveReleaseChoiceContractVersion = 21;
    public const int ContactCycleControlContractVersion = 22;
    public const int ContactRepeatMovementContractVersion = 23;
    public const int NativeRepeatBreakdownContractVersion = 24;
    public const int ContactRepeatCombatSelectionContractVersion = 25;
    public const int NativeRepeatCombatNoAttackContractVersion = 26;
    public const int NativeCombatRbaCancellationContractVersion = 27;
    public const int NativeCycleControlTimeoutContractVersion = 28;
    public const int NativeReserveReleaseTimeoutContractVersion = 29;
    public const int NativeCombatCustodyTimeoutContractVersion = 30;
    public const string LegacyKind = "legacy-action";
    public const string ContactMovementKind = "contact-movement";
    public const string ContactCombatSelectionKind = "contact-combat-selection";
    public const string NativeCombatNoAttackKind = "native-combat-no-attack";
    public const string NativeCombatBarrageKind = "native-combat-barrage";
    public const string ContactRbaDeclineKind = "contact-rba-decline";
    public const string ContactRbaMoveKind = "contact-rba-move";
    public const string ContactForceSealKind = "contact-force-seal";
    public const string NativeCombatForceCompletionKind = "native-combat-force-completion";
    public const string NativeCombatForceCancellationKind = "native-combat-force-cancellation";
    public const string NativeCombatAntiArmorKind = "native-combat-anti-armor";
    public const string NativeCombatCancelledAssaultKind = "native-combat-cancelled-assault";
    public const string NativeCombatAssaultResultKind = "native-combat-assault-result";
    public const string ContactPostAssaultRetreatKind = "contact-post-assault-retreat";
    public const string NativeCombatNoPostAssaultRetreatKind = "native-combat-no-post-assault-retreat";
    public const string NativeCombatSettlementKind = "native-combat-world-settlement";
    public const string NativeCombatNoCustodyKind = "native-combat-no-custody";
    public const string ContactCombatCustodyKind = "contact-combat-custody";
    public const string NativeCombatSuccessorKind = "native-combat-successor";
    public const string NativeReserveReleaseCompletionKind = "native-reserve-release-completion";
    public const string ContactReserveReleaseChoiceKind = "contact-reserve-release-choice";
    public const string ContactCycleControlKind = "contact-cycle-control";
    public const string ContactRepeatMovementKind = "contact-repeat-movement";
    public const string NativeRepeatBreakdownKind = "native-repeat-breakdown";
    public const string ContactRepeatCombatSelectionKind = "contact-repeat-combat-selection";
    public const string NativeRepeatCombatNoAttackKind = "native-repeat-combat-no-attack";
    public const string NativeCombatRbaCancellationKind = "native-combat-rba-cancellation";
    public const string NativeCycleControlTimeoutKind = "native-cycle-control-timeout";
    public const string NativeReserveReleaseTimeoutKind = "native-reserve-release-timeout";
    public const string NativeCombatCustodyTimeoutKind = "native-combat-custody-timeout";

    private readonly byte[] canonicalCommand;
    private readonly byte[][] eventRecords;
    private readonly byte[] canonicalReceipt;
    private readonly byte[] snapshotCheckpoint;

    public CampaignHostAcceptedStep(byte[] canonicalCommand, IReadOnlyList<byte[]> eventRecords,
        byte[] canonicalReceipt, byte[] snapshotCheckpoint, string kind = LegacyKind,
        string? actingElementId = null)
    {
        if (kind is not (LegacyKind or ContactMovementKind or ContactCombatSelectionKind
                or NativeCombatNoAttackKind or NativeCombatBarrageKind or ContactRbaDeclineKind
                or ContactRbaMoveKind or ContactForceSealKind or NativeCombatForceCompletionKind
                or NativeCombatForceCancellationKind or NativeCombatAntiArmorKind
                or NativeCombatCancelledAssaultKind or NativeCombatAssaultResultKind
                or ContactPostAssaultRetreatKind or NativeCombatNoPostAssaultRetreatKind
                or NativeCombatSettlementKind or NativeCombatNoCustodyKind
                or ContactCombatCustodyKind or NativeCombatSuccessorKind
                or NativeReserveReleaseCompletionKind
                or ContactReserveReleaseChoiceKind or ContactCycleControlKind
                or ContactRepeatMovementKind or NativeRepeatBreakdownKind
                or ContactRepeatCombatSelectionKind or NativeRepeatCombatNoAttackKind
                or NativeCombatRbaCancellationKind or NativeCycleControlTimeoutKind or NativeReserveReleaseTimeoutKind or NativeCombatCustodyTimeoutKind)
            || (kind is ContactMovementKind or ContactCombatSelectionKind or ContactRbaDeclineKind
                or ContactRbaMoveKind or ContactForceSealKind
                or ContactPostAssaultRetreatKind or ContactCombatCustodyKind
                or ContactReserveReleaseChoiceKind or ContactCycleControlKind
                or ContactRepeatMovementKind or ContactRepeatCombatSelectionKind
                && string.IsNullOrWhiteSpace(actingElementId))
            || (kind is LegacyKind or NativeCombatNoAttackKind or NativeCombatBarrageKind
                or NativeCombatForceCompletionKind or NativeCombatForceCancellationKind
                or NativeCombatAntiArmorKind or NativeCombatCancelledAssaultKind
                or NativeCombatAssaultResultKind or NativeCombatNoPostAssaultRetreatKind
                or NativeCombatSettlementKind or NativeCombatNoCustodyKind
                or NativeCombatSuccessorKind or NativeReserveReleaseCompletionKind
                or NativeRepeatBreakdownKind or NativeRepeatCombatNoAttackKind or NativeCombatRbaCancellationKind
                or NativeCycleControlTimeoutKind or NativeReserveReleaseTimeoutKind or NativeCombatCustodyTimeoutKind
                && actingElementId is not null))
            throw new ArgumentException("Accepted step kind and acting element differ.", nameof(kind));
        ContractVersion = kind switch
        {
            LegacyKind => CurrentContractVersion,
            ContactMovementKind => ContactMovementContractVersion,
            ContactCombatSelectionKind => ContactCombatSelectionContractVersion,
            ContactRbaDeclineKind => ContactRbaDeclineContractVersion,
            ContactRbaMoveKind => ContactRbaMoveContractVersion,
            ContactForceSealKind => ContactForceSealContractVersion,
            NativeCombatForceCompletionKind => NativeCombatForceCompletionContractVersion,
            NativeCombatForceCancellationKind => NativeCombatForceCancellationContractVersion,
            NativeCombatAntiArmorKind => NativeCombatAntiArmorContractVersion,
            NativeCombatCancelledAssaultKind => NativeCombatCancelledAssaultContractVersion,
            NativeCombatAssaultResultKind => NativeCombatAssaultResultContractVersion,
            ContactPostAssaultRetreatKind => ContactPostAssaultRetreatContractVersion,
            NativeCombatNoPostAssaultRetreatKind => NativeCombatNoPostAssaultRetreatContractVersion,
            NativeCombatSettlementKind => NativeCombatSettlementContractVersion,
            NativeCombatNoCustodyKind => NativeCombatNoCustodyContractVersion,
            ContactCombatCustodyKind => ContactCombatCustodyContractVersion,
            NativeCombatSuccessorKind => NativeCombatSuccessorContractVersion,
            NativeReserveReleaseCompletionKind => NativeReserveReleaseCompletionContractVersion,
            ContactReserveReleaseChoiceKind => ContactReserveReleaseChoiceContractVersion,
            ContactCycleControlKind => ContactCycleControlContractVersion,
            ContactRepeatMovementKind => ContactRepeatMovementContractVersion,
            NativeRepeatBreakdownKind => NativeRepeatBreakdownContractVersion,
            ContactRepeatCombatSelectionKind => ContactRepeatCombatSelectionContractVersion,
            NativeRepeatCombatNoAttackKind => NativeRepeatCombatNoAttackContractVersion,
            NativeCombatRbaCancellationKind => NativeCombatRbaCancellationContractVersion,
            NativeCycleControlTimeoutKind => NativeCycleControlTimeoutContractVersion,
            NativeReserveReleaseTimeoutKind => NativeReserveReleaseTimeoutContractVersion,
            NativeCombatCustodyTimeoutKind => NativeCombatCustodyTimeoutContractVersion,
            NativeCombatNoAttackKind => NativeCombatNoAttackContractVersion,
            _ => NativeCombatBarrageContractVersion,
        };
        Kind = kind;
        ActingElementId = actingElementId;
        this.canonicalCommand = canonicalCommand?.ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalCommand));
        ArgumentNullException.ThrowIfNull(eventRecords);
        if (eventRecords.Count == 0 || eventRecords.Any(value => value is null))
            throw new ArgumentException("Accepted steps require event records.", nameof(eventRecords));
        this.eventRecords = eventRecords.Select(value => value.ToArray()).ToArray();
        this.canonicalReceipt = canonicalReceipt?.ToArray()
            ?? throw new ArgumentNullException(nameof(canonicalReceipt));
        this.snapshotCheckpoint = snapshotCheckpoint?.ToArray()
            ?? throw new ArgumentNullException(nameof(snapshotCheckpoint));
    }

    public byte[] CanonicalCommand => canonicalCommand.ToArray();
    public int ContractVersion { get; }
    public string Kind { get; }
    public string? ActingElementId { get; }
    public IReadOnlyList<byte[]> EventRecords =>
        Array.AsReadOnly(eventRecords.Select(value => value.ToArray()).ToArray());
    public byte[] CanonicalReceipt => canonicalReceipt.ToArray();
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();

    internal bool Matches(ExerciseStepEvidence evidence) =>
        canonicalReceipt.AsSpan().SequenceEqual(
            CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt))
        && snapshotCheckpoint.AsSpan().SequenceEqual(evidence.SnapshotCheckpoint)
        && eventRecords.Length == evidence.EventRecords.Count
        && eventRecords.Zip(evidence.EventRecords).All(pair =>
            pair.First.AsSpan().SequenceEqual(pair.Second));
}

public enum CampaignHostImportRejectionReason { None, InvalidCreation, InvalidStep }

public sealed class CampaignHostImportResult
{
    private CampaignHostImportResult(ExerciseSession? session,
        CampaignHostImportRejectionReason rejectionReason)
    {
        Session = session;
        RejectionReason = rejectionReason;
    }

    public bool IsVerified => Session is not null;
    public ExerciseSession? Session { get; }
    public CampaignHostImportRejectionReason RejectionReason { get; }

    internal static CampaignHostImportResult Verified(ExerciseSession session) =>
        new(session, CampaignHostImportRejectionReason.None);
    internal static CampaignHostImportResult Rejected(CampaignHostImportRejectionReason reason) =>
        new(null, reason);
}

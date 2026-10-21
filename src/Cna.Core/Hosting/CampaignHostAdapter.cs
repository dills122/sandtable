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

    /// <summary>System-only no-effect Barrage for a selected infantry assault.</summary>
    public static ExerciseStepResult CompleteNativeCombatBarrage(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatBarrage(session);

    public static ExerciseStepResult CompleteNativeCombatForceAssignment(ExerciseSession session) =>
        CampaignExercises.CompleteNativeCombatForceAssignment(session);

    public static ExerciseStepResult CancelNativeCombatForceAssignment(ExerciseSession session,
        CampaignDecisionTimingConfigurationV1 configuration, long openedAtUnixMilliseconds,
        long deadlineUnixMilliseconds, long highWaterUnixMilliseconds,
        long? cancelledAtUnixMilliseconds, string reason) =>
        CampaignExercises.CancelNativeCombatForceAssignment(session, configuration,
            openedAtUnixMilliseconds, deadlineUnixMilliseconds,
            highWaterUnixMilliseconds, cancelledAtUnixMilliseconds, reason);

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
        session.CurrentCombatAssaultResult is { Facts.RequiredRetreat: 0 }
        && session.CurrentCombatPostResultRetreat is null;

    public static bool IsNativeCombatForcePrepared(ExerciseSession session) =>
        session.CurrentCombatForceSeals.Count == 2
        && session.CurrentCombatForceCompletion is null;

    public static bool IsNativeCombatVoluntaryFinish(ExerciseSession session) =>
        session.CurrentCombatDecision?.Decision.Choice == "finish-combat-without-attack";

    public static bool IsNativeCombatSelectedAssault(ExerciseSession session) =>
        session.CurrentCombatDecision?.Decision.Choice == "select-contact-assault";

    public static (string DecisionActionId, string DeclineEventHash)
        QueryNativeCombatForceRoundBinding(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
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

    public static CampaignActionSubmission NativeCombatBarrageCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || predecessor.CurrentCombatDecision is null
            || accepted.SuccessorSession?.CurrentCombatBarrage is null)
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
        if (!accepted.IsAccepted || predecessor.CurrentCombatForceSeals.Count != 2
            || accepted.SuccessorSession?.CurrentCombatForceCompletion is null)
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
        if (!accepted.IsAccepted || predecessor.CurrentCombatForceSeals.Count > 1
            || accepted.SuccessorSession?.CurrentCombatForceCancellation is null)
            throw new ArgumentException("Accepted Force cancellation required.", nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static CampaignActionSubmission NativeCombatAntiArmorCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.SuccessorSession?.CurrentCombatAntiArmor is null)
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
        if (!accepted.IsAccepted || accepted.SuccessorSession?.CurrentCombatCancelledAssault is null)
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
        if (!accepted.IsAccepted || accepted.SuccessorSession?.CurrentCombatAssaultResult is null)
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
            || accepted.SuccessorSession?.CurrentCombatPostResultRetreat
                is not { Disposition: "not-required" })
            throw new ArgumentException("Accepted no-retreat completion required.",
                nameof(accepted));
        var checkpoint = CampaignExercises.QueryCheckpoint(predecessor);
        return new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
            CampaignActionAudience.System, accepted.Evidence!.Receipt.ActionId);
    }

    public static ExerciseStepResult SettleNativeCombatWorld(ExerciseSession session) =>
        CampaignExercises.SettleNativeCombatWorld(session);

    public static CampaignActionSubmission NativeCombatSettlementCommand(
        ExerciseSession predecessor, ExerciseStepResult accepted)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        if (!accepted.IsAccepted || accepted.SuccessorSession?.CurrentCombatSettlement is null)
            throw new ArgumentException("Accepted native settlement required.", nameof(accepted));
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
                or CampaignHostAcceptedStep.NativeCombatAntiArmorKind
                or CampaignHostAcceptedStep.NativeCombatCancelledAssaultKind
                or CampaignHostAcceptedStep.NativeCombatAssaultResultKind
                or CampaignHostAcceptedStep.ContactPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatNoPostAssaultRetreatKind
                or CampaignHostAcceptedStep.NativeCombatSettlementKind))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            CampaignNativeCombatForceCancellationV19? forceCancellation = null;
            if (step.Kind == CampaignHostAcceptedStep.NativeCombatForceCancellationKind)
            {
                if (timing is null || step.EventRecords.Count != 1)
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                try
                {
                    forceCancellation = CampaignNativeCombatForceCancellationV19.Deserialize(
                        step.EventRecords[0]);
                }
                catch (System.Text.Json.JsonException)
                {
                    return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
                }
            }
            var accepted = step.Kind switch
            {
                CampaignHostAcceptedStep.LegacyKind => Submit(session, submission),
                CampaignHostAcceptedStep.ContactMovementKind => SubmitContactOwnerMovement(
                    session, submission, step.ActingElementId!),
                CampaignHostAcceptedStep.NativeCombatNoAttackKind => CompleteNativeCombatNoAttack(session),
                CampaignHostAcceptedStep.NativeCombatBarrageKind => CompleteNativeCombatBarrage(session),
                CampaignHostAcceptedStep.NativeCombatForceCompletionKind =>
                    CompleteNativeCombatForceAssignment(session),
                CampaignHostAcceptedStep.NativeCombatForceCancellationKind =>
                    CancelNativeCombatForceAssignment(session, timing!,
                        forceCancellation!.OpenedAtUnixMilliseconds,
                        forceCancellation.DeadlineUnixMilliseconds,
                        forceCancellation.HighWaterUnixMilliseconds,
                        forceCancellation.CancelledAtUnixMilliseconds,
                        forceCancellation.Reason),
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
                or NativeCombatSettlementKind)
            || (kind is ContactMovementKind or ContactCombatSelectionKind or ContactRbaDeclineKind
                or ContactRbaMoveKind or ContactForceSealKind
                or ContactPostAssaultRetreatKind
                && string.IsNullOrWhiteSpace(actingElementId))
            || (kind is LegacyKind or NativeCombatNoAttackKind or NativeCombatBarrageKind
                or NativeCombatForceCompletionKind or NativeCombatForceCancellationKind
                or NativeCombatAntiArmorKind or NativeCombatCancelledAssaultKind
                or NativeCombatAssaultResultKind or NativeCombatNoPostAssaultRetreatKind
                or NativeCombatSettlementKind
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

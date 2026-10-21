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

    public static CampaignHostAudienceCut QueryAudience(
        ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
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
        IReadOnlyList<CampaignHostAcceptedStep> steps)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);
        var started = Create(request);
        if (!started.IsStarted
            || !creationEvent.Span.SequenceEqual(started.CreationEventBytes)
            || !initialSnapshot.Span.SequenceEqual(started.InitialSnapshotBytes))
            return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidCreation);

        var session = started.Session!;
        foreach (var step in steps)
        {
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

            var accepted = Submit(session, submission);
            if (!accepted.IsAccepted
                || !step.Matches(accepted.Evidence!))
                return CampaignHostImportResult.Rejected(CampaignHostImportRejectionReason.InvalidStep);
            session = accepted.SuccessorSession!;
        }

        return CampaignHostImportResult.Verified(session);
    }
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

    private readonly byte[] canonicalCommand;
    private readonly byte[][] eventRecords;
    private readonly byte[] canonicalReceipt;
    private readonly byte[] snapshotCheckpoint;

    public CampaignHostAcceptedStep(byte[] canonicalCommand, IReadOnlyList<byte[]> eventRecords,
        byte[] canonicalReceipt, byte[] snapshotCheckpoint)
    {
        ContractVersion = CurrentContractVersion;
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

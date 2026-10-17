using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Hosting;

/// <summary>Trusted, in-process boundary. None of these private records are browser DTOs.</summary>
public static class CampaignHostAdapter
{
    public static ExerciseStartResult Create(CampaignCreationRequest request) =>
        CampaignExercises.Begin(request);

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

    public static CampaignLegalActionQueryResult QuerySystem(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignExercises.Query(session, CampaignActionAudience.System);
    }

    public static ExerciseStepResult Submit(
        ExerciseSession session, CampaignActionSubmission submission) =>
        CampaignExercises.Submit(session, submission);

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

public enum CampaignHostQueryRejectionReason { None, InvalidAudience, InvalidState }

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

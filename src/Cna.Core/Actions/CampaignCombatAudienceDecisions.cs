using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Actions;

// Task 020A0 (dormant): Core-owned audience-safe Combat decision surface. It is the
// in-process counterpart of the Host owner opening (CampaignDurableAuthorityJournal.Openings):
// the outward record carries only owner-visible values and the side contact audience revision,
// never the private authority version, Core action IDs or bound defender. Internal until the
// CMB-CON-005 successor is frozen; see docs/work/plans/2026-11-20-task-020-combat-legal-actions-scoping.md.

/// <summary>One audience-safe Combat decision derived from a single authority cut.</summary>
internal sealed class CampaignCombatAudienceDecision
{
    public const int CurrentContractVersion = 1;
    private readonly byte[] visibleBytes;
    private readonly string[] offerRefs;

    internal CampaignCombatAudienceDecision(string campaignId, CampaignActionAudience audience,
        string stage, string actingElementId, long audienceRevision, byte[] visibleBytes,
        IReadOnlyList<string> offerRefs)
    {
        ContractVersion = CurrentContractVersion;
        CampaignId = campaignId;
        Audience = audience;
        Stage = stage;
        ActingElementId = actingElementId;
        AudienceRevision = audienceRevision;
        this.visibleBytes = visibleBytes.ToArray();
        VisibleDigest = CampaignCombatAudienceDecisions.Digest(visibleBytes);
        this.offerRefs = offerRefs.ToArray();
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public CampaignActionAudience Audience { get; }
    public string Stage { get; }
    public string ActingElementId { get; }
    public long AudienceRevision { get; }
    public string VisibleDigest { get; }
    public byte[] VisibleBytes => visibleBytes.ToArray();
    public IReadOnlyList<string> OfferRefs => Array.AsReadOnly(offerRefs.ToArray());

    /// <summary>Complete outward bytes: every field an audience may receive, and nothing else.</summary>
    public byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteString("audience", CampaignCombatAudienceDecisions.AudienceName(Audience));
            writer.WriteString("stage", Stage);
            writer.WriteString("actingElementId", ActingElementId);
            writer.WriteNumber("audienceRevision", AudienceRevision);
            writer.WriteString("visibleDigest", VisibleDigest);
            writer.WritePropertyName("visible");
            writer.WriteRawValue(visibleBytes);
            writer.WriteStartArray("offerRefs");
            foreach (var value in offerRefs) writer.WriteStringValue(value);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignCombatAudienceSubmission Choose(int ordinal) => new(
        CampaignCombatAudienceSubmission.CurrentContractVersion, CampaignId, Audience, Stage,
        ActingElementId, AudienceRevision, VisibleDigest, offerRefs[ordinal]);
}

/// <summary>Audience submission: names an offered public ref against the opened visible cut.</summary>
internal sealed record CampaignCombatAudienceSubmission(
    int ContractVersion,
    string CampaignId,
    CampaignActionAudience Audience,
    string Stage,
    string ActingElementId,
    long AudienceRevision,
    string VisibleDigest,
    string OfferRef)
{
    public const int CurrentContractVersion = 1;
}

/// <summary>
/// Outcome of an audience submission. The outward envelope names only the accepted ref, or one
/// generic rejection. The successor step stays with the trusted in-process caller.
/// </summary>
internal sealed class CampaignCombatAudienceSubmissionResult
{
    private CampaignCombatAudienceSubmissionResult(string? acceptedRef, ExerciseStepResult? trustedStep)
    {
        AcceptedRef = acceptedRef;
        TrustedStep = trustedStep;
    }

    public bool IsAccepted => AcceptedRef is not null;
    public string? AcceptedRef { get; }
    internal ExerciseStepResult? TrustedStep { get; }

    public byte[] SerializeOutwardCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            if (AcceptedRef is { } accepted)
            {
                writer.WriteString("outcome", "accepted");
                writer.WriteString("offerRef", accepted);
            }
            else writer.WriteString("outcome", "rejected");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignCombatAudienceSubmissionResult Accepted(string offerRef, ExerciseStepResult step) =>
        new(offerRef, step);

    internal static readonly CampaignCombatAudienceSubmissionResult Rejected = new(null, null);
}

internal static class CampaignCombatAudienceDecisions
{
    public const string SelectionStage = "combat-selection";
    public const string ForceAssignmentStage = "force-assignment";
    private const string RefDomain = "sandtable.combat-audience-ref.v1";
    private const string RefPrefix = "cmbref.v1.";

    /// <summary>
    /// Owner Combat Selection decision for the acting side's first-cut position. Returns null for
    /// every reason a decision is unavailable, so the caller cannot distinguish hidden causes.
    /// </summary>
    public static CampaignCombatAudienceDecision? QuerySelection(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId) =>
        Query(session, audience, SelectionStage, actingElementId);

    /// <summary>Owner Force Assignment seal decision; the other owner's seal never changes it.</summary>
    public static CampaignCombatAudienceDecision? QueryForceAssignment(ExerciseSession session,
        CampaignActionAudience audience, string elementId) =>
        Query(session, audience, ForceAssignmentStage, elementId);

    public static CampaignCombatAudienceDecision? Query(ExerciseSession session,
        CampaignActionAudience audience, string stage, string actingElementId) =>
        Project(session, audience, stage, actingElementId)?.Decision;

    public static CampaignCombatAudienceSubmissionResult SubmitSelection(ExerciseSession session,
        CampaignCombatAudienceSubmission submission) =>
        submission?.Stage == SelectionStage ? Submit(session, submission)
            : CampaignCombatAudienceSubmissionResult.Rejected;

    public static CampaignCombatAudienceSubmissionResult SubmitForceAssignment(ExerciseSession session,
        CampaignCombatAudienceSubmission submission) =>
        submission?.Stage == ForceAssignmentStage ? Submit(session, submission)
            : CampaignCombatAudienceSubmissionResult.Rejected;

    public static CampaignCombatAudienceSubmissionResult Submit(ExerciseSession session,
        CampaignCombatAudienceSubmission submission)
    {
        if (session is null || submission is null
            || submission.ContractVersion != CampaignCombatAudienceSubmission.CurrentContractVersion
            || string.IsNullOrWhiteSpace(submission.OfferRef))
            return CampaignCombatAudienceSubmissionResult.Rejected;
        var projected = Project(session, submission.Audience, submission.Stage, submission.ActingElementId);
        if (projected is not { } current) return CampaignCombatAudienceSubmissionResult.Rejected;
        var decision = current.Decision;
        // Same-cut rule: a timely ref stays valid while the owner-visible bytes are unchanged, even
        // if private authority has advanced. Anything else is one generic rejection.
        if (submission.CampaignId != decision.CampaignId
            || submission.AudienceRevision != decision.AudienceRevision
            || submission.VisibleDigest != decision.VisibleDigest)
            return CampaignCombatAudienceSubmissionResult.Rejected;
        var ordinal = -1;
        for (var index = 0; index < decision.OfferRefs.Count; index++)
        {
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(decision.OfferRefs[index]),
                    Encoding.UTF8.GetBytes(submission.OfferRef)))
                ordinal = index;
        }
        if (ordinal < 0) return CampaignCombatAudienceSubmissionResult.Rejected;

        ExerciseStepResult step;
        try
        {
            var checkpoint = CampaignExercises.QueryCheckpoint(session);
            var command = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                decision.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
                decision.Audience, current.PrivateActionIds[ordinal]);
            step = decision.Stage switch
            {
                SelectionStage => CampaignExercises.SubmitContactOwnerCombatSelection(session,
                    command, decision.ActingElementId),
                ForceAssignmentStage => CampaignExercises.SubmitContactOwnerForceSeal(session,
                    command, decision.ActingElementId),
                _ => throw new InvalidOperationException("Unsupported Combat audience stage."),
            };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return CampaignCombatAudienceSubmissionResult.Rejected;
        }
        return step.IsAccepted
            ? CampaignCombatAudienceSubmissionResult.Accepted(submission.OfferRef, step)
            : CampaignCombatAudienceSubmissionResult.Rejected;
    }

    internal static string AudienceName(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    internal static string Digest(byte[] bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static string Reference(string campaignId, CampaignActionAudience audience,
        string stage, string actingElementId, string visibleDigest, int ordinal)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain", RefDomain);
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("audience", AudienceName(audience));
            writer.WriteString("stage", stage);
            writer.WriteString("actingElementId", actingElementId);
            writer.WriteString("visibleDigest", visibleDigest);
            writer.WriteNumber("ordinal", ordinal);
            writer.WriteEndObject();
        }
        return RefPrefix + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static (CampaignCombatAudienceDecision Decision, IReadOnlyList<string> PrivateActionIds)?
        Project(ExerciseSession? session, CampaignActionAudience audience, string? stage,
            string? actingElementId)
    {
        if (session is null || string.IsNullOrWhiteSpace(actingElementId)
            || audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return null;
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        CampaignContactObservationV1 observation;
        byte[] visible;
        string[] privateIds;
        try
        {
            switch (stage)
            {
                case SelectionStage:
                    {
                        var cut = CampaignExercises.QueryContactOwnerCombatCut(session, side, actingElementId);
                        observation = cut.OwnerCut.ContactObservation;
                        visible = CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(cut);
                        // Ordinal order matches CampaignHostAdapter.QueryContactCombatOwnerDraft so both
                        // surfaces enumerate the same offers in the same order.
                        privateIds = cut.AssaultOffers
                            .OrderBy(value => value.ContactHandle, StringComparer.Ordinal)
                            .ThenBy(value => value.ApparentLocationId, StringComparer.Ordinal)
                            .Select(value => value.ActionId)
                            .Concat(cut.FinishOffer is { } finish ? [finish.ActionId] : [])
                            .ToArray();
                        break;
                    }
                case ForceAssignmentStage:
                    {
                        var cut = CampaignExercises.QueryContactForceAssignmentCut(session, side);
                        if (cut.SealOffer.ElementId != actingElementId || cut.OwnerSide != side) return null;
                        observation = cut.ContactObservation;
                        visible = CampaignContactForceAssignmentOwnerVisibleCutV1.SerializeCanonical(cut);
                        privateIds = [cut.SealOffer.ActionId];
                        break;
                    }
                default:
                    return null;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return null;
        }
        if (observation.Side != side) return null;
        var campaignId = observation.CampaignId;
        var digest = Digest(visible);
        var stageId = stage!;
        var refs = privateIds.Select((_, index) => Reference(campaignId, audience,
            stageId, actingElementId, digest, index)).ToArray();
        return (new CampaignCombatAudienceDecision(campaignId, audience, stageId,
            actingElementId, observation.AudienceRevision, visible, refs), privateIds);
    }
}

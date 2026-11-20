using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Actions;

// Task 020: Core-owned audience-safe Combat decision surface. It is the in-process counterpart
// of the Host owner opening (CampaignDurableAuthorityJournal.Openings): the outward record carries
// only owner-visible values and the side contact audience revision, never the private authority
// version, Core action IDs or bound defender. The six v1 stages' bytes are the frozen
// combat-audience-decision-v1 contract (docs/specs/combat-audience-decision-v1.md); stages added after
// it (020E, owner ruling: v2 superset) carry envelope contractVersion 2 and leave v1 bytes untouched.
// Since 020D (2026-11-20) the four types
// below are public C# API for later in-process consumers (021/022); constructors, the trusted
// successor step, the ref derivation and the private projection stay internal, and
// AuthorityBoundaryTests pins the exact public member set. Public C# visibility is not HTTP
// exposure: the Host route stays loopback-only Development (scoping doc §7l).

/// <summary>One audience-safe Combat decision derived from a single authority cut.</summary>
public sealed class CampaignCombatAudienceDecision
{
    /// <summary>Envelope version of the six frozen combat-audience-decision-v1 stages.</summary>
    public const int CurrentContractVersion = 1;
    /// <summary>
    /// Envelope version of the stages the v2 superset adds (owner ruling 2026-11-20, option a). The
    /// six v1 stages keep version 1 and byte-identical envelopes.
    /// </summary>
    public const int SuccessorContractVersion = 2;
    private readonly byte[] visibleBytes;
    private readonly string[] offerRefs;

    internal CampaignCombatAudienceDecision(string campaignId, CampaignActionAudience audience,
        string stage, string actingElementId, long audienceRevision, byte[] visibleBytes,
        IReadOnlyList<string> offerRefs)
    {
        ContractVersion = CampaignCombatAudienceDecisions.ContractVersionFor(stage);
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
        ContractVersion, CampaignId, Audience, Stage,
        ActingElementId, AudienceRevision, VisibleDigest, offerRefs[ordinal]);
}

/// <summary>Audience submission: names an offered public ref against the opened visible cut.</summary>
public sealed record CampaignCombatAudienceSubmission(
    int ContractVersion,
    string CampaignId,
    CampaignActionAudience Audience,
    string Stage,
    string ActingElementId,
    long AudienceRevision,
    string VisibleDigest,
    string OfferRef)
{
    /// <summary>
    /// v1 submission version. A submission must carry its decision's envelope version: 1 for the six
    /// v1 stages, <see cref="CampaignCombatAudienceDecision.SuccessorContractVersion"/> for v2 stages.
    /// </summary>
    public const int CurrentContractVersion = 1;
}

/// <summary>
/// Outcome of an audience submission. The outward envelope names only the accepted ref, or one
/// generic rejection. The successor step stays with the trusted in-process caller.
/// </summary>
public sealed class CampaignCombatAudienceSubmissionResult
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

public static class CampaignCombatAudienceDecisions
{
    public const string SelectionStage = "combat-selection";
    public const string ForceAssignmentStage = "force-assignment";
    public const string ReserveReleaseStage = "reserve-release";
    public const string CycleControlStage = "cycle-control";
    /// <summary>Cycle control belongs to the side, not an element; this fixed subject names it.</summary>
    public const string CycleControlSubject = "cycle-control";
    public const string CustodyStage = "custody";
    /// <summary>Decline-only Retreat Before Assault (bounded CMB-POL-001; T020-Q7 option b).</summary>
    public const string RbaDeclineStage = "rba-decline";
    /// <summary>Bound defender's one-hex retreat or explicit refusal after an assault result (020E1, v2).</summary>
    public const string PostAssaultRetreatStage = "post-assault-retreat";
    /// <summary>Repeated-cycle Combat selection (Core V31) after a repeated Movement (020E2, v2).</summary>
    public const string RepeatCombatSelectionStage = "repeat-combat-selection";
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

    /// <summary>Next ordered owned Reserve Release member decision (020A2).</summary>
    public static CampaignCombatAudienceDecision? QueryReserveRelease(ExerciseSession session,
        CampaignActionAudience audience, string reserveElementId) =>
        Query(session, audience, ReserveReleaseStage, reserveElementId);

    /// <summary>Bound defender's decline-only Retreat Before Assault decision (020A4).</summary>
    public static CampaignCombatAudienceDecision? QueryRbaDecline(ExerciseSession session,
        CampaignActionAudience audience, string defenderElementId) =>
        Query(session, audience, RbaDeclineStage, defenderElementId);

    /// <summary>Repeat-or-finish decision after completed Reserve Release (020A2).</summary>
    public static CampaignCombatAudienceDecision? QueryCycleControl(ExerciseSession session,
        CampaignActionAudience audience) =>
        Query(session, audience, CycleControlStage, CycleControlSubject);

    /// <summary>Bound defender's post-assault retreat decision (020E1, contract v2 superset).</summary>
    public static CampaignCombatAudienceDecision? QueryPostAssaultRetreat(ExerciseSession session,
        CampaignActionAudience audience, string defenderElementId) =>
        Query(session, audience, PostAssaultRetreatStage, defenderElementId);

    /// <summary>Repeated-cycle Combat selection decision (020E2, contract v2 superset).</summary>
    public static CampaignCombatAudienceDecision? QueryRepeatCombatSelection(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId) =>
        Query(session, audience, RepeatCombatSelectionStage, actingElementId);

    /// <summary>Captor custody disposition for an accepted capture (020A3).</summary>
    public static CampaignCombatAudienceDecision? QueryCustody(ExerciseSession session,
        CampaignActionAudience audience, string captorElementId) =>
        Query(session, audience, CustodyStage, captorElementId);

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
            || string.IsNullOrWhiteSpace(submission.OfferRef))
            return CampaignCombatAudienceSubmissionResult.Rejected;
        var projected = Project(session, submission.Audience, submission.Stage, submission.ActingElementId);
        if (projected is not { } current) return CampaignCombatAudienceSubmissionResult.Rejected;
        var decision = current.Decision;
        // Same-cut rule: a timely ref stays valid while the owner-visible bytes are unchanged, even
        // if private authority has advanced. Anything else is one generic rejection.
        if (submission.ContractVersion != decision.ContractVersion
            || submission.CampaignId != decision.CampaignId
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
                ReserveReleaseStage => CampaignHostAdapter.SubmitNativeReserveReleaseChoice(session,
                    command, decision.ActingElementId),
                CycleControlStage => CampaignHostAdapter.SubmitNativeCycleControl(session, command),
                CustodyStage => CampaignHostAdapter.SubmitNativeCombatCustody(session, command,
                    decision.ActingElementId),
                RbaDeclineStage => CampaignHostAdapter.SubmitContactDefenderRbaDecline(session, command,
                    decision.ActingElementId),
                PostAssaultRetreatStage => CampaignHostAdapter.SubmitContactPostAssaultRetreat(session,
                    command, decision.ActingElementId),
                RepeatCombatSelectionStage => CampaignHostAdapter.SubmitNativeRepeatCombat(session,
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

    /// <summary>
    /// Envelope contract version for a stage: 1 for the six frozen v1 stages, 2 for the stages the
    /// v2 superset adds. Unknown stages have no version.
    /// </summary>
    public static int ContractVersionFor(string stage) => stage switch
    {
        SelectionStage or ForceAssignmentStage or ReserveReleaseStage or CycleControlStage
            or CustodyStage or RbaDeclineStage => CampaignCombatAudienceDecision.CurrentContractVersion,
        PostAssaultRetreatStage or RepeatCombatSelectionStage
            => CampaignCombatAudienceDecision.SuccessorContractVersion,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

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
        CampaignContactObservationV1? observation = null;
        byte[] visible;
        string[] privateIds;
        try
        {
            switch (stage)
            {
                case ReserveReleaseStage:
                    {
                        var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(session,
                            audience, actingElementId);
                        visible = draft.VisibleBytes;
                        privateIds = draft.PrivateActionIds.ToArray();
                        break;
                    }
                case CustodyStage:
                    {
                        // T020-Q5 (resolved 2026-11-20): the existing V1 captor cut field set is the
                        // custody inference allowlist; no field is added or removed here.
                        var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(session,
                            audience, actingElementId);
                        visible = draft.VisibleBytes;
                        privateIds = draft.PrivateActionIds.ToArray();
                        break;
                    }
                case RbaDeclineStage:
                    {
                        // Only the bound defender's own decline; Core move offers are never exposed
                        // or mapped (RBA movement is outside the bounded profile).
                        var cut = CampaignExercises.QueryContactRbaCut(session);
                        if (cut.DefenderSide != side || cut.DefenderElementId != actingElementId) return null;
                        observation = cut.ContactObservation;
                        visible = CampaignContactRbaDeclineOwnerVisibleCutV1.SerializeCanonical(cut);
                        privateIds = [cut.DeclineOffer.ActionId];
                        break;
                    }
                case PostAssaultRetreatStage:
                    {
                        // Defender-only, after an assault result that requires a retreat. Offers are
                        // the source-directed one-hex moves plus explicit refusal, in the Host draft's
                        // order. T020-Q9 (scoping §7n): an offered hex cannot hold an unseen opposing
                        // unit, because every opposing unit adjacent to the defender is a contact.
                        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
                        if (cut.DefenderSide != side || cut.DefenderElementId != actingElementId) return null;
                        observation = cut.ContactObservation;
                        visible = CampaignContactRetreatAfterAssaultOwnerVisibleCutV1.SerializeCanonical(cut);
                        privateIds = cut.MoveOffers
                            .OrderBy(value => value.DestinationLocationId, StringComparer.Ordinal)
                            .Select(value => value.ActionId)
                            .Append(cut.RefuseOffer.ActionId)
                            .ToArray();
                        break;
                    }
                case RepeatCombatSelectionStage:
                    {
                        // T020-Q8 applies as for first-cycle Selection (owner, 2026-11-20): an offer on
                        // a contact bound to a non-combat unit stays; the downstream force-unavailable
                        // System cancellation handles it. Ordinal order matches the Host draft.
                        var cut = CampaignExercises.QueryNativeRepeatCombatCut(session, side, actingElementId);
                        observation = cut.Observation;
                        visible = cut.VisibleBytes;
                        privateIds = cut.AssaultOffers
                            .OrderBy(value => value.ContactHandle, StringComparer.Ordinal)
                            .ThenBy(value => value.ApparentLocationId, StringComparer.Ordinal)
                            .Select(value => value.ActionId)
                            .Append(cut.FinishOffer.ActionId)
                            .ToArray();
                        break;
                    }
                case CycleControlStage:
                    {
                        if (actingElementId != CycleControlSubject) return null;
                        var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, audience);
                        visible = draft.VisibleBytes;
                        privateIds = draft.PrivateActionIds.ToArray();
                        break;
                    }
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
                        // A cancelled Force round (timeout or V19 force-unavailable) offers nothing.
                        // The private cut still projects after cancellation and only submission
                        // rejects it; the audience surface must not show an unsubmittable offer.
                        var cancelled = session.CurrentRepeatAssault is { Decline: not null, ForceCompletion: null } repeat
                            ? repeat.ForceCancellation is not null
                            : session.CurrentCombatForceCancellation is not null;
                        if (cancelled) return null;
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
        if (observation is not null && observation.Side != side) return null;
        // Contact-bound stages carry the side contact audience revision. Reserve Release, Cycle
        // Control and Custody cuts carry no contact observation; they report revision 0 and rely
        // on the visible digest as the only fence (in-process subset accepted under T020-Q4).
        var campaignId = observation?.CampaignId ?? CampaignExercises.QueryCheckpoint(session).CampaignId;
        var revision = observation?.AudienceRevision ?? 0;
        var digest = Digest(visible);
        var stageId = stage!;
        var refs = privateIds.Select((_, index) => Reference(campaignId, audience,
            stageId, actingElementId, digest, index)).ToArray();
        return (new CampaignCombatAudienceDecision(campaignId, audience, stageId,
            actingElementId, revision, visible, refs), privateIds);
    }
}

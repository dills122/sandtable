namespace Cna.OrleansHost.Persistence;

/// <summary>
/// The one Marten event CLR type for the campaign event stream (Phase C1 of issue #439,
/// <c>docs/work/plans/2026-11-18-postgres-migration-design.md</c>). One instance represents one
/// row of today's SQLite <c>accepted_steps</c> table
/// (<c>CampaignDurableAuthorityJournal.cs</c>'s <c>CREATE TABLE accepted_steps</c>), minus the
/// columns that move elsewhere in the Marten shape: <c>campaign_id</c> becomes the stream's own
/// string key (see <see cref="CampaignEventStreamSchema"/>), and <c>sequence</c>/<c>prior_hash</c>/
/// <c>record_hash</c> become event headers (Phase A's proven mechanism, reused by
/// <see cref="CampaignEventStreamSchema.BuildStepEvent"/>) rather than event body fields.
/// </summary>
/// <remarks>
/// <para><b>Why one envelope instead of registering Cna.Core's ~40+ real event types
/// (<c>ElementMovedV3</c>, <c>CampaignNativeCombatAssaultResultV22</c>,
/// <c>CampaignNativeSecondCombatDecisionV47</c>, and every other case in
/// <c>CampaignCurrentEventSerializer.Serialize</c>/<c>Deserialize</c>) directly with Marten.</b>
/// Two independent reasons, both verified against this repo's actual code, not assumed:</para>
/// <para>1. <b>They are not reachable from here.</b> Every one of those types is declared
/// <c>internal</c> inside <c>Cna.Core</c>, and <c>Cna.Core.csproj</c>'s only
/// <c>InternalsVisibleTo</c> entry is <c>Cna.Core.Tests</c> -- not <c>Cna.OrleansHost</c>, where
/// Marten lives. This is not an oversight to work around: <c>Cna.OrleansHost</c> never receives a
/// domain event CLR instance today either. <c>CampaignDurableAuthorityJournal.PersistAccepted</c>
/// reads <c>evidence.EventRecords.Single()</c> -- already a <c>byte[]</c>, produced on the
/// <c>Cna.Core</c> side of <c>ExerciseStepEvidence</c>'s public surface -- and persists that byte
/// array verbatim. This envelope keeps exactly that boundary; it does not cross it.</para>
/// <para>2. <b>Even if they were public, Marten's default System.Text.Json-based event serializer
/// would not reliably reproduce their canonical bytes.</b> These types are hand-serialized via
/// per-type <c>SerializeCanonical()</c>/<c>Deserialize()</c> pairs keyed on an
/// <c>eventType</c>+<c>contractVersion</c> JSON shape that does not match the CLR type's own
/// property layout -- constructors run validation and compute derived, re-sorted collections (see
/// <c>ElementMovedV3</c>'s constructor sorting <c>BreakdownAccounting</c> by cohort id). The hash
/// chain (<c>CampaignDurableAuthorityJournal.StepHash</c>) is computed directly over those exact
/// canonical bytes; re-deriving them through Marten's reflection-based serializer would risk a
/// silent mismatch with the bytes this project already treats as canonical, which would undermine
/// <c>DET-001</c>/<c>DET-002</c>/<c>EVT-001</c>. Storing the already-canonical bytes verbatim, as
/// <see cref="CanonicalEvent"/> below, sidesteps the question rather than needing one hand-written
/// converter per real event type.</para>
/// <para>Net effect: ~40+ real event kinds are represented by exactly one Marten CLR event type.
/// <see cref="ActionKind"/> is the discriminator a reader (C2's projections) switches on, exactly
/// as <c>accepted_steps.action_kind</c> is switched on today.</para>
/// <para><b>Byte arrays and serialization.</b> Marten's default event serializer is
/// System.Text.Json, which encodes <c>byte[]</c> as base64 -- lossless, and no custom converter is
/// needed for this type. Note that record-synthesized <c>Equals</c>/<c>GetHashCode</c> compare
/// <c>byte[]</c> fields by reference, not content, so round-trip tests must compare array contents
/// explicitly (e.g. <c>SequenceEqual</c>) rather than relying on this record's own equality.</para>
/// </remarks>
public sealed record CampaignAcceptedStepEvent(
    string ActionKind,
    string? ActingElementId,
    string? OwnerPublicRef,
    long? SeatEpoch,
    long? OwnerRevision,
    byte[] CanonicalCommand,
    byte[] CanonicalEvent,
    byte[] CanonicalReceipt,
    byte[] SnapshotCheckpoint,
    byte[] CanonicalContacts,
    // Mirrors accepted_steps.owner_side (added alongside it to close a cross-audience
    // idempotency-retry gap in SubmitOwnerActionCore; see that method's own remarks). A
    // trailing optional property, not a required one, so every event constructed by an
    // older build -- there is no migration mechanism for already-persisted Marten events,
    // same as the SQLite journal (see CampaignDurableAuthorityJournal.OpenExisting's own
    // remarks) -- deserializes with Side = null rather than failing, and falls back to the
    // pre-fix (audience-blind) retry behavior for that one historical row only.
    string? Side = null);

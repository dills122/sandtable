using JasperFx.Events;
using Marten.Events.Aggregation;

namespace Cna.OrleansHost.Persistence;

/// <summary>
/// The current-state read model Phase C2 of issue #439
/// (<c>docs/work/plans/2026-11-18-postgres-migration-design.md</c> §3.2) folds from a campaign's
/// <see cref="CampaignAcceptedStepEvent"/> stream, replacing the hand-rolled <c>snapshots</c> table
/// the (superseded) SQLite snapshotting design
/// (<c>docs/work/plans/2026-11-18-campaign-replay-snapshotting-design.md</c> §5.1) proposed
/// hand-building. One document per campaign (<see cref="Id"/> is the campaign id, i.e. the
/// stream's own string key -- see <see cref="CampaignEventStreamSchema"/>), updated in the same
/// transaction as each event append via <see cref="CampaignStateProjection"/>'s
/// <c>SnapshotLifecycle.Inline</c> registration -- immediate, ACID-consistent, O(1) to read back,
/// with no replay. This is the same property the hand-rolled <c>snapshots</c> table was built to
/// deliver, as a supported Marten feature instead of hand-written cadence/bookkeeping logic.
/// </summary>
/// <remarks>
/// <para><b>What this document is for, precisely.</b> It carries exactly the bytes a caller needs
/// to seed a resumed replay via <c>Cna.Core.Hosting.CampaignHostAdapter.ImportResumed</c> (Phase B
/// of #439, PR #441) instead of a from-genesis <c>Create</c> call: the latest
/// <see cref="SnapshotCheckpoint"/> (a serialized engine checkpoint) and <see cref="CanonicalContacts"/>
/// (the contact journal), plus the hash-chain position (<see cref="Sequence"/>/<see cref="RecordHash"/>)
/// needed to verify this document's own provenance against the stream it was folded from. Wiring
/// this into <c>CampaignDurableAuthorityJournal.Load()</c>'s actual read path is Phase C3's job
/// (see issue #439's "Phase C breakdown" comment) -- this type only materializes the data.</para>
/// <para><b>Every event updates this document, not only "clean boundary" ones.</b> The snapshotting
/// design doc's §2/§4 established that only a "clean boundary" (all of <c>ExerciseSession</c>'s 33
/// optional fields and 8 predecessor-chain fields null) is a safe point to actually *resume* from --
/// mid-round <see cref="SnapshotCheckpoint"/> bytes exist (every step carries one) but cannot seed a
/// fresh <c>ExerciseSession</c> the way a clean-boundary one can. This projection does not gate on
/// that distinction: Marten's inline snapshot lifecycle updates the document on every single event,
/// unconditionally, which is simpler and strictly more up-to-date than the hand-rolled design's
/// selective cadence. The clean-boundary check stays the responsibility of whatever Phase C3 code
/// decides *when* it is safe to actually resume from this document's current bytes -- a concern this
/// read model does not need to resolve to stay correct, since it is a faithful record of "the last
/// step's own checkpoint," not a claim about resumability.</para>
/// <para><b>Round tables are deliberately not folded into this document.</b> See
/// <c>docs/work/plans/2026-11-19-phase-c2-round-table-projection-fork.md</c> for why the seven
/// round tables (<c>selection_rounds</c>, <c>rba_rounds</c>, etc.) are not built as Marten
/// projections in this phase -- their <c>opened_ms</c>/<c>deadline_ms</c>/<c>high_water_ms</c>
/// columns are wall-clock bookkeeping written between accepted-step events, not data the accepted
/// event stream carries for the common (non-timeout) completion path, which a deterministic
/// event-fold projection cannot reproduce without either dropping that data or inventing new event
/// schema. That is a genuine architectural fork, reported rather than guessed through, per this
/// project's standing discipline.</para>
/// </remarks>
public sealed record CampaignStateDocument(
    string Id,
    long Sequence,
    string PriorHash,
    string RecordHash,
    string ActionKind,
    string? ActingElementId,
    byte[] SnapshotCheckpoint,
    byte[] CanonicalContacts);

/// <summary>
/// Marten <c>SingleStreamProjection&lt;TDoc, TId&gt;</c> (verified against Marten 9.62.0's actual
/// API, not assumed -- see <see cref="CampaignEventStreamSchema"/>'s remarks on why this project
/// checks rather than trusts XML-doc prose for this library) that folds
/// <see cref="CampaignAcceptedStepEvent"/>s into one <see cref="CampaignStateDocument"/> per
/// campaign. Registered via <c>StoreOptions.Projections.Add(new CampaignStateProjection(),
/// ProjectionLifecycle.Inline)</c> inside <see cref="CampaignEventStreamSchema.Configure"/> (see
/// that method's remarks on why the single-type-argument <c>Snapshot&lt;TDoc&gt;()</c> helper does
/// not work for a distinct projection class like this one) so every store that knows about the
/// campaign event type also knows about this projection.
/// </summary>
public sealed class CampaignStateProjection : SingleStreamProjection<CampaignStateDocument, string>
{
    /// <summary>Builds the document from the stream's first event (Marten's generated pattern
    /// matching dispatches here for a brand-new stream).</summary>
    public static CampaignStateDocument Create(CampaignAcceptedStepEvent @event, IEvent metadata) =>
        FoldRaw(null, @event, metadata);

    /// <summary>
    /// Phase C3b: a journal-written stream starts with <see cref="CampaignGenesisEvent"/> (sequence
    /// 0). The document then holds the pre-step-1 state: the initial snapshot and contacts, at the
    /// chain's initial hash.
    /// </summary>
    public static CampaignStateDocument Create(CampaignGenesisEvent @event, IEvent metadata)
    {
        var (sequence, priorHash, recordHash) = CampaignEventStreamSchema.ReadStepHeaders(metadata);
        var streamKey = metadata.StreamKey
            ?? throw new InvalidOperationException("Campaign event has no string stream key.");
        return new CampaignStateDocument(streamKey, sequence, priorHash, recordHash, "genesis", null,
            @event.InitialSnapshot, @event.InitialContacts);
    }

    /// <summary>Folds every subsequent event into the existing document.</summary>
    public static CampaignStateDocument Apply(CampaignStateDocument current, CampaignAcceptedStepEvent @event,
        IEvent metadata) => FoldRaw(current, @event, metadata);

    private static CampaignStateDocument FoldRaw(CampaignStateDocument? current, CampaignAcceptedStepEvent data,
        IEvent metadata)
    {
        var (sequence, priorHash, recordHash) = CampaignEventStreamSchema.ReadStepHeaders(metadata);
        var streamKey = metadata.StreamKey
            ?? current?.Id
            ?? throw new InvalidOperationException("Campaign event has no string stream key.");
        return new CampaignStateDocument(streamKey, sequence, priorHash, recordHash,
            data.ActionKind, data.ActingElementId, data.SnapshotCheckpoint, data.CanonicalContacts);
    }
}

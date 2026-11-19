using System.Globalization;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;

namespace Cna.OrleansHost.Persistence;

/// <summary>
/// Registers the campaign event stream's Marten schema (Phase C1 of issue #439,
/// <c>docs/work/plans/2026-11-18-postgres-migration-design.md</c>) on top of Phase A's
/// <see cref="MartenStoreFactory"/>. Pass <see cref="Configure"/> as (part of) the
/// <c>configure</c> callback when building a store whose database will hold campaign streams.
/// This is schema/registration code only -- it defines how an accepted step becomes a Marten
/// event and attaches the hash-chain header pair, nothing more. It does not read or write
/// <c>CampaignDurableAuthorityJournal</c>'s SQLite tables; wiring this into that journal's actual
/// persistence is Phase C3's job.
/// </summary>
/// <remarks>
/// <para><b>Stream identity: <c>campaign_id</c>, directly, as a Marten string stream key.</b>
/// Today's SQLite schema already uses <c>campaign_id TEXT</c> as the primary-key prefix on every
/// per-campaign table (<c>CampaignDurableAuthorityJournal.cs</c>'s <c>CREATE TABLE campaigns</c> /
/// <c>accepted_steps</c>), validated to a safe lowercase-ASCII-letter-or-digit/hyphen/underscore,
/// 1-200 character shape before it ever reaches storage
/// (<c>CampaignDurableAuthorityJournalStore.RequireSafeCampaignId</c>). Marten supports string
/// stream keys as a first-class, built-in alternative to Guid stream ids
/// (<see cref="StreamIdentity.AsString"/>) -- confirmed against the actual Marten 9.62.0 API
/// pinned in <c>Directory.Packages.props</c> by loading <c>Marten.dll</c> via reflection rather
/// than assumed from general knowledge: <c>EventStore.StartStream(string, object[])</c> and
/// <c>EventStore.Append(string, long, object[])</c> both exist, reachable from
/// <c>IDocumentSession.Events</c> via <c>JasperFx.Events.IEventOperations</c>, and
/// <c>IQueryEventStore.FetchStreamAsync(string, ...)</c> reads a string-keyed stream back. No
/// synthetic Guid mapping table is needed: one Marten stream per campaign, keyed by that
/// campaign's own existing id.</para>
/// <para><b>Sequence vs. Marten's own stream version -- kept explicit, not trusted blindly.</b>
/// Marten assigns a per-stream <c>Version</c> automatically on append and enforces it as an
/// optimistic-concurrency gate via <c>Append(stream, expectedVersion, events)</c> -- but that
/// gate's exact semantics were verified directly against a real Postgres instance rather than
/// trusted from Marten 9.62.0's own XML docs, which turned out to describe it two different (and
/// contradictory) ways in two places: <c>IEventOperations.Append</c>'s own doc implies
/// <c>expectedVersion</c> is the stream's version <i>before</i> this call, while
/// <c>JasperFx.Events.TestSupport.ProjectionScenario</c>'s says "the expected stream version
/// after appending." A small reflection+Testcontainers probe settled it empirically: passing the
/// stream's actual prior version threw <c>EventStreamUnexpectedMaxEventIdException</c>; passing
/// the version the stream will have <i>after</i> this append succeeded. So
/// <c>expectedVersion</c> is the resulting version, i.e. for this schema's one-event-per-step
/// model, exactly the new <c>sequence</c> value itself (not <c>sequence - 1</c>).
/// <c>StreamState.Version</c> ("current version of the stream in the database (the count of
/// events)") is the same 1-based, gapless, single-writer-per-stream shape as today's
/// <c>(campaign_id, sequence)</c> plus its <c>head_sequence</c>/<c>head_hash</c> compare-and-swap
/// in <c>CampaignDurableAuthorityJournal.PersistAccepted</c>. In ordinary use (append exactly one
/// event per accepted step, in order, passing <c>sequence</c> as <c>expectedVersion</c>) the two
/// numbers line up exactly. They are still not treated as interchangeable: the hash chain
/// (<c>CampaignDurableAuthorityJournal.StepHash</c>) binds <c>sequence</c> directly into the
/// cryptographic digest, and the API-semantics mismatch just described is itself a live example
/// of why the design doc's own &#167;3.3 discipline ("don't assume Marten's API, verify it")
/// matters here too. Rather than reconstruct <c>sequence</c> from <c>IEvent.Version</c> and trust
/// it always matches, this schema carries it a second time, explicitly, as the
/// <see cref="SequenceHeaderKey"/> event header -- verifiable on every read
/// (<see cref="ReadStepHeaders"/> returns both, deliberately) rather than assumed identical.
/// Phase C3's load/replay path should assert the two agree, not read only one of them.</para>
/// <para><b>C3b amendment: genesis at version 1, so <c>Version == sequence + 1</c>.</b> Phase C3b
/// (<c>docs/work/plans/2026-11-19-phase-c3b-durable-state-design-spike.md</c> &#167;3.1) puts a
/// <see cref="CampaignGenesisEvent"/> first in every journal-written campaign stream, carrying the
/// <c>sequence</c> header <c>0</c>. Accepted step <i>n</i> therefore lives at stream version
/// <i>n + 1</i>, and its expected-version argument is <i>n + 1</i> (still "the version after this
/// append"). The explicit <c>sequence</c> header is exactly what makes this offset safe: the journal's
/// loader asserts <c>sequence == Version - 1</c> for every event it reads. Streams written directly
/// against this schema without a genesis event (C1/C2's own schema-level tests) keep
/// <c>Version == sequence</c>; the projections accept either first-event type.</para>
/// <para><b>C3b correction: headers are per event, never per session.</b> C1 shipped a
/// session-scoped <c>SetStepHeaders(IDocumentSession, ...)</c>. Verified empirically against Marten
/// 9.62.0: session headers are copied onto <i>every</i> event appended in that session's
/// <c>SaveChangesAsync</c>, so any unit of work appending two or more steps (e.g. a Selection
/// timeout cascading into no-attack and Reserve Release completion) would have stored the same
/// <c>sequence</c>/<c>prior_hash</c>/<c>record_hash</c> on every one of them -- a corrupt hash
/// chain. <see cref="BuildStepEvent"/> builds each event with its own headers instead
/// (<c>EventGraph.BuildEvent</c> + <see cref="IEvent.SetHeader"/>, also verified to round-trip and to
/// be visible to inline projections); the session-scoped helper is removed.</para>
/// <para><b>Hash chain.</b> Reuses Phase A's already-proven mechanism unchanged
/// (<see cref="MartenStoreFactory.CreateStore"/> turns on
/// <c>StoreOptions.Events.MetadataConfig.HeadersEnabled</c>; see
/// <c>MartenEventHeaderHashChainTests</c>) -- <see cref="PriorHashHeaderKey"/> and
/// <see cref="RecordHashHeaderKey"/> are the same header keys that proof-of-concept test used,
/// carrying the same <c>CampaignDurableAuthorityJournal.StepHash</c>-shaped values this project
/// already computes. Attaching them to a session ahead of an append is this file's job
/// (<see cref="BuildStepEvent"/>); computing the hash values themselves is unchanged, existing
/// logic this change does not touch.</para>
/// </remarks>
public static class CampaignEventStreamSchema
{
    /// <summary>Header key for the hash-chain's predecessor hash (Phase A's proven mechanism).</summary>
    public const string PriorHashHeaderKey = "prior_hash";

    /// <summary>Header key for this step's own record hash (Phase A's proven mechanism).</summary>
    public const string RecordHashHeaderKey = "record_hash";

    /// <summary>
    /// Header key for the explicit, belt-and-suspenders <c>sequence</c> value -- see this type's
    /// remarks on why Marten's own per-stream <c>Version</c> is not relied on alone.
    /// </summary>
    public const string SequenceHeaderKey = "sequence";

    /// <summary>
    /// Registers the campaign stream's event type and string stream identity on
    /// <paramref name="options"/>. Compose this into <see cref="MartenStoreFactory.CreateStore"/>'s
    /// <c>configure</c> callback for any store whose database will hold campaign event streams --
    /// the hot tier today, and (per design doc &#167;2.1) the cold/archival store once Phase-C
    /// archival exists, since it is the same event shape, just a different database.
    /// </summary>
    public static void Configure(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Events.StreamIdentity = StreamIdentity.AsString;
        options.Events.AddEventType<CampaignGenesisEvent>();
        options.Events.AddEventType<CampaignAcceptedStepEvent>();

        // Phase C3b: host operational state documents (seats, openings, reference key, open-round
        // high-water marks, operator capability). Each type carries [UseOptimisticConcurrency];
        // registering them here keeps every campaign store's schema in one place.
        options.Schema.For<CampaignSeatDocument>().Index(document => document.CampaignId);
        options.Schema.For<CampaignOwnerOpeningDocument>().Index(document => document.CampaignId);
        options.Schema.For<CampaignReferenceKeyDocument>();
        options.Schema.For<CampaignRoundHighWaterDocument>();
        options.Schema.For<HostOperatorCapabilityDocument>();

        // Phase C2 (docs/work/plans/2026-11-18-postgres-migration-design.md §3.2): the
        // campaign-state projection replacing the hand-rolled snapshot mechanism. Inline means
        // Marten updates CampaignStateDocument in the same transaction as the event append.
        // Verified empirically against Marten 9.62.0's actual behavior (its own XML doc comments
        // are not enough here -- same discipline as this file's Append/expectedVersion remarks):
        // ProjectionOptions.Snapshot<TDoc>(SnapshotLifecycle) registers a *self-aggregating*
        // projection, which requires the conventional Create/Apply methods to live on TDoc itself
        // -- it never references a separate SingleStreamProjection<TDoc,TId> subclass, so
        // CampaignStateProjection's own Create/Apply methods were silently never consulted and
        // registration failed at runtime with "No matching conventional Apply/Create/ShouldDelete
        // methods for the CampaignStateDocument aggregate." The inherited
        // ProjectionGraph.Add(IProjectionSource, ProjectionLifecycle, ...) call below is what
        // actually wires up a distinct projection class.
        options.Projections.Add(new CampaignStateProjection(), ProjectionLifecycle.Inline);

        // The seven round-table projections (same phase, same Inline lifecycle). Each covers only
        // the event-derivable, non-timing fields -- see CampaignRoundProjections.cs's remarks and
        // docs/work/plans/2026-11-19-phase-c2-round-table-projection-fork.md for why
        // opened_ms/deadline_ms/high_water_ms are intentionally excluded (a deliberate,
        // user-confirmed scope decision, not an oversight).
        options.Projections.Add(new SelectionRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new ForceRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new RbaRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new CycleRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new ReserveRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new CustodyRoundsProjection(), ProjectionLifecycle.Inline);
        options.Projections.Add(new RetreatRoundsProjection(), ProjectionLifecycle.Inline);
    }

    /// <summary>
    /// Builds one stream event for <paramref name="data"/> carrying its own three step headers
    /// (Phase A's <c>prior_hash</c>/<c>record_hash</c> pair plus the explicit <c>sequence</c>), and
    /// any <paramref name="extraHeaders"/> (e.g. <see cref="CampaignRoundRowHeader.HeaderKey"/>).
    /// Pass the result to <c>StartStream</c>/<c>Append</c>; Marten appends a pre-built
    /// <see cref="IEvent"/> as-is, so several events in one save each keep their own headers.
    /// </summary>
    public static IEvent BuildStepEvent(IDocumentStore store, object data, long sequence, string priorHash,
        string recordHash, IReadOnlyDictionary<string, string>? extraHeaders = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(priorHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordHash);
        var @event = ((Marten.Events.EventGraph)store.Options.Events).BuildEvent(data);
        @event.SetHeader(SequenceHeaderKey, sequence.ToString(CultureInfo.InvariantCulture));
        @event.SetHeader(PriorHashHeaderKey, priorHash);
        @event.SetHeader(RecordHashHeaderKey, recordHash);
        if (extraHeaders is not null)
            foreach (var (key, value) in extraHeaders) @event.SetHeader(key, value);
        return @event;
    }

    /// <summary>
    /// Reads the three step headers back off a fetched <see cref="IEvent"/> (e.g. from
    /// <c>IQuerySession.Events.FetchStreamAsync</c>), as plain strings/longs -- the same shape
    /// <see cref="BuildStepEvent"/> wrote them in, and the shape
    /// <c>CampaignDurableAuthorityJournal</c>'s hash chain already works in (hex-suffixed
    /// <c>sha256:...</c> strings; decimal sequence).
    /// </summary>
    public static (long Sequence, string PriorHash, string RecordHash) ReadStepHeaders(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var headers = @event.Headers
            ?? throw new InvalidOperationException("Event has no headers; was HeadersEnabled set on the store?");
        if (headers[SequenceHeaderKey] is not string sequenceText)
            throw new InvalidOperationException("Event is missing its sequence header.");
        if (headers[PriorHashHeaderKey] is not string priorHash)
            throw new InvalidOperationException("Event is missing its prior_hash header.");
        if (headers[RecordHashHeaderKey] is not string recordHash)
            throw new InvalidOperationException("Event is missing its record_hash header.");
        return (long.Parse(sequenceText, CultureInfo.InvariantCulture), priorHash, recordHash);
    }
}

using System.Collections.Immutable;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.OrleansHost.Persistence;
using JasperFx.Events;
using Marten;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Raised when a working set's flush loses an optimistic-concurrency race (stream version, stream
/// creation, or any host-state document version). The store first retries the whole unit of work
/// against fresh state (<see cref="CampaignDurableAuthorityJournalStore.RunAsync{T}"/>, reproducing
/// SQLite's serialization); only when retries are exhausted does this surface, as an
/// <see cref="InvalidOperationException"/> carrying the exact message SQLite's head compare-and-swap
/// failure used ("Campaign head changed before commit."), so every existing caller -- including
/// <c>CampaignDurableOutwardApi</c>'s 409 "unavailable" mapping -- treats it exactly as before.
/// <see cref="CampaignDurableAuthorityJournalStore.RedeemSeatGrantAsync"/> is the one deliberate
/// exception: there it means "another redeemer consumed this single-use grant first" and maps to
/// <see langword="null"/>, which is what the losing redeemer observed under SQLite's serialized
/// <c>BEGIN IMMEDIATE</c>.
/// </summary>
internal sealed class CampaignJournalConcurrencyException(Exception inner)
    : InvalidOperationException("Campaign head changed before commit.", inner);

/// <summary>
/// The Postgres+Marten backend for <see cref="CampaignDurableAuthorityJournal"/> (Phase C3b of
/// issue #439, <c>docs/work/plans/2026-11-19-phase-c3b-durable-state-design-spike.md</c>). Holds the
/// hot-tier <see cref="IDocumentStore"/> (configured with <see cref="CampaignEventStreamSchema.Configure"/>)
/// and the host-wide operator capability, and opens per-unit-of-work <see cref="CampaignJournalWorkingSet"/>s.
/// </summary>
internal sealed class CampaignJournalMartenBackend
{
    private readonly byte[] operatorDigest;

    private CampaignJournalMartenBackend(IDocumentStore store, byte[] operatorDigest, TimeProvider clock,
        IContentPackV6Resolver? contentResolver, Func<int, byte[]> randomBytes)
    {
        Store = store;
        this.operatorDigest = operatorDigest;
        Clock = clock;
        ContentResolver = contentResolver;
        RandomBytes = randomBytes;
    }

    internal IDocumentStore Store { get; }
    internal TimeProvider Clock { get; }
    internal IContentPackV6Resolver? ContentResolver { get; }
    internal Func<int, byte[]> RandomBytes { get; }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IDocumentStore, SemaphoreSlim>
        HarnessGates = new();

    /// <summary>Per-database serialization gate for the synchronous test harness only (see
    /// <see cref="CampaignDurableAuthorityJournal.ForMartenSynchronousHarness"/>).</summary>
    internal SemaphoreSlim HarnessGate => HarnessGates.GetValue(Store, static _ => new SemaphoreSlim(1, 1));

    /// <summary>
    /// Builds the backend, ensuring the host-wide operator capability document exists -- the
    /// Marten counterpart of <see cref="CampaignDurableAuthorityJournal.EnsureHostIdentity"/>, with the
    /// same "first writer wins, later secrets never overwrite it" semantics.
    /// </summary>
    internal static async Task<CampaignJournalMartenBackend> CreateAsync(IDocumentStore store, string operatorSecret,
        TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null,
        Func<int, byte[]>? randomBytes = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var digest = CampaignDurableAuthorityJournal.OperatorCapabilityDigest(operatorSecret);
        HostOperatorCapabilityDocument? stored;
        await using (var query = store.QuerySession())
            stored = await query.LoadAsync<HostOperatorCapabilityDocument>(
                HostOperatorCapabilityDocument.SingletonId, cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            try
            {
                await using var session = store.LightweightSession();
                session.Insert(new HostOperatorCapabilityDocument(HostOperatorCapabilityDocument.SingletonId, digest));
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                stored = new HostOperatorCapabilityDocument(HostOperatorCapabilityDocument.SingletonId, digest);
            }
            catch (JasperFx.DocumentAlreadyExistsException)
            {
                await using var query = store.QuerySession();
                stored = await query.LoadAsync<HostOperatorCapabilityDocument>(
                    HostOperatorCapabilityDocument.SingletonId, cancellationToken).ConfigureAwait(false);
            }
        }
        return new CampaignJournalMartenBackend(store, stored!.Digest, clock ?? TimeProvider.System, contentResolver,
            randomBytes ?? System.Security.Cryptography.RandomNumberGenerator.GetBytes);
    }

    internal bool AuthenticateOperator(string secret) =>
        CampaignDurableAuthorityJournal.OperatorCapabilityMatches(operatorDigest, secret);

    /// <summary>
    /// Loads <paramref name="campaignIds"/> (or, when <see langword="null"/>, every campaign stream
    /// in this database -- the synchronous test harness's mode) into a fresh in-memory working set.
    /// </summary>
    internal async Task<CampaignJournalWorkingSet> OpenAsync(IReadOnlyCollection<string>? campaignIds,
        Func<byte[]> newReferenceKey, CancellationToken cancellationToken)
    {
        var workingSet = new CampaignJournalWorkingSet(this);
        try
        {
            await workingSet.LoadAsync(campaignIds, newReferenceKey, cancellationToken).ConfigureAwait(false);
            return workingSet;
        }
        catch
        {
            workingSet.Dispose();
            throw;
        }
    }

    /// <summary>
    /// <see cref="Timeouts.ICampaignRoundDeadlineSink.GetDeadlineAsync"/>'s O(1) read side: the
    /// deadline of <paramref name="campaignId"/>'s currently collecting round of the given table,
    /// read straight from that round kind's C2 projection document (whose <c>Retained</c> rows now
    /// carry the write-once timing columns) -- no stream fetch, no replay.
    /// </summary>
    internal async Task<long?> GetCollectingDeadlineMsAsync(string campaignId, string table,
        CancellationToken cancellationToken)
    {
        await using var query = Store.QuerySession();
        var retained = await LoadRetainedAsync(query, campaignId, table, cancellationToken).ConfigureAwait(false);
        foreach (var row in retained.Values)
            if (row.Status == "collecting") return row.DeadlineMs;
        return null;
    }

    internal static async Task<ImmutableDictionary<string, RetainedRoundRow>> LoadRetainedAsync(
        IQuerySession session, string campaignId, string table, CancellationToken cancellationToken)
    {
        var empty = ImmutableDictionary<string, RetainedRoundRow>.Empty;
        return table switch
        {
            "force_rounds" => (await session.LoadAsync<ForceRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "rba_rounds" => (await session.LoadAsync<RbaRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "cycle_rounds" => (await session.LoadAsync<CycleRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "reserve_rounds" => (await session.LoadAsync<ReserveRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "custody_rounds" => (await session.LoadAsync<CustodyRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "retreat_rounds" => (await session.LoadAsync<RetreatRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            "selection_rounds" => (await session.LoadAsync<SelectionRoundsDocument>(campaignId, cancellationToken).ConfigureAwait(false))?.Retained ?? empty,
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, null),
        };
    }
}

/// <summary>
/// One unit of work against the Marten backend: the persisted state of one (production) or every
/// (test harness) campaign, loaded into a private in-memory SQLite database carrying exactly the
/// journal's current schema, so <see cref="CampaignDurableAuthorityJournal"/>'s existing relational
/// logic runs unchanged against it; then <see cref="FlushAsync"/> writes back everything that
/// unit of work committed, in one Postgres transaction.
/// </summary>
/// <remarks>
/// <para><b>Durable store mapping (design spike &#167;3, as approved).</b></para>
/// <list type="bullet">
/// <item><c>campaigns</c> &#8596; the stream's first event, <see cref="CampaignGenesisEvent"/>
/// (version 1, <c>sequence</c> header 0); <c>head_sequence</c>/<c>head_hash</c> are the stream tail.</item>
/// <item><c>accepted_steps</c> &#8596; one <see cref="CampaignAcceptedStepEvent"/> per row, with
/// per-event <c>sequence</c>/<c>prior_hash</c>/<c>record_hash</c> headers; loading asserts
/// <c>sequence == Version - 1</c>.</item>
/// <item>Seven round tables &#8596; each row transition (insert = round opened, status change =
/// round closed) is captured by a trigger together with the accepted-step sequence it happened
/// after, and flushed as a <see cref="CampaignRoundRowHeader"/> header on that step's event; the C2
/// projections fold them into <c>Retained</c>. The open round's moving high-water mark goes to
/// <see cref="CampaignRoundHighWaterDocument"/>. A transition with no event of its own unit of work
/// to ride on fails the flush loudly (the spike's T1 fallback trigger) -- verified never to occur
/// across the equivalence suite.</item>
/// <item><c>seats</c>, <c>owner_openings</c>+<c>owner_actions</c>, <c>reference_key</c> &#8596;
/// <see cref="CampaignSeatDocument"/>, <see cref="CampaignOwnerOpeningDocument"/>,
/// <see cref="CampaignReferenceKeyDocument"/>, all optimistic-concurrency documents.</item>
/// </list>
/// <para><b>Why an in-memory relational working set.</b> It lets the journal's ~6,000 lines of
/// already-verified adjudication-orchestration logic run byte-for-byte unchanged while the durable
/// store underneath is swapped, which is what makes the SQLite-vs-Marten equivalence proof a proof
/// about storage alone. The in-memory database is process-private scratch that lives for one unit
/// of work; nothing durable is ever written to it, and it is not a store in its own right.</para>
/// </remarks>
internal sealed class CampaignJournalWorkingSet : IDisposable
{
    private static readonly string[] RoundTables =
        ["force_rounds", "rba_rounds", "cycle_rounds", "reserve_rounds", "custody_rounds", "retreat_rounds", "selection_rounds"];

    private readonly CampaignJournalMartenBackend backend;
    private readonly IDocumentSession session;
    private readonly SqliteConnection anchor;
    private readonly Dictionary<string, LoadedCampaign> loaded = new(StringComparer.Ordinal);

    private sealed record LoadedCampaign(long StreamVersion, long HeadSequence,
        Dictionary<string, CampaignSeatDocument> Seats, Dictionary<string, CampaignOwnerOpeningDocument> Openings,
        CampaignReferenceKeyDocument? ReferenceKey, CampaignRoundHighWaterDocument? HighWater);

    internal CampaignJournalWorkingSet(CampaignJournalMartenBackend backend)
    {
        this.backend = backend;
        session = backend.Store.LightweightSession();
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = "campaign-working-set-" + Guid.NewGuid().ToString("N"),
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
        }.ToString();
        anchor = new SqliteConnection(ConnectionString);
        anchor.Open();
    }

    internal string ConnectionString { get; }

    /// <summary>A fresh connection to this working set (what the journal's <c>Connect</c> returns).</summary>
    internal SqliteConnection Connect()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    internal bool CampaignExists(string campaignId) => loaded.ContainsKey(campaignId);

    public void Dispose()
    {
        anchor.Dispose();
        session.Dispose();
    }

    internal async Task LoadAsync(IReadOnlyCollection<string>? campaignIds, Func<byte[]> newReferenceKey,
        CancellationToken cancellationToken)
    {
        CampaignDurableAuthorityJournal.CreateCurrentSchema(anchor);
        Exec(anchor, "PRAGMA foreign_keys=ON");
        var ids = campaignIds ?? await session.Query<CampaignStateDocument>().Select(document => document.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        byte[]? referenceKey = null;
        foreach (var id in ids)
        {
            var events = await session.Events.FetchStreamAsync(id, token: cancellationToken).ConfigureAwait(false);
            if (events.Count == 0) continue;
            var head = LoadStream(id, events);
            var seats = (await session.Query<CampaignSeatDocument>().Where(document => document.CampaignId == id)
                .ToListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(document => document.Id, StringComparer.Ordinal);
            var openings = (await session.Query<CampaignOwnerOpeningDocument>().Where(document => document.CampaignId == id)
                .ToListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(document => document.Id, StringComparer.Ordinal);
            var key = await session.LoadAsync<CampaignReferenceKeyDocument>(id, cancellationToken).ConfigureAwait(false);
            var highWater = await session.LoadAsync<CampaignRoundHighWaterDocument>(id, cancellationToken).ConfigureAwait(false);
            foreach (var seat in seats.Values.OrderBy(document => document.Id, StringComparer.Ordinal)) InsertSeat(seat);
            foreach (var opening in openings.Values.OrderBy(document => document.Id, StringComparer.Ordinal)) InsertOpening(opening);
            foreach (var table in RoundTables)
            {
                var retained = await CampaignJournalMartenBackend.LoadRetainedAsync(session, id, table, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var row in retained.Values.OrderBy(row => row.Key, StringComparer.Ordinal))
                {
                    var current = row;
                    if (row.Status == "collecting"
                        && highWater?.Entries.TryGetValue(table + "/" + row.Key, out var mark) == true)
                        current = row with { HighWaterMs = mark.HighWaterMs, RecordHash = mark.RecordHash };
                    InsertRound(id, table, current);
                }
            }
            if (key is not null)
            {
                if (referenceKey is not null && !referenceKey.AsSpan().SequenceEqual(key.Secret))
                    throw new InvalidOperationException("Working set spans campaigns with different reference keys.");
                referenceKey = key.Secret;
            }
            loaded[id] = new LoadedCampaign(events.Count, head, seats, openings, key, highWater);
        }
        using (var command = anchor.CreateCommand())
        {
            command.CommandText = "INSERT INTO reference_key VALUES (1,$secret)";
            command.Parameters.AddWithValue("$secret", referenceKey ?? newReferenceKey());
            command.ExecuteNonQuery();
        }
        InstallRoundTriggers();
    }

    private long LoadStream(string id, IReadOnlyList<IEvent> events)
    {
        if (events[0].Data is not CampaignGenesisEvent genesis)
            throw new InvalidOperationException("Campaign stream does not start with its genesis event.");
        var (genesisSequence, _, initialHash) = CampaignEventStreamSchema.ReadStepHeaders(events[0]);
        if (genesisSequence != 0 || events[0].Version != 1)
            throw new InvalidOperationException("Campaign genesis is not at sequence 0 / version 1.");
        var headSequence = 0L;
        var headHash = initialHash;
        using var transaction = anchor.BeginTransaction();
        using (var insert = anchor.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO campaigns VALUES ($id,$version,$rules,$seed,$setup,$setupHash,
                  $pack,$content,$scenario,$creation,$snapshot,$contacts,$timing,$headSeq,$headHash)
                """;
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$version", genesis.ContractVersion);
            insert.Parameters.AddWithValue("$rules", genesis.RulesetHash);
            insert.Parameters.AddWithValue("$seed", genesis.Seed);
            insert.Parameters.AddWithValue("$setup", genesis.SetupId);
            insert.Parameters.AddWithValue("$setupHash", genesis.SetupHash);
            insert.Parameters.AddWithValue("$pack", genesis.ContentPackId);
            insert.Parameters.AddWithValue("$content", genesis.ContentHash);
            insert.Parameters.AddWithValue("$scenario", genesis.ScenarioId);
            insert.Parameters.AddWithValue("$creation", genesis.CreationEvent);
            insert.Parameters.AddWithValue("$snapshot", genesis.InitialSnapshot);
            insert.Parameters.AddWithValue("$contacts", genesis.InitialContacts);
            insert.Parameters.AddWithValue("$timing", genesis.TimingConfiguration);
            if (events.Count > 1)
            {
                var (lastSequence, _, lastHash) = CampaignEventStreamSchema.ReadStepHeaders(events[^1]);
                headSequence = lastSequence;
                headHash = lastHash;
            }
            insert.Parameters.AddWithValue("$headSeq", headSequence);
            insert.Parameters.AddWithValue("$headHash", headHash);
            insert.ExecuteNonQuery();
        }
        for (var index = 1; index < events.Count; index++)
        {
            var @event = events[index];
            if (@event.Data is not CampaignAcceptedStepEvent step)
                throw new InvalidOperationException("Campaign stream carries an unexpected event type.");
            var (sequence, priorHash, recordHash) = CampaignEventStreamSchema.ReadStepHeaders(@event);
            // C1's explicit-sequence discipline, as amended by C3b: assert, never infer.
            if (sequence != @event.Version - 1)
                throw new InvalidOperationException("Campaign step sequence differs from its stream version.");
            using var insert = anchor.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO accepted_steps VALUES ($id,$seq,$kind,$element,$ref,$epoch,$revision,$side,
                  $command,$event,$receipt,$checkpoint,$contacts,$prior,$hash)
                """;
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$seq", sequence);
            insert.Parameters.AddWithValue("$kind", step.ActionKind);
            insert.Parameters.AddWithValue("$element", (object?)step.ActingElementId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$ref", (object?)step.OwnerPublicRef ?? DBNull.Value);
            insert.Parameters.AddWithValue("$epoch", (object?)step.SeatEpoch ?? DBNull.Value);
            insert.Parameters.AddWithValue("$revision", (object?)step.OwnerRevision ?? DBNull.Value);
            insert.Parameters.AddWithValue("$side", (object?)step.Side ?? DBNull.Value);
            insert.Parameters.AddWithValue("$command", step.CanonicalCommand);
            insert.Parameters.AddWithValue("$event", step.CanonicalEvent);
            insert.Parameters.AddWithValue("$receipt", step.CanonicalReceipt);
            insert.Parameters.AddWithValue("$checkpoint", step.SnapshotCheckpoint);
            insert.Parameters.AddWithValue("$contacts", step.CanonicalContacts);
            insert.Parameters.AddWithValue("$prior", priorHash);
            insert.Parameters.AddWithValue("$hash", recordHash);
            insert.ExecuteNonQuery();
        }
        transaction.Commit();
        return headSequence;
    }

    private void InsertSeat(CampaignSeatDocument seat)
    {
        using var insert = anchor.CreateCommand();
        insert.CommandText = "INSERT INTO seats VALUES ($id,$side,$epoch,$grant,$grantExpires,$credential,$credentialExpires)";
        insert.Parameters.AddWithValue("$id", seat.CampaignId);
        insert.Parameters.AddWithValue("$side", seat.Side);
        insert.Parameters.AddWithValue("$epoch", seat.Epoch);
        insert.Parameters.AddWithValue("$grant", (object?)seat.GrantDigest ?? DBNull.Value);
        insert.Parameters.AddWithValue("$grantExpires", (object?)seat.GrantExpiresMs ?? DBNull.Value);
        insert.Parameters.AddWithValue("$credential", (object?)seat.CredentialDigest ?? DBNull.Value);
        insert.Parameters.AddWithValue("$credentialExpires", (object?)seat.CredentialExpiresMs ?? DBNull.Value);
        insert.ExecuteNonQuery();
    }

    private void InsertOpening(CampaignOwnerOpeningDocument opening)
    {
        using (var insert = anchor.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO owner_openings VALUES ($id,$side,$element,$epoch,$revision,$digest,$bytes,$authority,$count)
                """;
            insert.Parameters.AddWithValue("$id", opening.CampaignId);
            insert.Parameters.AddWithValue("$side", opening.Side);
            insert.Parameters.AddWithValue("$element", opening.ActingElementId);
            insert.Parameters.AddWithValue("$epoch", opening.SeatEpoch);
            insert.Parameters.AddWithValue("$revision", opening.Revision);
            insert.Parameters.AddWithValue("$digest", opening.VisibleDigest);
            insert.Parameters.AddWithValue("$bytes", opening.VisibleBytes);
            insert.Parameters.AddWithValue("$authority", opening.AuthorityVersion);
            insert.Parameters.AddWithValue("$count", opening.ActionCount);
            insert.ExecuteNonQuery();
        }
        foreach (var action in opening.Actions)
        {
            using var insert = anchor.CreateCommand();
            insert.CommandText = "INSERT INTO owner_actions VALUES ($id,$side,$element,$ref,$ordinal,$action)";
            insert.Parameters.AddWithValue("$id", opening.CampaignId);
            insert.Parameters.AddWithValue("$side", opening.Side);
            insert.Parameters.AddWithValue("$element", opening.ActingElementId);
            insert.Parameters.AddWithValue("$ref", action.PublicRef);
            insert.Parameters.AddWithValue("$ordinal", action.Ordinal);
            insert.Parameters.AddWithValue("$action", action.CoreActionId);
            insert.ExecuteNonQuery();
        }
    }

    private static string RoundColumns(string table) => CampaignRoundRowHeader.HasDecisionActionId(table)
        ? $"campaign_id,decision_action_id,{CampaignRoundRowHeader.KeyColumns[table]},configuration_hash,opened_ms,deadline_ms,high_water_ms,status,record_hash"
        : $"campaign_id,{CampaignRoundRowHeader.KeyColumns[table]},entry_state_version,configuration_hash,opened_ms,deadline_ms,high_water_ms,status,record_hash";

    private void InsertRound(string campaignId, string table, RetainedRoundRow row)
    {
        using var insert = anchor.CreateCommand();
        var decision = CampaignRoundRowHeader.HasDecisionActionId(table);
        insert.CommandText = $"INSERT INTO {table} ({RoundColumns(table)}) VALUES ($id,{(decision ? "$decision,$key" : "$key,$version")},$config,$opened,$deadline,$highWater,$status,$hash)";
        insert.Parameters.AddWithValue("$id", campaignId);
        insert.Parameters.AddWithValue("$key", row.Key);
        if (decision) insert.Parameters.AddWithValue("$decision", row.DecisionActionId);
        else insert.Parameters.AddWithValue("$version", row.EntryStateVersion);
        insert.Parameters.AddWithValue("$config", row.ConfigurationHash);
        insert.Parameters.AddWithValue("$opened", row.OpenedMs);
        insert.Parameters.AddWithValue("$deadline", row.DeadlineMs);
        insert.Parameters.AddWithValue("$highWater", row.HighWaterMs);
        insert.Parameters.AddWithValue("$status", row.Status);
        insert.Parameters.AddWithValue("$hash", row.RecordHash);
        insert.ExecuteNonQuery();
    }

    /// <summary>
    /// Installs (after loading, so loading itself is never logged) one insert trigger and one
    /// status-change trigger per round table, recording the full row plus the highest accepted-step
    /// sequence present at that moment -- i.e. the step the round opened/closed after. Rows written
    /// inside a transaction that later rolls back are rolled back out of this log with it.
    /// </summary>
    private void InstallRoundTriggers()
    {
        Exec(anchor, """
            CREATE TABLE ws_round_log (
              log_id INTEGER PRIMARY KEY AUTOINCREMENT, tbl TEXT NOT NULL, campaign_id TEXT NOT NULL,
              row_key TEXT NOT NULL, step_sequence INTEGER NOT NULL, decision_action_id TEXT,
              entry_state_version INTEGER, configuration_hash TEXT NOT NULL, opened_ms INTEGER NOT NULL,
              deadline_ms INTEGER NOT NULL, high_water_ms INTEGER NOT NULL, status TEXT NOT NULL,
              record_hash TEXT NOT NULL)
            """);
        foreach (var table in RoundTables)
        {
            var key = CampaignRoundRowHeader.KeyColumns[table];
            var decision = CampaignRoundRowHeader.HasDecisionActionId(table)
                ? "NEW.decision_action_id, NULL" : "NULL, NEW.entry_state_version";
            var values = $"""
                ('{table}', NEW.campaign_id, NEW.{key},
                 (SELECT COALESCE(MAX(sequence),0) FROM accepted_steps WHERE campaign_id=NEW.campaign_id),
                 {decision}, NEW.configuration_hash, NEW.opened_ms, NEW.deadline_ms, NEW.high_water_ms,
                 NEW.status, NEW.record_hash)
                """;
            const string columns = "(tbl,campaign_id,row_key,step_sequence,decision_action_id,entry_state_version,"
                + "configuration_hash,opened_ms,deadline_ms,high_water_ms,status,record_hash)";
            Exec(anchor, $"CREATE TRIGGER ws_{table}_insert AFTER INSERT ON {table} BEGIN INSERT INTO ws_round_log {columns} VALUES {values}; END");
            Exec(anchor, $"CREATE TRIGGER ws_{table}_status AFTER UPDATE ON {table} WHEN OLD.status <> NEW.status BEGIN INSERT INTO ws_round_log {columns} VALUES {values}; END");
        }
    }

    /// <summary>
    /// Writes back everything this unit of work committed into the working set: new stream events
    /// (with their per-event step headers and round-row headers), and every changed host-state
    /// document, in one <see cref="IDocumentSession.SaveChangesAsync"/> -- one Postgres transaction,
    /// all-or-nothing, with optimistic concurrency on the stream and on every document.
    /// </summary>
    internal async Task FlushAsync(CancellationToken cancellationToken)
    {
        var any = false;
        foreach (var campaignId in ReadStrings("SELECT campaign_id FROM campaigns ORDER BY campaign_id"))
            any |= StageCampaign(campaignId);
        if (!any) return;
        try
        {
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JasperFx.ConcurrencyException
            or JasperFx.DocumentAlreadyExistsException
            or Marten.Exceptions.ExistingStreamIdCollisionException
            or JasperFx.Events.EventStreamUnexpectedMaxEventIdException)
        {
            throw new CampaignJournalConcurrencyException(exception);
        }
    }

    private bool StageCampaign(string campaignId)
    {
        loaded.TryGetValue(campaignId, out var prior);
        var staged = false;
        var priorHead = prior?.HeadSequence ?? -1;

        // 1. Events: genesis (new campaigns only) + every accepted step past the loaded head.
        var changes = ReadRoundLog(campaignId);
        foreach (var sequence in changes.Keys)
            if (sequence <= priorHead)
                throw new InvalidOperationException(
                    "A round opened or closed without an accepted step of its own unit of work to carry it "
                    + "(design spike T1 fallback condition).");
        var events = new List<IEvent>();
        using (var query = anchor.CreateCommand())
        {
            query.CommandText = """
                SELECT contract_version,ruleset_hash,seed,setup_id,setup_hash,content_pack_id,content_hash,
                  scenario_id,creation_event,initial_snapshot,initial_contacts,timing_configuration,
                  head_sequence,head_hash
                FROM campaigns WHERE campaign_id=$id
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            reader.Read();
            var headSequence = reader.GetInt64(12);
            var headHash = reader.GetString(13);
            var steps = ReadSteps(campaignId, Math.Max(priorHead, 0));
            if (prior is null)
            {
                var genesis = new CampaignGenesisEvent(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
                    reader.GetString(7), Blob(reader, 8), Blob(reader, 9), Blob(reader, 10), Blob(reader, 11));
                var initialHash = steps.Count > 0 ? steps[0].PriorHash : headHash;
                events.Add(CampaignEventStreamSchema.BuildStepEvent(backend.Store, genesis, 0,
                    CampaignGenesisEvent.PriorHashSentinel, initialHash, RoundHeaders(changes, 0)));
            }
            foreach (var step in steps)
                events.Add(CampaignEventStreamSchema.BuildStepEvent(backend.Store, step.Event, step.Sequence,
                    step.PriorHash, step.RecordHash, RoundHeaders(changes, step.Sequence)));
            var expectedHead = steps.Count > 0 ? steps[^1].Sequence : Math.Max(priorHead, 0);
            var expectedHash = steps.Count > 0 ? steps[^1].RecordHash : null;
            if (headSequence != expectedHead || (expectedHash is not null && headHash != expectedHash))
                throw new InvalidOperationException("Campaign head differs from its accepted steps.");
            foreach (var sequence in changes.Keys)
                if (sequence > expectedHead || (sequence == 0 && prior is not null))
                    throw new InvalidOperationException("Round transition refers to an unknown accepted step.");
        }
        if (prior is null)
        {
            session.Events.StartStream(campaignId, events.ToArray());
            staged = true;
        }
        else if (events.Count > 0)
        {
            session.Events.Append(campaignId, prior.StreamVersion + events.Count, events.ToArray());
            staged = true;
        }

        // 2. Open-round high-water marks.
        var marks = ReadHighWaterMarks(campaignId);
        var priorMarks = prior?.HighWater?.Entries ?? ImmutableDictionary<string, CampaignRoundHighWater>.Empty;
        if (!SameMarks(marks, priorMarks))
        {
            var document = new CampaignRoundHighWaterDocument(campaignId, marks);
            if (prior?.HighWater is null) session.Insert(document); else session.Store(document);
            staged = true;
        }

        // 3. Seats.
        foreach (var seat in ReadSeats(campaignId))
        {
            CampaignSeatDocument? before = null;
            prior?.Seats.TryGetValue(seat.Id, out before);
            if (before is not null && Same(before, seat)) continue;
            if (before is null) session.Insert(seat); else session.Store(seat);
            staged = true;
        }

        // 4. Owner openings (+ their actions).
        foreach (var opening in ReadOpenings(campaignId))
        {
            CampaignOwnerOpeningDocument? before = null;
            prior?.Openings.TryGetValue(opening.Id, out before);
            if (before is not null && Same(before, opening)) continue;
            if (before is null) session.Insert(opening); else session.Store(opening);
            staged = true;
        }

        // 5. Reference key (written once, with the campaign).
        if (prior?.ReferenceKey is null)
        {
            session.Insert(new CampaignReferenceKeyDocument(campaignId,
                ReadBlob("SELECT secret FROM reference_key WHERE singleton=1")));
            staged = true;
        }
        return staged;
    }

    private sealed record StepRow(long Sequence, string PriorHash, string RecordHash, CampaignAcceptedStepEvent Event);

    private List<StepRow> ReadSteps(string campaignId, long after)
    {
        using var query = anchor.CreateCommand();
        query.CommandText = """
            SELECT sequence,action_kind,acting_element_id,owner_public_ref,seat_epoch,owner_revision,
              canonical_command,canonical_event,canonical_receipt,snapshot_checkpoint,canonical_contacts,
              prior_hash,record_hash,owner_side
            FROM accepted_steps WHERE campaign_id=$id AND sequence>$after ORDER BY sequence
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        query.Parameters.AddWithValue("$after", after);
        using var reader = query.ExecuteReader();
        var steps = new List<StepRow>();
        while (reader.Read())
            steps.Add(new StepRow(reader.GetInt64(0), reader.GetString(11), reader.GetString(12),
                new CampaignAcceptedStepEvent(reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    reader.IsDBNull(5) ? null : reader.GetInt64(5),
                    Blob(reader, 6), Blob(reader, 7), Blob(reader, 8), Blob(reader, 9), Blob(reader, 10),
                    reader.IsDBNull(13) ? null : reader.GetString(13))));
        return steps;
    }

    /// <summary>Round-row transitions of this unit of work, grouped by the step sequence they ride on
    /// (the last transition per round key per step wins).</summary>
    private SortedDictionary<long, List<RoundRowChange>> ReadRoundLog(string campaignId)
    {
        using var query = anchor.CreateCommand();
        query.CommandText = """
            SELECT tbl,row_key,step_sequence,decision_action_id,entry_state_version,configuration_hash,
              opened_ms,deadline_ms,high_water_ms,status,record_hash
            FROM ws_round_log WHERE campaign_id=$id ORDER BY log_id
            """;
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var bySequence = new SortedDictionary<long, List<RoundRowChange>>();
        while (reader.Read())
        {
            var change = new RoundRowChange(reader.GetString(0), new RetainedRoundRow(reader.GetString(1),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4),
                reader.GetString(5), reader.GetInt64(6), reader.GetInt64(7), reader.GetInt64(8),
                reader.GetString(9), reader.GetString(10)));
            var sequence = reader.GetInt64(2);
            if (!bySequence.TryGetValue(sequence, out var list)) bySequence[sequence] = list = [];
            list.RemoveAll(existing => existing.Table == change.Table && existing.Row.Key == change.Row.Key);
            list.Add(change);
        }
        return bySequence;
    }

    private static Dictionary<string, string>? RoundHeaders(SortedDictionary<long, List<RoundRowChange>> changes,
        long sequence) =>
        changes.TryGetValue(sequence, out var list) && list.Count > 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CampaignRoundRowHeader.HeaderKey] = CampaignRoundRowHeader.Serialize(list),
            }
            : null;

    private ImmutableDictionary<string, CampaignRoundHighWater> ReadHighWaterMarks(string campaignId)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CampaignRoundHighWater>(StringComparer.Ordinal);
        foreach (var table in RoundTables)
        {
            using var query = anchor.CreateCommand();
            query.CommandText = $"SELECT {CampaignRoundRowHeader.KeyColumns[table]},high_water_ms,record_hash FROM {table} WHERE campaign_id=$id AND status='collecting'";
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            while (reader.Read())
                builder[table + "/" + reader.GetString(0)] = new CampaignRoundHighWater(reader.GetInt64(1), reader.GetString(2));
        }
        return builder.ToImmutable();
    }

    private static bool SameMarks(ImmutableDictionary<string, CampaignRoundHighWater> left,
        ImmutableDictionary<string, CampaignRoundHighWater> right) =>
        left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var other) && other == pair.Value);

    private List<CampaignSeatDocument> ReadSeats(string campaignId)
    {
        using var query = anchor.CreateCommand();
        query.CommandText = "SELECT side,epoch,grant_digest,grant_expires_ms,credential_digest,credential_expires_ms FROM seats WHERE campaign_id=$id ORDER BY side";
        query.Parameters.AddWithValue("$id", campaignId);
        using var reader = query.ExecuteReader();
        var seats = new List<CampaignSeatDocument>();
        while (reader.Read())
            seats.Add(new CampaignSeatDocument(CampaignSeatDocument.IdFor(campaignId, reader.GetString(0)), campaignId,
                reader.GetString(0), reader.GetInt64(1),
                reader.IsDBNull(2) ? null : Blob(reader, 2), reader.IsDBNull(3) ? null : reader.GetInt64(3),
                reader.IsDBNull(4) ? null : Blob(reader, 4), reader.IsDBNull(5) ? null : reader.GetInt64(5)));
        return seats;
    }

    private List<CampaignOwnerOpeningDocument> ReadOpenings(string campaignId)
    {
        var openings = new List<CampaignOwnerOpeningDocument>();
        using (var query = anchor.CreateCommand())
        {
            query.CommandText = """
                SELECT side,acting_element_id,seat_epoch,revision,visible_digest,visible_bytes,authority_version,action_count
                FROM owner_openings WHERE campaign_id=$id ORDER BY side,acting_element_id
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            using var reader = query.ExecuteReader();
            while (reader.Read())
                openings.Add(new CampaignOwnerOpeningDocument(
                    CampaignOwnerOpeningDocument.IdFor(campaignId, reader.GetString(0), reader.GetString(1)), campaignId,
                    reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetString(4),
                    Blob(reader, 5), reader.GetInt64(6), reader.GetInt64(7), []));
        }
        for (var index = 0; index < openings.Count; index++)
        {
            var opening = openings[index];
            using var query = anchor.CreateCommand();
            query.CommandText = """
                SELECT public_ref,ordinal,core_action_id FROM owner_actions
                WHERE campaign_id=$id AND side=$side AND acting_element_id=$element ORDER BY ordinal,public_ref
                """;
            query.Parameters.AddWithValue("$id", campaignId);
            query.Parameters.AddWithValue("$side", opening.Side);
            query.Parameters.AddWithValue("$element", opening.ActingElementId);
            using var reader = query.ExecuteReader();
            var actions = new List<CampaignOwnerActionRow>();
            while (reader.Read())
                actions.Add(new CampaignOwnerActionRow(reader.GetString(0), reader.GetInt64(1), reader.GetString(2)));
            openings[index] = opening with { Actions = [.. actions] };
        }
        return openings;
    }

    private static bool Same<T>(T left, T right) =>
        JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);

    private List<string> ReadStrings(string sql)
    {
        using var query = anchor.CreateCommand();
        query.CommandText = sql;
        using var reader = query.ExecuteReader();
        var values = new List<string>();
        while (reader.Read()) values.Add(reader.GetString(0));
        return values;
    }

    private byte[] ReadBlob(string sql)
    {
        using var query = anchor.CreateCommand();
        query.CommandText = sql;
        return (byte[])query.ExecuteScalar()!;
    }

    private static byte[] Blob(SqliteDataReader reader, int ordinal) => reader.GetFieldValue<byte[]>(ordinal);

    private static void Exec(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

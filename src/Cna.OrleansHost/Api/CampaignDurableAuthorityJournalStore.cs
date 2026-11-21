using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Timeouts;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Production-side routing layer over <see cref="CampaignDurableAuthorityJournal"/> for the
/// design doc's &#167;3 file-per-campaign hot-tier topology
/// (<c>docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md</c>).
/// </summary>
/// <remarks>
/// <para><b>Phase C3b of issue #439: two backends, one contract.</b> The production store is now
/// the Postgres + Marten backend (<see cref="CampaignDurableAuthorityJournalStore(CampaignJournalMartenBackend)"/>,
/// wired in <c>Program.cs</c>): one Marten stream per campaign, keyed by the campaign id (C1's
/// stream-identity convention), replacing the file-per-campaign routing below; every entry point is
/// reached through its generated <c>*Async</c> twin (Marten 9.62 has no synchronous
/// <c>SaveChanges</c>). The SQLite file constructor and the synchronous forwarders remain only as
/// the test-side baseline the Marten store's equivalence was proven against
/// (<c>CampaignDurableAuthorityJournalTests.MartenEquivalence.cs</c>); the host no longer accepts
/// <c>CampaignJournal:Path</c>. The remarks below describe that file-backed baseline.</para>
/// <para><b>Why this exists.</b> Before this change, one <see cref="CampaignDurableAuthorityJournal"/>
/// instance -- opened once at host startup against one fixed path -- served every campaign on the
/// host for its entire lifetime, because every campaign shared the one file that instance wrapped.
/// <see cref="CampaignDurableAuthorityJournal"/> itself is deliberately left as a per-file handle
/// (own <c>path</c>, own <c>schemaVersion</c>, own connections) rather than being taught to juggle
/// many files internally -- see its own remarks and <see cref="JournalFor"/> below. Something still
/// has to decide, for an arbitrary incoming campaign id, which file (lazily created on first use)
/// that campaign's journal instance is, and cache it for the life of the host; that is this class's
/// entire job. It is <see cref="CampaignDurableOutwardApi"/>'s injected dependency now, in place of
/// a bare <see cref="CampaignDurableAuthorityJournal"/>, and otherwise forwards every entry point
/// <see cref="CampaignDurableAuthorityJournal"/> exposes to that API unchanged -- see the generated
/// forwarding block below.</para>
/// <para><b>Why <see cref="CampaignDurableAuthorityJournal"/> itself did not need a directory-aware
/// rewrite.</b> Nearly every one of its methods already took the campaign's identity as a plain
/// parameter (directly, or via a credential/grant/request that carries a <c>CampaignId</c>) purely
/// because the SQL inside needed it -- not because the instance was multi-campaign-aware. That made
/// the actual topology change small and contained: <see cref="CampaignDurableAuthorityJournal.CreateNew"/>
/// and <c>OpenExisting</c> keep their existing single-file signatures untouched (this class is what
/// computes the per-campaign path and passes it in), its crash-safety and migration machinery runs
/// completely unmodified per file, and the one piece of genuinely cross-campaign behavior left
/// (the seven <c>CollectingXCampaigns</c> sweep-enumeration entry points) is handled by
/// <see cref="CampaignDurableAuthorityJournal.CollectingAcrossSiblingFiles"/> -- any single
/// instance can answer that correctly by looking at its own directory, so <see
/// cref="CampaignCombatTimeoutService"/> needs no changes at all and keeps depending directly on a
/// <see cref="CampaignDurableAuthorityJournal"/> (see the hand-written <c>CollectingXCampaigns</c>
/// wrappers below for how this class answers the same question with an empty cache).</para>
/// <para><b>Host identity.</b> <c>operator_capability</c>/<c>reference_key</c> moved out of the
/// per-campaign schema into one small shared file per directory (see
/// <see cref="CampaignDurableAuthorityJournal.EnsureHostIdentity"/>'s remarks for why they are
/// host-wide identity, not per-campaign data) -- this constructor ensures it exists eagerly, before
/// the host ever serves a request, mirroring how today's single shared file was always fully
/// initialized before <c>Program.cs</c> finished building the host.</para>
/// <para><b>Deliberately out of scope here</b> (see the PR description for the full list): the
/// hot/cold archival split (&#167;4), replacing the sweep with per-grain reminders (&#167;7), and
/// snapshotting (&#167;5, separately documented as blocked). This class only resolves "which file"
/// for the topology that already exists today, campaign-by-campaign.</para>
/// </remarks>
internal sealed class CampaignDurableAuthorityJournalStore
{
    private readonly string? directory;
    private readonly string? operatorSecret;
    private readonly TimeProvider clock;
    private readonly IContentPackV6Resolver? contentResolver;
    private readonly CampaignDurableAuthorityJournal? fixedJournal;
    private readonly CampaignJournalMartenBackend? marten;
    private readonly ConcurrentDictionary<string, Lazy<CampaignDurableAuthorityJournal>> journals = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, List<(CampaignRoundKind RoundKind, long? DeadlineMs)>> martenTimerActions =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Phase C3b of issue #439: the production store, backed by Postgres + Marten
    /// (<see cref="CampaignJournalMartenBackend"/>). One Marten stream per campaign, keyed by the
    /// campaign id itself (C1's stream-identity convention), so "which store" routing is simply the
    /// stream key -- there are no per-campaign files and nothing to cache. Async-only: every
    /// <c>*Async</c> entry point loads that campaign's working set, runs the unchanged journal
    /// method against it, and flushes the result back in one Postgres transaction.
    /// </summary>
    internal CampaignDurableAuthorityJournalStore(CampaignJournalMartenBackend backend)
    {
        marten = backend ?? throw new ArgumentNullException(nameof(backend));
        clock = backend.Clock;
        contentResolver = backend.ContentResolver;
    }

    /// <summary>True when this store's durable backend is Postgres + Marten.</summary>
    internal bool IsMartenBacked => marten is not null;

    /// <summary>
    /// Runs <paramref name="body"/> against <paramref name="campaignId"/>'s journal. SQLite file
    /// store: the existing per-campaign file instance, synchronously. Marten store: a fresh working
    /// set of that one campaign; whatever the journal method committed is flushed back even when it
    /// then throws (several journal paths deliberately commit a timeout and then reject the caller's
    /// request), and the method's own exception is rethrown afterwards.
    /// </summary>
    /// <remarks>
    /// <b>Optimistic-concurrency conflicts are retried, reproducing SQLite's serialization.</b>
    /// Under SQLite a second writer's <c>BEGIN IMMEDIATE</c> waited for the first to commit and then
    /// ran against the committed state. Here, a flush that loses an optimistic-concurrency race
    /// discards that attempt entirely (nothing of it reached Postgres) and re-runs the same journal
    /// method against a freshly loaded working set -- i.e. serialized after the winner, with the
    /// same deterministic journal logic. Only after <see cref="MaxConcurrencyAttempts"/> lost races
    /// does <see cref="CampaignJournalConcurrencyException"/> surface (an
    /// <see cref="InvalidOperationException"/> carrying SQLite's own head-CAS message, so callers
    /// map it to 409 exactly as before). Found by the full journal suite run against Marten
    /// (<c>ConcurrentScheduledArrivalRetriesCommitOnce</c>: two concurrent idempotent retries must
    /// both receive the one committed reply, not a conflict).
    /// </remarks>
    internal async Task<T> RunAsync<T>(string campaignId, bool createIfMissing,
        Func<CampaignDurableAuthorityJournal, T> body, CancellationToken cancellationToken = default)
    {
        if (marten is null) return body(JournalFor(campaignId, createIfMissing));
        RequireSafeCampaignId(campaignId);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await RunOnceAsync(campaignId, createIfMissing, body, cancellationToken).ConfigureAwait(false);
            }
            catch (CampaignJournalConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                // Lost the race; nothing of this attempt was persisted. Re-run against fresh state.
            }
        }
    }

    /// <summary>Attempts per call before an optimistic-concurrency conflict surfaces (see <see cref="RunAsync"/>).</summary>
    internal const int MaxConcurrencyAttempts = 8;

    private async Task<T> RunOnceAsync<T>(string campaignId, bool createIfMissing,
        Func<CampaignDurableAuthorityJournal, T> body, CancellationToken cancellationToken)
    {
        var marten = this.marten!;
        using var workingSet = await marten.OpenAsync([campaignId], () => marten.RandomBytes(32), cancellationToken)
            .ConfigureAwait(false);
        if (!createIfMissing && !workingSet.CampaignExists(campaignId))
            throw new InvalidOperationException($"Campaign '{campaignId}' has no durable journal.");
        var journal = CampaignDurableAuthorityJournal.ForWorkingSet(workingSet.Connect, marten.AuthenticateOperator,
            marten.Clock, marten.ContentResolver, marten.RandomBytes);
        T result = default!;
        ExceptionDispatchInfo? failure = null;
        try { result = body(journal); }
        catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
        await workingSet.FlushAsync(cancellationToken).ConfigureAwait(false);
        var actions = journal.DrainPendingRoundTimerActions();
        if (actions.Count > 0)
        {
            var pending = martenTimerActions.GetOrAdd(campaignId, _ => []);
            lock (pending) pending.AddRange(actions);
        }
        failure?.Throw();
        return result;
    }

    internal CampaignDurableAuthorityJournalStore(string directory, string operatorSecret,
        TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorSecret);
        this.directory = Path.GetFullPath(directory);
        this.operatorSecret = operatorSecret;
        this.clock = clock ?? TimeProvider.System;
        this.contentResolver = contentResolver;
        if (!Directory.Exists(this.directory))
        {
            Directory.CreateDirectory(this.directory);
            // Match CreateNew's own directory-creation path (CampaignDurableAuthorityJournal.cs):
            // a directory created with the process's default umask can leave group/other read
            // access, which RequirePrivateDirectory (run by EnsureHostIdentity below, and by every
            // per-campaign CreateNew/OpenExisting this store performs) refuses.
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(this.directory, UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute);
        }
        CampaignDurableAuthorityJournal.EnsureHostIdentity(
            CampaignDurableAuthorityJournal.HostIdentityPathForDirectory(this.directory), operatorSecret);
    }

    private CampaignDurableAuthorityJournalStore(CampaignDurableAuthorityJournal fixedJournal)
    {
        this.fixedJournal = fixedJournal;
        clock = TimeProvider.System;
    }

    /// <summary>
    /// Test-only convenience: every campaign id routes to the one already-constructed <paramref
    /// name="journal"/>, matching how this project's HTTP-layer tests built their setup before this
    /// change (one shared <see cref="CampaignDurableAuthorityJournal"/> registered directly in DI) --
    /// see the PR description for exactly which test files use this and why no other test change was
    /// needed to keep them exercising real per-campaign-file production code otherwise.
    /// </summary>
    internal static CampaignDurableAuthorityJournalStore ForSingleCampaign(
        CampaignDurableAuthorityJournal journal) => new(journal);

    /// <summary>
    /// Resolves (lazily creating or opening, and caching for the life of this store) the journal
    /// instance for <paramref name="campaignId"/>. <paramref name="createIfMissing"/> distinguishes
    /// "this call is itself creating the campaign" (its file may not exist yet) from every other
    /// call (the campaign must already have a file, or this throws exactly like today's single-file
    /// <see cref="CampaignDurableAuthorityJournal.OpenExisting"/> does for an unknown campaign).
    /// </summary>
    internal CampaignDurableAuthorityJournal JournalFor(string campaignId, bool createIfMissing)
    {
        if (fixedJournal is not null) return fixedJournal;
        if (marten is not null)
            throw new InvalidOperationException("The Marten-backed store is async-only; use its *Async entry points.");
        if (directory is null) throw new InvalidOperationException("Store has no backing directory.");
        if (journals.TryGetValue(campaignId, out var cached)) return cached.Value;
        var path = JournalPath(directory, campaignId);
        if (!createIfMissing && !File.Exists(path))
            throw new InvalidOperationException($"Campaign '{campaignId}' has no durable journal.");
        var lazy = journals.GetOrAdd(campaignId, _ => new Lazy<CampaignDurableAuthorityJournal>(
            () => File.Exists(path)
                ? CampaignDurableAuthorityJournal.OpenExisting(path, clock, contentResolver)
                : CampaignDurableAuthorityJournal.CreateNew(path, operatorSecret!, clock, contentResolver),
            LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private static string JournalPath(string directory, string campaignId) =>
        Path.Combine(directory, RequireSafeCampaignId(campaignId) + CampaignDurableAuthorityJournal.CampaignFileExtension);

    /// <summary>
    /// Defense-in-depth against path traversal/injection before <paramref name="campaignId"/> ever
    /// becomes part of a file path. <see cref="CampaignCreationRequest.CampaignId"/> is already
    /// validated upstream to the same character class this enforces (<c>Cna.Core</c>'s
    /// <c>ContentContractGuards.RequireStableId</c>, not reusable here across the assembly
    /// boundary -- it is <see langword="internal"/> to <c>Cna.Core</c>), but a campaign id reaching
    /// this store via an outward-API route parameter (e.g. <c>/{campaignId}/source-axis-supplement</c>)
    /// is bound straight from the URL and never passes through that check, so this store cannot
    /// assume it already holds a safe value.
    /// </summary>
    private static string RequireSafeCampaignId(string campaignId)
    {
        static bool IsAsciiLetterOrDigit(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9';
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId, nameof(campaignId));
        if (campaignId.Length > 200 || !IsAsciiLetterOrDigit(campaignId[0]) || !IsAsciiLetterOrDigit(campaignId[^1]))
            throw new ArgumentException(
                "Campaign id must be 1-200 characters, beginning and ending with a lowercase ASCII letter or digit.",
                nameof(campaignId));
        var previousWasSeparator = false;
        foreach (var character in campaignId)
        {
            if (IsAsciiLetterOrDigit(character)) { previousWasSeparator = false; continue; }
            if (character is '-' or '_' && !previousWasSeparator) { previousWasSeparator = true; continue; }
            throw new ArgumentException(
                "Campaign id must use lowercase ASCII letters, digits, and nonadjacent hyphen or underscore separators.",
                nameof(campaignId));
        }
        return campaignId;
    }

    /// <summary>
    /// Host-level operator authentication, usable even when this store has never yet resolved a
    /// single campaign -- see <see cref="CampaignDurableAuthorityJournal.AuthenticateOperatorAt"/>'s
    /// remarks (operator capability is host-identity state keyed by directory, not by campaign).
    /// </summary>
    internal bool AuthenticateOperator(string secret) => marten is not null
        ? marten.AuthenticateOperator(secret)
        : fixedJournal is not null
        ? fixedJournal.AuthenticateOperator(secret)
        : CampaignDurableAuthorityJournal.AuthenticateOperatorAt(
            directory ?? throw new InvalidOperationException("Store has no backing directory."), secret);

    /// <summary>
    /// Any already-resolved journal instance can answer a <c>CollectingXCampaigns</c> query
    /// correctly on its own (<see cref="CampaignDurableAuthorityJournal.CollectingAcrossSiblingFiles"/>
    /// looks at its own directory, not just its own file) -- so this store only needs to hand back
    /// *some* instance, preferring one it has already opened over opening a fresh one, and returning
    /// null only when the directory holds no campaign at all yet.
    /// </summary>
    private CampaignDurableAuthorityJournal? AnyJournal()
    {
        if (fixedJournal is not null) return fixedJournal;
        foreach (var lazy in journals.Values)
            if (lazy.IsValueCreated) return lazy.Value;
        if (directory is null) return null;
        string[] siblings;
        try { siblings = Directory.GetFiles(directory, "*" + CampaignDurableAuthorityJournal.CampaignFileExtension); }
        catch (DirectoryNotFoundException) { return null; }
        if (siblings.Length == 0) return null;
        try { return CampaignDurableAuthorityJournal.OpenExisting(siblings[0], clock, contentResolver); }
        catch (InvalidOperationException) { return null; }
    }

    /// <summary>
    /// Drains the round-open/round-close signals <see cref="CampaignDurableAuthorityJournal"/>
    /// recorded for <paramref name="campaignId"/> since the last drain. Phase C3a of issue #439:
    /// the only consumer is <c>CampaignGrain</c>, which turns each signal into a real
    /// <c>ICampaignRoundTimeoutGrain.ArmAsync</c>/<c>DisarmAsync</c> call after this store call's
    /// own transaction has already committed -- see <see cref="CampaignDurableAuthorityJournal.pendingRoundTimerActions"/>'s
    /// remarks for why that ordering matters.
    /// </summary>
    internal IReadOnlyList<(CampaignRoundKind RoundKind, long? DeadlineMs)> DrainPendingRoundTimerActions(
        string campaignId)
    {
        if (marten is null) return JournalFor(campaignId, createIfMissing: false).DrainPendingRoundTimerActions();
        if (!martenTimerActions.TryRemove(campaignId, out var pending)) return [];
        lock (pending) return [.. pending];
    }

    /// <summary>
    /// <see cref="Timeouts.ICampaignRoundDeadlineSink.GetDeadlineAsync"/>'s read side: the current
    /// deadline for campaign <paramref name="campaignId"/>'s open round of <paramref
    /// name="roundKind"/>, or <see langword="null"/> if none is open (or the campaign has no
    /// durable journal yet -- a timeout grain can be armed and later queried for a campaign whose
    /// file does not exist in this store instance, e.g. in an isolated test).
    /// </summary>
    internal long? GetRoundDeadlineMs(string campaignId, CampaignRoundKind roundKind)
    {
        CampaignDurableAuthorityJournal journal;
        try { journal = JournalFor(campaignId, createIfMissing: false); }
        catch (InvalidOperationException) { return null; }
        return roundKind switch
        {
            CampaignRoundKind.Selection => journal.GetSelectionRoundDeadlineMs(campaignId),
            CampaignRoundKind.Rba => journal.GetRbaRoundDeadlineMs(campaignId),
            CampaignRoundKind.Force => journal.GetForceRoundDeadlineMs(campaignId),
            CampaignRoundKind.Retreat => journal.GetRetreatRoundDeadlineMs(campaignId),
            CampaignRoundKind.Custody => journal.GetCustodyRoundDeadlineMs(campaignId),
            CampaignRoundKind.Reserve => journal.GetReserveRoundDeadlineMs(campaignId),
            CampaignRoundKind.Cycle => journal.GetCycleRoundDeadlineMs(campaignId),
            _ => throw new ArgumentOutOfRangeException(nameof(roundKind), roundKind, null),
        };
    }

    /// <summary>
    /// <see cref="Timeouts.ICampaignRoundDeadlineSink.TickAsync"/>'s eventual target, reached via
    /// <c>CampaignGrain.TickRoundAsync</c>: the moral equivalent of
    /// <see cref="CampaignCombatTimeoutService"/>'s per-lane <c>journal.TickXRound(campaignId)</c>
    /// call, dispatched by round kind instead of by a dedicated per-lane delegate.
    /// </summary>
    /// <summary>
    /// Async twin of <see cref="GetRoundDeadlineMs"/>. Marten store: an O(1) read of that round
    /// kind's C2 projection document (no stream fetch) -- the read side every timeout-grain
    /// activation and reminder tick hits.
    /// </summary>
    internal async Task<long?> GetRoundDeadlineMsAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken = default)
    {
        if (marten is null) return GetRoundDeadlineMs(campaignId, roundKind);
        RequireSafeCampaignId(campaignId);
        return await marten.GetCollectingDeadlineMsAsync(campaignId, RoundTable(roundKind), cancellationToken)
            .ConfigureAwait(false);
    }

    private static string RoundTable(CampaignRoundKind roundKind) => roundKind switch
    {
        CampaignRoundKind.Selection => "selection_rounds",
        CampaignRoundKind.Rba => "rba_rounds",
        CampaignRoundKind.Force => "force_rounds",
        CampaignRoundKind.Retreat => "retreat_rounds",
        CampaignRoundKind.Custody => "custody_rounds",
        CampaignRoundKind.Reserve => "reserve_rounds",
        CampaignRoundKind.Cycle => "cycle_rounds",
        _ => throw new ArgumentOutOfRangeException(nameof(roundKind), roundKind, null),
    };

    /// <summary>Async twin of <see cref="TickRound"/>.</summary>
    internal Task<bool> TickRoundAsync(string campaignId, CampaignRoundKind roundKind) => roundKind switch
    {
        CampaignRoundKind.Selection => TickSelectionRoundAsync(campaignId),
        CampaignRoundKind.Rba => TickRbaRoundAsync(campaignId),
        CampaignRoundKind.Force => TickForceRoundAsync(campaignId),
        CampaignRoundKind.Retreat => TickRetreatRoundAsync(campaignId),
        CampaignRoundKind.Custody => TickCustodyRoundAsync(campaignId),
        CampaignRoundKind.Reserve => TickReserveRoundAsync(campaignId),
        CampaignRoundKind.Cycle => TickCycleRoundAsync(campaignId),
        _ => throw new ArgumentOutOfRangeException(nameof(roundKind), roundKind, null),
    };

    /// <summary>
    /// Async twin of <see cref="RedeemSeatGrant"/>. Seat grants are single-use: under SQLite two
    /// racing redeemers were serialized by <c>BEGIN IMMEDIATE</c> and the loser saw a consumed grant
    /// and got <see langword="null"/>. Under Marten both may read the unconsumed grant, but only one
    /// seat-document write survives optimistic concurrency. The loser is retried by
    /// <see cref="RunAsync"/> against fresh state, sees the consumed grant and returns
    /// <see langword="null"/> -- SQLite's outcome. Should retries ever be exhausted, the surfacing
    /// <see cref="CampaignJournalConcurrencyException"/> is mapped to that same
    /// <see langword="null"/> here rather than a 409.
    /// </summary>
    internal async Task<CampaignSeatCredential?> RedeemSeatGrantAsync(CampaignSeatGrant grant)
    {
        try
        {
            return await RunAsync(grant.CampaignId, false, journal => journal.RedeemSeatGrant(grant)).ConfigureAwait(false);
        }
        catch (CampaignJournalConcurrencyException)
        {
            return null;
        }
    }

    internal bool TickRound(string campaignId, CampaignRoundKind roundKind) => roundKind switch
    {
        CampaignRoundKind.Selection => TickSelectionRound(campaignId),
        CampaignRoundKind.Rba => TickRbaRound(campaignId),
        CampaignRoundKind.Force => TickForceRound(campaignId),
        CampaignRoundKind.Retreat => TickRetreatRound(campaignId),
        CampaignRoundKind.Custody => TickCustodyRound(campaignId),
        CampaignRoundKind.Reserve => TickReserveRound(campaignId),
        CampaignRoundKind.Cycle => TickCycleRound(campaignId),
        _ => throw new ArgumentOutOfRangeException(nameof(roundKind), roundKind, null),
    };

    internal IReadOnlyList<string> CollectingSelectionCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingSelectionCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingRbaCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingRbaCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingForceCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingForceCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingRetreatCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingRetreatCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingCustodyCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingCustodyCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingReserveCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingReserveCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingCycleCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingCycleCampaigns(after, limit) ?? [];

    // <auto-generated-by-hand>
    // Everything below is a one-to-one forwarding surface onto JournalFor(campaignId), mirroring
    // every internal entry point CampaignDurableAuthorityJournal exposes to CampaignDurableOutwardApi.
    // Generated mechanically from each method's own signature (never hand-typed, to avoid a
    // transcription mistake across ~150 near-identical forwarders) -- see the PR description for
    // the generator. Keep it that way: if CampaignDurableAuthorityJournal's outward-facing surface
    // changes, regenerate this block rather than hand-editing it.
    internal bool TickCustodyRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickCustodyRound(campaignId);

    internal bool TickCycleRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickCycleRound(campaignId);

    internal bool TickForceRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickForceRound(campaignId);

    internal CampaignOwnerOpening QueryOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryOwnerOpening(credential, actingElementId);

    internal CampaignOwnerOpening QueryPregameAxisSupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPregameAxisSupplyOpening(credential);

    internal CampaignOwnerOpening QueryPregameAxisSupplyFinishOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPregameAxisSupplyFinishOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisDumpOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisDumpOpening(credential);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCommonwealthSupplyOpening(credential);

    internal CampaignOwnerOpening QuerySourceDummyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceDummyOpening(credential);

    internal CampaignOwnerOpening QuerySourceMotorizationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMotorizationOpening(credential);

    internal CampaignOwnerOpening QuerySourceUnattachedCargoOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceUnattachedCargoOpening(credential);

    internal CampaignOwnerOpening QuerySourceActionsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceActionsOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisGt1SupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisGt1SupplyOpening(credential);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyPlanOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCommonwealthSupplyPlanOpening(credential);

    internal CampaignOwnerOpening QuerySourceAutoblindaOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAutoblindaOpening(credential);

    internal CampaignOwnerOpening QuerySourcePriorityMotorizationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourcePriorityMotorizationOpening(credential);

    internal CampaignOwnerOpening QuerySourceAttachedInitialLoadsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAttachedInitialLoadsOpening(credential);

    internal CampaignOwnerOpening QuerySourceInitialWrecksOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceInitialWrecksOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisFreeSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisFreeSetupOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisCorpsSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisCorpsSetupOpening(credential);

    internal CampaignOwnerOpening QuerySourceMalettiHierarchyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMalettiHierarchyOpening(credential);

    internal CampaignOwnerOpening QuerySourceGt2CorrectionOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceGt2CorrectionOpening(credential);

    internal CampaignOwnerOpening QuerySourceCwPriorityCatalogOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCwPriorityCatalogOpening(credential);

    internal CampaignOwnerOpening QuerySourceMatruhFormationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMatruhFormationOpening(credential);

    internal CampaignOwnerOpening QuerySourceCwFreeGunsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCwFreeGunsOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisPriorityOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisPriorityOpening(credential);

    internal CampaignOwnerOpening QuerySourceCairoSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCairoSetupOpening(credential);

    internal CampaignOwnerOpening QueryCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCombatOwnerOpening(credential, actingElementId);

    internal CampaignOwnerOpening QueryRbaOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRbaOwnerOpening(credential, defenderElementId);

    internal CampaignOwnerOpening QueryForceOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryForceOwnerOpening(credential, elementId);

    internal CampaignOwnerOpening QueryPostAssaultRetreatOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPostAssaultRetreatOwnerOpening(credential, defenderElementId);

    internal CampaignOwnerOpening QueryCustodyOwnerOpening(CampaignSeatCredential credential, string captorElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCustodyOwnerOpening(credential, captorElementId);

    internal CampaignOwnerOpening QueryReserveReleaseOwnerOpening(CampaignSeatCredential credential, string reserveElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryReserveReleaseOwnerOpening(credential, reserveElementId);

    internal CampaignOwnerOpening QueryCycleControlOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCycleControlOwnerOpening(credential);

    internal CampaignOwnerOpening QueryInitiativeOrderOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryInitiativeOrderOwnerOpening(credential);

    internal CampaignOwnerOpening QueryReserveDesignationOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryReserveDesignationOwnerOpening(credential);

    internal CampaignOwnerOpening QueryTruckConvoyPassOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryTruckConvoyPassOwnerOpening(credential);

    internal CampaignOwnerOpening QueryPostCombatPassOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPostCombatPassOwnerOpening(credential);

    internal CampaignOwnerOpening QuerySecondReserveOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondReserveOwnerOpening(credential);

    internal CampaignOwnerOpening QuerySecondMovementOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondMovementOwnerOpening(credential, elementId);

    internal CampaignOwnerOpening QuerySecondMovementCompletionOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondMovementCompletionOwnerOpening(credential);

    internal CampaignOwnerOpening QueryRepeatMovementOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRepeatMovementOwnerOpening(credential);

    internal CampaignOwnerOpening QueryRepeatCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRepeatCombatOwnerOpening(credential, actingElementId);

    internal bool TickRbaRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickRbaRound(campaignId);

    internal bool TickReserveRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickReserveRound(campaignId);

    internal bool TickRetreatRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickRetreatRound(campaignId);

    internal void CreateScheduledWorld6(CampaignCreationRequest request) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateScheduledWorld6(request);

    internal IReadOnlyList<ScheduledWorld6Offer> QueryScheduledWorld6(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryScheduledWorld6(credential);

    internal byte[]? SubmitScheduledWorld6(CampaignSeatCredential credential, string publicReference) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitScheduledWorld6(credential, publicReference);

    internal CampaignSeatGrant IssueSeatGrant(string campaignId, CampaignActionAudience audience, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).IssueSeatGrant(campaignId, audience, operatorSecret);

    internal CampaignSeatCredential? RedeemSeatGrant(CampaignSeatGrant grant) =>
        JournalFor(grant.CampaignId, createIfMissing: false).RedeemSeatGrant(grant);

    internal CampaignSeatAuthority? AuthenticateSeat(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).AuthenticateSeat(credential);

    internal byte[] QuerySourceCounterContacts(CampaignSeatCredential credential, string counterId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCounterContacts(credential, counterId);

    internal bool TickSelectionRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickSelectionRound(campaignId);

    internal CampaignOwnerSubmissionResult SubmitOwnerMovement(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerMovement(credential, actingElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitPregameAxisSupply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupplyFinish(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitPregameAxisSupplyFinish(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisDump(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisDump(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCommonwealthSupply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceDummy(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceDummy(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMotorization(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMotorization(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceUnattachedCargo(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceUnattachedCargo(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceActions(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceActions(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisGt1Supply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisGt1Supply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupplyPlan(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCommonwealthSupplyPlan(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAutoblinda(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAutoblinda(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourcePriorityMotorization(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourcePriorityMotorization(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAttachedInitialLoads(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAttachedInitialLoads(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceInitialWreck(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceInitialWreck(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisFreeUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceAxisFreeUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisCorpsUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceAxisCorpsUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMalettiHierarchy(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMalettiHierarchy(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceGt2Correction(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceGt2Correction(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCwPriorityCatalog(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCwPriorityCatalog(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMatruhFormation(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMatruhFormation(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceCwFreeGun(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceCwFreeGun(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisPriority(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisPriority(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCairoUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCairoUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCombatSelection(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCombatSelection(credential, actingElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaDecline(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRbaDecline(credential, defenderElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerForceSeal(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerForceSeal(credential, elementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostAssaultRetreat(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerPostAssaultRetreat(credential, defenderElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCustody(CampaignSeatCredential credential, string captorElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCustody(credential, captorElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerReserveRelease(CampaignSeatCredential credential, string reserveElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerReserveRelease(credential, reserveElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCycleControl(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCycleControl(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerInitiativeOrder(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerInitiativeOrder(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerReserveDesignation(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerReserveDesignation(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerTruckConvoyPass(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerTruckConvoyPass(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostCombatPass(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerPostCombatPass(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondReserve(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondReserve(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovement(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondMovement(credential, elementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovementCompletion(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondMovementCompletion(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatMovement(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRepeatMovement(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatCombat(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRepeatCombat(credential, actingElementId, openingRevision, publicActionRef);

    internal ExerciseSession CreateCampaign(CampaignCreationRequest request, CampaignDecisionTimingConfigurationV1? timing = null) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateCampaign(request, timing);

    internal CampaignSourceSession CreateCampaign(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateCampaign(request, content, initialization);

    internal ExerciseSession CreatePregameSourceCampaign(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreatePregameSourceCampaign(request, content, initialization);

    internal ExerciseSession ReadVerified(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).ReadVerified(campaignId);

    internal CampaignSourceSession ReadSourceVerified(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).ReadSourceVerified(campaignId);

    internal ExerciseStepResult Append(string campaignId, CampaignActionSubmission submission) =>
        JournalFor(campaignId, createIfMissing: false).Append(campaignId, submission);

    internal bool AdmitSourceAxisSupplement(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisSupplement(campaignId, operatorSecret);

    internal bool AdmitSourceCommonwealthSupplement(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCommonwealthSupplement(campaignId, operatorSecret);

    internal bool AdmitSourcePriorityMotorization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourcePriorityMotorization(campaignId, operatorSecret);

    internal bool AdmitSourceAttachedInitialLoads(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAttachedInitialLoads(campaignId, operatorSecret);

    internal bool AdmitSourceInitialWrecks(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceInitialWrecks(campaignId, operatorSecret);

    internal bool AdmitSourceAxisFreeSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisFreeSetup(campaignId, operatorSecret);

    internal bool AdmitSourceAxisCorpsSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisCorpsSetup(campaignId, operatorSecret);

    internal bool AdmitSourceMalettiHierarchy(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMalettiHierarchy(campaignId, operatorSecret);

    internal bool AdmitSourceGt2Correction(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceGt2Correction(campaignId, operatorSecret);

    internal bool AdmitSourceCwPriorityCatalog(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCwPriorityCatalog(campaignId, operatorSecret);

    internal bool AdmitSourceCompactCheckpoint(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCompactCheckpoint(campaignId, operatorSecret);

    internal bool AdmitSourceFourthIndianWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceFourthIndianWorld(campaignId, operatorSecret);

    internal bool AdmitSourceMatruhGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMatruhGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceMalettiGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMalettiGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceLibyanGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceLibyanGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceItalianDivisionsGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceItalianDivisionsGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceSeventhArmoredGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceSeventhArmoredGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceSixteenthBrigadeGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceSixteenthBrigadeGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceCatanzaroGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCatanzaroGroupedWorld(campaignId, operatorSecret);

    internal bool StartSourceTurn(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).StartSourceTurn(campaignId, operatorSecret);

    internal bool ResolveSourceInitiative(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ResolveSourceInitiative(campaignId, operatorSecret);

    internal bool CompleteSourceGt1Scheduling(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1Scheduling(campaignId, operatorSecret);

    internal bool CompleteSourceGt1Organization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1Organization(campaignId, operatorSecret);

    internal bool CompleteSourceGt1StageOneArrivals(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1StageOneArrivals(campaignId, operatorSecret);

    internal bool CompleteSourceGt1StageOneFleet(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1StageOneFleet(campaignId, operatorSecret);

    internal bool CompleteSourceGt1FirstBreakdown(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1FirstBreakdown(campaignId, operatorSecret);

    internal bool ResolveSourceGt1Weather(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ResolveSourceGt1Weather(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1Lane2(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1Lane2(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1Lane3(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1Lane3(campaignId, operatorSecret);

    internal bool CorrectSourceSeventhShell(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CorrectSourceSeventhShell(campaignId, operatorSecret);

    internal bool ActivateSourceAxisMinorGarrisons(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceAxisMinorGarrisons(campaignId, operatorSecret);

    internal bool ActivateSourceAxisGarrisonRemainder(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceAxisGarrisonRemainder(campaignId, operatorSecret);

    internal bool ActivateSourceGiarabubMotorMachineGun(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceGiarabubMotorMachineGun(campaignId, operatorSecret);

    internal bool ActivateSourceCwFrontline(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceCwFrontline(campaignId, operatorSecret);

    internal bool AdmitSourceGiarabubMotorPlacementPolicy(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceGiarabubMotorPlacementPolicy(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1MultiLanes(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1MultiLanes(campaignId, operatorSecret);

    internal bool AdmitSourceMatruhFormation(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMatruhFormation(campaignId, operatorSecret);

    internal bool AdmitSourceCwFreeGuns(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCwFreeGuns(campaignId, operatorSecret);

    internal bool AdmitSourceAxisPriorityMotorization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisPriorityMotorization(campaignId, operatorSecret);

    internal bool AdmitSourceAxisMinorGarrisons(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisMinorGarrisons(campaignId, operatorSecret);

    internal bool AdmitSourceBattlefield(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceBattlefield(campaignId, operatorSecret);

    internal bool AdmitSourceCairoSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCairoSetup(campaignId, operatorSecret);

    internal bool AdmitSourceCairoWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCairoWorld(campaignId, operatorSecret);

    internal bool PromoteSourceMaletti(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceMaletti(campaignId, operatorSecret);

    internal bool PromoteSourceIndependent(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceIndependent(campaignId, operatorSecret);

    internal bool PromoteSourceExpandedIndependent(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceExpandedIndependent(campaignId, operatorSecret);

    internal bool PromoteSourceAxisFreeWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceAxisFreeWorld(campaignId, operatorSecret);

    internal bool PromoteSourceAxisCorpsWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceAxisCorpsWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCertifiedMalettiWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCertifiedMalettiWorld(campaignId, operatorSecret);

    internal bool PromoteSourceMatruhDetachedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceMatruhDetachedWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCwPriorityWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCwPriorityWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCwFreeGunsWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCwFreeGunsWorld(campaignId, operatorSecret);

    internal bool AdmitSourceLocalMap(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceLocalMap(campaignId, operatorSecret);

    internal int AdmitAvailableSourceProfiles(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitAvailableSourceProfiles(campaignId, operatorSecret);

    // <auto-generated-by-hand: async>
    // Phase C3b of issue #439: the async twin of every forwarder above, generated mechanically from
    // the block above (same generator discipline). Works against either backend: a SQLite file
    // store answers synchronously; the Marten store loads a working set, runs the same journal
    // method, and flushes it back (see RunAsync). Production callers use only these.
    internal Task<bool> TickCustodyRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickCustodyRound(campaignId));

    internal Task<bool> TickCycleRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickCycleRound(campaignId));

    internal Task<bool> TickForceRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickForceRound(campaignId));

    internal Task<CampaignOwnerOpening> QueryOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryOwnerOpening(credential, actingElementId));

    internal Task<CampaignOwnerOpening> QueryPregameAxisSupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryPregameAxisSupplyOpening(credential));

    internal Task<CampaignOwnerOpening> QueryPregameAxisSupplyFinishOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryPregameAxisSupplyFinishOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAxisDumpOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAxisDumpOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceCommonwealthSupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCommonwealthSupplyOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceDummyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceDummyOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceMotorizationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceMotorizationOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceUnattachedCargoOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceUnattachedCargoOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceActionsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceActionsOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAxisGt1SupplyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAxisGt1SupplyOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceCommonwealthSupplyPlanOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCommonwealthSupplyPlanOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAutoblindaOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAutoblindaOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourcePriorityMotorizationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourcePriorityMotorizationOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAttachedInitialLoadsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAttachedInitialLoadsOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceInitialWrecksOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceInitialWrecksOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAxisFreeSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAxisFreeSetupOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAxisCorpsSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAxisCorpsSetupOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceMalettiHierarchyOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceMalettiHierarchyOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceGt2CorrectionOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceGt2CorrectionOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceCwPriorityCatalogOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCwPriorityCatalogOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceMatruhFormationOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceMatruhFormationOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceCwFreeGunsOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCwFreeGunsOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceAxisPriorityOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceAxisPriorityOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySourceCairoSetupOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCairoSetupOpening(credential));

    internal Task<CampaignOwnerOpening> QueryCombatOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryCombatOwnerOpening(credential, actingElementId));

    internal Task<CampaignOwnerOpening> QueryRbaOwnerOpeningAsync(CampaignSeatCredential credential, string defenderElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryRbaOwnerOpening(credential, defenderElementId));

    internal Task<CampaignOwnerOpening> QueryForceOwnerOpeningAsync(CampaignSeatCredential credential, string elementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryForceOwnerOpening(credential, elementId));

    internal Task<CampaignOwnerOpening> QueryPostAssaultRetreatOwnerOpeningAsync(CampaignSeatCredential credential, string defenderElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryPostAssaultRetreatOwnerOpening(credential, defenderElementId));

    internal Task<CampaignOwnerOpening> QueryCustodyOwnerOpeningAsync(CampaignSeatCredential credential, string captorElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryCustodyOwnerOpening(credential, captorElementId));

    internal Task<CampaignOwnerOpening> QueryReserveReleaseOwnerOpeningAsync(CampaignSeatCredential credential, string reserveElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryReserveReleaseOwnerOpening(credential, reserveElementId));

    internal Task<CampaignOwnerOpening> QueryCycleControlOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryCycleControlOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryInitiativeOrderOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryInitiativeOrderOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryReserveDesignationOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryReserveDesignationOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryTruckConvoyPassOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryTruckConvoyPassOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryPostCombatPassOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryPostCombatPassOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySecondReserveOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySecondReserveOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QuerySecondMovementOwnerOpeningAsync(CampaignSeatCredential credential, string elementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySecondMovementOwnerOpening(credential, elementId));

    internal Task<CampaignOwnerOpening> QuerySecondMovementCompletionOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySecondMovementCompletionOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryRepeatMovementOwnerOpeningAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryRepeatMovementOwnerOpening(credential));

    internal Task<CampaignOwnerOpening> QueryRepeatCombatOwnerOpeningAsync(CampaignSeatCredential credential, string actingElementId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryRepeatCombatOwnerOpening(credential, actingElementId));

    internal Task<bool> TickRbaRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickRbaRound(campaignId));

    internal Task<bool> TickReserveRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickReserveRound(campaignId));

    internal Task<bool> TickRetreatRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickRetreatRound(campaignId));

    internal Task CreateScheduledWorld6Async(CampaignCreationRequest request) =>
        RunAsync(request.CampaignId, true, journal => { journal.CreateScheduledWorld6(request); return true; });

    internal Task<IReadOnlyList<ScheduledWorld6Offer>> QueryScheduledWorld6Async(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryScheduledWorld6(credential));

    internal Task<byte[]?> SubmitScheduledWorld6Async(CampaignSeatCredential credential, string publicReference) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitScheduledWorld6(credential, publicReference));

    internal Task<CampaignSeatGrant> IssueSeatGrantAsync(string campaignId, CampaignActionAudience audience, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.IssueSeatGrant(campaignId, audience, operatorSecret));

    internal Task<CampaignSeatAuthority?> AuthenticateSeatAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.AuthenticateSeat(credential));

    internal Task<byte[]> QuerySourceCounterContactsAsync(CampaignSeatCredential credential, string counterId) =>
        RunAsync(credential.CampaignId, false, journal => journal.QuerySourceCounterContacts(credential, counterId));

    internal Task<bool> TickSelectionRoundAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.TickSelectionRound(campaignId));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerMovementAsync(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerMovement(credential, actingElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitPregameAxisSupplyAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitPregameAxisSupply(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitPregameAxisSupplyFinishAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitPregameAxisSupplyFinish(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceAxisDumpAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceAxisDump(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceCommonwealthSupplyAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceCommonwealthSupply(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceDummyAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceDummy(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceMotorizationAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceMotorization(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceUnattachedCargoAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceUnattachedCargo(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceActionsAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceActions(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceAxisGt1SupplyAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceAxisGt1Supply(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceCommonwealthSupplyPlanAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceCommonwealthSupplyPlan(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceAutoblindaAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceAutoblinda(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourcePriorityMotorizationAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourcePriorityMotorization(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceAttachedInitialLoadsAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceAttachedInitialLoads(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> PlaceSourceInitialWreckAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.PlaceSourceInitialWreck(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> PlaceSourceAxisFreeUnitAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.PlaceSourceAxisFreeUnit(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> PlaceSourceAxisCorpsUnitAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.PlaceSourceAxisCorpsUnit(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceMalettiHierarchyAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceMalettiHierarchy(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceGt2CorrectionAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceGt2Correction(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceCwPriorityCatalogAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceCwPriorityCatalog(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceMatruhFormationAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceMatruhFormation(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> PlaceSourceCwFreeGunAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.PlaceSourceCwFreeGun(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceAxisPriorityAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceAxisPriority(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitSourceCairoUnitAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitSourceCairoUnit(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerCombatSelectionAsync(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerCombatSelection(credential, actingElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerRbaDeclineAsync(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerRbaDecline(credential, defenderElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerForceSealAsync(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerForceSeal(credential, elementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerPostAssaultRetreatAsync(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerPostAssaultRetreat(credential, defenderElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerCustodyAsync(CampaignSeatCredential credential, string captorElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerCustody(credential, captorElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerReserveReleaseAsync(CampaignSeatCredential credential, string reserveElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerReserveRelease(credential, reserveElementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerCycleControlAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerCycleControl(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerInitiativeOrderAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerInitiativeOrder(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerReserveDesignationAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerReserveDesignation(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerTruckConvoyPassAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerTruckConvoyPass(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerPostCombatPassAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerPostCombatPass(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerSecondReserveAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerSecondReserve(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerSecondMovementAsync(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerSecondMovement(credential, elementId, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerSecondMovementCompletionAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerSecondMovementCompletion(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerRepeatMovementAsync(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerRepeatMovement(credential, openingRevision, publicActionRef));

    internal Task<CampaignOwnerSubmissionResult> SubmitOwnerRepeatCombatAsync(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        RunAsync(credential.CampaignId, false, journal => journal.SubmitOwnerRepeatCombat(credential, actingElementId, openingRevision, publicActionRef));

    internal Task<ExerciseSession> CreateCampaignAsync(CampaignCreationRequest request, CampaignDecisionTimingConfigurationV1? timing = null) =>
        RunAsync(request.CampaignId, true, journal => journal.CreateCampaign(request, timing));

    internal Task<CampaignSourceSession> CreateCampaignAsync(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        RunAsync(request.CampaignId, true, journal => journal.CreateCampaign(request, content, initialization));

    internal Task<ExerciseSession> CreatePregameSourceCampaignAsync(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        RunAsync(request.CampaignId, true, journal => journal.CreatePregameSourceCampaign(request, content, initialization));

    internal Task<ExerciseSession> ReadVerifiedAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.ReadVerified(campaignId));

    internal Task<CampaignSourceSession> ReadSourceVerifiedAsync(string campaignId) =>
        RunAsync(campaignId, false, journal => journal.ReadSourceVerified(campaignId));

    internal Task<ExerciseStepResult> AppendAsync(string campaignId, CampaignActionSubmission submission) =>
        RunAsync(campaignId, false, journal => journal.Append(campaignId, submission));

    internal Task<bool> AdmitSourceAxisSupplementAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAxisSupplement(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCommonwealthSupplementAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCommonwealthSupplement(campaignId, operatorSecret));

    internal Task<bool> AdmitSourcePriorityMotorizationAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourcePriorityMotorization(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceAttachedInitialLoadsAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAttachedInitialLoads(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceInitialWrecksAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceInitialWrecks(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceAxisFreeSetupAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAxisFreeSetup(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceAxisCorpsSetupAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAxisCorpsSetup(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceMalettiHierarchyAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceMalettiHierarchy(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceGt2CorrectionAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceGt2Correction(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCwPriorityCatalogAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCwPriorityCatalog(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCompactCheckpointAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCompactCheckpoint(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceFourthIndianWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceFourthIndianWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceMatruhGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceMatruhGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceMalettiGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceMalettiGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceLibyanGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceLibyanGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceItalianDivisionsGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceItalianDivisionsGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceSeventhArmoredGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceSeventhArmoredGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceSixteenthBrigadeGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceSixteenthBrigadeGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCatanzaroGroupedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCatanzaroGroupedWorld(campaignId, operatorSecret));

    internal Task<bool> StartSourceTurnAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.StartSourceTurn(campaignId, operatorSecret));

    internal Task<bool> ResolveSourceInitiativeAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ResolveSourceInitiative(campaignId, operatorSecret));

    internal Task<bool> CompleteSourceGt1SchedulingAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CompleteSourceGt1Scheduling(campaignId, operatorSecret));

    internal Task<bool> CompleteSourceGt1OrganizationAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CompleteSourceGt1Organization(campaignId, operatorSecret));

    internal Task<bool> CompleteSourceGt1StageOneArrivalsAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CompleteSourceGt1StageOneArrivals(campaignId, operatorSecret));

    internal Task<bool> CompleteSourceGt1StageOneFleetAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CompleteSourceGt1StageOneFleet(campaignId, operatorSecret));

    internal Task<bool> CompleteSourceGt1FirstBreakdownAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CompleteSourceGt1FirstBreakdown(campaignId, operatorSecret));

    internal Task<bool> ResolveSourceGt1WeatherAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ResolveSourceGt1Weather(campaignId, operatorSecret));

    internal Task<bool> AnnounceSourceGt1Lane2Async(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AnnounceSourceGt1Lane2(campaignId, operatorSecret));

    internal Task<bool> AnnounceSourceGt1Lane3Async(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AnnounceSourceGt1Lane3(campaignId, operatorSecret));

    internal Task<bool> CorrectSourceSeventhShellAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.CorrectSourceSeventhShell(campaignId, operatorSecret));

    internal Task<bool> ActivateSourceAxisMinorGarrisonsAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ActivateSourceAxisMinorGarrisons(campaignId, operatorSecret));

    internal Task<bool> ActivateSourceAxisGarrisonRemainderAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ActivateSourceAxisGarrisonRemainder(campaignId, operatorSecret));

    internal Task<bool> ActivateSourceGiarabubMotorMachineGunAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ActivateSourceGiarabubMotorMachineGun(campaignId, operatorSecret));

    internal Task<bool> ActivateSourceCwFrontlineAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.ActivateSourceCwFrontline(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceGiarabubMotorPlacementPolicyAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceGiarabubMotorPlacementPolicy(campaignId, operatorSecret));

    internal Task<bool> AnnounceSourceGt1MultiLanesAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AnnounceSourceGt1MultiLanes(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceMatruhFormationAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceMatruhFormation(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCwFreeGunsAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCwFreeGuns(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceAxisPriorityMotorizationAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAxisPriorityMotorization(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceAxisMinorGarrisonsAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceAxisMinorGarrisons(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceBattlefieldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceBattlefield(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCairoSetupAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCairoSetup(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceCairoWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceCairoWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceMalettiAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceMaletti(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceIndependentAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceIndependent(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceExpandedIndependentAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceExpandedIndependent(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceAxisFreeWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceAxisFreeWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceAxisCorpsWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceAxisCorpsWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceCertifiedMalettiWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceCertifiedMalettiWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceMatruhDetachedWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceMatruhDetachedWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceCwPriorityWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceCwPriorityWorld(campaignId, operatorSecret));

    internal Task<bool> PromoteSourceCwFreeGunsWorldAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.PromoteSourceCwFreeGunsWorld(campaignId, operatorSecret));

    internal Task<bool> AdmitSourceLocalMapAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitSourceLocalMap(campaignId, operatorSecret));

    internal Task<int> AdmitAvailableSourceProfilesAsync(string campaignId, string operatorSecret) =>
        RunAsync(campaignId, false, journal => journal.AdmitAvailableSourceProfiles(campaignId, operatorSecret));
}

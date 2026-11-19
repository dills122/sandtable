using System.Globalization;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Tests.Persistence;
using Cna.OrleansHost.Timeouts;
using Marten;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

namespace Cna.OrleansHost.Tests;

/// <summary>
/// Phase C3b of issue #439: the equivalence-proof-before-cutover gate for the SQLite&#8594;Marten
/// storage swap (same discipline as C3a's <c>GrainEquivalence.cs</c>). Every scenario below is
/// scripted once and run twice -- against a SQLite file journal and against the Postgres+Marten
/// backend (a real Testcontainers Postgres, no mocks), each with the same deterministic clock and
/// random source (DET-002) -- and then the two backends' complete persisted authority is compared
/// table by table: <c>campaigns</c>, every <c>accepted_steps</c> byte and hash, seats, openings and
/// their action references, the reference key, and every row of all seven round tables including
/// the write-once timing columns and high-water marks. The Marten side is re-read from Postgres
/// into a fresh working set for the comparison, so this is a durable round-trip, not an in-memory
/// one. Both C2-projection consistency (event-derived status agrees with the journal's retained
/// row) and the deadline sink's O(1) projection read are checked on every scenario too.
/// </summary>
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static readonly string[] EquivalenceTables =
    [
        "campaigns", "accepted_steps", "seats", "reference_key", "owner_openings", "owner_actions",
        "force_rounds", "rba_rounds", "cycle_rounds", "reserve_rounds", "custody_rounds", "retreat_rounds",
        "selection_rounds",
    ];

    private static void AssertBackendsEquivalent(Func<CampaignDurableAuthorityJournal, ManualClock, string> scenario)
    {
        using var fixture = new JournalFixture();
        var fileClock = new ManualClock();
        var fileRandom = new DeterministicRandom(439);
        var file = CampaignDurableAuthorityJournal.CreateNew(fixture.Path, OperatorSecret, fileClock,
            randomBytes: fileRandom.Next);
        var fileCampaign = scenario(file, fileClock);

        var martenClock = new ManualClock();
        var martenRandom = new DeterministicRandom(439);
        using var database = MartenJournalDatabase.Create(OperatorSecret, martenClock, martenRandom.Next);
        var martenCampaign = scenario(database.Journal(), martenClock);
        Assert.Equal(fileCampaign, martenCampaign);

        var fileDump = DumpTables(() =>
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = fixture.Path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
            }.ToString());
            connection.Open();
            return connection;
        });
        var workingSet = database.Backend.OpenAsync(null,
            () => throw new InvalidOperationException("Reference key was not persisted."),
            TestContext.Current.CancellationToken).GetAwaiter().GetResult();
        using (workingSet)
        {
            var martenDump = DumpTables(workingSet.Connect);
            foreach (var table in EquivalenceTables)
                Assert.True(fileDump[table].SequenceEqual(martenDump[table]),
                    $"{table} differs between SQLite and Marten:\nSQLite:\n  {string.Join("\n  ", fileDump[table])}\nMarten:\n  {string.Join("\n  ", martenDump[table])}");
            Assert.NotEmpty(martenDump["accepted_steps"]);
        }

        var fileStore = CampaignDurableAuthorityJournalStore.ForSingleCampaign(file);
        var martenStore = new CampaignDurableAuthorityJournalStore(database.Backend);
        foreach (var kind in Enum.GetValues<CampaignRoundKind>())
            Assert.Equal(fileStore.GetRoundDeadlineMs(fileCampaign, kind),
                martenStore.GetRoundDeadlineMsAsync(fileCampaign, kind, TestContext.Current.CancellationToken)
                    .GetAwaiter().GetResult());
        AssertProjectionStatusesMatchRetainedRows(database.Store, fileCampaign);
        Assert.True(CampaignExercises.Reconstruct(file.ReadVerified(fileCampaign)).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(database.Journal().ReadVerified(fileCampaign)).IsVerified);
    }

    private static Dictionary<string, List<string>> DumpTables(Func<SqliteConnection> connect)
    {
        using var connection = connect();
        var dump = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var table in EquivalenceTables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM {table}";
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
            {
                var values = new string[reader.FieldCount];
                for (var index = 0; index < reader.FieldCount; index++)
                    values[index] = reader.GetName(index) + "=" + (reader.IsDBNull(index) ? "null"
                        : reader.GetValue(index) is byte[] bytes ? Convert.ToHexStringLower(bytes)
                        : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture));
                rows.Add(string.Join("|", values));
            }
            rows.Sort(StringComparer.Ordinal);
            dump[table] = rows;
        }
        return dump;
    }

    /// <summary>C2's independent, event-derived round status must agree with the journal's retained rows.</summary>
    private static void AssertProjectionStatusesMatchRetainedRows(DocumentStore store, string campaignId)
    {
        using var query = store.QuerySession();
        void Check(string table, IReadOnlyDictionary<string, RoundEntry>? rounds,
            IReadOnlyDictionary<string, RetainedRoundRow>? retained)
        {
            rounds ??= new Dictionary<string, RoundEntry>();
            retained ??= new Dictionary<string, RetainedRoundRow>();
            Assert.Equal(retained.Keys.Order(StringComparer.Ordinal), rounds.Keys.Order(StringComparer.Ordinal));
            foreach (var (key, row) in retained)
                Assert.True(rounds[key].Status == row.Status, $"{table} {key}: C2 '{rounds[key].Status}' vs retained '{row.Status}'");
        }
        var ct = TestContext.Current.CancellationToken;
        var force = query.LoadAsync<ForceRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("force_rounds", force?.Rounds, force?.Retained);
        var rba = query.LoadAsync<RbaRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("rba_rounds", rba?.Rounds, rba?.Retained);
        var cycle = query.LoadAsync<CycleRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("cycle_rounds", cycle?.Rounds, cycle?.Retained);
        var reserve = query.LoadAsync<ReserveRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("reserve_rounds", reserve?.Rounds, reserve?.Retained);
        var custody = query.LoadAsync<CustodyRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("custody_rounds", custody?.Rounds, custody?.Retained);
        var retreat = query.LoadAsync<RetreatRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("retreat_rounds", retreat?.Rounds, retreat?.Retained);
        var selection = query.LoadAsync<SelectionRoundsDocument>(campaignId, ct).GetAwaiter().GetResult();
        Check("selection_rounds", selection?.Rounds, selection?.Retained);
    }

    // ---- Round opened, then completed normally (the common path C2 could not see timing for) ----

    [Fact]
    public void MartenEquivalenceSelectionRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, owner) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry"); // high-water advance (poll)
        Assert.True(journal.SubmitOwnerCombatSelection(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[^1]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceRbaRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, defender, defenderId) = PrepareRbaRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var opening = journal.QueryRbaOwnerOpening(defender, defenderId);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, defenderId, opening.Revision,
            opening.PublicActionRefs[^1]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceForceRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true, seed: 7);
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceRetreatRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, attacker, defender, defenderId) = PrepareRetreatRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var (captor, element, opening) = RetreatOpening(journal, attacker, defender, defenderId);
        Assert.True(journal.SubmitOwnerPostAssaultRetreat(captor, element,
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceCustodyRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, attacker, defender, defenderId) = PrepareCustodyRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var (captor, element, opening) = CustodyOpening(journal, attacker, defender, defenderId);
        Assert.True(journal.SubmitOwnerCustody(captor, element,
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceReserveRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, owner) = PrepareReserveRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var opening = journal.QueryReserveReleaseOwnerOpening(owner, "axis-infantry");
        Assert.True(journal.SubmitOwnerReserveRelease(owner, "axis-infantry",
            opening.Revision, opening.PublicActionRefs[0]).IsAccepted);
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceCycleRoundOpenThenCompleted() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, owner) = PrepareCycleRound(journal);
        clock.Advance(TimeSpan.FromSeconds(5));
        var opening = journal.QueryCycleControlOwnerOpening(owner);
        Assert.True(journal.SubmitOwnerCycleControl(owner, opening.Revision,
            opening.PublicActionRefs[^1]).IsAccepted);
        return request.CampaignId;
    });

    // ---- Deadline timeout (multi-step cascades written in one unit of work) ----

    [Fact]
    public void MartenEquivalenceSelectionRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickSelectionRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceRbaRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _, _) = PrepareRbaRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickRbaRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceForceRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _, _, _, _, _) = PrepareInfantryForceRound(journal, positive: true, seed: 8);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickForceRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceRetreatRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _, _, _) = PrepareRetreatRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickRetreatRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceCustodyRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _, _, _) = PrepareCustodyRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickCustodyRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceReserveRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _) = PrepareReserveRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickReserveRound(request.CampaignId));
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceCycleRoundTimesOut() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, _) = PrepareCycleRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.True(journal.TickCycleRound(request.CampaignId));
        return request.CampaignId;
    });

    // ---- High-water durability: a poll-time advance survives, and a later regression is caught ----

    [Fact]
    public void MartenEquivalenceSelectionClockRegressionAfterPolledHighWater() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, owner) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(10));
        _ = journal.QueryCombatOwnerOpening(owner, "axis-infantry"); // durable high-water advance, no event
        clock.Advance(TimeSpan.FromSeconds(-5));
        Assert.True(journal.TickSelectionRound(request.CampaignId)); // "clock-regression" timeout
        return request.CampaignId;
    });

    [Fact]
    public void MartenEquivalenceQueryPathTimeoutCommitsAndRejects() => AssertBackendsEquivalent((journal, clock) =>
    {
        var (request, owner) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        // The opening query's gate commits the timeout cascade, then rejects the caller.
        Assert.Throws<InvalidOperationException>(() => journal.QueryCombatOwnerOpening(owner, "axis-infantry"));
        return request.CampaignId;
    });

    // ---- Stream shape: genesis + per-event headers (regression for C1's session-scoped headers) ----

    [Fact]
    public async Task MartenStreamStartsWithGenesisAndEveryStepCarriesItsOwnHeaders()
    {
        var clock = new ManualClock();
        using var database = MartenJournalDatabase.Create(OperatorSecret, clock);
        var journal = database.Journal();
        var (request, _) = PrepareSelectionRound(journal);
        clock.Advance(TimeSpan.FromSeconds(30));
        // One unit of work appending several steps: selection timeout + no-attack + reserve completion.
        Assert.True(journal.TickSelectionRound(request.CampaignId));

        await using var query = database.Store.QuerySession();
        var events = await query.Events.FetchStreamAsync(request.CampaignId, token: TestContext.Current.CancellationToken);
        Assert.IsType<CampaignGenesisEvent>(events[0].Data);
        string? priorRecord = null;
        foreach (var @event in events)
        {
            var (sequence, priorHash, recordHash) = CampaignEventStreamSchema.ReadStepHeaders(@event);
            Assert.Equal(@event.Version - 1, sequence); // C1 amendment: version == sequence + 1
            if (priorRecord is not null) Assert.Equal(priorRecord, priorHash);
            priorRecord = recordHash;
        }
        Assert.Equal(events.Count, events.Select(e => CampaignEventStreamSchema.ReadStepHeaders(e).Sequence).Distinct().Count());
        var state = await query.LoadAsync<CampaignStateDocument>(request.CampaignId, TestContext.Current.CancellationToken);
        Assert.Equal(events.Count - 1, state!.Sequence);
    }

    // ---- Optimistic concurrency ----

    [Fact]
    public async Task MartenConcurrentRedeemOfOneGrantYieldsExactlyOneCredential()
    {
        var clock = new ManualClock();
        using var database = MartenJournalDatabase.Create(OperatorSecret, clock);
        var (request, _) = PrepareSelectionRound(database.Journal());
        var grant = database.Journal().IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth,
            OperatorSecret);
        var ct = TestContext.Current.CancellationToken;

        // Both redeemers read the unconsumed grant before either writes -- the race SQLite's
        // BEGIN IMMEDIATE serialized away. Only one seat-document write may survive.
        using var first = await database.Backend.OpenAsync([request.CampaignId], () => [], ct);
        using var second = await database.Backend.OpenAsync([request.CampaignId], () => [], ct);
        var a = CampaignDurableAuthorityJournal.ForWorkingSet(first.Connect, database.Backend.AuthenticateOperator, clock);
        var b = CampaignDurableAuthorityJournal.ForWorkingSet(second.Connect, database.Backend.AuthenticateOperator, clock);
        Assert.NotNull(a.RedeemSeatGrant(grant));
        Assert.NotNull(b.RedeemSeatGrant(grant));
        await first.FlushAsync(ct);
        var loser = await Assert.ThrowsAsync<CampaignJournalConcurrencyException>(() => second.FlushAsync(ct));
        Assert.Equal("Campaign head changed before commit.", loser.Message);

        // Through the store, the losing redeemer sees exactly SQLite's outcome: null.
        var store = new CampaignDurableAuthorityJournalStore(database.Backend);
        var again = database.Journal().IssueSeatGrant(request.CampaignId, CampaignActionAudience.Commonwealth,
            OperatorSecret);
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => store.RedeemSeatGrantAsync(again)));
        Assert.Single(results, credential => credential is not null);
    }

    [Fact]
    public async Task MartenConcurrentTimeoutTicksCommitExactlyOneTimeout()
    {
        var clock = new ManualClock();
        using var database = MartenJournalDatabase.Create(OperatorSecret, clock);
        var (request, _) = PrepareSelectionRound(database.Journal());
        clock.Advance(TimeSpan.FromSeconds(30));
        var ct = TestContext.Current.CancellationToken;
        using var first = await database.Backend.OpenAsync([request.CampaignId], () => [], ct);
        using var second = await database.Backend.OpenAsync([request.CampaignId], () => [], ct);
        Assert.True(CampaignDurableAuthorityJournal.ForWorkingSet(first.Connect, database.Backend.AuthenticateOperator, clock)
            .TickSelectionRound(request.CampaignId));
        Assert.True(CampaignDurableAuthorityJournal.ForWorkingSet(second.Connect, database.Backend.AuthenticateOperator, clock)
            .TickSelectionRound(request.CampaignId));
        await first.FlushAsync(ct);
        await Assert.ThrowsAsync<CampaignJournalConcurrencyException>(() => second.FlushAsync(ct));
        Assert.True(CampaignExercises.Reconstruct(database.Journal().ReadVerified(request.CampaignId)).IsVerified);
        Assert.Null(database.Journal().GetSelectionRoundDeadlineMs(request.CampaignId));
    }

    // ---- Grain path against the Marten store ----

    [Fact]
    public async Task MartenGrainTickMatchesSqliteSweepOutcome()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var fileJournal = CampaignDurableAuthorityJournal.CreateNew(fixtureA.Path, OperatorSecret, clockA);
        var (requestA, _) = PrepareSelectionRound(fileJournal);
        clockA.Advance(TimeSpan.FromSeconds(30));
        new CampaignCombatTimeoutService(CampaignDurableAuthorityJournalStore.ForSingleCampaign(fileJournal),
            clockA, Microsoft.Extensions.Logging.Abstractions.NullLogger<CampaignCombatTimeoutService>.Instance)
            .SweepOnce(CancellationToken.None);
        var swept = CampaignExercises.QueryCheckpoint(fileJournal.ReadVerified(requestA.CampaignId)).ContractVersion;

        var clockB = new ManualClock();
        using var database = MartenJournalDatabase.Create(OperatorSecret, clockB);
        var (requestB, _) = PrepareSelectionRound(database.Journal());
        clockB.Advance(TimeSpan.FromSeconds(30));
        var host = await BuildCampaignGrainHostAsync(new CampaignDurableAuthorityJournalStore(database.Backend), clockB);
        try
        {
            Assert.True(await host.Services.GetRequiredService<IGrainFactory>()
                .GetGrain<ICampaignGrain>(requestB.CampaignId).TickRoundAsync(CampaignRoundKind.Selection));
        }
        finally { await StopCampaignGrainHostAsync(host); }
        var ticked = CampaignExercises.QueryCheckpoint(database.Journal().ReadVerified(requestB.CampaignId)).ContractVersion;
        Assert.Equal(swept, ticked);
    }

    /// <summary>
    /// Opening queries now run behind <c>CampaignGrain</c>: a query whose gate times the round out
    /// commits that timeout durably (Marten), rejects the caller, and disarms the round's timeout
    /// grain -- the close is no longer invisible to deadline enforcement.
    /// </summary>
    [Fact]
    public async Task MartenOpeningQueryThroughGrainTimesOutRoundAndDisarmsItsTimer()
    {
        var clock = new ManualClock();
        using var database = MartenJournalDatabase.Create(OperatorSecret, clock);
        var (request, owner) = PrepareSelectionRound(database.Journal());
        var deadline = database.Journal().GetSelectionRoundDeadlineMs(request.CampaignId)!.Value;
        var host = await BuildCampaignGrainHostAsync(new CampaignDurableAuthorityJournalStore(database.Backend), clock);
        try
        {
            var grains = host.Services.GetRequiredService<IGrainFactory>();
            var timer = grains.GetGrain<ICampaignRoundTimeoutGrain>(
                CampaignRoundTimeoutGrainKey.For(request.CampaignId, CampaignRoundKind.Selection));
            await timer.ArmAsync(DateTimeOffset.FromUnixTimeMilliseconds(deadline));
            var fresh = await grains.GetGrain<ICampaignGrain>(request.CampaignId)
                .QueryOwnerOpeningAsync("combat-selection", owner, "axis-infantry");
            Assert.Equal(request.CampaignId, fresh.CampaignId);

            clock.Advance(TimeSpan.FromSeconds(30));
            await Assert.ThrowsAsync<InvalidOperationException>(() => grains.GetGrain<ICampaignGrain>(request.CampaignId)
                .QueryOwnerOpeningAsync("combat-selection", owner, "axis-infantry"));
            Assert.Null(await timer.ArmedDeadlineForTestAsync());
        }
        finally { await StopCampaignGrainHostAsync(host); }
        Assert.Null(database.Journal().GetSelectionRoundDeadlineMs(request.CampaignId));
        Assert.True(CampaignExercises.Reconstruct(database.Journal().ReadVerified(request.CampaignId)).IsVerified);
    }
}

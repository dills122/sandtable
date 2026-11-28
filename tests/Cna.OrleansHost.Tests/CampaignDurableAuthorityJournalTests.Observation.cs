using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Tests.Persistence;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static async Task<(HttpStatusCode Status, string Body)> ReadObservation(
        OwnerHttpHarness outward, CampaignSeatCredential seat, bool sameOrigin = true)
    {
        using var client = new HttpClient { BaseAddress = outward.Address };
        if (sameOrigin) client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        using var response = await client.PostAsJsonAsync($"/api/v2/campaigns/{seat.CampaignId}/observation",
            new
            {
                contractVersion = 2,
                credential = new
                {
                    campaignId = seat.CampaignId,
                    audience = seat.Audience == CampaignActionAudience.Axis ? "axis" : "commonwealth",
                    epoch = seat.Epoch,
                    secret = seat.Secret,
                    expiresUnixMs = seat.ExpiresUnixMs,
                },
            }, TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ObservationIsSeatScopedRepeatableAndReadOnly()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var cw = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);
        var before = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
        var tables = new[] { "campaigns", "accepted_steps", "owner_openings", "owner_actions", "seats",
            "selection_rounds", "force_rounds", "rba_rounds", "retreat_rounds", "custody_rounds",
            "reserve_rounds", "cycle_rounds" };
        var beforeRows = JournalFixture.UsesMarten ? null
            : tables.Select(table => IntegratedCampaignRows(fixture.Path, table)).ToArray();
        var discovery = await outward.OpenForSeatResponse(axis);
        var (status, body) = await ReadObservation(outward, axis);
        Assert.Equal(HttpStatusCode.OK, status);
        using var observation = JsonDocument.Parse(body);
        var root = observation.RootElement;
        Assert.Equal("axis", root.GetProperty("audience").GetString());
        Assert.Equal(1, root.GetProperty("position").GetProperty("gameTurn").GetInt32());
        Assert.NotEmpty(root.GetProperty("visibleLocations").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("visibleEdges").EnumerateArray());
        Assert.Contains("axis-infantry", body);
        Assert.DoesNotContain("cw-infantry", body);
        Assert.DoesNotContain("commonwealth-infantry", body);
        Assert.DoesNotContain("commonwealth-truck", body);
        Assert.DoesNotContain("\"currentLocationId\":\"north-east\"", body);
        Assert.DoesNotContain("\"currentLocationId\":\"east\"", body);
        var other = await ReadObservation(outward, cw);
        Assert.Equal(HttpStatusCode.OK, other.Status);
        Assert.DoesNotContain("axis-infantry", other.Body);
        using var otherObservation = JsonDocument.Parse(other.Body);
        Assert.NotEqual(root.GetProperty("observationRevision").GetString(),
            otherObservation.RootElement.GetProperty("observationRevision").GetString());

        var elapsed = new List<double>();
        for (var i = 0; i < 20; i++)
        {
            var start = Stopwatch.GetTimestamp();
            Assert.Equal((status, body), await ReadObservation(outward, axis));
            elapsed.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        elapsed.Sort();
        TestContext.Current.TestOutputHelper!.WriteLine(
            $"Observation {System.Text.Encoding.UTF8.GetByteCount(body)} bytes; p50={elapsed[9]:F2}ms p95={elapsed[18]:F2}ms");
        Assert.Equal(discovery, await outward.OpenForSeatResponse(axis));
        if (beforeRows is not null)
            for (var i = 0; i < tables.Length; i++)
                Assert.Equal(beforeRows[i], IntegratedCampaignRows(fixture.Path, tables[i]));
        var after = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
        Assert.Equal(before.Length, after.Length);
        for (var i = 0; i < before.Length; i++) Assert.Equal(before[i], after[i]);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ReadObservation(outward, axis with { Secret = cw.Secret })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await ReadObservation(outward, axis, sameOrigin: false)).Status);
        clock.Advance(TimeSpan.FromHours(3));
        Assert.Equal(HttpStatusCode.Unauthorized, (await ReadObservation(outward, axis)).Status);
    }

    [Fact]
    public async Task ObservationReadsOneRevisionAcrossConcurrentOwnerWrite()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = CombatInfantryRequest();
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var cw = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);
        var holder = ParseOpen((await outward.OpenForSeatResponse(axis)).Body).Count > 0 ? axis : cw;
        var opening = await QueryOpeningForOpenSeatTest(outward, holder, "land.initiative-order", "initiative-order");
        var before = await ReadObservation(outward, holder);
        Assert.Equal(HttpStatusCode.OK, before.Status);
        var reads = Enumerable.Range(0, 8).Select(_ => ReadObservation(outward, holder)).ToArray();
        var write = outward.SubmitResponse(holder, "land.initiative-order", "initiative-order",
            opening.Revision, opening.Refs[0]);
        Assert.Equal(HttpStatusCode.OK, (await write).Status);
        var after = await ReadObservation(outward, holder);
        Assert.Equal(HttpStatusCode.OK, after.Status);
        Assert.NotEqual(before.Body, after.Body);
        foreach (var read in await Task.WhenAll(reads))
            Assert.True(read == before || read == after, $"Mixed observation: {read}");
    }

    [Fact]
    public async Task ObservationTracksCommittedOwnPlacement()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = ContactRequest();
        var session = AdvanceToContactMovement(journal, request);
        var axis = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Axis, OperatorSecret))!;
        var cw = journal.RedeemSeatGrant(journal.IssueSeatGrant(
            request.CampaignId, CampaignActionAudience.Commonwealth, OperatorSecret))!;
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        var set = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!;
        var move = set.Candidates.OfType<MoveElementAction>().First(value => value.ElementId == "axis-infantry");
        async Task<string> Location()
        {
            var response = await ReadObservation(outward, axis);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            using var doc = JsonDocument.Parse(response.Body);
            return doc.RootElement.GetProperty("ownElements").EnumerateArray()
                .Single(value => value.GetProperty("elementId").GetString() == move.ElementId)
                .GetProperty("currentLocationId").GetString()!;
        }
        Assert.Equal(move.OriginLocationId, await Location());
        Assert.True(journal.Append(request.CampaignId, new CampaignActionSubmission(1,
            set.CampaignId, set.StateVersion, set.PositionId, set.Audience, move.ActionId)).IsAccepted);
        Assert.Equal(move.DestinationLocationId, await Location());
        var opposing = await ReadObservation(outward, cw);
        Assert.Equal(HttpStatusCode.OK, opposing.Status);
        Assert.DoesNotContain(move.ElementId, opposing.Body);
    }

    [Fact]
    public async Task ObservationLargestAvailableSyntheticFixture()
    {
        using var fixture = new JournalFixture();
        var journal = fixture.Create();
        var request = PositiveTripleContactRequest(12345);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var baseline = await ReadObservation(outward, axis);
        Assert.Equal(HttpStatusCode.OK, baseline.Status);
        var durations = new List<double>();
        for (var i = 0; i < 30; i++)
        {
            var start = Stopwatch.GetTimestamp();
            Assert.Equal(baseline, await ReadObservation(outward, axis));
            durations.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        durations.Sort();
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Eight-element fixture: {System.Text.Encoding.UTF8.GetByteCount(baseline.Body)} bytes; p50={durations[14]:F2}ms p95={durations[28]:F2}ms");
    }

    [Fact]
    public async Task ObservationDeadlineIsOwnerScopedAndExpiresWithoutWriting()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, axis, cw) = AdvanceToSecondCombatSelection(journal);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        var deadline = journal.GetSelectionRoundDeadlineMs(request.CampaignId);
        Assert.NotNull(deadline);
        var events = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
        _ = journal.DrainPendingRoundTimerActions();

        async Task<long?> Deadline(CampaignSeatCredential seat)
        {
            var response = await ReadObservation(outward, seat);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            using var doc = JsonDocument.Parse(response.Body);
            var value = doc.RootElement.GetProperty("deadline");
            return value.ValueKind == JsonValueKind.Null ? null : value.GetInt64();
        }

        Assert.Equal(deadline, await Deadline(cw));
        Assert.Null(await Deadline(axis));
        clock.Advance(TimeSpan.FromMilliseconds(deadline.Value - clock.GetUtcNow().ToUnixTimeMilliseconds() + 1));
        Assert.Null(await Deadline(cw));
        Assert.Empty(journal.DrainPendingRoundTimerActions());
        Assert.Equal(deadline, journal.GetSelectionRoundDeadlineMs(request.CampaignId));
        var after = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
        Assert.Equal(events.Length, after.Length);
        for (var i = 0; i < events.Length; i++) Assert.Equal(events[i], after[i]);
    }

    [Fact]
    public async Task MartenStoreObservationReadsCoherentCutWithoutFlush()
    {
        using var database = MartenJournalDatabase.Create(OperatorSecret);
        var store = new CampaignDurableAuthorityJournalStore(database.Backend);
        var request = CombatInfantryRequest();
        var token = TestContext.Current.CancellationToken;
        await store.CreateCampaignAsync(request, Timing(request));
        async Task<CampaignSeatCredential> Seat(CampaignActionAudience audience) =>
            (await store.RedeemSeatGrantAsync(await store.IssueSeatGrantAsync(
                request.CampaignId, audience, OperatorSecret)))!;
        var axis = await Seat(CampaignActionAudience.Axis);
        var cw = await Seat(CampaignActionAudience.Commonwealth);
        var holder = (await store.QueryOpenForSeatAsync(axis)).Open.Count > 0 ? axis : cw;
        var opening = await store.QueryInitiativeOrderOwnerOpeningAsync(holder);
        async Task<string> Read()
        {
            var (audience, view, deadline) = await store.QueryObservationAsync(holder, token);
            return JsonSerializer.Serialize(new { audience, view, deadline });
        }
        var before = await Read();
        var reads = Enumerable.Range(0, 8).Select(_ => Read()).ToArray();
        Assert.True((await store.SubmitOwnerInitiativeOrderAsync(holder,
            opening.Revision, opening.PublicActionRefs[0])).IsAccepted);
        var after = await Read();
        Assert.NotEqual(before, after);
        foreach (var read in await Task.WhenAll(reads)) Assert.True(read == before || read == after);
        await using var query = database.Store.QuerySession();
        var events = await query.Events.FetchStreamAsync(request.CampaignId, token: token);
        Assert.Equal(after, await Read());
        Assert.Equal(events.Count, (await query.Events.FetchStreamAsync(request.CampaignId, token: token)).Count);
        Assert.Empty(store.DrainPendingRoundTimerActions(request.CampaignId));
    }

    [Fact]
    public Task ObservationFollowsStageAndTurnBoundary() =>
        RunStagesHttp(includeStageThree: true, boundaryTurn: 1,
            observationCheck: async (outward, seat, committed, expectedTurn) =>
            {
                var observation = await ReadObservation(outward, seat);
                Assert.Equal(HttpStatusCode.OK, observation.Status);
                using var json = JsonDocument.Parse(observation.Body);
                var root = json.RootElement;
                var position = root.GetProperty("position");
                Assert.Equal(committed.PositionId, position.GetProperty("positionId").GetString());
                Assert.True(root.GetProperty("worldAvailable").GetBoolean());
                Assert.NotEmpty(root.GetProperty("ownElements").EnumerateArray());
                var expectedStage = committed.PositionId.Contains("operation-3", StringComparison.Ordinal) ? 3
                    : committed.PositionId.Contains("operation-2", StringComparison.Ordinal) ? 2
                    : committed.PositionId.Contains("operation-1", StringComparison.Ordinal) ? 1 : 0;
                Assert.Equal(expectedStage, position.GetProperty("operationStage").GetInt32());
                Assert.Equal(expectedTurn, position.GetProperty("gameTurn").GetInt32());
                if (committed.PositionId == "land.position.end-of-turn")
                    Assert.Equal(JsonValueKind.Null, root.GetProperty("deadline").ValueKind);
            });
}

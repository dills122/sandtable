using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatHistoryReplayTests
{
    internal static IEnumerable<(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events)> SnapshotHistories()
    {
        foreach (var values in Cases())
        {
            var trace = Trace((ulong)values[0], (string)values[1], (bool)values[2]);
            for (var cut = 0; cut <= (int)values[3]; cut++)
                yield return (trace.Request, trace.Created.ToArray(), trace.Events.Take(cut).Select(bytes => bytes.ToArray()).ToArray());
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (ulong seed in new ulong[] { 0, 1, 2, 3, 9, 16, 18, 33, 70, 74, 80, 81, 97, 141, 183, 12345, ulong.MaxValue })
            foreach (var choice in new[] { "act-first", "act-last" })
            {
                yield return [seed, choice, false, seed <= 3 ? 10 : seed is 81 or ulong.MaxValue ? 9 : 5];
                if (seed <= 3) yield return [seed, choice, true, 11];
            }
    }

    [Fact]
    public void SelectedCasesCoverEveryFrozenPrecycleHistoryIdentity()
    {
        using var fixture = Fixture("combat-inherited-snapshot-v1.json");
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in fixture.RootElement.GetProperty("roots").EnumerateArray())
        {
            using var document = JsonDocument.Parse(row.GetProperty("canonicalJson").GetString()!);
            var root = document.RootElement;
            var cycle = root.GetProperty("cycleState");
            if (cycle.ValueKind == JsonValueKind.Null || cycle.GetProperty("kind").GetString() == "reserve-designation" ||
                cycle.GetProperty("kind").GetString() == "inherited-cycle" &&
                cycle.GetProperty("cycle").GetProperty("openedAuthorityVersion").GetInt64() == root.GetProperty("stateVersion").GetInt64())
                expected.Add(HistoryIdentity(row.GetProperty("creationHash").GetString()!,
                    row.GetProperty("eventHashes").EnumerateArray().Select(hash => hash.GetString()!)));
        }
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var values in Cases())
        {
            var trace = Trace((ulong)values[0], (string)values[1], (bool)values[2]);
            for (var cut = 0; cut <= (int)values[3]; cut++)
                actual.Add(HistoryIdentity(Digest(trace.Created), trace.Events.Take(cut).Select(Digest)));
        }
        // Cases() also drives actual router replay and H0 projection comparisons above.
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    private static string HistoryIdentity(string creationHash, IEnumerable<string> eventHashes) =>
        creationHash + ":" + string.Join(",", eventHashes);

    [Theory]
    [MemberData(nameof(Cases))]
    public void EverySelectedPrefixMatchesFrozenH0HistoryAndActualCausalEvidence(ulong seed, string choice, bool designate, int terminal)
    {
        var trace = Trace(seed, choice, designate);
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-inherited-snapshot-v1.json")));
        for (var cut = 0; cut <= terminal; cut++)
        {
            var history = trace.Events.Take(cut).ToArray();
            var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, history);
            var hashes = history.Select(Digest).ToArray();
            var matches = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(row =>
                row.GetProperty("creationHash").GetString() == Digest(trace.Created) &&
                row.GetProperty("eventHashes").EnumerateArray().Select(x => x.GetString()).SequenceEqual(hashes)).ToArray();
            Assert.NotEmpty(matches);
            var root = JsonNode.Parse(matches[0].GetProperty("canonicalJson").GetString()!)!;
            var actual = JsonNode.Parse(Serialize(result.Projection))!;
            Assert.Equal(cut + 1, result.Projection.StateVersion);
            Assert.Equal(root["chroniclePrefix"]!.GetValue<string>(), result.Projection.Prefix);
            Assert.Equal(root["currentPosition"]!["sequencePosition"]!["positionId"]!.GetValue<string>(), result.Projection.SequencePosition.PositionId);
            Assert.Equal(cut, result.Projection.Receipts.Count);
            Assert.True(JsonNode.DeepEquals(root["commandReceipts"], actual["receipts"]));
            Assert.True(JsonNode.DeepEquals(root["currentPosition"]!["sequencePosition"], actual["sequencePosition"]));
            foreach (var field in new[] { "campaignId", "rulesetHash", "stateVersion", "initiativeHolder", "operationStageOrders", "operationStageWeather", "randomState", "world" })
                if (actual.AsObject().ContainsKey(field)) Assert.True(JsonNode.DeepEquals(root[field], actual[field]), field);
            Assert.Equal(trace.Created, result.History.Created);
            Assert.Equal(hashes, result.History.Events.Select(Digest));
            if (cut >= 9)
            {
                var reserve = Assert.IsType<CampaignCombatHistoryProjection.ReserveOpening>(result.Projection).State;
                Assert.NotEmpty(reserve.Predecessor.Members);
                foreach (var field in new[] { "firstActingSide", "members" })
                    Assert.True(JsonNode.DeepEquals(root["cycleState"]![field], actual[field]), field);
                Assert.Equal(cut > (designate ? 10 : 9), reserve.Cycle is not null);
                if (reserve.Cycle is not null)
                    foreach (var field in new[] { "cycle", "cycleId", "openingBaseHash", "completionReceiptId", "sequencePosition" })
                        Assert.True(JsonNode.DeepEquals(root["cycleState"]![field], actual[field]), field);
            }
            // The legacy creation-only reader must remain creation-only.
            var rootBytes = Encoding.UTF8.GetBytes(root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
            if (cut > 0) Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(rootBytes, trace.Created, trace.Request));
        }
    }

    [Theory]
    [InlineData("act-first", false)]
    [InlineData("act-first", true)]
    [InlineData("act-last", false)]
    [InlineData("act-last", true)]
    public void ActualTypedProjectionsPreservePredecessorGoldens(string choice, bool designate)
    {
        var trace = Trace(choice: choice, designate: designate);
        using var preamble = Fixture("combat-opening-preamble-v1.json");
        using var weather = Fixture("combat-weather-v1.json");
        using var stage = Fixture("combat-stage-entry-v1.json");
        using var reserve = Fixture("combat-reserve-designation-v1.json");
        var preambleGold = preamble.RootElement.GetProperty("cases").EnumerateArray().Single(row =>
            row.GetProperty("seed").GetUInt64() == 0 && row.GetProperty("choice").GetString() == choice).GetProperty("goldens");
        var weatherGold = weather.RootElement.GetProperty("cases")[0].GetProperty("goldens").GetProperty(choice);
        var stageGold = stage.RootElement.GetProperty("cases")[0].GetProperty("goldens").GetProperty(choice);
        var reserveGold = reserve.RootElement.GetProperty("cases").EnumerateArray().Single(row =>
            row.GetProperty("seed").GetUInt64() == 0 && row.GetProperty("choice").GetString() == choice &&
            row.GetProperty("reserve").GetString() == (designate ? "I" : "none")).GetProperty("goldens");
        CheckGolden(preambleGold, "creation", trace.Created);
        for (var cut = 0; cut <= trace.Events.Length; cut++)
        {
            var projection = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, trace.Events.Take(cut).ToArray()).Projection;
            if (cut <= 4)
            {
                CheckGolden(preambleGold, $"state-{cut}", Serialize(projection));
                if (cut > 0) CheckGolden(preambleGold, $"event-{cut}", trace.Events[cut - 1]);
            }
            else if (cut == 5)
            {
                CheckGolden(weatherGold, "after", Serialize(projection));
                CheckGolden(weatherGold, "weather", trace.Events[4]);
            }
            else if (cut < 9)
            {
                CheckGolden(stageGold, $"state-{cut + 1}", Serialize(projection));
                CheckGolden(stageGold, $"event-{cut - 5}", trace.Events[cut - 1]);
            }
            else
            {
                var state = Assert.IsType<CampaignCombatHistoryProjection.ReserveOpening>(projection).State;
                CheckGolden(stageGold, "state-10", CampaignCombatStageEntryCodec.SerializeState(state.Predecessor.Stage));
                CheckGolden(stageGold, "event-4", trace.Events[8]);
                CheckGolden(reserveGold, $"state-{cut + 1}", Serialize(projection));
                if (cut > 9) CheckGolden(reserveGold, $"event-{cut - 9}", trace.Events[cut - 1]);
            }
        }
    }

    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static void CheckGolden(JsonElement goldens, string key, byte[] bytes)
    {
        Assert.Equal(goldens.GetProperty(key).GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(goldens.GetProperty(key).GetProperty("sha256").GetString(), Digest(bytes));
    }

    [Fact]
    public void MissingRetainedCreationRejectsWithoutGenerationFallback()
    {
        var trace = Trace();
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, bytes, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatRetainedHistory.Capture([], []));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatHistoryReplay.Replay(null!, trace.Created, []));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, null!));
    }

    [Fact]
    public void EveryOccurrenceRejectsOmissionDuplicationReorderWrongSuccessorAndNoncanonicalBytes()
    {
        var trace = Trace(designate: true);
        for (var index = 0; index < trace.Events.Length; index++)
        {
            if (index < trace.Events.Length - 1)
            {
                Reject(trace, trace.Events.Where((_, i) => i != index).ToArray());
                var reordered = trace.Events.ToArray();
                (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
                Reject(trace, reordered);
            }
            Reject(trace, trace.Events.Take(index).Concat([trace.Events[index], trace.Events[index]]).ToArray());
            foreach (var changed in Mutations(trace.Events[index]))
                Reject(trace, trace.Events.Take(index).Append(changed).ToArray());
        }
        var opposite = Trace(choice: "act-last", designate: true);
        Reject(trace, trace.Events.Take(9).Concat(opposite.Events.Skip(9)).ToArray());
        var foreign = Trace(seed: 1, designate: true);
        Reject(trace, trace.Events.Take(4).Concat(foreign.Events.Skip(4)).ToArray());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(foreign.Request, trace.Created, trace.Events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, foreign.Created, trace.Events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, [.. trace.Created, (byte)' '], trace.Events));
    }

    [Fact]
    public void CanonicallyResignedForgedWeatherStillRejectsCausalRngMismatch()
    {
        var trace = Trace();
        var weather = CampaignCombatWeather.Replay(trace.Request, trace.Created, trace.Events.Take(4).ToArray(), [trace.Events[4]]);
        var accepted = weather.AcceptedEvent!;
        var forged = CampaignCombatWeatherCodec.SerializeEvent(accepted with { RandomState = accepted.Before.RandomState });
        Assert.NotEqual(trace.Events[4], forged);
        Reject(trace, trace.Events.Take(4).Append(forged).ToArray());
    }

    [Fact]
    public void ValidOrdinaryMovementTailNowReplaysWithoutTruncation()
    {
        var trace = Trace(seed: 1);
        var opening = trace.Events.Take(4).ToArray();
        var stage = trace.Events.Skip(5).Take(4).ToArray();
        var reserve = trace.Events.Skip(9).ToArray();
        var state = CampaignCombatInheritedMovement.Replay(trace.Request, trace.Created, opening, [trace.Events[4]], stage, reserve, []);
        var input = CampaignCombatInheritedMovement.Command(state, "axis-rear");
        var next = CampaignCombatInheritedMovement.Apply(trace.Request, trace.Created, opening, [trace.Events[4]], stage, reserve, [], input);
        Assert.Equal(state.StateVersion + 1, next.State.StateVersion);
        var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, trace.Events.Append(next.EventBytes).ToArray());
        Assert.Equal(next.State.StateVersion, result.Projection.StateVersion);
        Assert.Equal(next.State.Prefix, result.Projection.Prefix);
    }

    [Fact]
    public void CapacityChecksPrecedeIndexingAndCopyingAndEnforceExactBounds()
    {
        Assert.ThrowsAny<JsonException>(() => CampaignCombatRetainedHistory.Capture([1], new CountOnlyList(513)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatRetainedHistory.Capture([1], new CountOnlyList(-1)));
        Assert.Equal(512, CampaignCombatRetainedHistory.Capture([1], Enumerable.Repeat(new byte[] { 1 }, 512).ToArray()).Count);
        var max = new byte[1_048_576];
        Assert.Equal(16, CampaignCombatRetainedHistory.Capture(max, Enumerable.Repeat(max, 16).ToArray()).Count);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatRetainedHistory.Capture([1], Enumerable.Repeat(max, 16).Append(new byte[] { 1 }).ToArray()));
        foreach (var invalid in new byte[][] { [], new byte[1_048_577], null! })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatRetainedHistory.Capture([1], [invalid]));
    }

    [Fact]
    public void CallerAndReturnedBuffersCannotMutateTranscriptOrTypedProjection()
    {
        var trace = Trace(designate: true);
        var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, trace.Events);
        var projection = Serialize(result.Projection);
        var created = result.History.Created;
        var events = result.History.Events.Select(bytes => bytes.ToArray()).ToArray();
        foreach (var bytes in trace.Events.Append(trace.Created).Concat(result.History.Events).Append(result.History.Created))
            Array.Fill(bytes, (byte)0);
        Assert.Equal(created, result.History.Created);
        Assert.Equal(events.Select(Digest), result.History.Events.Select(Digest));
        Assert.Equal(projection, Serialize(result.Projection));
        var replay = CampaignCombatHistoryReplay.Replay(trace.Request, result.History.Created, result.History.Events);
        Assert.Equal(projection, Serialize(replay.Projection));
    }

    private sealed class CountOnlyList(int count) : IReadOnlyList<byte[]>
    {
        public int Count => count;
        public byte[] this[int index] => throw new InvalidOperationException("Indexing forbidden before count rejection.");
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException("Enumeration forbidden.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static IEnumerable<byte[]> Mutations(byte[] bytes)
    {
        yield return [.. bytes, (byte)' '];
        var root = JsonNode.Parse(bytes)!.AsObject();
        root["contractVersion"] = 999;
        yield return Encoding.UTF8.GetBytes(root.ToJsonString());
        root = JsonNode.Parse(bytes)!.AsObject();
        root["stateVersion"] = 999;
        yield return Encoding.UTF8.GetBytes(root.ToJsonString());
        root = JsonNode.Parse(bytes)!.AsObject();
        root["eventType"] = "unknown";
        yield return Encoding.UTF8.GetBytes(root.ToJsonString());
    }
    private static void Reject(TraceData trace, byte[][] events) =>
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events));
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static byte[] Serialize(CampaignCombatHistoryProjection projection) => projection switch
    {
        CampaignCombatHistoryProjection.Preamble p => CampaignOpeningPreambleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.Weather p => CampaignCombatWeatherCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.StageEntry p => CampaignCombatStageEntryCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReserveOpening p => CampaignCombatReserveOpeningCodec.SerializeState(p.State),
        _ => throw new InvalidOperationException(),
    };
    private sealed record TraceData(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events);
    private static TraceData Trace(ulong seed = 0, string choice = "act-first", bool designate = false)
    {
        var (request, created, opening, weather, stage) = Chain(seed, choice);
        var reserve = new List<byte[]>();
        if (designate)
        {
            var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
            reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state)).EventBytes);
        }
        var completion = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, reserve, completion).EventBytes);
        return new(request, created, opening.Append(weather).Concat(stage).Concat(reserve).ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 0, string choice = "act-first")
    {
        var (request, created, opening, weather) = Predecessor(seed, choice);
        var stage = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], stage);
            stage.Add(CampaignCombatStageEntry.Apply(request, created, opening, [weather], stage, CampaignCombatStageEntry.Command(state)).EventBytes);
        }
        return (request, created, opening, weather, stage.ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather) Predecessor(
        ulong seed = 0, string choice = "act-first")
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", seed,
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        var opening = new List<byte[]>();
        for (var index = 0; index < 4; index++)
        {
            var state = CampaignOpeningPreamble.Replay(request, created, opening);
            var input = CampaignOpeningPreamble.Command(state, index == 3
                ? choice == "act-first" ? InitiativeOrderChoice.ActFirst : InitiativeOrderChoice.ActLast : null);
            opening.Add(CampaignOpeningPreamble.Apply(request, created, opening, input).EventBytes);
        }
        var weatherState = CampaignCombatWeather.Replay(request, created, opening, []);
        var weather = CampaignCombatWeather.Apply(request, created, opening, [], CampaignCombatWeather.Command(weatherState)).EventBytes;
        return (request, created, opening.ToArray(), weather);
    }


}

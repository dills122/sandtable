using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatWeatherTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var value in fixture.RootElement.GetProperty("cases").EnumerateArray())
            foreach (var choice in new[] { "act-first", "act-last" })
                yield return [value.GetProperty("seed").GetUInt64(), choice];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FrozenChainsMatchAllBytesOutcomesReceiptsAndBothCuts(ulong seed, string choice)
    {
        using var fixture = Fixture();
        var test = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("seed").GetUInt64() == seed);
        var goldens = test.GetProperty("goldens").GetProperty(choice);
        var expected = test.GetProperty("expected");
        var (request, created, opening) = Opening(seed, choice);
        CheckGolden(goldens, "creation", created);
        for (var index = 0; index < 4; index++) CheckGolden(goldens, $"preamble-{index + 1}", opening[index]);
        var before = CampaignCombatWeather.Replay(request, created, opening, []);
        var input = CampaignCombatWeather.Command(before);
        var inputBytes = CampaignCombatWeatherCodec.SerializeInput(input);
        CheckGolden(goldens, "input", inputBytes);
        Assert.Equal(input, CampaignCombatWeatherCodec.DeserializeInput(inputBytes));
        var result = CampaignCombatWeather.Apply(request, created, opening, [], input);
        Assert.False(result.Duplicate);
        CheckGolden(goldens, "weather", result.EventBytes);
        var after = result.State;
        var weather = Assert.Single(after.Weather);
        Assert.Equal(expected.GetProperty("firstDie").GetInt32(), weather.FirstDie);
        Assert.Equal(expected.GetProperty("secondDie").GetInt32(), weather.SecondDie);
        Assert.Equal(expected.GetProperty("kind").GetString(), CampaignOperationStageWeatherCodec.FormatKind(weather.Kind));
        Assert.Equal(expected.GetProperty("scope").GetString(), CampaignOperationStageWeatherCodec.FormatScope(weather.Scope));
        Assert.Equal(expected.GetProperty("locationDie").ValueKind == JsonValueKind.Null ? (int?)null : expected.GetProperty("locationDie").GetInt32(), weather.LocationDie);
        Assert.Equal(expected.GetProperty("affectedAreas").EnumerateArray().Select(v => v.GetString()),
            weather.AffectedAreas.Select(CampaignOperationStageWeatherCodec.FormatArea));
        Assert.Equal(expected.GetProperty("cursor").GetUInt64(), after.RandomState.NextByteCursor);
        Assert.Equal(WeatherSeason.Fall, weather.Season);
        Assert.Equal(LandSide.Axis, weather.DeterminingSide);
        Assert.Equal(0, weather.FuelWaterReductionSubjectCount);
        Assert.Equal(0, weather.RestoredWellCount);
        Assert.Equal(0, weather.DamagedGroundedAircraftCount);
        Assert.Equal(before.Opening.Orders, after.Opening.Orders);
        Assert.Equal(WorldBytes(before), WorldBytes(after));
        Assert.Equal("land.position.operation-1.organization", after.SequencePosition.PositionId);
        Assert.Null(after.SequencePosition.ActiveSide);
        Assert.Equal(5, after.Receipts.Count);
        Assert.Equal(before.Receipts, after.Receipts.Take(4));
        Assert.Equal(Digest(inputBytes), after.Receipts[^1].CommandHash);
        Assert.Equal(Digest(result.EventBytes), after.Receipts[^1].EventHash);
        Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), after.Receipts[^1].ReceiptId);
        Assert.Equal(Prefix(before.Prefix, result.EventBytes), after.Prefix);
        foreach (var (state, suffix, key) in new[]
        {
            (before, Array.Empty<byte[]>(), "before"),
            (after, new[] { result.EventBytes }, "after"),
        })
        {
            var bytes = CampaignCombatWeatherCodec.SerializeState(state);
            CheckGolden(goldens, key, bytes);
            Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
            Assert.Equal(bytes, CampaignCombatWeatherCodec.SerializeState(
                CampaignCombatWeather.ReadState(bytes, request, created, opening, suffix)));
        }
        var retry = CampaignCombatWeather.Apply(request, created, opening, [result.EventBytes], input);
        Assert.True(retry.Duplicate);
        Assert.Equal(result.EventBytes, retry.EventBytes);
        Assert.Equal(CampaignCombatWeatherCodec.SerializeState(after), CampaignCombatWeatherCodec.SerializeState(retry.State));
        foreach (var suffix in new[] { Array.Empty<byte[]>(), new[] { result.EventBytes } })
            Assert.Throws<JsonException>(() => CampaignCombatWeather.Apply(request, created, opening, suffix,
                input with { Actor = CampaignOpeningPreambleActor.Axis }));
        Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(result.EventBytes));
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Command(after));
        var consumed = Convert.FromHexString(expected.GetProperty("consumedHex").GetString()!);
        var seedBytes = BitConverter.GetBytes(seed);
        if (BitConverter.IsLittleEndian) Array.Reverse(seedBytes);
        var block = SHA256.HashData(Encoding.ASCII.GetBytes("sandtable.random.v1\0")
            .Concat(seedBytes).Concat(new byte[8]).ToArray());
        Assert.Equal(consumed, block.Take(consumed.Length));
        Assert.Equal(expected.GetProperty("rejectedBytes").GetInt32(), consumed.Count(b => b >= 252));
        Assert.Equal(new[] { weather.FirstDie, weather.SecondDie }.Concat(weather.LocationDie is { } location ? [location] : []),
            consumed.Where(b => b < 252).Select(b => (b % 6) + 1));
    }

    [Fact]
    public void ArtifactAndFoulLocationCoverageMatchFrozenAuthority()
    {
        var artifact = Cna1979Weather.CreateArtifact();
        var bytes = WeatherRulesArtifactCodec.SerializeCanonical(Cna1979Weather.Definition);
        Assert.Equal("sha256:10c92c736d61c6f88359b203d0a735df0d8a676b60e170b79b46053cfd223037", Digest(bytes));
        var retained = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "cna-1979.1.weather-tables.v1.golden.json")).TrimEnd('\n');
        Assert.Equal(Encoding.UTF8.GetBytes(retained), bytes);
        var manifest = Cna1979CombatRuleset.Manifest.Artifacts.Single(a => a.ArtifactId == Cna1979Weather.ArtifactId);
        Assert.Equal(artifact.ContentHash, manifest.ContentHash);
        Assert.Equal(artifact.Sources, manifest.Sources);
        using var fixture = Fixture();
        var pairs = fixture.RootElement.GetProperty("cases").EnumerateArray()
            .Select(c => c.GetProperty("expected")).Where(e => e.GetProperty("locationDie").ValueKind != JsonValueKind.Null)
            .Select(e => (e.GetProperty("kind").GetString(), e.GetProperty("locationDie").GetInt32())).ToHashSet();
        Assert.Equal(12, pairs.Count);
        foreach (var kind in new[] { "sandstorm", "rainstorm" })
            foreach (var die in Enumerable.Range(1, 6)) Assert.Contains((kind, die), pairs);
    }

    [Fact]
    public void HistoryAndChangedOccurrenceRejectWithoutMutatingCallerBytes()
    {
        var (request, created, opening, input, result) = Trace();
        var retained = opening.Select(Convert.ToHexString).ToArray();
        foreach (var predecessor in new[] { opening.Take(3).ToArray(), opening.Skip(1).ToArray(),
            opening.AsEnumerable().Reverse().ToArray(), opening.Concat([opening[0]]).ToArray() })
            Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, predecessor, []));
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, opening, [result.EventBytes, result.EventBytes]));
        var foreign = Opening(1, "act-first");
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, foreign.Opening, []));
        var opposite = Opening(0, "act-last");
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, opposite.Opening, [result.EventBytes]));
        foreach (var changed in new[]
        {
            input with { Command = input.Command with { ExpectedPriorVersion = 4 } },
            input with { Command = input.Command with { ExpectedPriorVersion = 6 } },
            input with { Command = input.Command with { ExpectedPositionId = "land.position.operation-1.organization" } },
            input with { Command = input.Command with { CreationBinding = "creation.other" } },
            input with { Command = input.Command with { CreationEventHash = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { ContractVersion = 1 } },
            input with { Command = input.Command with { Kind = "resolve-organization" } },
        })
            foreach (var suffix in new[] { Array.Empty<byte[]>(), new[] { result.EventBytes } })
                Assert.Throws<JsonException>(() => CampaignCombatWeather.Apply(request, created, opening, suffix, changed));
        Assert.Equal(retained, opening.Select(Convert.ToHexString));
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, [], opening, []));
    }

    [Fact]
    public void CoherentlyResignedInventedWeatherAndCacheStillReject()
    {
        var (request, created, opening, _, original) = Trace();
        var (_, _, _, _, normal) = Trace(1);
        var forged = JsonNode.Parse(original.EventBytes)!;
        var invented = JsonNode.Parse(normal.EventBytes)!;
        foreach (var field in new[] { "firstDie", "secondDie", "kind", "scope", "locationDie", "affectedAreas",
            "randomCursorAfter", "sources" }) forged[field] = invented[field]?.DeepClone();
        forged["receiptId"] = Receipt(forged);
        var bytes = Encoding.UTF8.GetBytes(forged.ToJsonString());
        var cache = JsonNode.Parse(CampaignCombatWeatherCodec.SerializeState(original.State))!;
        var normalCache = JsonNode.Parse(CampaignCombatWeatherCodec.SerializeState(normal.State))!;
        cache["operationStageWeather"] = normalCache["operationStageWeather"]!.DeepClone();
        cache["randomState"]!["nextByteCursor"] = normal.State.RandomState.NextByteCursor;
        var before = CampaignCombatWeather.Replay(request, created, opening, []);
        cache["prefix"] = Prefix(before.Prefix, bytes);
        cache["receipts"]![4]!["receiptId"] = forged["receiptId"]!.DeepClone();
        cache["receipts"]![4]!["eventHash"] = Digest(bytes);
        Assert.Equal(Receipt(forged), forged["receiptId"]!.GetValue<string>());
        Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, opening, [bytes]));
        Assert.Throws<JsonException>(() => CampaignCombatWeather.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()), request, created, opening, [bytes]));
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void AlteredLeavesCanonicalBytesAndWellFormedForgeriesReject(ulong seed)
    {
        var (request, created, opening, input, result) = Trace(seed);
        foreach (var mutation in Mutations(result.EventBytes))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatWeather.Replay(request, created, opening, [mutation]));
        foreach (var state in new[] { CampaignCombatWeather.Replay(request, created, opening, []), result.State })
            foreach (var mutation in Mutations(CampaignCombatWeatherCodec.SerializeState(state)))
                Assert.Throws<JsonException>(() => CampaignCombatWeather.ReadState(mutation, request, created, opening,
                    state.StateVersion == 5 ? [] : [result.EventBytes]));
        foreach (var mutation in Mutations(CampaignCombatWeatherCodec.SerializeInput(input)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatWeather.Apply(request, created, opening, [],
                CampaignCombatWeatherCodec.DeserializeInput(mutation)));
        foreach (var (field, value) in new (string, JsonNode)[]
        {
            ("priorPrefix", JsonValue.Create("sha256:" + new string('0', 64))!),
            ("randomCursorAfter", JsonValue.Create(100)!),
            ("fuelWaterReductionSubjectCount", JsonValue.Create(1)!),
            ("restoredWellCount", JsonValue.Create(1)!),
            ("damagedGroundedAircraftCount", JsonValue.Create(1)!),
            ("receiptId", JsonValue.Create("wth." + new string('0', 64))!),
        })
        {
            var forged = JsonNode.Parse(result.EventBytes)!;
            forged[field] = value;
            Assert.Throws<JsonException>(() => CampaignCombatWeather.Replay(request, created, opening,
                [Encoding.UTF8.GetBytes(forged.ToJsonString())]));
        }
    }

    [Fact]
    public void BoundedRecordsAndCallerBufferIsolation()
    {
        var (request, created, opening, input, result) = Trace();
        foreach (var invalid in new[] { Array.Empty<byte>(), "null"u8.ToArray(), "[]"u8.ToArray(), new byte[1_048_577] })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatWeatherCodec.DeserializeInput(invalid));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatWeather.Replay(request, created, opening, [invalid]));
            Assert.Throws<JsonException>(() => CampaignCombatWeather.ReadState(invalid, request, created, opening, []));
        }
        var expected = CampaignCombatWeatherCodec.SerializeState(result.State);
        var retry = CampaignCombatWeather.Apply(request, created, opening, [result.EventBytes], input);
        Array.Fill(retry.EventBytes, (byte)0);
        Array.Fill(result.EventBytes, (byte)0);
        Array.Fill(created, (byte)0);
        foreach (var bytes in opening) Array.Fill(bytes, (byte)0);
        Assert.Equal(expected, CampaignCombatWeatherCodec.SerializeState(result.State));
        Assert.Equal(expected, CampaignCombatWeatherCodec.SerializeState(retry.State));
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening,
        CampaignCombatWeatherInput Input, CampaignCombatWeatherResult Result) Trace(ulong seed = 0)
    {
        var (request, created, opening) = Opening(seed, "act-first");
        var state = CampaignCombatWeather.Replay(request, created, opening, []);
        var input = CampaignCombatWeather.Command(state);
        return (request, created, opening, input, CampaignCombatWeather.Apply(request, created, opening, [], input));
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening) Opening(ulong seed, string choice)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var createdGolden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = createdGolden.RootElement;
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
        return (request, created, opening.ToArray());
    }

    private static IEnumerable<byte[]> Mutations(byte[] bytes)
    {
        var root = JsonNode.Parse(bytes)!;
        foreach (var path in Leaves(root, []))
        {
            var copy = root.DeepClone();
            var parent = copy;
            foreach (var part in path[..^1]) parent = parent is JsonArray array ? array[int.Parse(part,
                System.Globalization.CultureInfo.InvariantCulture)]! : parent[part]!;
            if (parent is JsonArray list) list[int.Parse(path[^1], System.Globalization.CultureInfo.InvariantCulture)] = "invalid";
            else parent[path[^1]] = "invalid";
            yield return Encoding.UTF8.GetBytes(copy.ToJsonString());
        }
        var reversed = new JsonObject();
        foreach (var property in root.AsObject().Reverse()) reversed.Add(property.Key, property.Value?.DeepClone());
        yield return Encoding.UTF8.GetBytes(reversed.ToJsonString());
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var raw in new[] { " " + text, text + "\n", "\uFEFF" + text,
            text.Replace("\"contractVersion\":2,", "\"contractVersion\":2.0,", StringComparison.Ordinal)
                .Replace("\"contractVersion\":1,", "\"contractVersion\":1.0,", StringComparison.Ordinal),
            text.Replace("creation.", "cre\\u0061tion.", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":", "\"contractVersion\":1,\"contractVersion\":", StringComparison.Ordinal),
            text[..^1] + ",\"unknown\":null}" }) yield return Encoding.UTF8.GetBytes(raw);
    }
    private static IEnumerable<string[]> Leaves(JsonNode? node, string[] path)
    {
        if (node is JsonObject obj)
            foreach (var property in obj)
                foreach (var leaf in Leaves(property.Value, [.. path, property.Key])) yield return leaf;
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++)
                foreach (var leaf in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf;
        else yield return path;
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-weather-v1.json")));
    private static void CheckGolden(JsonElement goldens, string kind, byte[] bytes)
    {
        Assert.Equal(goldens.GetProperty(kind).GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(goldens.GetProperty(kind).GetProperty("sha256").GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Receipt(JsonNode value)
    {
        var unsigned = value.DeepClone();
        unsigned.AsObject().Remove("receiptId");
        return "wth." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.weather-receipt.v1\0" + unsigned.ToJsonString()))[7..];
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0")
            .Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
    private static byte[] WorldBytes(CampaignCombatWeatherState state) => CampaignWorldV7InitialCodec.Serialize(
        state.Opening.Creation.World, state.Opening.Creation.Setup.Artifact, state.Opening.Creation.Setup.Scenario,
        state.Opening.Creation.Setup.CombatInitialization);
}

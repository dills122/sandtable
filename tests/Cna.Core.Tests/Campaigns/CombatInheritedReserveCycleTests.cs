using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using static Cna.Core.Campaigns.CampaignCombatInheritedReserveCycle;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedReserveCycleTests
{
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void HeldReserveTraversesAllFrozenEventsAndReplayCuts(string choice)
    {
        var chain = Held(choice);
        using var fixture = Fixture();
        var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("choice").GetString() == choice).GetProperty("goldens");
        var records = new[] { CampaignCombatCreationRequestCodec.Serialize(chain.Request), chain.Created }
            .Concat(chain.Opening).Append(chain.Weather).Concat(chain.Stage).Concat(chain.Reserve).ToArray();
        Check(0, JsonSerializer.SerializeToUtf8Bytes(records.Select(Hash)));
        var history = new List<byte[]>(); var inputs = new List<Input>();
        var initial = Replay(chain, history); var basis = CampaignCombatReserveOpeningCodec.SerializeState(initial.Base);
        Check(1, basis);
        for (var cut = 0; cut <= 10; cut++)
        {
            var state = Replay(chain, history); var bytes = CampaignCombatInheritedReserveCycleCodec.SerializeControl(state);
            Check(12 + cut, bytes);
            Assert.Equal(basis, CampaignCombatReserveOpeningCodec.SerializeState(state.Base));
            Assert.Equal(12 + cut, state.StateVersion); Assert.Equal(cut == 10, state.Closed);
            Assert.Equal(CampaignElementReserveStatus.ReserveI, Assert.Single(state.Base.Predecessor.Members).Status);
            Assert.All(state.Base.Predecessor.World.Elements, e => Assert.Equal(new CapabilityPointAmount(0, 1), e.OperationalState.CapabilityPointsExpended));
            Assert.Equal(bytes, CampaignCombatInheritedReserveCycleCodec.SerializeControl(ReadControl(chain, history, bytes)));
            for (var retry = 0; retry < cut; retry++)
            {
                var result = Apply(chain, history, inputs[retry], bytes);
                Assert.True(result.IsDuplicate); Assert.Equal(history[retry], result.EventBytes);
                Assert.Equal(bytes, CampaignCombatInheritedReserveCycleCodec.SerializeControl(result.State));
            }
            if (cut == 10)
            {
                Assert.Equal("land.position.operation-1.first-player.movement-and-combat.reserve-release", state.Position.PositionId);
                Assert.ThrowsAny<JsonException>(() => CreateInput(state));
                Assert.Empty(state.MovementEnd!.ExcludedBefore);
                Assert.ThrowsAny<JsonException>(() => Apply(chain, history, inputs[^1] with { Command = inputs[^1].Command with { ExpectedPriorVersion = 22 } }));
                break;
            }
            var input = CreateInput(state); inputs.Add(input);
            var fresh = Apply(chain, history, input, bytes); Assert.False(fresh.IsDuplicate);
            Check(2 + cut, fresh.EventBytes);
            Assert.Equal(Hash(CampaignCombatInheritedReserveCycleCodec.SerializeInput(input)), fresh.State.Receipts[^1].CommandHash);
            Assert.Equal(Hash(fresh.EventBytes), fresh.State.Receipts[^1].EventHash);
            Assert.Equal(CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, fresh.EventBytes), fresh.State.Prefix);
            history.Add(fresh.EventBytes);
        }
        void Check(int index, byte[] bytes)
        {
            Assert.Equal(golden.GetProperty("bytes")[index].GetInt32(), bytes.Length);
            Assert.Equal(golden.GetProperty("sha256")[index].GetString(), Hash(bytes));
        }
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryOccurrenceRejectsChangedIdentityActorOrderAndEffects(string choice)
    {
        var chain = Held(choice); var history = new List<byte[]>(); var other = Held(choice == "act-first" ? "act-last" : "act-first");
        for (var index = 0; index < 10; index++)
        {
            var state = Replay(chain, history); var input = CreateInput(state); var c = input.Command;
            foreach (var command in new[] { c with { ContractVersion = 1 }, c with { Kind = "release-I" }, c with { CreationBinding = "foreign" },
                c with { CreationEventHash = "sha256:" + new string('0', 64) }, c with { CycleId = "sha256:" + new string('1', 64) },
                c with { ExpectedPriorVersion = c.ExpectedPriorVersion - 1 }, c with { ExpectedPriorVersion = long.MaxValue },
                c with { ExpectedPositionId = "foreign" }, c with { PriorReceiptId = "foreign" } })
                Assert.ThrowsAny<JsonException>(() => Apply(chain, history, input with { Command = command }));
            foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != input.Actor))
                Assert.ThrowsAny<JsonException>(() => Apply(chain, history, input with { Actor = actor }));
            var bytes = Apply(chain, history, input).EventBytes;
            foreach (var mutation in Mutations(bytes))
                Assert.ThrowsAny<JsonException>(() => Replay(chain, history.Append(mutation).ToArray()));
            Assert.ThrowsAny<JsonException>(() => Replay(other, history.Append(bytes).ToArray()));
            history.Add(bytes);
        }
        foreach (var bad in new[] { history.Skip(1).ToArray(), history.AsEnumerable().Reverse().ToArray(),
            new[] { history[0], history[0] }, history.Append(history[^1]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => Replay(chain, bad));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ForgedControlCannotReplaceHistoryAndReturnedBytesAreDetached(string choice)
    {
        var chain = Held(choice); var history = new List<byte[]>();
        for (var i = 0; i < 10; i++) history.Add(Apply(chain, history, CreateInput(Replay(chain, history))).EventBytes);
        var state = Replay(chain, history); var bytes = CampaignCombatInheritedReserveCycleCodec.SerializeControl(state);
        var first = CreateInput(Replay(chain, []));
        foreach (var mutation in Mutations(bytes))
            Assert.ThrowsAny<JsonException>(() => ReadControl(chain, history, mutation));
        var forged = bytes.ToArray(); forged[^2] ^= 1;
        Assert.ThrowsAny<JsonException>(() => Apply(chain, history, first, forged));
        Assert.ThrowsAny<JsonException>(() => ReadControl(chain, history.Take(9).ToArray(), bytes));
        Assert.ThrowsAny<JsonException>(() => ReadControl(chain, history, []));
        Assert.ThrowsAny<JsonException>(() => ReadControl(chain, history, new byte[1_048_577]));
        var result = Apply(chain, history, first); var emitted = result.EventBytes; emitted[0] ^= 1;
        Assert.Equal(history[0], result.EventBytes);
        var copiedEvents = state.Events; copiedEvents[0][0] ^= 1;
        Assert.Equal(bytes, CampaignCombatInheritedReserveCycleCodec.SerializeControl(state));
        Assert.Equal(history[0], state.Events[0]);
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, chain.Created, chain.Request));
    }

    [Fact]
    public void MissingDesignationForeignHistoryAndUnsupportedWeatherNeverAdmit()
    {
        var chain = Held("act-first");
        Assert.ThrowsAny<JsonException>(() => Replay(chain with { Reserve = [] }, []));
        Assert.ThrowsAny<JsonException>(() => Replay(chain with { Reserve = [chain.Reserve[^1]] }, []));
        Assert.ThrowsAny<JsonException>(() => Replay(chain with { Opening = chain.Opening.Skip(1).ToArray() }, []));
        Assert.ThrowsAny<JsonException>(() => Replay(chain with { Stage = chain.Stage.Take(3).ToArray() }, []));
        var other = Held("act-last");
        Assert.ThrowsAny<JsonException>(() => Replay(chain with { Reserve = other.Reserve }, []));
        var rejectedWeatherProfiles = 0;
        foreach (var seed in new ulong[] { 0, 2, 3 })
        {
            var alternate = Held("act-first", seed); var opening = CampaignCombatReserveOpening.Replay(alternate.Request, alternate.Created,
                alternate.Opening, [alternate.Weather], alternate.Stage, alternate.Reserve);
            if (opening.Predecessor.Stage.Weather.Weather[0].Kind != WeatherKind.Normal)
            {
                rejectedWeatherProfiles++;
                Assert.ThrowsAny<JsonException>(() => Replay(alternate, []));
            }
        }
        Assert.True(rejectedWeatherProfiles > 0, "At least one non-Normal profile must exercise the rejection boundary.");
    }

    private sealed record History(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve);
    private static History Held(string choice, ulong seed = 1)
    {
        var (request, created, opening, weather, stage) = Chain(seed, choice); var reserve = new List<byte[]>();
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, reserve,
            CampaignCombatReserveDesignation.Command(state)).EventBytes);
        reserve.Add(CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, reserve,
            CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, reserve)).EventBytes);
        return new(request, created, opening, weather, stage, reserve.ToArray());
    }
    private static State Replay(History h, IReadOnlyList<byte[]> events) => CampaignCombatInheritedReserveCycle.Replay(h.Request, h.Created, h.Opening, [h.Weather], h.Stage, h.Reserve, events);
    private static Result Apply(History h, IReadOnlyList<byte[]> events, Input input, byte[]? cache = null) => CampaignCombatInheritedReserveCycle.Apply(h.Request, h.Created, h.Opening, [h.Weather], h.Stage, h.Reserve, events, input, cache);
    private static State ReadControl(History h, IReadOnlyList<byte[]> events, byte[] bytes) => CampaignCombatInheritedReserveCycle.ReadControl(bytes, h.Request, h.Created, h.Opening, [h.Weather], h.Stage, h.Reserve, events);
    private static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-inherited-reserve-cycle-v1.json")));

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
            text.Replace("\"contractVersion\":", "\"contractVersion\":1,\"contractVersion\":", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":2,", "\"contractVersion\":2.0,", StringComparison.Ordinal)
                .Replace("\"contractVersion\":1,", "\"contractVersion\":1.0,", StringComparison.Ordinal),
            text.Replace("contractVersion", "contract\\u0056ersion", StringComparison.Ordinal),
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
}

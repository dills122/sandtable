using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualReactionCombatEntryTests
{
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void AuthenticRebuiltWorldCertifiesPositiveCp4Geometry(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        foreach (var kind in new[] { "decline-reaction-window", "close-reaction-window-scripted-unavailable", "close-reaction-window-timeout" })
        {
            var before = Replay(p, moves, trigger, []);
            var after = Apply(p, moves, trigger, [], CampaignCombatReactionClosure.Command(before, kind)).State;
            var cycle = after.Opening.Cycle!;
            var candidate = CampaignCombatCertification.CertifyInitialProfileFacts(p.Request, p.Created, after.World, cycle, cycle.ActingSide, WeatherKind.Normal, WeatherKind.Normal);
            Assert.NotNull(candidate);
            Assert.Equal(new CapabilityPointAmount(4, 1), after.World.Elements.Single(e => e.ElementId == candidate.Attacker.Unit.ElementId).OperationalState.CapabilityPointsExpended);
            Assert.Equal(candidate.Defender.LocationId, candidate.TargetLocationId);
            Assert.Equal(14, after.StateVersion);
            using var bytes = JsonDocument.Parse(CampaignCombatReactionClosureCodec.SerializeState(after));
            Assert.Equal((ulong)1, bytes.RootElement.GetProperty("randomState").GetProperty("seed").GetUInt64());
            Assert.Equal((ulong)2, bytes.RootElement.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(13, after.Receipts.Count);
        }
    }
    [Theory]
    [InlineData("act-first", "decline-reaction-window")]
    [InlineData("act-first", "close-reaction-window-scripted-unavailable")]
    [InlineData("act-first", "close-reaction-window-timeout")]
    [InlineData("act-last", "decline-reaction-window")]
    [InlineData("act-last", "close-reaction-window-scripted-unavailable")]
    [InlineData("act-last", "close-reaction-window-timeout")]
    public void AuthenticatedReactionClosureReachesCertifiedCombatEntryWithoutResettingCp(string choice, string reason)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var before = Replay(p, moves, trigger, []);
        var closure = Apply(p, moves, trigger, [], CampaignCombatReactionClosure.Command(before, reason));
        var prefix = p.Opening.Concat(new[] { p.Weather }).Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Append(closure.EventBytes).ToArray();
        var trusted = prefix.Select(InputBytes).ToArray();
        var source = Source(p.Created, prefix); var suffix = new List<byte[]>(); var breakdown = new List<byte[]>();
        var current = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, trusted, suffix, breakdown, Dependency);
        Assert.Equal(14, current.Control.GetProperty("stateVersion").GetInt64());
        for (var i = 0; i < 4; i++)
        {
            var input = current.CommandBytes!;
            current = CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(current.SourceBytes, p.Request, trusted, suffix, breakdown, input, Dependency);
            if (i < 3) suffix.Add(input); else breakdown.Add(input);
        }
        Assert.Equal(18, current.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", current.Control.GetProperty("sequencePosition").GetProperty("positionId").GetString());
        Assert.Equal(17, current.Control.GetProperty("receipts").GetArrayLength());
        Assert.Equal("idle", current.Control.GetProperty("breakdownFlow").GetProperty("kind").GetString());
        Assert.NotNull(current.Candidate);
        Assert.Equal(new CapabilityPointAmount(4, 1), current.World.Elements.Single(e => e.ElementId == current.Candidate!.Attacker.Unit.ElementId).OperationalState.CapabilityPointsExpended);
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void FreshAuthenticCampaignIdentityUsesNoFixtureHashAllowlist(string choice)
    {
        var (p, source, prefix) = Prepare(choice, "decline-reaction-window", "authentic.changed-campaign");
        var result = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix, [], [], Dependency);
        Assert.Equal("authentic.changed-campaign", result.Control.GetProperty("campaignId").GetString());
        var normal = Ready(1, choice);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, normal.Request, prefix, [], [], Dependency));
    }
    [Fact]
    public void SourceAndTrustedLedgersAreOwnedBeforeDependencyCallbackCanMutateCallerMemory()
    {
        var (p, source, prefix) = Prepare("act-first", "decline-reaction-window");
        var expected = source.ToArray(); var oldInput = prefix[0].ToArray();
        var mutated = false;
        byte[] Acquire(string path)
        {
            if (!mutated) { source[0] = 0; prefix[0][0] = 0; mutated = true; }
            return Dependency(path);
        }
        var result = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix, [], [], Acquire);
        Assert.True(mutated); Assert.Equal(expected, result.SourceBytes); Assert.Equal(oldInput, result.PrefixInputs[0]);
    }
    [Fact]
    public void NoncanonicalSourceAndAllIndependentPrefixInputSubstitutionsReject()
    {
        var (p, source, prefix) = Prepare("act-first", "close-reaction-window-timeout");
        var text = Encoding.ASCII.GetString(source);
        foreach (var malformed in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.ASCII.GetBytes("null"), Encoding.ASCII.GetBytes("{}"),
            Encoding.ASCII.GetBytes(" " + text), Encoding.ASCII.GetBytes(text + "\n"),
            Encoding.ASCII.GetBytes(text.Replace("\"contractVersion\":1,", "\"contractVersion\":1.0,", StringComparison.Ordinal)),
            Encoding.ASCII.GetBytes(text.Replace("\"contractVersion\":1,", "\"contractVersion\":1,\"contractVersion\":1,", StringComparison.Ordinal)) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(malformed, p.Request, prefix, [], [], Dependency));
        for (var i = 0; i < prefix.Length; i++)
        {
            var changed = prefix.Select(v => v.ToArray()).ToArray();
            var node = System.Text.Json.Nodes.JsonNode.Parse(changed[i])!;
            node["actor"] = node["actor"]!.GetValue<string>() == "system" ? "axis" : "system";
            changed[i] = JsonSerializer.SerializeToUtf8Bytes(node, SourceOptions);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, changed, [], [], Dependency));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, [], [], Encoding.ASCII.GetBytes("{}"), Dependency));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, [], [], Encoding.ASCII.GetBytes("{\"command\":null}"), Dependency));
    }
    [Fact]
    public void ResignedPrefixAndLifecycleEffectsDoNotBecomeAuthenticatedAuthority()
    {
        var (p, source, prefix) = Prepare("act-first", "decline-reaction-window");
        var packet = System.Text.Json.Nodes.JsonNode.Parse(source)!;
        var ev = System.Text.Json.Nodes.JsonNode.Parse(packet["prefixEventCanonicalUtf8"]![12]!.GetValue<string>())!;
        ev["closedOpportunityIds"] = new System.Text.Json.Nodes.JsonArray();
        ev.AsObject().Remove("receiptId");
        ev["receiptId"] = "irl." + Digest(Encoding.ASCII.GetBytes("sandtable.combat.inherited-reaction-window-closed-receipt.v3 ").Concat(JsonSerializer.SerializeToUtf8Bytes(ev, SourceOptions)).ToArray())[7..];
        packet["prefixEventCanonicalUtf8"]![12] = Encoding.ASCII.GetString(JsonSerializer.SerializeToUtf8Bytes(ev, SourceOptions));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(JsonSerializer.SerializeToUtf8Bytes(packet, SourceOptions), p.Request, prefix, [], [], Dependency));
        var initial = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix, [], [], Dependency);
        var cmd = initial.CommandBytes!;
        var after = CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, [], [], cmd, Dependency);
        var moved = System.Text.Json.Nodes.JsonNode.Parse(after.SourceBytes)!;
        var stopped = System.Text.Json.Nodes.JsonNode.Parse(moved["lifecycleEventCanonicalUtf8"]![0]!.GetValue<string>())!;
        stopped["breakdownFlowAfter"]!["stop"]!["route"]!["currentLocationId"] = "axis-rear";
        var receiptPrefix = stopped["receiptId"]!.GetValue<string>().Split('.')[0] + ".";
        stopped.AsObject().Remove("receiptId");
        stopped["receiptId"] = receiptPrefix + Digest(Encoding.ASCII.GetBytes("sandtable.combat.inherited-movement-stop-receipt.v2 ").Concat(JsonSerializer.SerializeToUtf8Bytes(stopped, SourceOptions)).ToArray())[7..];
        moved["lifecycleEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(JsonSerializer.SerializeToUtf8Bytes(stopped, SourceOptions));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(JsonSerializer.SerializeToUtf8Bytes(moved, SourceOptions), p.Request, prefix, [cmd], [], Dependency));
        foreach (var trusted in new[] { Array.Empty<byte[]>(), new[] { Encoding.ASCII.GetBytes("{}") } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(after.SourceBytes, p.Request, prefix, trusted, [], Dependency));
    }
    private static (Pack Pack, byte[] Source, byte[][] Inputs) Prepare(string choice, string reason, string campaignId = "rules-lab.combat-creation.1")
    {
        var p = Ready(1, choice, campaignId: campaignId); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var before = Replay(p, moves, trigger, []);
        var ev = Apply(p, moves, trigger, [], CampaignCombatReactionClosure.Command(before, reason)).EventBytes;
        var prefix = p.Opening.Concat(new[] { p.Weather }).Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Append(ev).ToArray();
        return (p, Source(p.Created, prefix), prefix.Select(InputBytes).ToArray());
    }
    internal static void VerifyCanonicalCase(JsonElement item)
    {
        var choice = item.GetProperty("name").GetString()!.StartsWith("axis-", StringComparison.Ordinal) ? "act-first" : "act-last";
        var p = Ready(1, choice);
        static byte[][] Texts(JsonElement array) => array.EnumerateArray().Select(e => Encoding.ASCII.GetBytes(e.GetString()!)).ToArray();
        var prefix = Texts(item.GetProperty("prefixInputCanonicalUtf8"));
        var inputs = Texts(item.GetProperty("inputCanonicalUtf8"));
        var events = Texts(item.GetProperty("eventCanonicalUtf8"));
        var cuts = item.GetProperty("cuts").EnumerateArray().ToArray();
        for (var cut = 0; cut < cuts.Length; cut++)
        {
            var source = Encoding.ASCII.GetBytes(cuts[cut].GetProperty("sourceCanonicalUtf8").GetString()!);
            var lifecycle = inputs.Take(Math.Min(cut, 3)).ToArray();
            var breakdown = cut == 4 ? inputs.Skip(3).ToArray() : [];
            var result = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix, lifecycle, breakdown, Dependency);
            Assert.Equal(source, result.SourceBytes);
            Assert.Equal(Encoding.ASCII.GetBytes(cuts[cut].GetProperty("controlCanonicalUtf8").GetString()!), result.ControlBytes);
            Assert.Equal(Encoding.ASCII.GetBytes(cuts[cut].GetProperty("proofCanonicalUtf8").GetString()!), result.ProofBytes);
            Assert.Equal(14 + cut, result.Control.GetProperty("stateVersion").GetInt64());
            Assert.Equal(13 + cut, result.Control.GetProperty("receipts").GetArrayLength());
            Assert.Equal(result.ControlBytes, CampaignCombatActualReactionCombatEntry.ReadControl(result.ControlBytes, source, p.Request, prefix, lifecycle, breakdown, Dependency).ControlBytes);
            Assert.Equal(result.ProofBytes, CampaignCombatActualReactionCombatEntry.ReadProof(result.ProofBytes, source, p.Request, prefix, lifecycle, breakdown, Dependency).ProofBytes);
            for (var i = 0; i < cut; i++)
            {
                var retry = CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, lifecycle, breakdown, inputs[i], Dependency);
                Assert.True(retry.Duplicate); Assert.Equal(events[i], retry.EventBytes);
                Assert.Equal(source, retry.SourceBytes); Assert.Equal(result.ControlBytes, retry.ControlBytes);
            }
            if (cut < 4)
            {
                Assert.Equal(inputs[cut], result.CommandBytes);
                var applied = CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, lifecycle, breakdown, inputs[cut], Dependency);
                Assert.False(applied.Duplicate); Assert.Equal(events[cut], applied.EventBytes);
                Assert.Equal(Encoding.ASCII.GetBytes(cuts[cut + 1].GetProperty("sourceCanonicalUtf8").GetString()!), applied.SourceBytes);
                var bad = System.Text.Json.Nodes.JsonNode.Parse(inputs[cut])!;
                bad["actor"] = cut == 1 || cut == 3 ? "axis" : "system";
                Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(source, p.Request, prefix, lifecycle, breakdown, JsonSerializer.SerializeToUtf8Bytes(bad, SourceOptions), Dependency));
            }
            else
            {
                Assert.Null(result.CommandBytes); Assert.NotNull(result.Candidate);
                Assert.Equal(events[3], result.FinalBreakdownEventBytes);
                Assert.Equal(Digest(events[3]), result.Proof.GetProperty("finalBreakdownEventHash").GetString());
                Assert.Equal(new CapabilityPointAmount(4, 1), result.World.Elements.Single(e => e.ElementId == result.Candidate!.Attacker.Unit.ElementId).OperationalState.CapabilityPointsExpended);
            }
            var control = System.Text.Json.Nodes.JsonNode.Parse(result.ControlBytes)!;
            foreach (var field in new[] { "world", "receipts", "actualProgressRefs", "movementEnd", "prefix" })
            {
                var forged = control.DeepClone(); forged[field] = control[field] is null ? new System.Text.Json.Nodes.JsonObject() : null;
                Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReadControl(JsonSerializer.SerializeToUtf8Bytes(forged, SourceOptions), source, p.Request, prefix, lifecycle, breakdown, Dependency));
            }
            var proof = System.Text.Json.Nodes.JsonNode.Parse(result.ProofBytes)!;
            proof["candidate"] = new System.Text.Json.Nodes.JsonObject();
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReadProof(JsonSerializer.SerializeToUtf8Bytes(proof, SourceOptions), source, p.Request, prefix, lifecycle, breakdown, Dependency));
            var copy = result.SourceBytes; copy[0] = 0; Assert.Equal(source, result.SourceBytes);
            var owned = result.ControlBytes; owned[0] = 0; Assert.NotEqual(owned, result.ControlBytes);
            var exported = result.PrefixInputs; exported[0][0] = 0; Assert.Equal(prefix[0], result.PrefixInputs[0]);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix[..^1], lifecycle, breakdown, Dependency));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, prefix, lifecycle, breakdown, _ => [0]));
            var extra = System.Text.Json.Nodes.JsonNode.Parse(source)!.AsObject(); extra["world"] = control["world"]!.DeepClone();
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(JsonSerializer.SerializeToUtf8Bytes(extra, SourceOptions), p.Request, prefix, lifecycle, breakdown, Dependency));
            var changed = prefix.Select(i => i.ToArray()).ToArray(); changed[12][0] = (byte)' ';
            Assert.ThrowsAny<JsonException>(() => CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, changed, lifecycle, breakdown, Dependency));
            Assert.Equal(prefix[0], result.PrefixInputs[0]);
        }
    }
    private static byte[] InputBytes(byte[] ev)
    { using var d = JsonDocument.Parse(ev); return Encoding.UTF8.GetBytes(d.RootElement.GetProperty("input").GetRawText()); }
    private static readonly JsonSerializerOptions SourceOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static byte[] Source(byte[] created, byte[][] events)
    {
        var obj = new System.Text.Json.Nodes.JsonObject
        {
            ["contractVersion"] = 1,
            ["createdCanonicalUtf8"] = Encoding.ASCII.GetString(created),
            ["prefixEventCanonicalUtf8"] = new System.Text.Json.Nodes.JsonArray(events.Select(e => (System.Text.Json.Nodes.JsonNode?)System.Text.Json.Nodes.JsonValue.Create(Encoding.ASCII.GetString(e))).ToArray()),
            ["lifecycleEventCanonicalUtf8"] = new System.Text.Json.Nodes.JsonArray(),
            ["breakdownEventCanonicalUtf8"] = new System.Text.Json.Nodes.JsonArray()
        };
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj, SourceOptions);
    }
    private static byte[] Dependency(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Sandtable.slnx"))) root = root.Parent;
        return File.ReadAllBytes(Path.Combine(root!.FullName, path));
    }
    private sealed record Pack(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve);
    private static byte[][] FirstMove(Pack p, string choice = "act-first")
    {
        var state = CampaignCombatInheritedMovement.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, []);
        return [CampaignCombatInheritedMovement.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, [], CampaignCombatInheritedMovement.Command(state, choice == "act-first" ? "axis-rear" : "commonwealth-rear")).EventBytes];
    }
    private static byte[][] Trigger(Pack p, byte[][] moves)
    {
        var state = CampaignCombatReactionTrigger.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, []);
        return [CampaignCombatReactionTrigger.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, [], CampaignCombatReactionTrigger.Command(state)).EventBytes];
    }
    private static CampaignCombatReactionClosureState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
    private static CampaignCombatReactionClosureResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionClosureInput input) => CampaignCombatReactionClosure.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events, input);
    private static CampaignCombatReactionClosureState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0").Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
    private static Pack Ready(
        ulong seed = 1, string choice = "act-first", bool designated = false, string campaignId = "rules-lab.combat-creation.1")
    {
        var (request, created, opening, weather, stage) = Chain(seed, choice, campaignId);
        var reserve = new List<byte[]>();
        if (designated)
        {
            var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
            reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state)).EventBytes);
        }
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, reserve, input).EventBytes);
        return new(request, created, opening, weather, stage, reserve.ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 1, string choice = "act-first", string campaignId = "rules-lab.combat-creation.1")
    {
        var (request, created, opening, weather) = Predecessor(seed, choice, campaignId);
        var stage = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], stage);
            stage.Add(CampaignCombatStageEntry.Apply(request, created, opening, [weather], stage, CampaignCombatStageEntry.Command(state)).EventBytes);
        }
        return (request, created, opening, weather, stage.ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather) Predecessor(
        ulong seed = 1, string choice = "act-first", string campaignId = "rules-lab.combat-creation.1")
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create(campaignId, seed,
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

    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

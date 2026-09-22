using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatResultReleaseTests
{
    [Fact]
    public void SettledCombatEmitsExactNativeReleaseOpeningAndCompletion()
    {
        using var bridge = Fixture("combat-result-cycle-finish-v1.json");
        using var results = Fixture("combat-result-settlement-v2.json");
        var row = bridge.RootElement.GetProperty("traces")[0];
        var source = Source(FindResult(results, Text(row, "caseId")));
        var inputs = ReleaseInputs(row); var events = ReleaseEvents(row);
        var opened = CampaignCombatResultRelease.Apply(source, [], [], inputs[0]);
        Assert.Equal(events[0], opened.EventBytes); Assert.Equal("open", opened.State.Status);
        Assert.Empty(opened.State.Pending); Assert.Null(opened.State.Timing); Assert.Null(opened.State.DecisionId);
        var done = CampaignCombatResultRelease.Apply(source, inputs[..1], events[..1], inputs[1]);
        Assert.Equal(events[1], done.EventBytes); Assert.Equal("completed", done.State.Status);
        Assert.Equal(opened.State.StateVersion + 1, done.State.StateVersion);
    }


    [Fact]
    public void ThirtyTwoNativeBasesSixtyFourEventsAndNinetySixReplayCutsPreserveSettledWorld()
    {
        var fixtureBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-cycle-finish-v1.json"));
        Assert.Equal("sha256:a1b6cb5f611586da62667fd4315365aa3a38c34af8afe016314eba4c7c1057c2", Hash(fixtureBytes));
        using var bridge = JsonDocument.Parse(fixtureBytes); using var results = Fixture("combat-result-settlement-v2.json");
        var bases = 0; var eventsCount = 0; var cuts = 0; var guards = 0; var entitlements = 0;
        foreach (var row in bridge.RootElement.GetProperty("traces").EnumerateArray())
        {
            var resultRow = FindResult(results, Text(row, "caseId")); var source = Source(resultRow);
            var inputs = ReleaseInputs(row); var events = ReleaseEvents(row);
            var initial = CampaignCombatResultRelease.Replay(source, [], []);
            var basisBytes = CampaignCombatReserveReleaseCodec.SerializeBase(initial.Basis, source.Request);
            Assert.Equal(Text(row, "releaseBase"), Encoding.UTF8.GetString(basisBytes)); bases++;
            Assert.Equal(basisBytes, CampaignCombatReserveReleaseCodec.SerializeBase(CampaignCombatResultRelease.ReadBase(basisBytes, source), source.Request));
            Assert.Null(initial.Basis.AcceptedHighWater); Assert.True(initial.Result.Closed); Assert.Null(initial.Result.Window);
            Assert.Equal(initial.Result.StateVersion, initial.Release.StateVersion); Assert.Equal(initial.Result.Prefix, initial.Release.Prefix);
            Assert.Equal(initial.Result.CaCompletionReceiptId, initial.Basis.CombatCompletionReceiptId);
            var member = Assert.Single(initial.Basis.Members); Assert.Equal(CampaignElementReserveStatus.None, member.Status);
            var own = initial.Result.World.Elements.Single(e => e.ElementId == member.Unit.ElementId);
            Assert.Equal(own.OperationalState.CapabilityPointsExpended, member.SpentCp); Assert.Equal(10, member.BaseCpa);
            var resultBytes = Encoding.UTF8.GetBytes(Text(resultRow, "stateCanonicalUtf8"));
            Assert.Equal(resultBytes, CampaignCombatResolutionCodec.SerializeState(initial.Result));
            var worldBytes = WorldBytes(initial.Result); Assert.Equal(Text(row, "worldHash"), Hash(worldBytes));
            Assert.Equal(initial.Basis.RetainedWorldHash, Hash(worldBytes));
            if (initial.Result.World.Guards.Count > 0) guards++;
            if (initial.Result.World.ReplacementEntitlements.Count > 0) entitlements++;
            for (var cut = 0; cut <= 2; cut++)
            {
                var state = CampaignCombatResultRelease.Replay(source, inputs[..cut], events[..cut]);
                var bytes = CampaignCombatReserveReleaseCodec.SerializeState(state.Release);
                var restored = CampaignCombatResultRelease.ReadState(bytes, source, inputs[..cut], events[..cut]);
                Assert.Equal(bytes, CampaignCombatReserveReleaseCodec.SerializeState(restored.Release)); cuts++;
                Assert.Equal(resultBytes, CampaignCombatResolutionCodec.SerializeState(state.Result)); Assert.Equal(worldBytes, WorldBytes(state.Result));
                Assert.Equal(initial.Result.World, state.Result.World); Assert.Equal(initial.Result.RandomState, state.Release.RandomState);
                Assert.Equal(initial.Result.Context.Committed.AttackHistory, state.Release.AttackHistory);
                Assert.Equal(initial.Basis.Members, state.Release.Members); Assert.Null(state.Release.AcceptedHighWater);
                Assert.Null(state.Release.Timing); Assert.Null(state.Release.DecisionId); Assert.Empty(state.Release.Pending); Assert.Empty(state.Release.Dispositions);
                Assert.Equal(initial.Basis.PositionId, state.Basis.PositionId); Assert.Equal(initial.Result.StateVersion + cut, state.Release.StateVersion);
                if (cut < 2)
                {
                    var applied = CampaignCombatResultRelease.Apply(source, inputs[..cut], events[..cut], inputs[cut]);
                    Assert.Equal(CombatStepsDisposition.Accepted, applied.Disposition); Assert.Equal(events[cut], applied.EventBytes); eventsCount++;
                    Assert.Equal(bytes, CampaignCombatReserveReleaseCodec.SerializeState(CampaignCombatResultRelease.Replay(source, inputs[..cut], events[..cut]).Release));
                }
                for (var retryIndex = 0; retryIndex < cut; retryIndex++)
                {
                    var retry = CampaignCombatResultRelease.Apply(source, inputs[..cut], events[..cut], inputs[retryIndex]);
                    Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Null(retry.EventBytes);
                    Assert.Equal(bytes, CampaignCombatReserveReleaseCodec.SerializeState(retry.State));
                    using var native = JsonDocument.Parse(events[retryIndex]); Assert.Equal(Text(native.RootElement, "receiptId"), retry.ReceiptId);
                }
            }
            Assert.Equal("completed", CampaignCombatResultRelease.Replay(source, inputs, events).Release.Status);
        }
        Assert.Equal((32, 64, 96, 8, 8), (bases, eventsCount, cuts, guards, entitlements));
    }

    [Fact]
    public void ReliableSourceRetimingPreservesAuditAndStartsIndependentEmptyReleaseClock()
    {
        using var fixture = Fixture("combat-result-settlement-v2.json"); var changed = 0; var untimed = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var original = Source(row); var baseline = CampaignCombatResultRelease.Replay(original, [], []);
            if (!original.ResultInputs.Any(i => i.AdmittedAt is not null)) { untimed++; continue; }
            var retimed = Rebuild(row, (i, _) => i with { AdmittedAt = i.AdmittedAt is { } now ? now + 100 : null });
            var admitted = CampaignCombatResultRelease.Replay(retimed, [], []);
            Assert.Equal(WorldBytes(baseline.Result), WorldBytes(admitted.Result));
            Assert.Equal(baseline.Result.AcceptedHighWater + 100, admitted.Result.AcceptedHighWater);
            Assert.NotEqual(baseline.Result.Prefix, admitted.Result.Prefix);
            Assert.Null(admitted.Basis.AcceptedHighWater); Assert.Null(admitted.Release.AcceptedHighWater);
            var open = new CombatReleaseInput(new(1, "open", admitted.Release.ReleaseId, admitted.Release.StateVersion), CampaignOpeningPreambleActor.System);
            var opened = CampaignCombatResultRelease.Apply(retimed, [], [], open);
            var complete = new CombatReleaseInput(new(1, "complete", opened.State.ReleaseId, opened.State.StateVersion), CampaignOpeningPreambleActor.System);
            var closed = CampaignCombatResultRelease.Apply(retimed, [open], [opened.EventBytes!], complete);
            Assert.Equal("completed", closed.State.Status); Assert.Null(closed.State.Timing); Assert.Equal(admitted.Result.RandomState, closed.State.RandomState);
            changed++;
        }
        Assert.Equal(24, changed); Assert.Equal(8, untimed);
    }

    [Fact]
    public void AuthenticatedSourceFallbackRejectsEvenWhenSettledWorldMatchesOwnerChoice()
    {
        using var fixture = Fixture("combat-result-settlement-v2.json"); var rejected = 0; var sameWorld = 0; var openingFaults = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where(r => Text(r, "name").Contains("capture", StringComparison.Ordinal)))
        {
            var changed = false;
            var fault = Rebuild(row, (input, state) =>
            {
                if (input.Command.Kind == "choose" && state.Window?.Kind == "custody") { changed = true; return input with { ClockAvailable = false }; }
                return input;
            });
            Assert.True(changed); var native = Result(fault); Assert.True(native.Closed);
            var original = Result(Source(row));
            if (WorldBytes(original).AsSpan().SequenceEqual(WorldBytes(native))) sameWorld++;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(fault, [], [])); rejected++;
            // Opening failure produces a valid native closure with a different command sequence.
            if (Text(row, "name").StartsWith("defender-capture", StringComparison.Ordinal))
            {
                var failedOpen = Rebuild(row, (input, state) => input.Command.Kind == "advance" && state.Status == "retreat"
                    ? input with { ClockAvailable = false } : input, skipUnneededChoice: true);
                Assert.True(Result(failedOpen).Closed);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(failedOpen, [], [])); openingFaults++;
            }
        }
        Assert.Equal(16, rejected); Assert.Equal(8, sameWorld); Assert.Equal(8, openingFaults);
    }

    [Fact]
    public void UnfinishedForeignAndChangedCanonicalUpstreamSourcesReject()
    {
        using var results = Fixture("combat-result-settlement-v2.json"); var row = results.RootElement.GetProperty("traces")[0]; var source = Source(row);
        for (var cut = 0; cut < source.ResultEvents.Count; cut++)
        {
            var shortSource = Copy(source, resultInputs: source.ResultInputs.Take(cut).ToArray(), resultEvents: source.ResultEvents.Take(cut).ToArray());
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(shortSource, [], []));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(Copy(source, caseId: "unknown.axis.attacker"), [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(Copy(source, caseId: "ordinary.commonwealth.attacker"), [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(Copy(source, resultEvents: source.ResultEvents.Reverse().ToArray()), [], []));
        var forged = source.ResultEvents.ToArray(); var node = JsonNode.Parse(forged[^1])!; node["effect"]!["toPositionId"] = "foreign"; forged[^1] = RehashResult(node);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(Copy(source, resultEvents: forged), [], []));
        var wrongInput = source.ResultInputs.ToArray(); wrongInput[0] = wrongInput[0] with { Actor = CampaignOpeningPreambleActor.Axis };
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(Copy(source, resultInputs: wrongInput), [], []));
        // Valid Round2/Result2 retiming is outside this bridge's pinned upstream lineage.
        var test = CombatResolutionTests.Case(row); var inputs = new List<CombatRoundInput>(); var events = new List<byte[]>();
        foreach (var original in test.Inputs)
        {
            var prior = CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, inputs, events);
            var input = original with
            {
                AdmittedAt = original.AdmittedAt is { } now ? now + 1 : null,
                Command = original.Command with
                {
                    RoundId = original.Command.RoundId is null ? null : prior.RoundId,
                    SlotId = original.Command.SlotId is null ? null : prior.Slots.Single(s => s.Allocation == original.Command.Allocation).SlotId
                }
            };
            var accepted = CampaignCombatSealedRound.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, inputs, events, input);
            Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition); inputs.Add(input); events.Add(accepted.EventBytes!);
        }
        var retimedRound = test with { Inputs = inputs.ToArray(), Events = events.ToArray() };
        var changedSource = Rebuild(row, (input, _) => input, overrideTest: retimedRound);
        Assert.True(Result(changedSource).Closed);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(changedSource, [], []));
    }

    [Fact]
    public void ForgedBaseStateSuffixAndPostCompletionCommandsRejectWithoutProgress()
    {
        using var bridge = Fixture("combat-result-cycle-finish-v1.json"); using var results = Fixture("combat-result-settlement-v2.json");
        var row = bridge.RootElement.GetProperty("traces")[0]; var source = Source(FindResult(results, Text(row, "caseId")));
        var inputs = ReleaseInputs(row); var events = ReleaseEvents(row); var done = CampaignCombatResultRelease.Replay(source, inputs, events);
        var canonicalBase = CampaignCombatReserveReleaseCodec.SerializeBase(done.Basis, source.Request); var canonicalState = CampaignCombatReserveReleaseCodec.SerializeState(done.Release);
        foreach (var (bytes, isBase) in new[] { (canonicalBase, true), (canonicalState, false) })
        {
            foreach (var path in Leaves(JsonNode.Parse(bytes)))
            {
                var forged = JsonNode.Parse(bytes)!; var parent = forged;
                foreach (var key in path[..^1]) parent = parent is JsonArray a ? a[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)]! : parent[key]!;
                var last = path[^1]; var value = parent is JsonArray values ? values[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : parent[last];
                JsonNode? replacement = value?.GetValueKind() switch
                {
                    JsonValueKind.Number => JsonValue.Create(value.GetValue<long>() + 1),
                    JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!value.GetValue<bool>()),
                    JsonValueKind.String => JsonValue.Create(value.GetValue<string>() + "x"),
                    _ => JsonValue.Create("forged")
                };
                if (parent is JsonArray items) items[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else parent[last] = replacement;
                var raw = JsonSerializer.SerializeToUtf8Bytes(forged);
                if (isBase) Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.ReadBase(raw, source));
                else Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.ReadState(raw, source, inputs, events));
            }
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.ReadState(canonicalState, source, inputs[..1], events[..1]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(source, [.. inputs, inputs[^1]], [.. events, events[^1]]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(source, inputs, events.Reverse().ToArray()));
        var forgedEvent = JsonNode.Parse(events[0])!; forgedEvent["effect"]!["pending"] = new JsonArray(JsonNode.Parse(canonicalBase)!["members"]![0]!["unit"]!.DeepClone());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(source, inputs[..1], [JsonSerializer.SerializeToUtf8Bytes(forgedEvent)]));
        var before = CampaignCombatResultRelease.Replay(source, [], []);
        foreach (var kind in new[] { "choose", "complete-release", "expire", "unavailable", "fallback-step", "repeat", "finish" })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Apply(source, inputs, events, inputs[^1] with { Command = inputs[^1].Command with { Kind = kind } }));
        foreach (var altered in new[] { inputs[0] with { Actor = CampaignOpeningPreambleActor.Axis }, inputs[0] with { AdmittedAt = 0 }, inputs[0] with { ClockAvailable = false }, inputs[0] with { Command = inputs[0].Command with { ExpectedPriorVersion = 0 } } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Apply(source, [], [], altered));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Apply(source, inputs, events, inputs[^1] with { Command = inputs[^1].Command with { ExpectedPriorVersion = done.Release.StateVersion } }));
        Assert.Equal(canonicalState, CampaignCombatReserveReleaseCodec.SerializeState(CampaignCombatResultRelease.Replay(source, inputs, events).Release));
        Assert.Equal("unopened", before.Release.Status);
    }

    [Fact]
    public void RawSyntaxRejectsBeforeMissingSourceAndSourceCollectionsAreOwned()
    {
        using var bridge = Fixture("combat-result-cycle-finish-v1.json"); using var results = Fixture("combat-result-settlement-v2.json");
        var row = bridge.RootElement.GetProperty("traces")[0]; var source = Source(FindResult(results, Text(row, "caseId")));
        var inputs = ReleaseInputs(row); var events = ReleaseEvents(row); var initial = CampaignCombatResultRelease.Replay(source, [], []);
        var baseBytes = CampaignCombatReserveReleaseCodec.SerializeBase(initial.Basis, source.Request); var stateBytes = CampaignCombatReserveReleaseCodec.SerializeState(initial.Release);
        foreach (var (bytes, kind) in new[] { (baseBytes, "base"), (stateBytes, "state"), (events[0], "event") })
        {
            var text = Encoding.UTF8.GetString(bytes);
            var invalid = new[] { Array.Empty<byte>(), new byte[1_048_577], bytes[..^1], Encoding.UTF8.GetBytes(text + "\n"),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"extra\":1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) };
            foreach (var raw in invalid)
            {
                if (kind == "base") Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.ReadBase(raw, null!));
                else if (kind == "state") Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.ReadState(raw, null!, [], []));
                else Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(null!, [inputs[0]], [raw]));
            }
        }
        var test = CombatResolutionTests.Case(FindResult(results, Text(row, "caseId")));
        var resultInputs = source.ResultInputs.ToArray(); var resultEvents = source.ResultEvents.ToArray();
        var owned = NewSource(source.CaseId, test, resultInputs, resultEvents);
        test.Created[0] = 0; test.PredecessorEvents[0][0] = 0; test.Events[0][0] = 0; resultEvents[0][0] = 0; resultInputs[0] = resultInputs[0] with { ClockAvailable = false };
        owned.Created[0] = 0; owned.PredecessorEvents[0][0] = 0; owned.RoundEvents[0][0] = 0; owned.ResultEvents[0][0] = 0;
        Assert.Equal(baseBytes, CampaignCombatReserveReleaseCodec.SerializeBase(CampaignCombatResultRelease.Replay(owned, [], []).Basis, owned.Request));
        Assert.Throws<NotSupportedException>(() => ((IList<CombatResolutionInput>)owned.ResultInputs)[0] = resultInputs[0]);
        var output = CampaignCombatResultRelease.Apply(owned, [], [], inputs[0]); var copy = output.EventBytes!; copy[0] = 0; Assert.Equal(events[0], output.EventBytes);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResultRelease.Replay(null!, [inputs[0]], [new byte[1_048_577]]));
    }

    private static byte[] WorldBytes(CombatResolutionState state)
    { using var document = JsonDocument.Parse(CampaignCombatResolutionCodec.SerializeState(state)); return Encoding.UTF8.GetBytes(document.RootElement.GetProperty("world").GetRawText()); }
    private static CombatResolutionState Result(CombatResultReleaseSource source) => CampaignCombatResolution.ReplayTrustedBoundary(source.Request, source.Created,
        source.Boundary, source.PredecessorInputs, source.PredecessorEvents, source.RoundInputs, source.RoundEvents, source.ResultInputs, source.ResultEvents);
    private static CombatResultReleaseSource Copy(CombatResultReleaseSource source, string? caseId = null,
        IReadOnlyList<CombatResolutionInput>? resultInputs = null, IReadOnlyList<byte[]>? resultEvents = null) => new(caseId ?? source.CaseId, source.Request, source.Created,
        source.Boundary, source.PredecessorInputs, source.PredecessorEvents, source.RoundInputs, source.RoundEvents, resultInputs ?? source.ResultInputs, resultEvents ?? source.ResultEvents);
    private static CombatResultReleaseSource Rebuild(JsonElement row, Func<CombatResolutionInput, CombatResolutionState, CombatResolutionInput> change,
        bool skipUnneededChoice = false, TestCase? overrideTest = null)
    {
        var test = overrideTest ?? CombatResolutionTests.Case(row); var original = Source(row); var inputs = new List<CombatResolutionInput>(); var events = new List<byte[]>();
        foreach (var input in original.ResultInputs)
        {
            var state = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
            if (skipUnneededChoice && input.Command.Kind == "choose" && state.Window is null) continue;
            var rebound = input with
            {
                Command = input.Command with
                {
                    RoundId = state.Context.Committed.RoundId!,
                    CommitmentId = state.Context.Committed.CommitmentId!,
                    ExpectedPriorVersion = input.Command.ExpectedPriorVersion is null ? null : state.StateVersion,
                    DecisionId = input.Command.Kind == "choose" ? state.Window!.DecisionId : null
                }
            };
            rebound = change(rebound, state);
            var result = CampaignCombatResolution.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events, rebound);
            Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition); inputs.Add(rebound); events.Add(result.EventBytes!);
        }
        return NewSource(Text(row, "name"), test, inputs, events);
    }
    private static IEnumerable<string[]> Leaves(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) foreach (var tail in Leaves(pair.Value)) yield return [pair.Key, .. tail];
        else if (node is JsonArray array) for (var index = 0; index < array.Count; index++) foreach (var tail in Leaves(array[index])) yield return [index.ToString(System.Globalization.CultureInfo.InvariantCulture), .. tail];
        else yield return [];
    }
    private static byte[] RehashResult(JsonNode node)
    {
        node.AsObject().Remove("receiptId"); var unsigned = JsonSerializer.SerializeToUtf8Bytes(node);
        node["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", unsigned)[7..]; return JsonSerializer.SerializeToUtf8Bytes(node);
    }

    private static CombatResultReleaseSource Source(JsonElement row)
    {
        var test = CombatResolutionTests.Case(row);
        return NewSource(Text(row, "name"), test,
            row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray(),
            row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray());
    }
    private static CombatResultReleaseSource NewSource(string name, TestCase test, IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events) =>
        new(name, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    private static CombatReleaseInput[] ReleaseInputs(JsonElement row) => row.GetProperty("events").EnumerateArray().Take(2).Select(e =>
    {
        using var native = JsonDocument.Parse(Text(e, "nativeEvent")); var i = native.RootElement.GetProperty("input"); var c = i.GetProperty("command");
        return new CombatReleaseInput(new(c.GetProperty("contractVersion").GetInt32(), Text(c, "kind"), Text(c, "releaseId"),
            c.GetProperty("expectedPriorVersion").ValueKind == JsonValueKind.Null ? null : c.GetProperty("expectedPriorVersion").GetInt64(),
            c.GetProperty("decisionId").GetString()), CampaignOpeningPreambleActor.System,
            i.GetProperty("admittedAt").ValueKind == JsonValueKind.Null ? null : i.GetProperty("admittedAt").GetInt64(), i.GetProperty("clockAvailable").GetBoolean());
    }).ToArray();
    private static byte[][] ReleaseEvents(JsonElement row) => row.GetProperty("events").EnumerateArray().Take(2).Select(e => Encoding.UTF8.GetBytes(Text(e, "nativeEvent"))).ToArray();
    private static JsonElement FindResult(JsonDocument fixture, string name) => fixture.RootElement.GetProperty("traces").EnumerateArray().Single(r => Text(r, "name") == name);
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static string Text(JsonElement root, string field) => root.GetProperty(field).GetString()!;
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
}

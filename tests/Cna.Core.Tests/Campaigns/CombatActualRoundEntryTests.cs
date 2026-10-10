using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRoundEntryTests
{
    public static IEnumerable<object[]> Cases() => Enumerable.Range(0, 34).Select(index => new object[] { index });

    [Theory]
    [MemberData(nameof(Cases))]
    public void NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index];
        var result = CampaignCombatActualRoundEntry.ReplayTrustedSource(Artifact(row.GetProperty("source")), Context(),
            Ledger(row, "trustedSelectionInputs"), Ledger(row, "trustedRoundInputs"), Dependency);
        Assert.Equal(Artifact(row.GetProperty("controls").EnumerateArray().Last()), result.ControlBytes);
        Assert.Equal(Artifact(row.GetProperty("proofs").EnumerateArray().Last()), result.ProofBytes);
        Assert.Equal(row.GetProperty("cancellation").ValueKind != JsonValueKind.Null, result.Control.GetProperty("closed").GetBoolean());
    }

    [Fact]
    public void IndependentOriginalAndRoundLedgerShapeCannotLeakRuntimeExceptions()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0];
        var source = Artifact(row.GetProperty("source")); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs"); var context = Context();
        var broken = old.Select(x => x.ToArray()).ToArray(); broken[0] = null!;
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, broken, round, Dependency), 4);
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, null!, round, Dependency), 4);
        broken = round.Select(x => x.ToArray()).ToArray(); broken[0] = null!;
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, broken, Dependency), 1);
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, null!, Dependency), 1);
    }
    private static void Reject(Action action, int code) => Assert.StartsWith($"CMB-ARE-{code:000}", Assert.Throws<JsonException>(action).Message);

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryOriginalCutSuffixAndExactRetryMatchesIndependentLiteralBytes(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var context = Context();
        var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var events = Ledger(row, "events"); var controls = Ledger(row, "controls"); var proofs = Ledger(row, "proofs");
        var dependencies = DependencyLookup();
        for (var cut = 0; cut <= events.Length; cut++)
        {
            var source = Cut(packet, cut); var retained = round.Take(cut).ToArray();
            var result = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, retained, dependencies);
            Assert.Equal(controls[cut], result.ControlBytes); Assert.Equal(proofs[cut], result.ProofBytes);
            Assert.Equal(source, CampaignCombatActualRoundEntryCodec.ReadSource(source, context, old, retained, dependencies));
            Assert.Equal(proofs[cut], CampaignCombatActualRoundEntryCodec.ReadProof(proofs[cut], source, context, old, retained, dependencies).ProofBytes);
            Assert.Equal(controls[cut], CampaignCombatActualRoundEntryCodec.ReadControl(controls[cut], source, context, old, retained, dependencies).ControlBytes);
            Assert.Equal(20 + cut, result.Control.GetProperty("stateVersion").GetInt64());
            var boundary = result.Proof.GetProperty("base").GetProperty("actualSelectionProof").GetProperty("boundary");
            Assert.Equal(boundary.GetProperty("world").GetRawText(), result.Control.GetProperty("world").GetRawText());
            Assert.Equal(boundary.GetProperty("randomState").GetRawText(), result.Control.GetProperty("randomState").GetRawText());
            Assert.Equal(0, result.Control.GetProperty("attackHistory").GetArrayLength()); Assert.Equal(0, result.Control.GetProperty("targetUses").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, result.Control.GetProperty("commitmentId").ValueKind);
            var suffixSource = JsonNode.Parse(source)!; var suffixLedger = retained.ToList();
            for (var next = cut; next < events.Length; next++)
            {
                var after = CampaignCombatActualRoundEntry.ApplyTrustedSource(Encode(suffixSource), context, old, suffixLedger, round[next], dependencies, admissionEnabled: cut == 0);
                Assert.Equal(events[next], after.EventBytes); Assert.False(after.Duplicate);
                Assert.Equal(controls[next + 1], after.ControlBytes); Assert.Equal(proofs[next + 1], after.ProofBytes);
                suffixSource["roundEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(after.EventBytes!)); suffixLedger.Add(round[next]);
            }
            Assert.Equal(Artifact(row.GetProperty("source")), Encode(suffixSource));
            for (var previous = 0; previous < cut; previous++)
                for (var mode = 0; mode < 4; mode++)
                {
                    var retry = JsonNode.Parse(round[previous])!;
                    if (mode == 1) { retry["admittedAt"] = null; retry["clockAvailable"] = false; }
                    if (mode == 2) { retry["admittedAt"] = 253402300799999L; retry["clockAvailable"] = true; }
                    if (mode == 3) retry["clockAvailable"] = !retry["clockAvailable"]!.GetValue<bool>();
                    var recovered = CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(retry), dependencies, admissionEnabled: false);
                    Assert.True(recovered.Duplicate); Assert.Equal(events[previous], recovered.EventBytes); Assert.Equal(controls[cut], recovered.ControlBytes); Assert.Equal(proofs[cut], recovered.ProofBytes);
                    if (mode != 0) continue;
                    retry["admittedAt"] = true; Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(retry), dependencies), 1);
                    retry = JsonNode.Parse(round[previous])!; retry["actor"] = retry["actor"]!.GetValue<string>() == "axis" ? "commonwealth" : "axis";
                    Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(retry), dependencies), 4);
                }
            if (result.Control.GetProperty("closed").GetBoolean() || result.Control.GetProperty("status").GetString() == "prepared")
                foreach (var kind in new[] { "expire-round", "controller-unavailable" })
                {
                    var timer = Input(result, kind); timer["clockAvailable"] = false;
                    var noop = CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(timer), dependencies, false);
                    var acceptedTimer = Array.FindIndex(round.Take(cut).ToArray(), x => JsonNode.DeepEquals(JsonNode.Parse(x)!["command"], timer["command"]));
                    if (acceptedTimer >= 0) { Assert.True(noop.Duplicate); Assert.Equal(events[acceptedTimer], noop.EventBytes); Assert.NotNull(noop.ReceiptId); }
                    else { Assert.Null(noop.EventBytes); Assert.Null(noop.ReceiptId); }
                    Assert.Equal(result.ControlBytes, noop.ControlBytes);
                }
        }
    }

    [Fact]
    public void AllDependencyPinsRejectBeforeMalformedSourceExactRetryAndReadbacks()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context();
        var source = Artifact(row.GetProperty("source")); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var pins = fixture.RootElement.GetProperty("originalSourceHashes").EnumerateObject().Concat(fixture.RootElement.GetProperty("additionalSourceHashes").EnumerateObject()).ToArray();
        Assert.Equal(57, pins.Length); var original = DependencyLookup();
        foreach (var pin in pins)
            foreach (var missing in new[] { false, true })
            {
                byte[] Lookup(string path)
                {
                    if (path != pin.Name) return original(path);
                    if (missing) throw new KeyNotFoundException(path);
                    return [.. original(path), (byte)' '];
                }
                Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, round, Lookup), 9);
                Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, round, round[0], Lookup, false), 9);
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource([], context, old, round, Lookup), 9);
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadControl(Artifact(row.GetProperty("controls").EnumerateArray().Last()), source, context, old, round, Lookup), 9);
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadProof(Artifact(row.GetProperty("proofs").EnumerateArray().Last()), source, context, old, round, Lookup), 9);
            }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentIsRecheckedAtConsumptionAfterTwoPassingPreflights(bool missing)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context();
        var source = Cut(JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(), 4); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var original = DependencyLookup(); var calls = 0;
        byte[] Lookup(string path)
        {
            if (path != "docs/specs/fixtures/combat-content-v7.canonical.json" || ++calls <= 2) return original(path);
            if (missing) throw new KeyNotFoundException(path);
            return [.. original(path), (byte)' '];
        }
        Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, round[..4], round[4], Lookup), 9);
        Assert.Equal(3, calls);
    }

    [Fact]
    public void ResultsAndCanonicalReadersOwnEveryReturnedBuffer()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context(); var dependencies = DependencyLookup();
        var source = Artifact(row.GetProperty("source")); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var result = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, round, dependencies); var expected = result.ProofBytes;
        Array.Fill(source, (byte)0); Array.Fill(result.ProofBytes, (byte)0); Array.Fill(result.ControlBytes, (byte)0); foreach (var input in old.Concat(round)) Array.Fill(input, (byte)0);
        Assert.Equal(expected, result.ProofBytes);
        source = Artifact(row.GetProperty("source")); old = Ledger(row, "trustedSelectionInputs"); round = Ledger(row, "trustedRoundInputs");
        var returned = CampaignCombatActualRoundEntryCodec.ReadSource(source, context, old, round, dependencies); Array.Fill(returned, (byte)0); Assert.Equal(Artifact(row.GetProperty("source")), source);
        var replay = CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, round, round[0], dependencies, false); var retainedEvent = replay.EventBytes!; Array.Fill(replay.EventBytes!, (byte)0); Assert.Equal(retainedEvent, replay.EventBytes);
        Assert.Equal(expected, CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, round, dependencies).ProofBytes);
    }

    [Fact]
    public void PublicRoundEffectShapeAndTagConflictsRetainReviewRegression()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context(); var dependencies = DependencyLookup();
        var source = JsonNode.Parse(Artifact(row.GetProperty("source")))!; var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var controls = Ledger(row, "controls"); var proofs = Ledger(row, "proofs");
        foreach (var effect in new JsonNode?[] { null, new JsonArray(), new JsonObject(), new JsonObject { ["kind"] = true }, new JsonObject { ["kind"] = 1 }, new JsonObject { ["kind"] = "unknown-effect" } })
            foreach (var conflict in new[] { "none", "version", "receipt" })
            {
                var forged = source.DeepClone(); var e = JsonNode.Parse(forged["roundEventCanonicalUtf8"]![0]!.GetValue<string>())!; e["effect"] = effect?.DeepClone();
                if (conflict == "version") e["contractVersion"] = 99; if (conflict == "receipt") e["receiptId"] = "arc." + new string('0', 64);
                forged["roundEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Encode(e)); var bytes = Encode(forged); var code = effect is JsonObject obj && obj["kind"]?.GetValueKind() == JsonValueKind.String ? 3 : 1;
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(bytes, context, old, round, dependencies), code);
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadControl(controls[^1], bytes, context, old, round, dependencies), code);
                Reject(() => CampaignCombatActualRoundEntryCodec.ReadProof(proofs[^1], bytes, context, old, round, dependencies), code);
            }
    }

    private static Func<string, byte[]> DependencyLookup()
    {
        using var fixture = Fixture();
        var pins = fixture.RootElement.GetProperty("originalSourceHashes").EnumerateObject().Concat(fixture.RootElement.GetProperty("additionalSourceHashes").EnumerateObject());
        var retained = pins.ToDictionary(x => x.Name, x => Dependency(x.Name), StringComparer.Ordinal); return path => retained[path].ToArray();
    }
    private static byte[] Encode(JsonNode value) => CampaignCombatPositiveEntryCodec.Encode(value);
    private static byte[] Cut(JsonObject source, int count)
    { var copy = source.DeepClone(); copy["roundEventCanonicalUtf8"] = new JsonArray(copy["roundEventCanonicalUtf8"]!.AsArray().Take(count).Select(x => x!.DeepClone()).ToArray()); return Encode(copy); }
    private static JsonObject Input(CombatActualRoundEntryResult result, string kind, string? role = null, long? now = null, bool available = true)
    {
        var s = JsonNode.Parse(result.ControlBytes)!; var boundary = result.Proof.GetProperty("base").GetProperty("actualSelectionProof").GetProperty("boundary");
        var route = Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != boundary.GetProperty("position").GetProperty("positionId").GetString()).Take(7).ToArray();
        var slot = s["slots"]!.AsArray().FirstOrDefault(x => x!["role"]!.GetValue<string>() == role);
        var cmd = new JsonObject
        {
            ["contractVersion"] = 1,
            ["kind"] = kind,
            ["clockConfigurationHash"] = s["clockConfigurationHash"]!.DeepClone(),
            ["segmentId"] = s["segmentId"]!.DeepClone(),
            ["roundId"] = kind == "open-round" ? null : s["roundId"]?.DeepClone(),
            ["slotId"] = kind == "seal-choice" ? slot?["slotId"]?.DeepClone() ?? JsonValue.Create("foreign") : null,
            ["expectedPriorVersion"] = kind is "open-round" or "complete-step" ? s["stateVersion"]!.DeepClone() : null,
            ["fromPositionId"] = kind == "complete-step" ? route[Math.Min(5, s["stepIndex"]!.GetValue<int>())].PositionId : null,
            ["allocation"] = kind == "seal-choice" ? slot?["allocation"]?.DeepClone() : null,
        };
        return new() { ["command"] = cmd, ["actor"] = kind == "seal-choice" && slot is not null ? slot["owner"]!.DeepClone() : JsonValue.Create("system"), ["admittedAt"] = now, ["clockAvailable"] = available };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryResignedEventInputProofBaseAndControlLeafRejects(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var context = Context(); var dependencies = DependencyLookup();
        var source = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var events = Ledger(row, "events");
        for (var position = 0; position < events.Length; position++)
        {
            var e = JsonNode.Parse(events[position])!;
            foreach (var path in Leaves(e))
            {
                var changed = Changed(e, path); if (path[0] != "receiptId") Resign(changed);
                var forged = JsonNode.Parse(Cut(source, position + 1))!; forged["roundEventCanonicalUtf8"]![position] = Encoding.ASCII.GetString(Encode(changed));
                AnyReject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(forged), context, old, round.Take(position + 1).ToArray(), dependencies));
            }
            foreach (var path in Leaves(JsonNode.Parse(round[position])))
            {
                var retained = round.Take(position + 1).Select(x => x.ToArray()).ToArray(); retained[position] = Encode(Changed(JsonNode.Parse(round[position])!, path));
                AnyReject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Cut(source, position + 1), context, old, retained, dependencies));
            }
        }
        var sourceBytes = Encode(source); var proof = JsonNode.Parse(Artifact(row.GetProperty("proofs").EnumerateArray().Last()))!;
        foreach (var path in Leaves(proof))
        {
            var forged = Changed(proof, path);
            if (path[0] == "base")
            {
                byte[] basis;
                try { basis = CampaignCombatActualRoundEntryCodec.Canonical(forged["base"]!, "Base"); }
                catch (JsonException) { basis = Encode(forged["base"]!); }
                forged["control"]!["baseHash"] = CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-round-entry.base.v1", basis);
            }
            AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadProof(Encode(forged), sourceBytes, context, old, round, dependencies));
        }
        var control = JsonNode.Parse(Artifact(row.GetProperty("controls").EnumerateArray().Last()))!;
        foreach (var path in Leaves(control)) AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadControl(Encode(Changed(control, path)), sourceBytes, context, old, round, dependencies));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void FullOriginalCreationHistoryAndSelectionCannotBeForgedOrTruncated(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var context = Context(); var dependencies = DependencyLookup();
        var source = JsonNode.Parse(Cut(JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(), 0))!; var old = Ledger(row, "trustedSelectionInputs");
        var selection = JsonNode.Parse(source["actualSelectionSourceCanonicalUtf8"]!.GetValue<string>())!;
        var original = JsonNode.Parse(selection["positiveEntrySourceCanonicalUtf8"]!.GetValue<string>())!;
        foreach (var field in original.AsObject().Where(x => x.Key.EndsWith("CanonicalUtf8", StringComparison.Ordinal)))
        {
            var records = field.Value is JsonArray array ? array.Select(x => x!.GetValue<string>()).ToArray() : [field.Value!.GetValue<string>()];
            for (var position = 0; position < records.Length; position++)
                foreach (var path in Leaves(JsonNode.Parse(records[position])))
                {
                    var entry = original.DeepClone(); var altered = Encoding.ASCII.GetString(Encode(Changed(JsonNode.Parse(records[position])!, path)));
                    if (entry[field.Key] is JsonArray) entry[field.Key]![position] = altered; else entry[field.Key] = altered;
                    var retained = selection.DeepClone(); retained["positiveEntrySourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(entry));
                    var forged = source.DeepClone(); forged["actualSelectionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(retained));
                    Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(forged), context, old, [], dependencies), 4);
                }
        }
        var selectedEvents = selection["selectionEventCanonicalUtf8"]!.AsArray();
        for (var position = 0; position < selectedEvents.Count; position++)
            foreach (var path in Leaves(JsonNode.Parse(selectedEvents[position]!.GetValue<string>())))
            {
                var retained = selection.DeepClone(); retained["selectionEventCanonicalUtf8"]![position] = Encoding.ASCII.GetString(Encode(Changed(JsonNode.Parse(selectedEvents[position]!.GetValue<string>())!, path)));
                var forged = source.DeepClone(); forged["actualSelectionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(retained));
                Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(forged), context, old, [], dependencies), 4);
            }
        foreach (var count in Enumerable.Range(0, 7))
        {
            var earlier = selection.DeepClone(); earlier["selectionEventCanonicalUtf8"] = new JsonArray(selectedEvents.Take(count).Select(x => x!.DeepClone()).ToArray());
            var forged = source.DeepClone(); forged["actualSelectionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(earlier));
            Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(forged), context, old.Take(count).ToArray(), [], dependencies), 4);
        }
        foreach (var position in Enumerable.Range(0, old.Length))
            foreach (var field in new[] { "actor", "admittedAt" })
            {
                var replacement = old.Select(x => x.ToArray()).ToArray(); var input = JsonNode.Parse(old[position])!;
                if (field == "actor") input[field] = input[field]!.GetValue<string>() == "axis" ? "commonwealth" : "axis"; else input[field] = 1999;
                replacement[position] = Encode(input); Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(source), context, replacement, [], dependencies), 4);
            }
        foreach (var replacement in new[] { old[..^1], old.Concat([old[0]]).ToArray(), old.Reverse().ToArray() })
            Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(source), context, replacement, [], dependencies), 4);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CanonicalGrammarCapacitiesAndOrderedHistoriesRejectIndependentForgeries(int index)
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[index]; var context = Context(); var dependencies = DependencyLookup();
        var source = Artifact(row.GetProperty("source")); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var proof = Artifact(row.GetProperty("proofs").EnumerateArray().Last()); var control = Artifact(row.GetProperty("controls").EnumerateArray().Last());
        foreach (var bad in Malformed(source)) AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadSource(bad, context, old, round, dependencies));
        foreach (var bad in Malformed(proof)) AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadProof(bad, source, context, old, round, dependencies));
        foreach (var bad in Malformed(control)) AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadControl(bad, source, context, old, round, dependencies));
        foreach (var input in round)
            foreach (var bad in Malformed(input)) AnyReject(() => CampaignCombatActualRoundEntryCodec.ReadInput(bad));
        foreach (var e in Ledger(row, "events"))
            foreach (var bad in Malformed(e))
            {
                var packet = JsonNode.Parse(source)!; var texts = packet["roundEventCanonicalUtf8"]!.AsArray(); var position = Array.FindIndex(Ledger(row, "events"), x => x.AsSpan().SequenceEqual(e));
                texts[position] = Encoding.UTF8.GetString(bad); AnyReject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(packet), context, old, round, dependencies));
            }
        foreach (var field in new[] { "observation", "publicAction", "provider", "transport", "hiddenSeal", "snapshot", "result", "settlement", "refund", "reseed" })
        {
            var packet = JsonNode.Parse(source)!; packet[field] = null; Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Encode(packet), context, old, round, dependencies), 1);
            var p = JsonNode.Parse(proof)!; p[field] = null; Reject(() => CampaignCombatActualRoundEntryCodec.ReadProof(Encode(p), source, context, old, round, dependencies), 1);
            var c = JsonNode.Parse(control)!; c[field] = null; Reject(() => CampaignCombatActualRoundEntryCodec.ReadControl(Encode(c), source, context, old, round, dependencies), 1);
        }
        foreach (var replacement in new[] { round[..^1], round.Concat([round[0]]).ToArray(), round.Reverse().ToArray() }) AnyReject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, replacement, dependencies));
        foreach (var mode in new[] { "missing", "duplicate", "reverse" })
        {
            var packet = JsonNode.Parse(source)!; var events = packet["roundEventCanonicalUtf8"]!.AsArray();
            if (mode == "missing") events.RemoveAt(0); else if (mode == "duplicate") events.Add(events[0]!.DeepClone()); else packet["roundEventCanonicalUtf8"] = new JsonArray(events.Reverse().Select(x => x!.DeepClone()).ToArray());
            AnyReject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(packet), context, old, round, dependencies));
        }
        var oversized = JsonNode.Parse(source)!; oversized["roundEventCanonicalUtf8"] = new JsonArray(Enumerable.Repeat("{}", 17).Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(oversized), context, old, Enumerable.Repeat(round[0], 17).ToArray(), dependencies), 1);
        oversized = JsonNode.Parse(source)!; oversized["actualSelectionSourceCanonicalUtf8"] = new string('x', 1_048_576);
        Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Encode(oversized), context, old, round, dependencies), 1);
        var reversed = JsonNode.Parse(control)!; reversed["stepReceipts"] = new JsonArray(reversed["stepReceipts"]!.AsArray().Reverse().Select(x => x!.DeepClone()).ToArray());
        Reject(() => CampaignCombatActualRoundEntryCodec.ReadControl(Encode(reversed), source, context, old, round, dependencies), 6);
    }

    private static void AnyReject(Action action) => Assert.StartsWith("CMB-ARE-", Assert.Throws<JsonException>(action).Message);
    private static void Resign(JsonNode value)
    { var unsigned = value.DeepClone().AsObject(); unsigned.Remove("receiptId"); value["receiptId"] = "arc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-round-entry.receipt.v1", Encode(unsigned))[7..]; }
    private static IEnumerable<string[]> Leaves(JsonNode? node, string[]? path = null)
    {
        path ??= [];
        if (node is JsonObject obj) { foreach (var field in obj) foreach (var leaf in Leaves(field.Value, [.. path, field.Key])) yield return leaf; }
        else if (node is JsonArray array && array.Count > 0) { for (var i = 0; i < array.Count; i++) foreach (var leaf in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf; }
        else yield return path;
    }
    private static JsonNode Changed(JsonNode original, string[] path)
    {
        var copy = original.DeepClone(); var target = copy;
        foreach (var part in path[..^1]) target = target is JsonArray a ? a[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : target[part]!;
        var last = path[^1]; var value = target is JsonArray array ? array[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : target[last]; JsonNode? replacement;
        if (value is null) replacement = JsonValue.Create(1);
        else if (value is JsonArray) replacement = new JsonArray((JsonNode?)null);
        else if (value.GetValueKind() is JsonValueKind.True or JsonValueKind.False) replacement = JsonValue.Create(!value.GetValue<bool>());
        else if (value.GetValueKind() == JsonValueKind.Number) replacement = JsonValue.Create(JsonSerializer.SerializeToElement(value).GetInt64() + 1);
        else
        {
            var text = value.GetValue<string>(); var hex = text.Length == 64 && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
            replacement = JsonValue.Create(text.StartsWith("sha256:", StringComparison.Ordinal) ? "sha256:" + new string(text == "sha256:" + new string('0', 64) ? '1' : '0', 64) : hex ? new string(text == new string('0', 64) ? '1' : '0', 64) : "tampered");
        }
        if (target is JsonArray list) list[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else target[last] = replacement; return copy;
    }
    private static IEnumerable<byte[]> Malformed(byte[] bytes)
    {
        yield return [(byte)' ', .. bytes]; yield return [.. bytes, (byte)' ']; yield return [0xef, 0xbb, 0xbf, .. bytes]; yield return [.. bytes, .. bytes];
        var value = JsonNode.Parse(bytes)!.AsObject(); yield return Encode(new JsonObject(value.Reverse().Select(x => new KeyValuePair<string, JsonNode?>(x.Key, x.Value?.DeepClone()))));
        var text = Encoding.ASCII.GetString(bytes);
        yield return Encoding.ASCII.GetBytes(text.Replace("{\"contractVersion\":1,", "{\"contractVersion\":1.0,", StringComparison.Ordinal));
        yield return Encoding.ASCII.GetBytes(text.Replace("{\"contractVersion\":1,", "{\"contractVersion\":1,\"contractVersion\":1,", StringComparison.Ordinal));
        yield return Encoding.ASCII.GetBytes(text.Replace("\"contractVersion\"", "\"\\u0063ontractVersion\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("axis", "attacker")]
    [InlineData("axis", "defender")]
    [InlineData("commonwealth", "attacker")]
    [InlineData("commonwealth", "defender")]
    public void OwnClockOutcomesAndWitnessDoNotRevealOpposingSeal(string owner, string role)
    {
        using var fixture = Fixture(); var other = role == "attacker" ? "defender" : "attacker";
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().First(x => x.GetProperty("owner").GetString() == owner && x.GetProperty("cancellation").ValueKind == JsonValueKind.Null && x.GetProperty("firstSeal").GetString() == other);
        var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs"); var context = Context(); var dependencies = DependencyLookup();
        var states = Enumerable.Range(1, 2).Select(cut => CampaignCombatActualRoundEntry.ReplayTrustedSource(Cut(packet, cut), context, old, round.Take(cut).ToArray(), dependencies)).ToArray();
        Assert.Equal(Witness(states[0], role), Witness(states[1], role));
        var count = 0;
        foreach (var now in new long?[] { null, 0, 2999, 3000, 3500, 3999, 4000, 4001, 32999, 33000, 33001, 253402300799999 })
            foreach (var available in new[] { true, false })
            {
                var outcomes = new List<string>();
                for (var cut = 1; cut <= 2; cut++)
                {
                    var input = Input(states[cut - 1], "seal-choice", role, now, available); var source = Cut(packet, cut); var retained = round.Take(cut).ToArray();
                    var expected = !available || now is null || now < 3000 ? "cancelled" : now < 33000 ? "accepted" : "rejected";
                    if (expected == "rejected") Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(input), dependencies), 5);
                    else
                    {
                        var result = CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(input), dependencies);
                        Assert.Equal(expected == "cancelled" ? "cancelled" : cut == 1 ? "collecting" : "prepared", result.Control.GetProperty("status").GetString());
                        var own = result.Control.GetProperty("slots").EnumerateArray().Single(x => x.GetProperty("role").GetString() == role);
                        Assert.Equal(expected == "accepted", own.GetProperty("sealedReceiptId").ValueKind != JsonValueKind.Null);
                        Assert.Equal(states[cut - 1].Control.GetProperty("world").GetRawText(), result.Control.GetProperty("world").GetRawText());
                        Assert.Equal(states[cut - 1].Control.GetProperty("randomState").GetRawText(), result.Control.GetProperty("randomState").GetRawText());
                    }
                    outcomes.Add(expected); count++;
                    foreach (var (field, value, code) in new (string, JsonNode, int)[]
                    { ("slotId", JsonValue.Create("foreign")!, 4), ("segmentId", JsonValue.Create("foreign")!, 4), ("clockConfigurationHash", JsonValue.Create("sha256:" + new string('0', 64))!, 4), ("expectedPriorVersion", JsonValue.Create(99)!, 3) })
                    {
                        var malformed = input.DeepClone(); malformed["command"]![field] = value.DeepClone();
                        Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(malformed), dependencies), code);
                    }
                }
                Assert.Equal(outcomes[0], outcomes[1]);
            }
        Assert.Equal(48, count);
    }
    // Test-only declassifier, with no observation/action/seat admission API.
    private static byte[] Witness(CombatActualRoundEntryResult result, string role)
    {
        var state = JsonNode.Parse(result.ControlBytes)!; var slot = state["slots"]!.AsArray().Single(x => x!["role"]!.GetValue<string>() == role)!;
        return Encode(new JsonObject
        {
            ["owner"] = slot["owner"]!.DeepClone(),
            ["role"] = role,
            ["allocation"] = slot["allocation"]!.DeepClone(),
            ["sealed"] = slot["sealedReceiptId"] is not null,
            ["opening"] = state["timing"]!["openedAtUnixMilliseconds"]!.DeepClone(),
            ["deadline"] = state["timing"]!["deadlineUnixMilliseconds"]!.DeepClone()
        });
    }

    [Fact]
    public void ConflictingErrorVectorsAndPrimitiveOverlaysMatchDocumentedOrder()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs"); var context = Context(); var dependencies = DependencyLookup();
        var states = Enumerable.Range(0, 6).Select(cut => CampaignCombatActualRoundEntry.ReplayTrustedSource(Cut(packet, cut), context, old, round.Take(cut).ToArray(), dependencies)).ToArray();
        var cases = new List<(JsonObject Input, int Code, int Cut)>();
        var live = Input(states[1], "seal-choice", "attacker", available: false);
        foreach (var (field, value, code) in new (string, JsonNode, int)[] { ("contractVersion", JsonValue.Create(2)!, 3), ("expectedPriorVersion", JsonValue.Create(0)!, 3), ("segmentId", JsonValue.Create("foreign")!, 4), ("slotId", JsonValue.Create("foreign")!, 4) })
        { var input = live.DeepClone().AsObject(); input["command"]![field] = value.DeepClone(); cases.Add((input, code, 1)); }
        var bad = live.DeepClone().AsObject(); bad["command"]!["segmentId"] = "foreign"; bad["command"]!["expectedPriorVersion"] = 20; cases.Add((bad, 3, 1));
        bad = live.DeepClone().AsObject(); bad["command"]!["contractVersion"] = 2; bad["admittedAt"] = true; cases.Add((bad, 1, 1));
        bad = live.DeepClone().AsObject(); bad["actor"] = "system"; cases.Add((bad, 4, 1));
        bad = live.DeepClone().AsObject(); bad["command"]!["allocation"]!["committedToe"] = 9; cases.Add((bad, 4, 1));
        bad = live.DeepClone().AsObject(); bad["command"]!["roundId"] = "foreign"; cases.Add((bad, 4, 1));
        foreach (var cut in new[] { 3, 4, 5 })
        {
            bad = Input(states[cut], "complete-step", now: 33000); bad["command"]!["expectedPriorVersion"] = 20 + cut - 1; cases.Add((bad, 6, cut));
            bad = Input(states[cut], "complete-step", now: 33000); bad["command"]!["fromPositionId"] = "foreign"; cases.Add((bad, 6, cut));
        }
        cases.Add((Input(states[5], "complete-step", now: 33000), 5, 5)); Assert.Equal(16, cases.Count);
        foreach (var vector in cases)
        {
            var source = Cut(packet, vector.Cut); var retained = round.Take(vector.Cut).ToArray();
            Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(vector.Input), dependencies), vector.Code);
            foreach (var value in new JsonNode[] { JsonValue.Create(true)!, JsonValue.Create(3.5)!, JsonValue.Create("3000")!, JsonValue.Create(253402300800000L)!, JsonValue.Create(-1)! })
            {
                bad = vector.Input.DeepClone().AsObject(); bad["admittedAt"] = value.DeepClone(); var code = value.GetValueKind() == JsonValueKind.Number && value.ToJsonString() != "3.5" ? 2 : 1;
                Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, retained, Encode(bad), dependencies), code);
            }
        }
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void IndependentLifecycleClockTableCoversEveryCommandAtEveryRepresentative(string owner)
    {
        using var fixture = Fixture(); var rows = fixture.RootElement.GetProperty("cases").EnumerateArray().Where(x => x.GetProperty("owner").GetString() == owner).ToArray();
        var positive = rows.First(x => x.GetProperty("cancellation").ValueKind == JsonValueKind.Null && x.GetProperty("firstSeal").GetString() == "attacker");
        var other = rows.First(x => x.GetProperty("cancellation").ValueKind == JsonValueKind.Null && x.GetProperty("firstSeal").GetString() == "defender");
        var cancelled = rows.First(x => x.GetProperty("cancellation").ValueKind == JsonValueKind.Array && x.GetProperty("cancellation")[0].GetString() == "empty" && x.GetProperty("cancellation")[1].GetString() == "deadline");
        var representatives = Enumerable.Range(0, 6).Select(cut => (Row: positive, Cut: cut)).Concat([(other, 2)]).Concat(Enumerable.Range(2, 4).Select(cut => (cancelled, cut))).ToArray();
        var context = Context(); var dependencies = DependencyLookup(); var count = 0;
        foreach (var (row, cut) in representatives)
        {
            var source = Cut(JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(), cut); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs").Take(cut).ToArray();
            var prior = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, round, dependencies);
            foreach (var kind in new[] { "open-round", "seal-choice", "expire-round", "controller-unavailable", "complete-step", "commit-attack", "resolve-attack", "release-reserves", "repeat-cycle", "finish-cycle", "open-later-stage", "consume-lineage", "refund", "reseed" })
                foreach (var role in kind == "seal-choice" && prior.Control.GetProperty("slots").GetArrayLength() > 0 ? new string?[] { "attacker", "defender" } : [null])
                    foreach (var now in new long?[] { null, 0, 2999, 3000, 3500, 32999, 33000, 33001, 253402300799999 })
                        foreach (var available in new[] { true, false })
                        {
                            var input = Input(prior, kind, role, now, available); var expected = ExpectedOutcome(prior.Control, input); count++;
                            if (expected.StartsWith("error", StringComparison.Ordinal)) Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, round, Encode(input), dependencies), int.Parse(expected[5..], System.Globalization.CultureInfo.InvariantCulture));
                            else
                            {
                                var after = CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, round, Encode(input), dependencies);
                                Assert.Equal(expected == "duplicate", after.Duplicate);
                                if (expected == "accepted") { Assert.NotNull(after.EventBytes); Assert.NotNull(after.ReceiptId); Assert.Equal(prior.Control.GetProperty("stateVersion").GetInt64() + 1, after.Control.GetProperty("stateVersion").GetInt64()); }
                                else { Assert.Equal(prior.ControlBytes, after.ControlBytes); Assert.Equal(prior.ProofBytes, after.ProofBytes); if (expected == "noop") { Assert.Null(after.EventBytes); Assert.Null(after.ReceiptId); } else { Assert.NotNull(after.EventBytes); Assert.NotNull(after.ReceiptId); } }
                            }
                        }
        }
        Assert.Equal(2952, count);
    }
    private static string ExpectedOutcome(JsonElement state, JsonObject input)
    {
        var command = input["command"]!; var kind = command["kind"]!.GetValue<string>(); var status = state.GetProperty("status").GetString(); var now = input["admittedAt"]?.GetValue<long>(); var available = input["clockAvailable"]!.GetValue<bool>();
        if (kind != "open-round" && state.GetProperty("roundId").ValueKind == JsonValueKind.Null || kind == "seal-choice" && state.GetProperty("slots").GetArrayLength() == 0) return "error3";
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatActualRoundEntryCodec.Canonical(command, "RoundCommand"));
        if (state.GetProperty("receipts").EnumerateArray().Any(x => x.GetProperty("commandHash").GetString() == hash && x.GetProperty("actor").GetString() == input["actor"]!.GetValue<string>())) return "duplicate";
        if (kind is "expire-round" or "controller-unavailable" && status != "collecting") return "noop";
        if (state.GetProperty("closed").GetBoolean()) return "error6";
        return kind switch
        {
            "open-round" => status != "unopened" ? "error6" : !available || now is null || now > 253402300769999 ? "error5" : "accepted",
            "seal-choice" => status != "collecting" ? "error6" : !available || now is null || now < 3000 ? "accepted" : now >= 33000 ? "error5" : "accepted",
            "expire-round" => available && now is >= 3000 and < 33000 ? "noop" : "accepted",
            "controller-unavailable" => "accepted",
            "complete-step" => status is not ("prepared" or "cancelled") ? "error6" : now is not null || !available ? "error5" : status == "prepared" && state.GetProperty("stepIndex").GetInt32() == 5 ? "error7" : "accepted",
            _ => now is not null || !available ? "error5" : "error7",
        };
    }

    [Fact]
    public void OpeningClockContinuationAndPrimitiveBoundariesRemainClosed()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context(); var dependencies = DependencyLookup();
        var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(); var old = Ledger(row, "trustedSelectionInputs"); var round = Ledger(row, "trustedRoundInputs");
        var source = Cut(packet, 0); var initial = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, old, [], dependencies);
        foreach (var now in new long?[] { null, 253402300770000 })
        { var input = Input(initial, "open-round", now: now); Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], Encode(input), dependencies), 5); }
        var last = Input(initial, "open-round", now: 253402300769999);
        Assert.Equal(253402300799999, CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], Encode(last), dependencies).Control.GetProperty("timing").GetProperty("deadlineUnixMilliseconds").GetInt64());
        var disabled = Input(initial, "open-round", available: false);
        Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], Encode(disabled), dependencies, false), 7);
        foreach (var (field, value, code) in new (string, JsonNode, int)[] { ("contractVersion", JsonValue.Create(2)!, 3), ("segmentId", JsonValue.Create("foreign")!, 4), ("expectedPriorVersion", JsonValue.Create(19)!, 6) })
        { var malformed = disabled.DeepClone(); malformed["command"]![field] = value.DeepClone(); Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], Encode(malformed), dependencies, false), code); }
        foreach (var (value, code) in new (JsonNode, int)[] { (JsonValue.Create(true)!, 1), (JsonValue.Create(3.5)!, 1), (JsonValue.Create("3000")!, 1), (JsonValue.Create(-1)!, 2), (JsonValue.Create(253402300800000L)!, 2) })
        { var malformed = last.DeepClone(); malformed["admittedAt"] = value.DeepClone(); malformed["command"]!["segmentId"] = "foreign"; Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], Encode(malformed), dependencies), code); }
        foreach (var (field, maximum) in new[] { ("contractVersion", (long)int.MaxValue), ("expectedPriorVersion", long.MaxValue) })
            foreach (var value in new JsonNode[] { JsonValue.Create(-1)!, JsonValue.Create((ulong)maximum + 1)!, JsonValue.Create(true)!, JsonValue.Create(1.0)!, JsonValue.Create("23")! })
            {
                var malformed = last.DeepClone(); malformed["command"]![field] = value.DeepClone(); malformed["command"]!["segmentId"] = "foreign"; malformed["clockAvailable"] = false;
                // A serialized 1.0 is a floating spelling and shape error; integers beyond bounds are range errors.
                var bytes = Encode(malformed); if (value.GetValueKind() == JsonValueKind.Number && value.ToJsonString() == "1") bytes = Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(bytes).Replace($"\"{field}\":1", $"\"{field}\":1.0", StringComparison.Ordinal));
                var code = value.GetValueKind() == JsonValueKind.Number && value.ToJsonString() != "1" ? 2 : 1;
                Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(source, context, old, [], bytes, dependencies), code);
            }
        var brokenConfiguration = JsonNode.Parse(source)!; brokenConfiguration["clockConfiguration"]!["decisionBudgetMilliseconds"] = 30001;
        Reject(() => CampaignCombatActualRoundEntry.ReplayTrustedSource(Encode(brokenConfiguration), context, old, [], dependencies), 4);
        foreach (var cut in Enumerable.Range(1, 5))
        {
            var currentSource = Cut(packet, cut); var retained = round.Take(cut).ToArray(); var current = CampaignCombatActualRoundEntry.ReplayTrustedSource(currentSource, context, old, retained, dependencies);
            foreach (var kind in new[] { "commit-attack", "resolve-attack", "release-reserves", "repeat-cycle", "finish-cycle", "open-later-stage", "consume-lineage", "refund", "reseed" })
            {
                var input = Input(current, kind); Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(currentSource, context, old, retained, Encode(input), dependencies), 7);
                input["admittedAt"] = 33000; Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(currentSource, context, old, retained, Encode(input), dependencies), 5);
            }
            if (cut == 5) Reject(() => CampaignCombatActualRoundEntry.ApplyTrustedSource(currentSource, context, old, retained, Encode(Input(current, "complete-step")), dependencies), 7);
            if (cut <= 2)
                foreach (var kind in new[] { "expire-round", "controller-unavailable" })
                {
                    var input = Input(current, kind, now: 33000); input["command"]!["roundId"] = "stale";
                    var noop = CampaignCombatActualRoundEntry.ApplyTrustedSource(currentSource, context, old, retained, Encode(input), dependencies);
                    Assert.Null(noop.EventBytes); Assert.Null(noop.ReceiptId); Assert.Equal(current.ControlBytes, noop.ControlBytes);
                }
        }
        foreach (var kind in new[] { "ActualRoundSource", "ActualRoundProof", "RoundControl", "RoundInput", "RoundEvent" })
            Reject(() => CampaignCombatActualRoundEntryCodec.Parse([], kind), 1);
        Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Artifact(row.GetProperty("proofs")[0]), context, old, [], dependencies), 1);
        Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Encoding.ASCII.GetBytes(packet["actualSelectionSourceCanonicalUtf8"]!.GetValue<string>()), context, old, [], dependencies), 1);
        Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Encoding.ASCII.GetBytes("{\"digest\":\"sha256:" + new string('0', 64) + "\"}"), context, old, [], dependencies), 1);
    }

    [Fact]
    public void CanonicalAsciiCarrierEscapesMatchReferenceBeforeOriginalAdmission()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var context = Context(); var dependencies = DependencyLookup();
        var initial = JsonNode.Parse(Cut(JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(), 0))!; var old = Ledger(row, "trustedSelectionInputs");
        foreach (var value in Enumerable.Range(0, 128))
        {
            var source = initial.DeepClone(); source["actualSelectionSourceCanonicalUtf8"] = source["actualSelectionSourceCanonicalUtf8"]!.GetValue<string>() + (char)value;
            var text = Encoding.ASCII.GetString(Encode(source));
            // Build the independent reference spelling, rather than the platform encoder's uppercase escapes.
            var upper = "\\u" + value.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
            var spelling = value < 32 ? "\\u" + value.ToString("x4", System.Globalization.CultureInfo.InvariantCulture) : ((char)value).ToString();
            text = text.Replace(upper, spelling, StringComparison.Ordinal);
            Reject(() => CampaignCombatActualRoundEntryCodec.ReadSource(Encoding.ASCII.GetBytes(text), context, old, [], dependencies), 4);
        }
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-round-entry-v1.json")));
    private static byte[] Artifact(JsonElement value) => Encoding.ASCII.GetBytes(value.GetProperty("canonicalUtf8").GetString()!);
    private static byte[][] Ledger(JsonElement row, string field) => row.GetProperty(field).EnumerateArray().Select(Artifact).ToArray();
    private static byte[] Dependency(string path) => File.ReadAllBytes(Path.Combine(Repository(), path));
    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx"))) directory = directory.Parent;
        return directory!.FullName;
    }
    private static CampaignCombatCreationContext Context()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        return new(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config);
    }
}

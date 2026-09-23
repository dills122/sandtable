using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCycleMovementTests
{
    [Theory]
    [InlineData("zero-engaged.axis.attacker")]
    [InlineData("zero-engaged.commonwealth.attacker")]
    public void SettledRelationshipCanEndOnlyThroughComputedSuccessor(string name)
    {
        var source = Source(name);
        var result = CampaignCombatResultRelease.Replay(source, [], []).Result;
        var prior = result.World; var own = result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
        var destination = Destination(source, prior, prior.Elements.Single(e => e.ElementId == own.ElementId).CurrentLocationId);
        var next = prior.ProjectOrdinaryMove(own, destination, "mov.test");
        Assert.False(Assert.Single(next.Relationships).Active);
        Assert.True(Assert.Single(prior.Relationships).Active);
        Assert.Equal(destination, next.Elements.Single(e => e.ElementId == own.ElementId).CurrentLocationId);
        Assert.Equal(prior.Settlements, next.Settlements);
    }

    [Theory]
    [InlineData("zero-engaged.axis.attacker")]
    [InlineData("zero-engaged.commonwealth.attacker")]
    public void NativeMoveChargesBreakOffAndReplaysWithoutResettingResources(string name)
    {
        var source = Source(name);
        var initial = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []);
        var own = initial.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
        var element = initial.World.Elements.Single(e => e.ElementId == own.ElementId);
        var destination = Destination(source, initial.World, element.CurrentLocationId);
        var input = CampaignCombatCycleMovement.Command(initial, destination);
        var moved = CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], input);
        var after = moved.State.World.Elements.Single(e => e.ElementId == own.ElementId);
        Assert.Equal(11, after.OperationalState.CapabilityPointsExpended.Numerator);
        Assert.Equal(element.OperationalState.CohesionLevel - 1, after.OperationalState.CohesionLevel);
        Assert.Equal(destination, after.CurrentLocationId);
        Assert.Equal(initial.World.Settlements, moved.State.World.Settlements);
        Assert.Equal(element.Ammunition, after.Ammunition); Assert.Equal(element.Components, after.Components);
        Assert.False(Assert.Single(moved.State.World.Relationships).Active);
        var replayed = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [input], [moved.EventBytes]);
        Assert.Equal(moved.State.World, replayed.World);
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void CatalogueMovesPreserveUnchangedWorldAndRetainSourceIdentity(string side)
    {
        foreach (var kind in new[] { "ordinary", "zero-retreat", "refusal-loss-dp", "zero-engaged", "defender-capture-guard", "defender-capture-escape", "attacker-capture-guard-cp-limit", "attacker-capture-escape" })
            foreach (var seal in new[] { "attacker", "defender" })
            {
                var source = Source($"{kind}.{side}.{seal}"); var initial = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []);
                var own = initial.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
                var element = initial.World.Elements.Single(e => e.ElementId == own.ElementId);
                var dest = Destination(source, initial.World, element.CurrentLocationId);
                var input = CampaignCombatCycleMovement.Command(initial, dest);
                var basis = CampaignCombatCycleMovementCodec.SerializeBase(initial.Basis);
                Assert.Equal(basis, CampaignCombatCycleMovementCodec.SerializeBase(CampaignCombatCycleMovementCodec.ReadBase(basis, source)));
                var result = CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], input);
                var eventNode = JsonNode.Parse(result.EventBytes)!;
                var receipt = eventNode["receiptId"]!.GetValue<string>(); eventNode.AsObject().Remove("receiptId");
                Assert.Equal("mov." + Hex(Encoding.UTF8.GetBytes("sandtable.combat.ordinary-movement-receipt.v1\0" + eventNode.ToJsonString())), receipt);
                var breakOff = initial.World.Relationships.Any(r => r.Active && r.Kind == "engaged") ? 4
                    : initial.World.Relationships.Any(r => r.Active) ? 2 : 0;
                var beforeCp = element.OperationalState.CapabilityPointsExpended.Numerator;
                var afterCp = beforeCp + breakOff + 2;
                var dp = Math.Max(0, afterCp - 10) - Math.Max(0, beforeCp - 10);
                var effect = eventNode["effect"]!;
                Assert.Equal(breakOff, effect["costs"]!["breakOffCost"]!.GetValue<int>());
                Assert.Equal(afterCp, effect["costs"]!["afterCp"]!.GetValue<long>());
                Assert.Equal(dp, effect["costs"]!["excessCpDp"]!.GetValue<long>());
                Assert.Equal(initial.World.Relationships.Count(r => r.Active), effect["endedMemberships"]!.AsArray().Count);
                AssertProjectedWorld(initial.World, result.State.World, own.ElementId, dest, receipt, afterCp, checked((int)dp));
                Assert.Equal(2, result.State.Basis.Cycle.Ordinal); Assert.Equal(initial.Basis.Position, result.State.Basis.Position);
                var beforeJson = JsonNode.Parse(CampaignCombatCycleMovementCodec.SerializeState(initial))!;
                var afterJson = JsonNode.Parse(CampaignCombatCycleMovementCodec.SerializeState(result.State))!;
                Assert.True(JsonNode.DeepEquals(beforeJson["randomState"], afterJson["randomState"]));
                Assert.True(JsonNode.DeepEquals(beforeJson["attackHistory"], afterJson["attackHistory"]));
                for (var cut = 0; cut <= 1; cut++)
                {
                    CycleMoveInput[] inputs = cut == 0 ? [] : [input]; byte[][] events = cut == 0 ? [] : [result.EventBytes];
                    var state = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, inputs, events);
                    var bytes = CampaignCombatCycleMovementCodec.SerializeState(state);
                    Assert.Equal(bytes, CampaignCombatCycleMovementCodec.SerializeState(CampaignCombatCycleMovementCodec.ReadState(bytes, source, inputs, events)));
                }
                Assert.Equal(initial.World, CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []).World);
            }
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void LaterMovesDoNotRechargeBreakOffAndEveryRetryReturnsOriginalBytes(string side)
    {
        var source = Source($"zero-engaged.{side}.attacker");
        var state = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []);
        var own = state.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit;
        var origin = state.World.Elements.Single(e => e.ElementId == own.ElementId).CurrentLocationId;
        var dest = Destination(source, state.World, origin);
        var inputs = new List<CycleMoveInput>(); var events = new List<byte[]>();
        foreach (var next in new[] { dest, origin, dest })
        {
            var input = CampaignCombatCycleMovement.Command(state, next);
            var moved = CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, inputs, events, input, CampaignCombatCycleMovementCodec.SerializeState(state));
            Assert.False(moved.Duplicate); inputs.Add(input); events.Add(moved.EventBytes); state = moved.State;
            var cut = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, inputs, events);
            Assert.Equal(CampaignCombatCycleMovementCodec.SerializeState(state), CampaignCombatCycleMovementCodec.SerializeState(cut));
            for (var i = 0; i < inputs.Count; i++)
            {
                var retry = CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, inputs, events, inputs[i]);
                Assert.True(retry.Duplicate); Assert.Equal(events[i], retry.EventBytes);
                retry.EventBytes[0] ^= 1; Assert.Equal(events[i], retry.EventBytes);
                Assert.Equal(state.World, retry.State.World);
            }
        }
        Assert.Equal(15, state.World.Elements.Single(e => e.ElementId == own.ElementId).OperationalState.CapabilityPointsExpended.Numerator);
        Assert.Equal(new[] { origin, dest, origin, dest }, Assert.Single(state.Tracks).Route);
        Assert.Equal(JsonNode.Parse(events[0])!["receiptId"]!.GetValue<string>(), Assert.Single(state.World.Relationships).EndedByReceiptId);
        var retained = CampaignCombatCycleMovementCodec.SerializeState(state);
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, inputs, events, CampaignCombatCycleMovement.Command(state, origin)));
        Assert.Equal(retained, CampaignCombatCycleMovementCodec.SerializeState(state));
        Assert.Throws<ArgumentException>(() => new CampaignWorldSnapshotV7(7, state.World.CreationBinding, state.World.Elements,
            state.World.Representations, state.World.BrokenVehicleLots, state.World.CohesionCauses, state.World.Relationships,
            state.World.CustodyLots, state.World.Guards, state.World.ReplacementEntitlements, state.World.FutureObligations, state.World.Settlements));
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void RejectsForgedBytesInputsHistoryAndCaches(string side)
    {
        var source = Source($"zero-engaged.{side}.attacker"); var initial = CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], []);
        var command = CampaignCombatCycleMovement.Command(initial, Destination(source, initial.World,
            initial.World.Elements.Single(e => e.ElementId == initial.Basis.Result.Context.Committed.Base.Steps.Selection!.Attacker.Unit.ElementId).CurrentLocationId));
        var moved = CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], command);
        var inputs = new[] { command }; var events = new[] { moved.EventBytes };
        foreach (var bytes in Mutations(moved.EventBytes))
            Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, inputs, [bytes]));
        var resigned = JsonNode.Parse(moved.EventBytes)!; resigned["effect"]!["costs"]!["totalCost"] = 2;
        resigned.AsObject().Remove("receiptId"); resigned["receiptId"] = "mov." + Hex(Encoding.UTF8.GetBytes("sandtable.combat.ordinary-movement-receipt.v1\0" + resigned.ToJsonString()));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, inputs, [Encoding.UTF8.GetBytes(resigned.ToJsonString())]));
        foreach (var bytes in Mutations(CampaignCombatCycleMovementCodec.SerializeBase(initial.Basis)))
            Assert.Throws<JsonException>(() => CampaignCombatCycleMovementCodec.ReadBase(bytes, source));
        foreach (var bytes in Mutations(CampaignCombatCycleMovementCodec.SerializeState(moved.State)))
            Assert.Throws<JsonException>(() => CampaignCombatCycleMovementCodec.ReadState(bytes, source, inputs, events));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], command, CampaignCombatCycleMovementCodec.SerializeState(moved.State)));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], command, new byte[1_048_577]));
        foreach (var invalid in new[] {
            command with { Actor = CampaignOpeningPreambleActor.System },
            command with { Command = command.Command with { ExpectedPriorVersion = initial.StateVersion - 1 } },
            command with { Command = command.Command with { PositionId = "foreign.position" } },
            command with { Command = command.Command with { CycleId = "sha256:" + new string('0', 64) } },
            command with { Command = command.Command with { Kind = "finish" } },
            command with { Command = command.Command with { OriginLocationId = "foreign.origin" } },
            command with { Command = command.Command with { DestinationLocationId = "foreign.destination" } },
            command with { Command = command.Command with { DestinationLocationId = initial.World.Elements.Single(e => e.ElementId != command.Command.Unit.ElementId).CurrentLocationId } },
            command with { Command = command.Command with { DestinationLocationId = command.Command.OriginLocationId } },
            command with { Command = command.Command with { Unit = new("foreign.creation", side, command.Command.Unit.ElementId) } } })
            Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, [], [], invalid));
        var changedRetry = command with { Command = command.Command with { DestinationLocationId = command.Command.OriginLocationId } };
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ApplyIsolatedBoundary(source, inputs, events, changedRetry));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [], events));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, [command, command], [moved.EventBytes, moved.EventBytes]));
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(source, Enumerable.Repeat(command, 33).ToArray(), Enumerable.Repeat(moved.EventBytes, 33).ToArray()));
        var foreign = Source($"zero-engaged.{(side == "axis" ? "commonwealth" : "axis")}.attacker");
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(foreign, inputs, events));
        var truncated = new CombatResultReleaseSource(source.CaseId, source.Request, source.Created, source.Boundary, source.PredecessorInputs,
            source.PredecessorEvents, source.RoundInputs, source.RoundEvents, source.ResultInputs.Take(source.ResultInputs.Count - 1).ToArray(), source.ResultEvents.Take(source.ResultEvents.Count - 1).ToArray());
        Assert.Throws<JsonException>(() => CampaignCombatCycleMovement.ReplayIsolatedBoundary(truncated, [], []));
    }

    private static void AssertProjectedWorld(CampaignWorldSnapshotV7 before, CampaignWorldSnapshotV7 after,
        string elementId, string destination, string receipt, long cp, int dp)
    {
        var expected = JsonSerializer.SerializeToNode(before)!;
        var element = expected["Elements"]!.AsArray().Single(e => e!["ElementId"]!.GetValue<string>() == elementId)!;
        var cohesion = element["OperationalState"]!["CohesionLevel"]!.GetValue<int>();
        element["CurrentLocationId"] = destination;
        element["OperationalState"]!["CapabilityPointsExpended"]!["Numerator"] = cp;
        element["OperationalState"]!["CohesionLevel"] = cohesion - dp;
        expected["Representations"]!.AsArray().Single(r => r!["BoundElementIds"]![0]!.GetValue<string>() == elementId)!["CurrentLocationId"] = destination;
        foreach (var r in expected["Relationships"]!.AsArray().Where(r => r!["Active"]!.GetValue<bool>()))
        { r!["Active"] = false; r["EndedByReceiptId"] = receipt; r["EndingCause"] = "ordinary-break-off"; }
        if (dp > 0) expected["CohesionCauses"]!.AsArray().Add(JsonSerializer.SerializeToNode(new CampaignCombatCohesionCause(
            receipt + ".dp", before.CohesionCauses.Count + 1, receipt, elementId, 1, 1, "ordinary-movement-excess-cp-dp", dp, cohesion, cohesion - dp)));
        Assert.True(JsonNode.DeepEquals(expected, JsonSerializer.SerializeToNode(after)), "Only move, CP/DP and affected relationship fields may change.");
    }
    private static string Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static IEnumerable<byte[]> Mutations(byte[] bytes)
    {
        var original = Encoding.UTF8.GetString(bytes);
        yield return Encoding.UTF8.GetBytes(original + " ");
        yield return Encoding.UTF8.GetBytes(original.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal));
        var node = JsonNode.Parse(bytes)!;
        foreach (var name in node.AsObject().Select(p => p.Key).ToArray())
        {
            var changed = node.DeepClone(); changed[name] = "invalid"; yield return Encoding.UTF8.GetBytes(changed.ToJsonString());
        }
        var duplicate = original.Replace("{", "{\"unknown\":1,", StringComparison.Ordinal);
        yield return Encoding.UTF8.GetBytes(duplicate);
        var reversed = new JsonObject(); foreach (var field in node.AsObject().Reverse()) reversed.Add(field.Key, field.Value?.DeepClone());
        yield return Encoding.UTF8.GetBytes(reversed.ToJsonString());
    }

    private static string Destination(CombatResultReleaseSource source, CampaignWorldSnapshotV7 world, string origin) =>
        source.Request.Context.Setup.Artifact.Definition.Edges.Where(e => e.FirstLocationId == origin || e.SecondLocationId == origin)
            .Select(e => e.FirstLocationId == origin ? e.SecondLocationId : e.FirstLocationId)
            .First(d => world.Elements.All(e => e.CurrentLocationId != d) && world.Guards.All(g => g.CurrentLocationId != d));

    internal static CombatResultReleaseSource Source(string name)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
        var row = fixture.RootElement.GetProperty("traces").EnumerateArray().Single(r => r.GetProperty("name").GetString() == name);
        var test = CombatResolutionTests.Case(row);
        var inputs = row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
        var events = row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
        return new(name, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    }
}

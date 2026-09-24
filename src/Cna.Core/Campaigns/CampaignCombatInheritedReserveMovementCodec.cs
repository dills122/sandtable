using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatInheritedReserveMovementCodec
{
    private static JsonNode Value<T>(T value) => CampaignCombatCycleMovementCodec.Value(value);
    private static byte[] Encode(JsonNode node) => CampaignCombatCycleMovementCodec.Encode(node);
    private static JsonNode Control(InheritedReserveMoveBasis basis) => JsonNode.Parse(CampaignCombatInheritedCycleControlCodec.SerializeState(basis.Control))!;
    public static byte[] SerializeBase(InheritedReserveMoveBasis basis)
    {
        var control = Control(basis);
        var predecessor = JsonNode.Parse(CampaignCombatInheritedCycleControlCodec.SerializeBase(basis.Control.Basis))!;
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        { w.WriteStartObject(); CampaignV11CanonicalCodec.WritePosition(w, "position", basis.Position); w.WriteEndObject(); }
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["actor"] = predecessor["actor"]!.DeepClone(),
            ["predecessorHash"] = CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedCycleControlCodec.SerializeState(basis.Control)),
            ["predecessorBase"] = predecessor.DeepClone(),
            ["predecessorInputs"] = new JsonArray(basis.Inputs.Select(i => JsonNode.Parse(CampaignCombatInheritedCycleControlCodec.SerializeInput(i))).ToArray()),
            ["predecessorEvents"] = new JsonArray(basis.Events.Select(b => JsonNode.Parse(b)).ToArray()),
            ["cycle"] = control["activeCycle"]!.DeepClone(),
            ["firstActingSide"] = predecessor["controlBase"]!["releaseBase"]!["firstActingSide"]!.DeepClone(),
            ["position"] = JsonNode.Parse(stream.ToArray())!["position"]!.DeepClone(),
            ["stateVersion"] = basis.Control.StateVersion,
            ["prefix"] = basis.Control.Prefix,
            ["world"] = control["world"]!.DeepClone(),
            ["randomState"] = control["randomState"]!.DeepClone(),
            ["attackHistory"] = control["attackHistory"]!.DeepClone(),
            ["releaseMember"] = control["members"]![0]!.DeepClone(),
            ["priorMovementEnd"] = predecessor["controlBase"]!["movementEnd"]!.DeepClone()
        });
    }
    public static byte[] SerializeState(InheritedReserveMoveState state)
    {
        var control = Control(state.Basis); var world = control["world"]!.DeepClone();
        foreach (var item in world["elements"]!.AsArray())
        {
            var element = state.World.Elements.Single(e => e.ElementId == item!["elementId"]!.GetValue<string>());
            item!["currentLocationId"] = element.CurrentLocationId;
            item["operationalState"]!["capabilityPointsExpended"] = Value(element.OperationalState.CapabilityPointsExpended);
            item["operationalState"]!["cohesionLevel"] = element.OperationalState.CohesionLevel;
        }
        foreach (var item in world["representations"]!.AsArray())
            item!["currentLocationId"] = state.World.Representations.Single(r => r.RepresentationId == item["representationId"]!.GetValue<string>()).CurrentLocationId;
        var member = control["members"]![0]!.DeepClone(); member["spentCp"] = Value(state.ReleaseMember.SpentCp);
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["baseHash"] = state.BaseHash,
            ["stateVersion"] = state.StateVersion,
            ["prefix"] = state.Prefix,
            ["world"] = world,
            ["randomState"] = control["randomState"]!.DeepClone(),
            ["attackHistory"] = control["attackHistory"]!.DeepClone(),
            ["releaseMember"] = member,
            ["tracks"] = Value(state.Tracks),
            ["receipts"] = new JsonArray(state.Receipts.Select(r => (JsonNode)new JsonObject
            {
                ["commandHash"] = r.CommandHash,
                ["eventHash"] = r.EventHash,
                ["receiptId"] = r.ReceiptId,
                ["actor"] = CampaignCombatCycleMovementCodec.Actor(r.Actor),
                ["stateVersion"] = r.StateVersion
            }).ToArray())
        });
    }
    internal static byte[] Event(InheritedReserveMoveState prior, CycleMoveInput input, JsonNode effect, string? receipt)
    {
        var cycle = prior.Basis.Control.ActiveCycle!;
        var node = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = "combat-cycle-element-moved",
            ["author"] = CampaignCombatCycleMovementCodec.Actor(input.Actor),
            ["campaignId"] = cycle.CampaignId,
            ["rulesetHash"] = cycle.RulesetHash,
            ["configurationHash"] = cycle.AdmittedPolicyBundleDigest,
            ["cycleId"] = CampaignCombatReserveCompletionCodec.CycleId(cycle),
            ["positionId"] = prior.Basis.Position.PositionId,
            ["baseHash"] = prior.BaseHash,
            ["priorVersion"] = prior.StateVersion,
            ["stateVersion"] = checked(prior.StateVersion + 1),
            ["priorPrefix"] = prior.Prefix,
            ["input"] = JsonNode.Parse(CampaignCombatCycleMovementCodec.SerializeInput(input)),
            ["effect"] = effect.DeepClone()
        };
        if (receipt is not null) node["receiptId"] = receipt;
        return Encode(node);
    }
    public static InheritedReserveMoveBasis ReadBase(ReadOnlySpan<byte> bytes, CombatInheritedReserveMovementSource source)
    {
        CampaignCombatCycleMovementCodec.Check(bytes); var basis = source.Derive();
        if (!bytes.SequenceEqual(SerializeBase(basis))) throw new JsonException("Released-I Movement base differs from authenticated repeat source.");
        return basis;
    }
    public static InheritedReserveMoveState ReadState(ReadOnlySpan<byte> bytes, CombatInheritedReserveMovementSource source,
        IReadOnlyList<CycleMoveInput> inputs, IReadOnlyList<byte[]> events)
    {
        CampaignCombatCycleMovementCodec.Check(bytes); var state = CampaignCombatInheritedReserveMovement.Replay(source, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Released-I Movement state differs from replay.");
        return state;
    }
    internal static byte[] Copy(byte[] bytes)
    { ArgumentNullException.ThrowIfNull(bytes); CampaignCombatCycleMovementCodec.Check(bytes); return bytes.ToArray(); }
}

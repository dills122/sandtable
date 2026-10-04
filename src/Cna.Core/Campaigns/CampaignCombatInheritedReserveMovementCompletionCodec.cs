using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using MoveCodec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCodec;
using Movement = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCompletion;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatInheritedReserveMovementCompletionCodec
{
    internal static readonly string[] Kinds = ["stop-element-movement", "resolve-breakdown-stop", "complete-movement-segment"];
    internal static readonly string[] Domains = ["sandtable.combat.inherited-reserve-movement-stop-receipt.v1",
        "sandtable.combat.inherited-reserve-breakdown-stop-resolved-receipt.v1", "sandtable.combat.inherited-reserve-movement-completion-receipt.v1"];
    private static readonly string[] EventTypes = ["combat-cycle-element-movement-stopped", "combat-cycle-breakdown-stop-resolved", "combat-cycle-movement-segment-completed"];
    private static JsonNode Value<T>(T value) => CampaignCombatCycleMovementCodec.Value(value);
    private static byte[] Encode(JsonNode node) => CampaignCombatCycleMovementCodec.Encode(node);
    private static JsonNode Position(LandSequencePosition position)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        { w.WriteStartObject(); CampaignV11CanonicalCodec.WritePosition(w, "position", position); w.WriteEndObject(); }
        return JsonNode.Parse(stream.ToArray())!["position"]!.DeepClone();
    }
    private static JsonNode Cycle(ReserveMovementCompletionState state) =>
        JsonNode.Parse(MoveCodec.SerializeBase(state.Basis.Movement.Basis))!["cycle"]!.DeepClone();
    private static JsonObject Context(CampaignCombatMovementInterrupt context, ReserveMovementCompletionState state) => new JsonObject
    { ["cycle"] = Cycle(state), ["cycleId"] = context.CycleId, ["sequencePosition"] = Position(context.SequencePosition) };
    private static JsonObject Stop(ReserveMovementStop stop, bool includeId = true)
    {
        var node = new JsonObject();
        if (includeId) node["stopId"] = stop.StopId;
        node["recordedStateVersion"] = stop.RecordedStateVersion; node["track"] = Value(stop.Track);
        node["movementReceiptId"] = stop.MovementReceiptId; node["reason"] = "deliberate";
        node["weatherKind"] = "normal"; node["cohortInputs"] = new JsonArray(); return node;
    }
    private static JsonObject Flow(ReserveMovementCompletionState state) => state.Receipts.Count == 0
        ? new JsonObject { ["kind"] = "moving", ["track"] = Value(state.Basis.Movement.Tracks.Single()), ["movementReceiptId"] = state.Basis.Movement.Receipts.Single().ReceiptId }
        : state.Stop is not null ? new JsonObject { ["kind"] = "phasing-stop", ["stop"] = Stop(state.Stop) }
        : new JsonObject { ["kind"] = "idle" };
    private static JsonObject Scope(CombatReleaseScope scope) => new JsonObject
    {
        ["gameTurn"] = scope.GameTurn,
        ["operationStage"] = scope.OperationStage,
        ["playerPhaseSlot"] = scope.PlayerPhaseSlot,
        ["actingSide"] = CampaignSnapshotSerializer.FormatSide(scope.ActingSide)
    };
    private static JsonObject Proof(CombatContinuationMovementEnd proof) => new JsonObject
    {
        ["scope"] = Scope(proof.Scope),
        ["ordinal"] = proof.Ordinal,
        ["completionReceiptId"] = proof.CompletionReceiptId,
        ["endLocations"] = Value(proof.Locations),
        ["excludedBefore"] = Value(proof.ExcludedBefore)
    };
    public static byte[] SerializeBase(ReserveMovementCompletionBasis basis) => Encode(new JsonObject
    {
        ["contractVersion"] = 1,
        ["actor"] = CampaignSnapshotSerializer.FormatSide(basis.Movement.Basis.Control.ActiveCycle!.ActingSide),
        ["predecessorHash"] = CampaignOpeningPreambleCodec.Hash(MoveCodec.SerializeState(basis.Movement)),
        ["predecessorBase"] = JsonNode.Parse(MoveCodec.SerializeBase(basis.Movement.Basis)),
        ["predecessorInputs"] = new JsonArray(basis.Inputs.Select(i => JsonNode.Parse(CampaignCombatCycleMovementCodec.SerializeInput(i))).ToArray()),
        ["predecessorEvents"] = new JsonArray(basis.Events.Select(b => JsonNode.Parse(b)).ToArray()),
        ["movementPosition"] = Position(basis.Movement.Basis.Position),
        ["interruptPosition"] = Position(basis.InterruptPosition),
        ["breakdownPosition"] = Position(basis.BreakdownPosition)
    });
    public static byte[] SerializeState(ReserveMovementCompletionState state)
    {
        var move = JsonNode.Parse(MoveCodec.SerializeState(state.Basis.Movement))!;
        var member = move["releaseMember"]!.DeepClone();
        var ex = state.ReleaseMember.History.NextMovement!;
        member["history"]!["nextMovement"]!["status"] = ex.Status;
        member["history"]!["nextMovement"]!["completionReceiptId"] = ex.CompletionReceiptId;
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["baseHash"] = state.BaseHash,
            ["stateVersion"] = state.StateVersion,
            ["prefix"] = state.Prefix,
            ["cycle"] = Cycle(state),
            ["sequencePosition"] = Position(state.Position),
            ["world"] = move["world"]!.DeepClone(),
            ["randomState"] = move["randomState"]!.DeepClone(),
            ["attackHistory"] = move["attackHistory"]!.DeepClone(),
            ["releaseMember"] = member,
            ["tracks"] = move["tracks"]!.DeepClone(),
            ["breakdownFlow"] = Flow(state),
            ["interruptContext"] = state.InterruptContext is null ? null : Context(state.InterruptContext, state),
            ["movementEnd"] = state.MovementEnd is null ? null : Proof(state.MovementEnd),
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
    public static byte[] SerializeCommand(ReserveMovementCompletionCommand command, bool includeAction = true)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Kinds.Contains(command.Kind, StringComparer.Ordinal)) throw new JsonException("Unsupported completion command kind.");
        var node = new JsonObject
        {
            ["contractVersion"] = command.ContractVersion,
            ["kind"] = command.Kind,
            ["baseHash"] = command.BaseHash,
            ["cycleId"] = command.CycleId,
            ["positionId"] = command.PositionId,
            ["expectedPriorVersion"] = command.ExpectedPriorVersion
        };
        if (command.Kind != Kinds[2]) node[command.Kind == Kinds[0] ? "routeId" : "stopId"] = command.CapabilityId;
        else if (command.CapabilityId is not null) throw new JsonException("Completion cannot carry a stop capability.");
        if (includeAction) node["actionId"] = command.ActionId;
        return Encode(node);
    }
    public static byte[] SerializeInput(ReserveMovementCompletionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Encode(new JsonObject
        {
            ["command"] = JsonNode.Parse(SerializeCommand(input.Command)),
            ["actor"] = CampaignCombatCycleMovementCodec.Actor(input.Actor)
        });
    }
    internal static string Capability(ReserveMovementCompletionState state, int index)
    {
        var c = Movement.Cycle(state);
        var node = new JsonObject
        {
            ["domain"] = index == 0 ? "sandtable.observation.movement-route.v1" : "sandtable.action.breakdown-stop.v1",
            ["campaignId"] = c.CampaignId,
            ["rulesetHash"] = c.RulesetHash,
            ["stateVersion"] = state.StateVersion,
            ["audience"] = index == 0 ? CampaignSnapshotSerializer.FormatSide(c.ActingSide) : "system",
            ["baseHash"] = state.BaseHash
        };
        if (index == 0)
        { node["track"] = Value(state.Basis.Movement.Tracks.Single()); node["movementReceiptId"] = state.Basis.Movement.Receipts.Single().ReceiptId; }
        else node["recordedStateVersion"] = state.Stop!.RecordedStateVersion;
        return CampaignOpeningPreambleCodec.Hash(Encode(node));
    }
    internal static string StopId(ReserveMovementCompletionState state, ReserveMovementStop stop)
    {
        var c = Movement.Cycle(state);
        var node = new JsonObject { ["domain"] = "sandtable.breakdown.stop.v1", ["campaignId"] = c.CampaignId, ["rulesetHash"] = c.RulesetHash };
        foreach (var pair in Stop(stop, false).AsObject()) node[pair.Key] = pair.Value?.DeepClone();
        return CampaignOpeningPreambleCodec.Hash(Encode(node));
    }
    internal static byte[] Event(ReserveMovementCompletionState prior, ReserveMovementCompletionState next,
        ReserveMovementCompletionInput input, CombatContinuationMovementEnd? proof, string? receipt)
    {
        var index = prior.Receipts.Count; var c = Movement.Cycle(prior); var move = JsonNode.Parse(MoveCodec.SerializeState(prior.Basis.Movement))!;
        var node = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = EventTypes[index],
            ["author"] = CampaignCombatCycleMovementCodec.Actor(input.Actor),
            ["campaignId"] = c.CampaignId,
            ["rulesetHash"] = c.RulesetHash,
            ["configurationHash"] = c.AdmittedPolicyBundleDigest,
            ["cycleId"] = input.Command.CycleId,
            ["positionId"] = input.Command.PositionId,
            ["baseHash"] = prior.BaseHash,
            ["priorVersion"] = prior.StateVersion,
            ["stateVersion"] = checked(prior.StateVersion + 1),
            ["priorPrefix"] = prior.Prefix,
            ["input"] = JsonNode.Parse(SerializeInput(input))
        };
        if (index == 0) node["stop"] = Stop(next.Stop!);
        else if (index == 1)
        {
            node["stop"] = Stop(prior.Stop!); node["randomStateBefore"] = move["randomState"]!.DeepClone();
            node["checks"] = new JsonArray(); node["createdLots"] = new JsonArray(); node["randomStateAfter"] = move["randomState"]!.DeepClone();
            node["sources"] = Value(new[] { new { sourceId = "spi-1979-land-rules", locator = "21.24-21.26" } });
        }
        else
        {
            node["gameTurn"] = c.GameTurn; node["operationStage"] = c.OperationStage; node["actingSide"] = CampaignSnapshotSerializer.FormatSide(c.ActingSide);
            node["endLocations"] = Value(proof!.Locations); node["excludedUnits"] = Value(proof.ExcludedBefore);
            var moved = JsonNode.Parse(prior.Basis.Events.Single())!;
            node["progress"] = new JsonArray(new JsonObject
            {
                ["eventType"] = moved["eventType"]!.DeepClone(),
                ["receiptId"] = moved["receiptId"]!.DeepClone(),
                ["eventHash"] = CampaignOpeningPreambleCodec.Hash(prior.Basis.Events.Single())
            });
        }
        node["sequencePosition"] = Position(next.Position);
        node["breakdownFlowAfter"] = index == 0 ? new JsonObject { ["kind"] = "phasing-stop", ["stop"] = Stop(next.Stop!) } : new JsonObject { ["kind"] = "idle" };
        node["interruptContextAfter"] = next.InterruptContext is null ? null : Context(next.InterruptContext, prior);
        if (receipt is not null) node["receiptId"] = receipt;
        return Encode(node);
    }
    public static ReserveMovementCompletionBasis ReadBase(ReadOnlySpan<byte> bytes, CombatInheritedReserveMovementCompletionSource source)
    {
        CampaignCombatCycleMovementCodec.Check(bytes); var basis = source.Derive();
        if (!bytes.SequenceEqual(SerializeBase(basis))) throw new JsonException("Completion base differs from authenticated Movement source.");
        return basis;
    }
    public static ReserveMovementCompletionState ReadState(ReadOnlySpan<byte> bytes, CombatInheritedReserveMovementCompletionSource source,
        IReadOnlyList<ReserveMovementCompletionInput> inputs, IReadOnlyList<byte[]> events)
    {
        CampaignCombatCycleMovementCodec.Check(bytes); var state = Movement.Replay(source, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Completion state differs from replay.");
        return state;
    }
}

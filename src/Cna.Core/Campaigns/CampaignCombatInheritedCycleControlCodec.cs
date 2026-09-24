using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatInheritedCycleControlCodec
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static JsonNode Value<T>(T value) => JsonSerializer.SerializeToNode(value, Options)!;
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static string Actor(CampaignOpeningPreambleActor actor) => CampaignCombatSealedRoundCodec.Actor(actor);
    private static JsonNode Release(CombatCycleControlBasis basis) => JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeState(basis.Proof.Source.Release))!;
    private static JsonNode Inherited(CombatCycleControlBasis basis) => JsonNode.Parse(CampaignCombatInheritedReserveCycleCodec.SerializeControl(basis.Proof.Source.Base.Predecessor))!;
    private static JsonNode? Cycle(CampaignCombatCycleAuthority? cycle)
    {
        if (cycle is null) return null;
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream)) CampaignCombatReserveCompletionCodec.WriteCycle(w, cycle);
        return JsonNode.Parse(stream.ToArray());
    }
    public static byte[] SerializeCommand(CombatCycleControlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ContentContractGuards.RequireStableId(command.Kind, nameof(command));
        ContentContractGuards.RequireStableId(command.ControlId, nameof(command));
        if (command.DecisionId is not null) ContentContractGuards.RequireStableId(command.DecisionId, nameof(command));
        if (command.CycleId is null || command.CycleId.Length != 71 || !command.CycleId.StartsWith("sha256:", StringComparison.Ordinal) ||
            command.CycleId[7..].Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')) || command.ExpectedPriorVersion < 0)
            throw new JsonException("Invalid control identity/version.");
        return Encode(Value(command));
    }
    private static JsonObject Input(CombatCycleControlInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.AdmittedAt is < 0 or > CampaignCombatSelectionSteps.UtcMaximum) throw new JsonException("Control clock outside UTC range.");
        return new JsonObject
        {
            ["command"] = JsonNode.Parse(SerializeCommand(input.Command)),
            ["actor"] = Actor(input.Actor),
            ["admittedAt"] = input.AdmittedAt,
            ["clockAvailable"] = input.ClockAvailable
        };
    }
    public static byte[] SerializeInput(CombatCycleControlInput input) => Encode(Input(input));
    internal static byte[] SerializeNestedBase(CombatCycleControlBasis basis)
    {
        var proof = CampaignCombatArmedContinuationCodec.Serialize(basis.Proof); var inherited = Inherited(basis);
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["profile"] = "inherited-armed-" + Hash(proof)[7..],
            ["releaseBase"] = JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeBase(basis.Proof.Source.Base.ReleaseBase, basis.Proof.Request)),
            ["releaseInputs"] = new JsonArray(basis.ReleaseInputs.Select(i => JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeInput(i))).ToArray()),
            ["releaseEvents"] = new JsonArray(basis.ReleaseEvents.Select(b => JsonNode.Parse(b)).ToArray()),
            ["world"] = inherited["base"]!["world"]!.DeepClone(),
            ["movementEnd"] = inherited["movementEnd"]!.DeepClone(),
            ["progress"] = new JsonArray(),
            ["targetUses"] = new JsonArray()
        });
    }
    public static byte[] SerializeBase(CombatCycleControlBasis basis)
    {
        var proof = CampaignCombatArmedContinuationCodec.Serialize(basis.Proof);
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["actor"] = CampaignSnapshotSerializer.FormatSide(basis.Proof.Source.Base.ReleaseBase.Cycle.ActingSide),
            ["releaseControlHash"] = basis.Proof.PredecessorHash,
            ["armedProofHash"] = Hash(proof),
            ["armedProof"] = JsonNode.Parse(proof),
            ["controlBase"] = JsonNode.Parse(SerializeNestedBase(basis))
        });
    }
    private static JsonObject Assessment(CombatCycleControlBasis basis)
    {
        var proof = basis.Proof; var member = proof.Source.Release.Members.Single();
        return new JsonObject
        {
            ["releaseCompletionReceiptId"] = proof.Source.Release.CompletionReceiptId,
            ["movementCompletionReceiptId"] = proof.Source.Base.Predecessor.MovementEnd!.CompletionReceiptId,
            ["progress"] = Value(proof.Source.Progress),
            ["witnesses"] = new JsonArray(new JsonObject
            {
                ["kind"] = "combat",
                ["unit"] = Value(member.Unit),
                ["destinationLocationId"] = proof.Candidate.Defender.LocationId,
                ["terrainCost"] = 0,
                ["breakOffCost"] = 0,
                ["afterCp"] = member.SpentCp.Numerator,
                ["excessCpDp"] = 0,
                ["usesReleaseException"] = false
            }),
            ["combatAssessment"] = "supported-armed-combat"
        };
    }
    public static byte[] SerializeState(CombatCycleControlState state)
    {
        var release = Release(state.Basis);
        // The earlier World is retained in the base; only authenticated Reserve status changes project into control.
        var world = Inherited(state.Basis)["base"]!["world"]!.DeepClone();
        foreach (var element in world["elements"]!.AsArray())
        {
            var member = state.Members.SingleOrDefault(m => m.Unit.ElementId == element!["elementId"]!.GetValue<string>());
            if (member is not null) element!["reserveStatus"] = member.Status switch
            { CampaignElementReserveStatus.None => "none", CampaignElementReserveStatus.ReserveI => "I", CampaignElementReserveStatus.ReserveII => "II", _ => throw new JsonException("Unknown Reserve status.") };
        }
        var members = JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeState(state.Basis.Proof.Source.Release with { Members = state.Members }))!["members"]!.DeepClone();
        return Encode(new JsonObject
        {
            ["contractVersion"] = 1,
            ["baseHash"] = state.BaseHash,
            ["controlId"] = state.ControlId,
            ["stateVersion"] = state.StateVersion,
            ["prefix"] = state.Prefix,
            ["status"] = state.Status,
            ["positionId"] = state.PositionId,
            ["activeCycle"] = Cycle(state.ActiveCycle),
            ["closure"] = state.Closure is null ? null : Value(state.Closure),
            ["decisionId"] = state.DecisionId,
            ["timing"] = state.Timing is null ? null : Value(state.Timing),
            ["openingClockFailure"] = state.OpeningClockFailure,
            ["acceptedHighWater"] = state.AcceptedHighWater,
            ["assessment"] = state.Status == "unopened" ? null : Assessment(state.Basis),
            ["members"] = members,
            ["world"] = world,
            ["randomState"] = release["randomState"]!.DeepClone(),
            ["attackHistory"] = release["attackHistory"]!.DeepClone(),
            ["targetUses"] = new JsonArray(),
            ["nextCycleProgress"] = new JsonArray(),
            ["receipts"] = new JsonArray(state.Receipts.Select(r => (JsonNode)new JsonObject
            {
                ["commandHash"] = r.CommandHash,
                ["eventHash"] = r.EventHash,
                ["receiptId"] = r.ReceiptId,
                ["actor"] = Actor(r.Actor),
                ["stateVersion"] = r.StateVersion
            }).ToArray())
        });
    }
    internal static byte[] Event(CombatCycleControlState prior, CombatCycleControlState next, CombatCycleControlInput input,
        CampaignOpeningPreambleActor author, string kind, string reason, CampaignCombatCycleAuthority? successor,
        IReadOnlyList<CampaignCombatUnitKey> expired, string? receipt)
    {
        var c = prior.Basis.Proof.Source.Base.ReleaseBase.Cycle;
        var node = new JsonObject
        {
            ["contractVersion"] = 1,
            ["eventType"] = kind switch { "control-opened" => "movement-combat-control-opened", "cycle-repeated" => "movement-combat-cycle-repeated", _ => "movement-combat-phase-finished" },
            ["author"] = Actor(author),
            ["campaignId"] = c.CampaignId,
            ["rulesetHash"] = c.RulesetHash,
            ["configurationHash"] = c.AdmittedPolicyBundleDigest,
            ["cycleId"] = input.Command.CycleId,
            ["positionId"] = prior.Basis.Proof.Source.Base.ReleaseBase.PositionId,
            ["baseHash"] = prior.BaseHash,
            ["controlId"] = prior.ControlId,
            ["priorVersion"] = prior.StateVersion,
            ["stateVersion"] = checked(prior.StateVersion + 1),
            ["priorPrefix"] = prior.Prefix,
            ["input"] = Input(input),
            ["effect"] = new JsonObject
            {
                ["kind"] = kind,
                ["reason"] = reason,
                ["assessment"] = Assessment(prior.Basis),
                ["timing"] = next.Timing is null ? null : Value(next.Timing),
                ["openingClockFailure"] = next.OpeningClockFailure,
                ["successorPositionId"] = next.PositionId,
                ["nextCycle"] = Cycle(successor),
                ["expiredUnits"] = Value(expired)
            }
        };
        if (receipt is not null) node["receiptId"] = receipt;
        return Encode(node);
    }
    public static CombatCycleControlBasis ReadBase(ReadOnlySpan<byte> bytes, CombatInheritedCycleControlSource source)
    {
        Check(bytes); var basis = source.Derive();
        if (!bytes.SequenceEqual(SerializeBase(basis))) throw new JsonException("Control base differs from authenticated source.");
        return basis;
    }
    public static CombatCycleControlState ReadState(ReadOnlySpan<byte> bytes, CombatInheritedCycleControlSource source,
        IReadOnlyList<CombatCycleControlInput> inputs, IReadOnlyList<byte[]> events)
    {
        Check(bytes); var state = CampaignCombatInheritedCycleControl.Replay(source, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Control state differs from replay.");
        return state;
    }
    internal static byte[] Copy(byte[] bytes) { ArgumentNullException.ThrowIfNull(bytes); Check(bytes); return bytes.ToArray(); }
    private static byte[] Encode(JsonNode node) { var bytes = JsonSerializer.SerializeToUtf8Bytes(node, Options); Check(bytes); return bytes; }
    private static void Check(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Control record size exceeds bounds.");
        foreach (var b in bytes) if (b <= 32 || b >= 127 || b == 92) throw new JsonException("Alternate control bytes.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        Walk(document.RootElement);
        static void Walk(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Array)
            { if (e.GetArrayLength() > 512) throw new JsonException("Control array capacity exceeded."); foreach (var c in e.EnumerateArray()) Walk(c); }
            if (e.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in e.EnumerateObject()) { if (!names.Add(p.Name)) throw new JsonException("Duplicate control key."); Walk(p.Value); }
            }
        }
    }
}

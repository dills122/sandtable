using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatResolutionCodec
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static object InputValue(CombatResolutionInput input) => new
    {
        command = input.Command,
        actor = CampaignCombatSealedRoundCodec.Actor(input.Actor),
        admittedAt = input.AdmittedAt,
        clockAvailable = input.ClockAvailable
    };
    public static byte[] SerializeInput(CombatResolutionInput input)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(input.Command);
        return Encode(InputValue(input), "ResultInput");
    }
    public static byte[] SerializeCommand(CombatResolutionCommand command) => Encode(command, "ResultCommand");
    public static CombatResolutionInput ReadInput(ReadOnlySpan<byte> bytes)
    {
        ValidateSyntax(bytes, "ResultInput");
        using var document = JsonDocument.Parse(bytes.ToArray()); var root = document.RootElement; var c = root.GetProperty("command");
        string Text(string field) => c.GetProperty(field).GetString()!;
        string? Optional(string field) => c.GetProperty(field).GetString();
        return new(new(c.GetProperty("contractVersion").GetInt32(), Text("roundClockConfigurationHash"), Text("resultClockPolicyId"),
            Text("kind"), Text("roundId"), Text("commitmentId"), c.GetProperty("expectedPriorVersion").ValueKind == JsonValueKind.Null ? null : c.GetProperty("expectedPriorVersion").GetInt64(),
            Optional("decisionId"), Optional("choice")), root.GetProperty("actor").GetString() switch
            {
                "system" => CampaignOpeningPreambleActor.System,
                "axis" => CampaignOpeningPreambleActor.Axis,
                _ => CampaignOpeningPreambleActor.Commonwealth
            }, root.GetProperty("admittedAt").ValueKind == JsonValueKind.Null ? null : root.GetProperty("admittedAt").GetInt64(), root.GetProperty("clockAvailable").GetBoolean());
    }
    public static CombatResolutionState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
        IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events)
    {
        ValidateSyntax(bytes, "ResultState");
        var state = CampaignCombatResolution.ReplayTrustedBoundary(request, created, boundary, predecessorInputs, predecessorEvents,
            roundInputs, roundEvents, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Result2 state differs from authenticated replay.");
        return state;
    }
    public static byte[] SerializeState(CombatResolutionState state)
    {
        var committed = state.Context.Committed;
        return Encode(new
        {
            contractVersion = 2,
            roundClockConfigurationHash = committed.Base.ConfigurationHash,
            resultClockPolicyId = CampaignCombatResolution.Policy,
            committedHash = state.Context.CommittedHash,
            stateVersion = state.StateVersion,
            prefix = state.Prefix,
            status = state.Status,
            result = state.Result is null ? null : ResultValue(state.Result),
            settlementId = state.World.Settlements.SingleOrDefault()?.SettlementId,
            window = (object?)null,
            acceptedHighWater = committed.Timing!.OpeningFloorUnixMilliseconds,
            world = WorldValue(state),
            randomState = state.RandomState,
            roundClosureReceiptId = (string?)null,
            caCompletionReceiptId = (string?)null,
            receipts = state.Receipts.Select(r => new
            {
                commandHash = r.CommandHash,
                eventHash = r.EventHash,
                receiptId = r.ReceiptId,
                actor = CampaignCombatSealedRoundCodec.Actor(r.Actor),
                stateVersion = r.StateVersion
            }),
            closed = false
        }, "ResultState");
    }
    internal static byte[] SerializeResultPreimage(CombatAssaultResult result)
    {
        var node = JsonSerializer.SerializeToNode(ResultValue(result), Options)!;
        node.AsObject().Remove("resultId");
        // This is the sole digest preimage, with frozen AssaultResult order minus resultId.
        return JsonSerializer.SerializeToUtf8Bytes(node);
    }
    private static object ResultValue(CombatAssaultResult result) => new
    {
        resultId = result.ResultId,
        commitmentId = result.CommitmentId,
        rulesInputHash = result.RulesInputHash,
        procedureId = result.ProcedureId,
        beforeRandomState = result.BeforeRandomState,
        afterRandomState = result.AfterRandomState,
        draws = result.Draws,
        attackerMoraleCoordinate = result.AttackerMoraleCoordinate,
        defenderMoraleCoordinate = result.DefenderMoraleCoordinate,
        attackerMorale = result.AttackerMorale,
        defenderMorale = result.DefenderMorale,
        basicDifferential = 0,
        facts = result.Facts
    };
    private static JsonNode WorldValue(CombatResolutionState state)
    {
        var paid = state.Context.Committed.World;
        CampaignCombatSettlementState? settlement = null;
        if (state.Result is { } result)
        {
            var committed = state.Context.Committed; var selection = committed.Base.Steps.Selection!;
            settlement = CampaignCombatSettlementState.CreateResolvedResultV2(committed.CommitmentId!, result.ResultId,
                committed.Base.Boundary.Cycle.GameTurn, committed.Base.Boundary.Cycle.OperationStage,
                selection.Attacker.Unit, selection.Defender.Unit, paid.Elements, result.Facts);
        }
        var allowed = settlement is null ? paid : CampaignCombatResolution.PendingWorld(paid, settlement);
        if (state.World != allowed) throw new JsonException("Result2 World differs from typed pending settlement.");
        var root = JsonNode.Parse(state.Context.WorldBytes)!;
        if (settlement is not null)
        {
            var pending = JsonSerializer.SerializeToNode(new
            {
                settlementId = settlement.SettlementId,
                commitmentId = settlement.CommitmentId,
                resultId = settlement.ResultId,
                gameTurn = settlement.GameTurn,
                operationStage = settlement.OperationStage,
                attacker = settlement.Attacker,
                defender = settlement.Defender,
                preLossElements = root["elements"]!.DeepClone(),
                result = settlement.Result,
                disposition = (object?)null,
                losses = (object?)null,
                retreat = (object?)null,
                custody = (object?)null,
                relationships = (object?)null
            }, Options);
            root["settlements"] = new JsonArray(pending);
        }
        return root;
    }
    internal static byte[] SerializeEvent(CombatResolutionState prior, CombatResolutionInput input, CombatAssaultResult result,
        string settlementId, string? receiptId)
    {
        var c = prior.Context.Committed; var b = c.Base.Boundary;
        var node = JsonSerializer.SerializeToNode(new
        {
            contractVersion = 2,
            roundClockConfigurationHash = c.Base.ConfigurationHash,
            resultClockPolicyId = CampaignCombatResolution.Policy,
            eventType = "combat-result-assault-resolved",
            author = "system",
            campaignId = b.Cycle.CampaignId,
            rulesetHash = b.Cycle.RulesetHash,
            configurationHash = c.Base.Configuration.ParentConfigurationHash,
            cycleId = CampaignCombatReserveCompletionCodec.CycleId(b.Cycle),
            segmentId = c.Base.Steps.SegmentId,
            roundId = c.RoundId,
            commitmentId = c.CommitmentId,
            priorVersion = prior.StateVersion,
            stateVersion = checked(prior.StateVersion + 1),
            priorPrefix = prior.Prefix,
            input = InputValue(input),
            effect = new { kind = "assault-resolved", result = ResultValue(result), settlementId },
            receiptId
        }, Options)!;
        if (receiptId is not null) return Encode(node, "ResultEvent");
        node.AsObject().Remove("receiptId"); return JsonSerializer.SerializeToUtf8Bytes(node);
    }
    private static byte[] Encode(object value, string kind)
    {
        var element = JsonSerializer.SerializeToElement(value, Options);
        CheckBounds(element, 0);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteSyntax(writer, element, kind);
        var bytes = stream.ToArray();
        if (bytes.Length > 1_048_576) throw new JsonException("Result2 byte bound exceeded.");
        return bytes;
    }
    internal static void ValidateSyntax(ReadOnlySpan<byte> bytes, string kind)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Result2 value.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        var canonical = Encode(document.RootElement, kind);
        if (!bytes.SequenceEqual(canonical)) throw new JsonException("Noncanonical Result2 bytes.");
    }
    private static void CheckBounds(JsonElement value, int depth)
    {
        if (depth > 32) throw new JsonException("Result2 depth bound exceeded.");
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("Result2 array bound exceeded.");
            foreach (var child in value.EnumerateArray()) CheckBounds(child, depth + 1);
        }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var field in value.EnumerateObject()) CheckBounds(field.Value, depth + 1);
    }
    private static void WriteSyntax(Utf8JsonWriter writer, JsonElement value, string kind)
    {
        if (kind.EndsWith('?'))
        {
            if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else WriteSyntax(writer, value, kind[..^1]);
            return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        {
            if (value.ValueKind != JsonValueKind.Array) throw new JsonException("Result2 array required.");
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) WriteSyntax(writer, item, kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind == "ResultEffect")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var tag) || tag.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing Result2 effect tag.");
            kind = tag.GetString() switch
            {
                "assault-resolved" => "ResolveEffect",
                "choice-opened" => "WindowEffect",
                "disposition-recorded" => "DispositionEffect",
                "losses-settled" => "LossEffect",
                "retreat-settled" => "RetreatEffect",
                "custody-settled" => "CustodyEffect",
                "relationships-settled" => "RelationsEffect",
                "round-closed" => "CloseEffect",
                "ca-completed" => "CaEffect",
                _ => throw new JsonException("Unknown Result2 effect tag.")
            };
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            if (value.ValueKind != JsonValueKind.Object) throw new JsonException("Result2 closed object required.");
            var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray(); var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || !fields.Any(f => f[0] == property.Name)) throw new JsonException("Duplicate or unknown Result2 field.");
            if (names.Count != fields.Length) throw new JsonException("Missing Result2 field.");
            writer.WriteStartObject();
            foreach (var field in fields) { writer.WritePropertyName(field[0]); WriteSyntax(writer, value.GetProperty(field[0]), field[1]); }
            writer.WriteEndObject(); return;
        }
        if (kind == "utc")
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var time) || time is < 0 or > CampaignCombatSelectionSteps.UtcMaximum)
                throw new JsonException("Invalid Result2 UTC.");
            writer.WriteNumberValue(time); return;
        }
        CampaignCombatIdentityCodec.WriteResultExternalSyntax(writer, value, kind);
    }
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["Draw"] = "purpose:id beforeCursor:ulong afterCursor:ulong die:int consumed:int[]",
        ["AssaultResult"] = "resultId:id commitmentId:id rulesInputHash:hash procedureId:id beforeRandomState:Random afterRandomState:Random draws:Draw[] attackerMoraleCoordinate:int defenderMoraleCoordinate:int attackerMorale:int defenderMorale:int basicDifferential:int facts:Result",
        ["ChoiceWindow"] = "decisionId:id kind:id owner:side timing:Timing",
        ["ResultCommand"] = "contractVersion:int roundClockConfigurationHash:hash resultClockPolicyId:id kind:id roundId:id commitmentId:id expectedPriorVersion:long? decisionId:id? choice:id?",
        ["ResultInput"] = "command:ResultCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["ResultEvent"] = "contractVersion:int roundClockConfigurationHash:hash resultClockPolicyId:id eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash segmentId:id roundId:id commitmentId:id priorVersion:long stateVersion:long priorPrefix:hash input:ResultInput effect:ResultEffect receiptId:id",
        ["ResolveEffect"] = "kind:id result:AssaultResult settlementId:id",
        ["WindowEffect"] = "kind:id window:ChoiceWindow",
        ["DispositionEffect"] = "kind:id reason:id timing:Timing? payload:Disposition",
        ["LossEffect"] = "kind:id payload:Losses",
        ["RetreatEffect"] = "kind:id payload:Retreat",
        ["CustodyEffect"] = "kind:id reason:id timing:Timing? payload:Custody",
        ["RelationsEffect"] = "kind:id payload:RelationsReceipt",
        ["CloseEffect"] = "kind:id settlementId:id proofReceipts:id[]",
        ["CaEffect"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id roundClosureReceiptId:id",
        ["ResultState"] = "contractVersion:int roundClockConfigurationHash:hash resultClockPolicyId:id committedHash:hash stateVersion:long prefix:hash status:id result:AssaultResult? settlementId:id? window:ChoiceWindow? acceptedHighWater:utc world:World randomState:Random roundClosureReceiptId:id? caCompletionReceiptId:id? receipts:Receipt[] closed:bool",
        ["Receipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["Timing"] = "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc",
    };
}

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatPositiveEntryCodec;

namespace Cna.Core.Campaigns;

internal sealed class CombatPositiveEntrySource
{
    private readonly byte[] bytes;
    internal CombatPositiveEntrySource(ReadOnlySpan<byte> value, CampaignCombatCreationContext context)
    { bytes = value.ToArray(); Context = context; }
    public byte[] CanonicalBytes => bytes.ToArray();
    internal CampaignCombatCreationContext Context { get; }
}
internal sealed class CombatPositiveEntryState
{
    private readonly byte[] bytes;
    private readonly byte[] proof;
    internal CombatPositiveEntryState(CampaignCombatBreakdownCompletionState state, JsonObject value)
    { bytes = CampaignCombatBreakdownCompletionCodec.SerializeState(state); proof = Encode(value); }
    public byte[] CanonicalBytes => bytes.ToArray();
    public byte[] ProofBytes => proof.ToArray();
    public JsonElement Projection => Parse(bytes, "CombatEntryState");
    public JsonElement Proof => Parse(proof, "PositiveEntryProof");
}
internal sealed class CombatPositiveEntryResult(CombatPositiveEntryState state, byte[] eventBytes, bool duplicate)
{
    private readonly byte[] bytes = eventBytes.ToArray();
    public CombatPositiveEntryState State { get; } = state;
    public byte[] EventBytes => bytes.ToArray();
    public bool Duplicate { get; } = duplicate;
}
/// <summary>Private creation-rooted idle entry. Selection and result authority remain separate.</summary>
internal static class CampaignCombatPositiveEntry
{
    private const string MovementKind = "complete-movement-segment";
    private const string BreakdownKind = "complete-breakdown-segment";
    private const string BreakdownPosition = "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";
    private const string CombatPosition = "land.position.operation-1.first-player.movement-and-combat.combat.position-determination";
    public static CombatPositiveEntryState Replay(CombatPositiveEntrySource source)
    {
        var (request, created, state, packet) = ReplayAuthority(source);
        CampaignCombatCandidate? candidate = null;
        if (state.Completion is not null)
        {
            Require(state.StateVersion == 13 && state.SequencePosition.PositionId == CombatPosition && state.Lifecycle.MovementEnd is not null, 6);
            var movement = state.Lifecycle.Movement;
            var weather = movement.Opening.Predecessor.Stage.Weather.Weather.Single().Kind;
            candidate = CampaignCombatCertification.CertifyInitialProfileFacts(request, created, movement.World,
                movement.Opening.Cycle!, movement.Opening.Predecessor.FirstActingSide, weather, weather);
            Require(candidate is not null, 6);
        }
        var digest = CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.positive-entry-source.v1", Bytes(packet));
        return new(state, new JsonObject
        {
            ["contractVersion"] = 1,
            ["sourceId"] = "pe." + digest[7..],
            ["sourceHash"] = digest,
            ["entry"] = JsonNode.Parse(CampaignCombatBreakdownCompletionCodec.SerializeState(state)),
            ["candidate"] = candidate is null ? null : JsonNode.Parse(CampaignCombatIdentityCodec.SerializeCandidate(candidate))
        });
    }
    public static byte[] Command(CombatPositiveEntrySource source, string kind) => Command(ReplayAuthority(source).State, kind);
    public static CombatPositiveEntryResult Apply(CombatPositiveEntrySource source, ReadOnlySpan<byte> inputBytes)
    {
        var (_, _, state, packet) = ReplayAuthority(source);
        var input = ReadInput(inputBytes);
        Authorize(state, input);
        foreach (var text in packet.GetProperty("entryEventCanonicalUtf8").EnumerateArray())
        {
            var bytes = Ascii(text);
            var accepted = ReadEvent(bytes).GetProperty("input");
            if (accepted.GetProperty("command").GetProperty("expectedPriorVersion").GetInt64() != input.GetProperty("command").GetProperty("expectedPriorVersion").GetInt64()) continue;
            Require(inputBytes.SequenceEqual(Bytes(accepted)), 6);
            return new(Replay(source), bytes, true);
        }
        var emitted = Emit(state, input).Event;
        var next = JsonNode.Parse(source.CanonicalBytes)!.AsObject();
        next["entryEventCanonicalUtf8"]!.AsArray().Add(Encoding.ASCII.GetString(emitted));
        return new(Replay(new CombatPositiveEntrySource(Encode(next), source.Context)), emitted, false);
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, CampaignCombatBreakdownCompletionState State, JsonElement Packet) ReplayAuthority(CombatPositiveEntrySource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var packet = Parse(source.CanonicalBytes, "PositiveSource");
        Require(packet.GetProperty("contractVersion").GetInt32() == 1, 3);
        var keys = new[] { "preambleEventCanonicalUtf8", "weatherEventCanonicalUtf8", "stageEventCanonicalUtf8", "reserveEventCanonicalUtf8" };
        var counts = new[] { 4, 1, 4, 1 };
        for (var i = 0; i < keys.Length; i++) Require(packet.GetProperty(keys[i]).GetArrayLength() == counts[i], 7);
        Require(packet.GetProperty("entryEventCanonicalUtf8").GetArrayLength() <= 2, 7);
        CampaignCombatCreationRequest request;
        CampaignCombatInheritedMovementState movement;
        var created = Ascii(packet.GetProperty("createdCanonicalUtf8"));
        try
        {
            request = CampaignCombatCreationRequestCodec.Deserialize(Ascii(packet.GetProperty("requestCanonicalUtf8")), source.Context);
            byte[][] History(string key) => packet.GetProperty(key).EnumerateArray().Select(Ascii).ToArray();
            // Existing NONE opening admission supports zero moves; old route lifecycle stays closed.
            movement = CampaignCombatInheritedMovement.Replay(request, created, History(keys[0]), History(keys[1]), History(keys[2]), History(keys[3]), []);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        { throw new JsonException("CMB-PEN-004", error); }
        var opening = JsonNode.Parse(source.CanonicalBytes)!.AsObject();
        opening["entryEventCanonicalUtf8"] = new JsonArray();
        var pin = movement.Opening.Predecessor.FirstActingSide == LandSide.Axis
            ? "sha256:07c0ca15a7daa89b39128da9c21a71d4e22608ab2cf0475c61bfae869ad35d8b"
            : "sha256:15e8ec05da47a61a146dee177491393f426d233b331f0d66d695ff3b781fa6e8";
        Require(CampaignOpeningPreambleCodec.Hash(Encode(opening)) == pin, 4);
        Require(movement.Opening.Cycle is { Ordinal: 1, OpenedAuthorityVersion: 11 } && request.RandomState.Seed == 1, 4);
        var lifecycle = new CampaignCombatMovementLifecycleState(movement, movement.StateVersion, movement.Prefix, movement.SequencePosition, new CampaignBreakdownFlow.Idle(), null, null, movement.Receipts, []);
        var state = new CampaignCombatBreakdownCompletionState(lifecycle, null);
        foreach (var text in packet.GetProperty("entryEventCanonicalUtf8").EnumerateArray())
        {
            var bytes = Ascii(text);
            var (after, expected) = Emit(state, ReadEvent(bytes).GetProperty("input"));
            Require(bytes.AsSpan().SequenceEqual(expected), 6);
            state = after;
        }
        return (request, created, state, packet);
    }
    private static JsonElement ReadInput(ReadOnlySpan<byte> bytes)
    {
        Require(bytes.Length is > 0 and <= 1_048_576, 1);
        try
        {
            using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
            var root = doc.RootElement;
            Require(root.ValueKind == JsonValueKind.Object && root.TryGetProperty("command", out var cmd) && cmd.ValueKind == JsonValueKind.Object, 1);
            var command = root.GetProperty("command");
            Require(command.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String && kind.GetString() is MovementKind or BreakdownKind, 3);
            return Parse(bytes, kind.GetString() == MovementKind ? "LifecycleInput" : "EntryInput");
        }
        catch (JsonException error) when (!error.Message.StartsWith("CMB-PEN-", StringComparison.Ordinal)) { throw new JsonException("CMB-PEN-001", error); }
    }
    private static JsonElement ReadEvent(byte[] bytes)
    {
        Require(bytes.Length is > 0 and <= 1_048_576, 1);
        try
        {
            using var doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 33 });
            var root = doc.RootElement;
            Require(root.ValueKind == JsonValueKind.Object, 4);
            Require(root.TryGetProperty("eventType", out var tag) && tag.ValueKind == JsonValueKind.String, 3);
            var kind = root.GetProperty("eventType").GetString() switch
            {
                "movement-segment-completed" => "CompleteEvent",
                "breakdown-segment-completed" => "EntryEvent",
                _ => throw new JsonException("CMB-PEN-003")
            };
            var result = Parse(bytes, kind);
            Require(result.GetProperty("contractVersion").GetInt32() == (kind == "CompleteEvent" ? 3 : 2), 4);
            return result;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            if (error is JsonException json && json.Message == "CMB-PEN-003") throw;
            throw new JsonException("CMB-PEN-004", error);
        }
    }
    private static void Authorize(CampaignCombatBreakdownCompletionState state, JsonElement input)
    {
        var command = input.GetProperty("command");
        Require(command.GetProperty("contractVersion").GetInt32() == 2, 3);
        var opening = state.Lifecycle.Movement.Opening;
        var expected = command.GetProperty("kind").GetString() == MovementKind ? CampaignSnapshotSerializer.FormatSide(opening.Cycle!.ActingSide) : "system";
        Require(input.GetProperty("actor").GetString() == expected, 5);
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        Require(command.GetProperty("creationBinding").GetString() == creation.CreationBinding && command.GetProperty("creationEventHash").GetString() == creation.CreationEventHash && command.GetProperty("cycleId").GetString() == opening.CycleId, 4);
    }
    private static byte[] Command(CampaignCombatBreakdownCompletionState state, string kind)
    {
        Require(kind is MovementKind or BreakdownKind, 3);
        var opening = state.Lifecycle.Movement.Opening;
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (kind == BreakdownKind)
            return CampaignCombatBreakdownCompletionCodec.SerializeInput(new(new(2, kind, 1, CampaignCombatBreakdownCompletionCodec.ActionId, creation.CreationBinding, creation.CreationEventHash, opening.CycleId!, state.StateVersion, state.SequencePosition.PositionId), CampaignOpeningPreambleActor.System));
        var identity = new CampaignCombatMovementLifecycleIdentity(2, CampaignCombatMovementLifecycleCodec.Action(2, null), creation.CreationBinding, creation.CreationEventHash, opening.CycleId!, state.StateVersion, state.SequencePosition.PositionId);
        return CampaignCombatMovementLifecycleCodec.SerializeInput(new(new CampaignCombatMovementLifecycleCommand.Complete(identity), opening.Cycle!.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth));
    }
    private static (CampaignCombatBreakdownCompletionState State, byte[] Event) Emit(CampaignCombatBreakdownCompletionState state, JsonElement input)
    {
        Authorize(state, input);
        var kind = input.GetProperty("command").GetProperty("kind").GetString()!;
        Require(Bytes(input).AsSpan().SequenceEqual(Command(state, kind)), 6);
        Require(state.Receipts.Count < 512 && state.StateVersion < long.MaxValue, 6);
        if (kind == BreakdownKind)
        {
            Require(state.StateVersion == 12 && state.Lifecycle.MovementEnd is not null && state.SequencePosition.PositionId == BreakdownPosition, 6);
            var emitted = new CampaignCombatBreakdownCompletionEvent(state.Lifecycle, CampaignCombatBreakdownCompletionCodec.DeserializeInput(Bytes(input)), Position(CombatPosition));
            return (new(state.Lifecycle, emitted), CampaignCombatBreakdownCompletionCodec.SerializeEvent(emitted));
        }
        Require(state.StateVersion == 11 && state.Completion is null && state.Lifecycle.MovementEnd is null, 6);
        var movement = state.Lifecycle.Movement;
        var pack = movement.Opening.Predecessor.Stage.Weather.Opening.Creation.Setup.Artifact.Definition;
        var locations = movement.World.Elements.Select(e => new CampaignCombatEndLocation(new(movement.World.CreationBinding, pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId), e.CurrentLocationId))
            .OrderBy(x => x.Unit.CreationBinding, StringComparer.Ordinal).ThenBy(x => x.Unit.OriginalSide, StringComparer.Ordinal).ThenBy(x => x.Unit.ElementId, StringComparer.Ordinal).ToArray();
        var owner = CampaignSnapshotSerializer.FormatSide(movement.Opening.Cycle!.ActingSide);
        var enemies = locations.Where(x => x.Unit.OriginalSide != owner).Select(x => x.LocationId).ToHashSet(StringComparer.Ordinal);
        var excluded = locations.Where(x => x.Unit.OriginalSide == owner && !WithinTwo(x.LocationId, enemies, pack.Edges)).Select(x => x.Unit).ToArray();
        var moveInput = CampaignCombatMovementLifecycleCodec.DeserializeInput(Bytes(input));
        var completed = new CampaignCombatMovementLifecycleEvent(state.Lifecycle, moveInput, Position(BreakdownPosition), new CampaignBreakdownFlow.Idle(), null, locations, excluded);
        var eventBytes = CampaignCombatMovementLifecycleCodec.SerializeEvent(completed);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(Bytes(input)), CampaignOpeningPreambleCodec.Hash(eventBytes), completed.ReceiptId, moveInput.Actor, completed.StateVersion);
        var cycle = movement.Opening.Cycle!;
        var proof = new CampaignCombatMovementEndProof(new(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide), completed.ReceiptId, locations, excluded);
        var after = new CampaignCombatMovementLifecycleState(movement, completed.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, eventBytes), completed.SequencePosition, completed.BreakdownFlow, null, proof, state.Receipts.Append(receipt), [completed]);
        return (new(after, null), eventBytes);
    }
    private static bool WithinTwo(string origin, HashSet<string> enemies, IReadOnlyList<Cna.Core.Content.ContentHexEdge> edges)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { origin };
        var frontier = new[] { origin };
        for (var distance = 0; distance <= 2; distance++)
        {
            if (frontier.Any(enemies.Contains)) return true;
            frontier = frontier.SelectMany(location => edges.Where(e => e.FirstLocationId == location || e.SecondLocationId == location).Select(e => e.FirstLocationId == location ? e.SecondLocationId : e.FirstLocationId)).Where(visited.Add).ToArray();
        }
        return false;
    }
    private static LandSequencePosition Position(string id)
    {
        var p = Cna1979LandSequence.CreateTurn(1).Single(x => x.PositionId == id);
        return new(5, p.PositionId, p.GameTurn, p.OperationStage, p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, p.ActiveSide, p.Sources);
    }
    private static byte[] Ascii(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
}

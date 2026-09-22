using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Dormant held-I no-move traversal. Every admission replays creation through first opening.</summary>
internal static class CampaignCombatInheritedReserveCycle
{
    internal sealed record Command(int ContractVersion, string Kind, string CreationBinding, string CreationEventHash,
        string CycleId, long ExpectedPriorVersion, string ExpectedPositionId, string PriorReceiptId);
    internal sealed record Input(Command Command, CampaignOpeningPreambleActor Actor);
    internal sealed class State
    {
        private readonly byte[][] events;
        internal State(CampaignCombatReserveOpeningState basis, string prefix, IEnumerable<CampaignOpeningPreambleReceipt> receipts,
            IEnumerable<byte[]> events, IReadOnlyList<CampaignCombatEndLocation> locations)
        {
            Base = basis; Prefix = prefix; Receipts = Array.AsReadOnly(receipts.ToArray());
            this.events = events.Select(e => e.ToArray()).ToArray();
            EndLocations = Array.AsReadOnly(locations.ToArray());
            BaseHash = CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveOpeningCodec.SerializeState(basis));
            SegmentId = "seg." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-reserve-cycle-segment.v1",
                CampaignCombatReserveOpeningCodec.SerializeState(basis))[7..];
        }
        public CampaignCombatReserveOpeningState Base { get; }
        public string BaseHash { get; }
        public string SegmentId { get; }
        public long StateVersion => Base.StateVersion + Receipts.Count;
        public string Prefix { get; }
        public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
        public IReadOnlyList<CampaignCombatEndLocation> EndLocations { get; }
        public int StepIndex => Math.Max(0, Receipts.Count - 4);
        public bool Closed => Receipts.Count == 10;
        public LandSequencePosition Position => PositionAfter(Receipts.Count);
        public CampaignCombatMovementEndProof? MovementEnd => Receipts.Count == 0 ? null : new(
            Base.Predecessor.Members[0].History.Scope, Receipts[0].ReceiptId, EndLocations, []);
        internal byte[][] Events => events.Select(e => e.ToArray()).ToArray();
    }
    internal sealed class Result(State state, byte[] bytes, bool duplicate)
    {
        private readonly byte[] bytes = bytes.ToArray();
        public State State { get; } = state;
        public byte[] EventBytes => bytes.ToArray();
        public bool IsDuplicate { get; } = duplicate;
    }
    public static State Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weather, IReadOnlyList<byte[]> stage,
        IReadOnlyList<byte[]> reserve, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count > 10) throw new JsonException("Held-I cycle event capacity exceeded.");
        var retained = events.Select(CampaignCombatInheritedReserveCycleCodec.CopyRecord).ToArray();
        var inputs = retained.Select(CampaignCombatInheritedReserveCycleCodec.ReadEventInput).ToArray();
        var state = Initial(CampaignCombatReserveOpening.Replay(request, created, preamble, weather, stage, reserve));
        for (var i = 0; i < retained.Length; i++)
        {
            var result = Transition(state, inputs[i]);
            if (result.IsDuplicate || !retained[i].AsSpan().SequenceEqual(result.EventBytes))
                throw new JsonException("Held-I cycle event differs from retained authority.");
            state = result.State;
        }
        return state;
    }
    public static Result Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weather, IReadOnlyList<byte[]> stage,
        IReadOnlyList<byte[]> reserve, IReadOnlyList<byte[]> events, Input input, byte[]? cachedControl = null)
    {
        var cache = cachedControl is null ? null : CampaignCombatInheritedReserveCycleCodec.CopyRecord(cachedControl);
        var state = Replay(request, created, preamble, weather, stage, reserve, events);
        if (cache is not null && !cache.AsSpan().SequenceEqual(CampaignCombatInheritedReserveCycleCodec.SerializeControl(state)))
            throw new JsonException("Held-I cycle cache differs from replay.");
        return Transition(state, input);
    }
    public static State ReadControl(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weather, IReadOnlyList<byte[]> stage,
        IReadOnlyList<byte[]> reserve, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Held-I cycle control exceeds bounds.");
        var state = Replay(request, created, preamble, weather, stage, reserve, events);
        if (!bytes.SequenceEqual(CampaignCombatInheritedReserveCycleCodec.SerializeControl(state)))
            throw new JsonException("Held-I cycle control differs from replay.");
        return state;
    }
    public static Input CreateInput(State state)
    {
        if (state.Closed) throw new JsonException("Held-I traversal is complete; Release is a separate family.");
        var receipt = state.Base.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        var kind = state.Receipts.Count switch
        {
            0 => "complete-movement-segment",
            1 => "complete-breakdown-segment",
            2 => "open-combat-selection",
            3 => "close-empty-selection",
            _ => "complete-combat-step",
        };
        return new(new(2, kind, receipt.CreationBinding, receipt.CreationEventHash, state.Base.CycleId!, state.StateVersion,
            state.Position.PositionId, state.Receipts.Count == 0 ? state.Base.CompletionReceiptId! : state.Receipts[^1].ReceiptId),
            state.Receipts.Count != 0 ? CampaignOpeningPreambleActor.System : state.Base.Cycle!.ActingSide == LandSide.Axis
                ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }
    private static State Initial(CampaignCombatReserveOpeningState basis)
    {
        var prior = basis.Predecessor;
        if (basis.Cycle is not { Ordinal: 1, GameTurn: 1, OperationStage: 1, PlayerPhaseSlot: "first-acting-side" } cycle ||
            basis.StateVersion != 12 || basis.Receipts.Count != 11 || basis.CompletionReceiptId is null ||
            basis.SequencePosition.PositionId != PositionAfter(0).PositionId || cycle.ActingSide != prior.FirstActingSide ||
            prior.Stage.Weather.Weather[0].Kind != WeatherKind.Normal || prior.Members.Count != 1 ||
            prior.Members[0].Status != CampaignElementReserveStatus.ReserveI || prior.Members[0].History.DesignationReceiptId is null ||
            prior.World.Elements.Any(e => e.OperationalState.CapabilityPointsExpended != new CapabilityPointAmount(0, 1)))
            throw new JsonException("Held-I cycle requires actual Normal Weather first opening with unspent designated Reserve I.");
        var pack = prior.Stage.Weather.Opening.Creation.Setup.Artifact.Definition;
        var locations = prior.World.Elements.Select(e => new CampaignCombatEndLocation(
            new(prior.Members[0].Unit.CreationBinding, pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId), e.CurrentLocationId))
            .OrderBy(e => e.Unit.OriginalSide, StringComparer.Ordinal).ThenBy(e => e.Unit.ElementId, StringComparer.Ordinal).ToArray();
        // This certified two-unit opening has no ordinary-proximity exclusions. Reject before
        // asserting that empty set if either participant is outside its retained two-edge reach.
        if (locations.Length != 2 || prior.World.Elements.Single(e => e.ElementId == prior.Members[0].Unit.ElementId).ReserveStatus != CampaignElementReserveStatus.ReserveI ||
            !WithinTwoEdges(locations[0].LocationId, locations[1].LocationId))
            throw new JsonException("Unsupported held-I Movement-end proximity profile.");
        return new(basis, basis.Prefix, [], [], locations);

        bool WithinTwoEdges(string origin, string destination)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal) { origin };
            for (var step = 0; step < 2; step++)
            {
                var next = pack.Edges.Where(e => reached.Contains(e.FirstLocationId) || reached.Contains(e.SecondLocationId))
                    .SelectMany(e => new[] { e.FirstLocationId, e.SecondLocationId }).ToArray();
                reached.UnionWith(next);
            }
            return reached.Contains(destination);
        }
    }
    private static Result Transition(State state, Input input)
    {
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedReserveCycleCodec.SerializeInput(input));
        var duplicate = state.Receipts.ToList().FindIndex(r => r.CommandHash == hash);
        if (duplicate >= 0) return new(state, state.Events[duplicate], true);
        if (state.Closed || state.StateVersion == long.MaxValue || input != CreateInput(state))
            throw new JsonException("Held-I cycle command, actor or occurrence mismatch.");
        var unsigned = CampaignCombatInheritedReserveCycleCodec.SerializeEvent(state, input, null);
        var receipt = "irc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-reserve-cycle-receipt.v1", unsigned)[7..];
        var bytes = CampaignCombatInheritedReserveCycleCodec.SerializeEvent(state, input, receipt);
        return new(new(state.Base, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes),
            state.Receipts.Append(new(hash, CampaignOpeningPreambleCodec.Hash(bytes), receipt, input.Actor, state.StateVersion + 1)),
            state.Events.Append(bytes), state.EndLocations), bytes, false);
    }
    internal static LandSequencePosition PositionAfter(int count)
    {
        if (count is < 0 or > 10) throw new JsonException("Held-I cycle position outside event bounds.");
        var catalog = Cna1979LandSequence.CreateTurn(1).ToArray();
        var first = Array.FindIndex(catalog, p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.movement");
        var offset = count <= 2 ? count : count <= 4 ? 2 : count - 2;
        var p = catalog[first + offset];
        return new(5, p.PositionId, p.GameTurn, p.OperationStage, p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, p.ActiveSide, p.Sources);
    }
}

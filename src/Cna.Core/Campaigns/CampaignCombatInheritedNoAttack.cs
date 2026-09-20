using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Six actual no-attack completions; arrival at Reserve Release releases nothing.</summary>
internal static class CampaignCombatInheritedNoAttack
{
    internal sealed record Command(int ContractVersion, string Kind, string SegmentId, long ExpectedPriorVersion,
        string FromPositionId, string DispositionReceiptId);
    internal sealed record Input(Command Command, CampaignOpeningPreambleActor Actor);
    internal sealed class State
    {
        private readonly byte[][] events;
        internal State(CampaignCombatInheritedSelection.State selection, long version, string prefix,
            IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<byte[]> events)
        {
            Selection = selection; StateVersion = version; Prefix = prefix; Receipts = Array.AsReadOnly(receipts.ToArray());
            this.events = events.Select(e => e.ToArray()).ToArray();
            SelectionHash = CampaignOpeningPreambleCodec.Hash(CampaignCombatIdentityCodec.SerializeSelectionControl(selection));
        }
        public CampaignCombatInheritedSelection.State Selection { get; }
        public string SelectionHash { get; }
        public long StateVersion { get; }
        public string Prefix { get; }
        public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
        public int StepIndex => Receipts.Count;
        public bool Closed => StepIndex == 6;
        public LandSequencePosition Position => Route(Selection)[StepIndex];
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
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents, IReadOnlyList<byte[]> stepEvents)
    {
        var history = CampaignCombatInheritedSelection.CaptureLocal(created, stepEvents, 6);
        var retained = history.Events.ToArray();
        var inputs = retained.Select(CampaignCombatIdentityCodec.ReadTraversalEventInput).ToArray();
        var selected = CampaignCombatInheritedSelection.Replay(request, history.CreatedSpan, predecessorEvents, selectionEvents);
        if (!selected.SelectionClosed) throw new JsonException("Traversal requires actual closed empty selection.");
        var state = new State(selected, selected.StateVersion, selected.Prefix, [], []);
        for (var index = 0; index < retained.Length; index++)
        {
            var result = Transition(state, inputs[index]);
            if (result.IsDuplicate || !retained[index].AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Traversal event differs from actual history.");
            state = result.State;
        }
        return state;
    }
    public static Result Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents, IReadOnlyList<byte[]> stepEvents, Input input, byte[]? cachedControl = null)
    {
        if (cachedControl is { Length: 0 or > 1_048_576 }) throw new JsonException("Traversal cache exceeds byte bounds.");
        var cache = cachedControl?.ToArray();
        if (cache is not null) CampaignCombatIdentityCodec.ValidateTraversalControl(cache);
        var state = Replay(request, created, predecessorEvents, selectionEvents, stepEvents);
        if (cache is not null && !cache.AsSpan().SequenceEqual(CampaignCombatIdentityCodec.SerializeTraversalControl(state))) throw new JsonException("Traversal cache differs from actual history.");
        return Transition(state, input);
    }
    public static State ReadControl(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents, IReadOnlyList<byte[]> stepEvents)
    {
        CampaignCombatIdentityCodec.ValidateTraversalControl(bytes);
        var state = Replay(request, created, predecessorEvents, selectionEvents, stepEvents);
        if (!bytes.SequenceEqual(CampaignCombatIdentityCodec.SerializeTraversalControl(state))) throw new JsonException("Traversal cache differs from actual history.");
        return state;
    }
    public static Input CreateInput(State state) => new(new(2, "complete-step", state.Selection.SegmentId, state.StateVersion,
        state.Position.PositionId, state.Selection.SelectionReceiptId!), CampaignOpeningPreambleActor.System);
    private static Result Transition(State state, Input input)
    {
        var bytes = CampaignCombatIdentityCodec.SerializeTraversalInput(input); var command = input.Command;
        if (command.ContractVersion != 2 || command.Kind != "complete-step" || input.Actor != CampaignOpeningPreambleActor.System ||
            command.SegmentId != state.Selection.SegmentId || command.DispositionReceiptId != state.Selection.SelectionReceiptId)
            throw new JsonException("Traversal command identity or actor mismatch.");
        var hash = CampaignOpeningPreambleCodec.Hash(bytes);
        var duplicate = state.Receipts.ToList().FindIndex(receipt => receipt.CommandHash == hash);
        if (duplicate >= 0) return new(state, state.Events[duplicate], true);
        if (state.Closed || state.StateVersion == long.MaxValue || command.ExpectedPriorVersion != state.StateVersion || command.FromPositionId != state.Position.PositionId)
            throw new JsonException("Traversal order, position or capacity mismatch.");
        var unsigned = CampaignCombatIdentityCodec.SerializeTraversalEvent(state, input, null);
        var receipt = "cin." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-no-attack-receipt.v2", unsigned)[7..];
        var data = CampaignCombatIdentityCodec.SerializeTraversalEvent(state, input, receipt);
        var next = new State(state.Selection, checked(state.StateVersion + 1), CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, data),
            state.Receipts.Append(new(hash, CampaignOpeningPreambleCodec.Hash(data), receipt, input.Actor, state.StateVersion + 1)), state.Events.Append(data));
        _ = CampaignCombatIdentityCodec.SerializeTraversalControl(next);
        return new(next, data, false);
    }
    internal static LandSequencePosition[] Route(CampaignCombatInheritedSelection.State selection)
    {
        var catalog = Cna1979LandSequence.CreateTurn(1).ToArray();
        var first = Array.FindIndex(catalog, p => p.PositionId == selection.Boundary.Entry.SequencePosition.PositionId);
        if (first < 0 || first + 6 >= catalog.Length) throw new JsonException("Missing Combat catalog route.");
        return catalog.Skip(first).Take(7).Select(p => new LandSequencePosition(5, p.PositionId, p.GameTurn, p.OperationStage,
            p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, p.ActiveSide, p.Sources)).ToArray();
    }
}

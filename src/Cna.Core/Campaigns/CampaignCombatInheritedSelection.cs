using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Actual G2 empty selection; no clock, decision window or structural advance.</summary>
internal static class CampaignCombatInheritedSelection
{
    internal sealed record Command(int ContractVersion, string Kind, string SegmentId, long ExpectedPriorVersion,
        string ExpectedPositionId, string? OpeningReceiptId);
    internal sealed record Input(Command Command, CampaignOpeningPreambleActor Actor);
    internal sealed class State
    {
        private readonly byte[][] events;
        internal State(CampaignCombatAdmissionBoundary boundary, long version, string prefix,
            IEnumerable<CampaignOpeningPreambleReceipt> receipts, IEnumerable<byte[]> events)
        {
            Boundary = boundary; StateVersion = version; Prefix = prefix;
            Receipts = Array.AsReadOnly(receipts.ToArray()); this.events = events.Select(e => e.ToArray()).ToArray();
            var bytes = CampaignCombatIdentityCodec.SerializeBoundary(boundary);
            BoundaryHash = CampaignOpeningPreambleCodec.Hash(bytes);
            SegmentId = "seg." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-selection-segment.v1", bytes)[7..];
        }
        public CampaignCombatAdmissionBoundary Boundary { get; }
        public string BoundaryHash { get; }
        public string SegmentId { get; }
        public long StateVersion { get; }
        public string Prefix { get; }
        public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }
        public string SelectionOutcome => Receipts.Count switch { 0 => "unopened", 1 => "system-no-selection", _ => "no-selection" };
        public string? OpeningReceiptId => Receipts.Count == 0 ? null : Receipts[0].ReceiptId;
        public string? SelectionReceiptId => SelectionClosed ? Receipts[1].ReceiptId : null;
        public bool SelectionClosed => Receipts.Count == 2;
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
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents)
    {
        var history = CaptureLocal(created, selectionEvents, 2);
        var retained = history.Events.ToArray();
        var inputs = retained.Select(CampaignCombatIdentityCodec.ReadSelectionEventInput).ToArray();
        var boundary = CampaignCombatCertification.Admit(request, history.CreatedSpan, predecessorEvents);
        var state = new State(boundary, boundary.Entry.StateVersion, boundary.Entry.Prefix, [], []);
        for (var index = 0; index < retained.Length; index++)
        {
            var result = Transition(state, inputs[index]);
            if (result.IsDuplicate || !retained[index].AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Selection event differs from actual G2 replay.");
            state = result.State;
        }
        return state;
    }
    public static Result Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents, Input input) =>
        Transition(Replay(request, created, predecessorEvents, selectionEvents), input);
    public static State ReadControl(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents)
    {
        CampaignCombatIdentityCodec.ValidateSelectionControl(bytes);
        var state = Replay(request, created, predecessorEvents, selectionEvents);
        if (!bytes.SequenceEqual(CampaignCombatIdentityCodec.SerializeSelectionControl(state))) throw new JsonException("Selection cache differs from actual history.");
        return state;
    }
    public static Input CreateInput(State state, string kind) => new(new(2, kind, state.SegmentId, state.StateVersion,
        state.Boundary.Entry.SequencePosition.PositionId, kind == "open-segment" ? null : state.OpeningReceiptId), CampaignOpeningPreambleActor.System);
    private static Result Transition(State state, Input input)
    {
        var bytes = CampaignCombatIdentityCodec.SerializeSelectionInput(input); var command = input.Command;
        if (command.ContractVersion != 2 || command.Kind is not ("open-segment" or "close-empty-selection") || input.Actor != CampaignOpeningPreambleActor.System ||
            command.SegmentId != state.SegmentId || command.ExpectedPositionId != state.Boundary.Entry.SequencePosition.PositionId ||
            (command.Kind == "open-segment" ? command.OpeningReceiptId is not null : command.OpeningReceiptId != state.OpeningReceiptId))
            throw new JsonException("Selection command identity, fields or actor mismatch.");
        var hash = CampaignOpeningPreambleCodec.Hash(bytes);
        var duplicate = state.Receipts.ToList().FindIndex(receipt => receipt.CommandHash == hash);
        if (duplicate >= 0) return new(state, state.Events[duplicate], true);
        if (state.SelectionClosed || state.StateVersion == long.MaxValue || command.ExpectedPriorVersion != state.StateVersion ||
            (command.Kind == "open-segment" ? state.Receipts.Count != 0 : state.Receipts.Count != 1)) throw new JsonException("Selection lifecycle or capacity mismatch.");
        var unsigned = CampaignCombatIdentityCodec.SerializeSelectionEvent(state, input, null);
        var receipt = "cis." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.inherited-selection-receipt.v2", unsigned)[7..];
        var data = CampaignCombatIdentityCodec.SerializeSelectionEvent(state, input, receipt);
        var next = new State(state.Boundary, checked(state.StateVersion + 1), CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, data),
            state.Receipts.Append(new(hash, CampaignOpeningPreambleCodec.Hash(data), receipt, input.Actor, state.StateVersion + 1)), state.Events.Append(data));
        _ = CampaignCombatIdentityCodec.SerializeSelectionControl(next);
        return new(next, data, false);
    }
    internal static CampaignCombatRetainedHistory CaptureLocal(ReadOnlySpan<byte> created, IReadOnlyList<byte[]> events, int limit)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (created.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized independently retained Created11.");
        var ownedCreated = created.ToArray();
        var count = events.Count;
        if (count < 0 || count > limit) throw new JsonException("Local Combat event capacity exceeded.");
        // Capture the count and each element exactly once before defensive ownership validation.
        var stable = new byte[count][];
        for (var index = 0; index < count; index++) stable[index] = events[index];
        return CampaignCombatRetainedHistory.Capture(ownedCreated, stable);
    }
}

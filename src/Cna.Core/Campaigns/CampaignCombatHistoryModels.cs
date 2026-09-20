using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Closed typed projections from accepted causal readers; no supplied family cache.</summary>
internal abstract record CampaignCombatHistoryProjection
{
    private CampaignCombatHistoryProjection() { }
    public abstract long StateVersion { get; }
    public abstract string Prefix { get; }
    public abstract LandSequencePosition SequencePosition { get; }
    public abstract IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get; }

    internal sealed record Preamble(CampaignOpeningPreambleState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }
    internal sealed record Weather(CampaignCombatWeatherState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }
    internal sealed record StageEntry(CampaignCombatStageEntryState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }
    internal sealed record ReserveOpening(CampaignCombatReserveOpeningState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }
    internal sealed record Movement(CampaignCombatInheritedMovementState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record MovementLifecycle(CampaignCombatMovementLifecycleState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record BreakdownCompletion(CampaignCombatBreakdownCompletionState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionTrigger(CampaignCombatReactionTriggerState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionLifecycle(CampaignCombatReactionLifecycleState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionClosure(CampaignCombatReactionClosureState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionFallback(CampaignCombatReactionFallbackState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionSecondMove(CampaignCombatReactionSecondMoveState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

    internal sealed record ReactionCompletion(CampaignCombatReactionCompletionState State) : CampaignCombatHistoryProjection
    {
        public override long StateVersion => State.StateVersion;
        public override string Prefix => State.Prefix;
        public override LandSequencePosition SequencePosition => State.SequencePosition;
        public override IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts => State.Receipts;
    }

}

internal sealed record CampaignCombatHistoryResult(CampaignCombatRetainedHistory History, CampaignCombatHistoryProjection Projection);

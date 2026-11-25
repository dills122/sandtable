using Cna.Core.Campaigns;

namespace Cna.Core.Exercises.Sequencing;

/// <summary>
/// How the last accepted event relates to the checkpoint cursor. Shadow-only: nothing in a
/// production path reads this (sequence executor S1, docs/work/plans/2026-11-24-pending-from-cursor-spike.md).
/// </summary>
internal enum PendingLastEvent
{
    /// <summary>The event completed the catalog position immediately before the cursor's position.</summary>
    CompletedPosition,

    /// <summary>A Move: the one Movement-segment event that leaves the cursor on the Movement position.</summary>
    MoveWithinPosition,

    /// <summary>
    /// A Reserve event (CampaignNativeSecondReserveV43). Designating an element leaves the cursor on the
    /// Reserve Designation position (observed in S2); completing it advances the cursor. The classifier
    /// cannot tell them apart, so Derive treats this value as CompletedPosition unless the cursor is on Reserve Designation.
    /// </summary>
    ReserveWithinPosition,

    /// <summary>Cycle Control Repeat: the cursor jumps back to the cycle's Movement position (observed in S0).</summary>
    CycleRepeat,
}

internal static class PendingLastEventClassifier
{
    /// <summary>Classifies from the retained history only; no new field is needed on the session.</summary>
    internal static PendingLastEvent Classify(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentHistory.Count == 0 ? PendingLastEvent.CompletedPosition : session.CurrentHistory[^1] switch
        {
            CampaignNativeSecondMoveV44 => PendingLastEvent.MoveWithinPosition,
            CampaignNativeSecondReserveV43 => PendingLastEvent.ReserveWithinPosition,
            CampaignNativeCycleControlV28 { Choice: CampaignNativeCycleControlChoiceV28.Repeat } => PendingLastEvent.CycleRepeat,
            _ => PendingLastEvent.CompletedPosition,
        };
    }
}

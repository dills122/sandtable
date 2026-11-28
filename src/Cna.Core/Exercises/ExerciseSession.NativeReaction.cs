using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

internal enum NativeReactionSlot
{
    Second = 1,
    StageEntry = 2,
    StageEntrySecond = 3,
    ThirdStage = 4,
    ThirdStageSecond = 5,
}

// The Reaction state at the last accepted event: the trigger V44 (retained by its stage slot), the last
// V44/V59/V60 and the World that event produced. Derived from history, never stored in a never-reset field.
internal sealed record NativeReactionCut(object Last, CampaignNativeSecondMoveV44 Trigger, NativeReactionSlot Slot,
    CampaignWorldSnapshotV6 World, ExerciseSession? OpenPredecessor, CampaignReactionWindow? Window)
{
    internal bool IsOpen => Window is not null;
    internal CampaignReactionWindowId WindowId => Trigger.ReactionWindow!.WindowId;
    internal string PositionId => Trigger.PositionId;
    internal long StateVersion => Last switch
    {
        CampaignNativeSecondMoveV44 move => move.StateVersion,
        CampaignNativeReactionMoveV59 moved => moved.StateVersion,
        CampaignNativeReactionClosedV60 closed => closed.StateVersion,
        _ => throw new InvalidOperationException("Unsupported Reaction cut."),
    };

    internal ExerciseCheckpoint Checkpoint(CampaignSnapshotV11 initial) => Last switch
    {
        CampaignNativeSecondMoveV44 move => new ExerciseCheckpoint(move, initial),
        CampaignNativeReactionMoveV59 moved => new ExerciseCheckpoint(moved, PositionId, initial),
        CampaignNativeReactionClosedV60 closed => new ExerciseCheckpoint(closed, initial),
        _ => throw new InvalidOperationException("Unsupported Reaction cut."),
    };
}

public sealed partial class ExerciseSession
{
    // The session that held the cut before the last V59/V60 (null until one is accepted).
    internal ExerciseSession? NativeReactionPredecessor { get; private set; }

    internal NativeReactionCut? NativeReaction
    {
        get
        {
            if (CurrentHistory is not { Count: > 0 }) return null;
            var last = CurrentHistory[^1];
            CampaignReactionWindowId id;
            CampaignReactionWindow? window;
            bool direct;
            switch (last)
            {
                case CampaignNativeSecondMoveV44 { ReactionWindow: { } opened }:
                    (id, window, direct) = (opened.WindowId, opened, true);
                    break;
                case CampaignNativeReactionMoveV59 moved:
                    (id, window, direct) = (moved.WindowId, moved.WindowAfter, false);
                    break;
                case CampaignNativeReactionClosedV60 closed:
                    (id, window, direct) = (closed.WindowId, closed.WindowAfter, false);
                    break;
                default:
                    return null;
            }
            var found = FindReactionSlot(id);
            if (found is not { } slot) return null;
            return new NativeReactionCut(last, slot.Trigger, slot.Slot, slot.World,
                direct ? slot.Predecessor : NativeReactionPredecessor, window);
        }
    }

    // Reconstruction walks every event, including the ones that close the window.
    internal PendingDecision? ReconstructionPending =>
        NativeReaction is { Last: CampaignNativeReactionMoveV59 or CampaignNativeReactionClosedV60 }
            ? new NativeReactionPending(NativeReactionPredecessor) : StagePending;

    private (NativeReactionSlot Slot, CampaignNativeSecondMoveV44 Trigger, CampaignWorldSnapshotV6 World,
        ExerciseSession? Predecessor)? FindReactionSlot(CampaignReactionWindowId id)
    {
        if (CurrentSecondMove is { ReactionWindow: { } a } second && a.WindowId == id && CurrentSecondMoveWorld is { } secondWorld)
            return (NativeReactionSlot.Second, second, secondWorld, SecondMovePredecessor);
        if (CurrentStageEntryMove is { ReactionWindow: { } b } entry && b.WindowId == id && CurrentStageEntryMoveWorld is { } entryWorld)
            return (NativeReactionSlot.StageEntry, entry, entryWorld, StageEntryMovePredecessor);
        if (CurrentStageEntrySecondMove is { ReactionWindow: { } c } entrySecond && c.WindowId == id
            && CurrentStageEntrySecondMoveWorld is { } entrySecondWorld)
            return (NativeReactionSlot.StageEntrySecond, entrySecond, entrySecondWorld, StageEntrySecondMovePredecessor);
        if (CurrentThirdStageMove is { ReactionWindow: { } d } third && d.WindowId == id && CurrentThirdStageMoveWorld is { } thirdWorld)
            return (NativeReactionSlot.ThirdStage, third, thirdWorld, ThirdStageMovePredecessor);
        if (CurrentThirdStageSecondMove is { ReactionWindow: { } e } thirdSecond && e.WindowId == id
            && CurrentThirdStageSecondMoveWorld is { } thirdSecondWorld)
            return (NativeReactionSlot.ThirdStageSecond, thirdSecond, thirdSecondWorld, ThirdStageSecondMovePredecessor);
        return null;
    }

    internal ExerciseSession WithNativeReactionMove(CampaignNativeReactionMoveV59 moved,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(moved);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(contacts);
        if (NativeReaction is not { IsOpen: true } cut || cut.WindowId != moved.WindowId
            || moved.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != moved.StateVersion
            || moved.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Native Reaction move must advance its retained World and contact cut.");
        return NativeReactionSuccessor(moved, cut.Slot, world, contacts);
    }

    internal ExerciseSession WithNativeReactionClose(CampaignNativeReactionClosedV60 closed, CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(closed);
        ArgumentNullException.ThrowIfNull(contacts);
        if (NativeReaction is not { IsOpen: true } cut || cut.WindowId != closed.WindowId
            || closed.PriorStateVersion != CurrentContacts.AuthorityVersion
            || contacts.AuthorityVersion != closed.StateVersion
            || closed.ResultWorldHash != CampaignNativeCombatPaidWorldV22.Hash(cut.World))
            throw new InvalidOperationException("Native Reaction closure must advance its retained World and contact cut.");
        return NativeReactionSuccessor(closed, cut.Slot, cut.World, contacts);
    }

    private ExerciseSession NativeReactionSuccessor(object campaignEvent, NativeReactionSlot slot,
        CampaignWorldSnapshotV6 world, CampaignContactJournal contacts)
    {
        var successor = (ExerciseSession)MemberwiseClone();
        successor.NativeReactionPredecessor = this;
        switch (slot)
        {
            case NativeReactionSlot.Second: successor.CurrentSecondMoveWorld = world; break;
            case NativeReactionSlot.StageEntry: successor.CurrentStageEntryMoveWorld = world; break;
            case NativeReactionSlot.StageEntrySecond: successor.CurrentStageEntrySecondMoveWorld = world; break;
            case NativeReactionSlot.ThirdStage: successor.CurrentThirdStageMoveWorld = world; break;
            case NativeReactionSlot.ThirdStageSecond: successor.CurrentThirdStageSecondMoveWorld = world; break;
            default: throw new InvalidOperationException("Unsupported Reaction slot.");
        }
        successor.CurrentContacts = contacts;
        successor.CurrentHistory = CurrentHistory.Add(campaignEvent);
        return successor;
    }
}

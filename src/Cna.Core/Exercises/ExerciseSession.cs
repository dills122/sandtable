using System.Collections.Immutable;
using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(CampaignSnapshotV11 snapshot, CampaignContentContext context,
        IEnumerable<object> history, CampaignContactJournal? contacts = null,
        CampaignNativeCombatDecisionStateV12? combatSelection = null,
        CampaignNativeCombatNoAttackCompletionV13? noAttackCompletion = null,
        CampaignNativeCombatBarrageV14? combatBarrage = null,
        CampaignNativeCombatRetreatDeclineV15? combatRetreatDecline = null,
        CampaignNativeCombatRetreatMoveV16? combatRetreatMove = null,
        CampaignWorldSnapshotV6? combatRetreatWorld = null,
        CampaignContactJournal? combatRetreatPriorContacts = null,
        IEnumerable<CampaignNativeCombatRetreatMoveV16>? combatRetreatMoves = null,
        IEnumerable<CampaignNativeCombatForceSealV17>? combatForceSeals = null,
        CampaignNativeCombatForceCompletionV18? combatForceCompletion = null,
        CampaignNativeCombatForceCancellationV19? combatForceCancellation = null,
        CampaignNativeCombatAntiArmorV20? combatAntiArmor = null,
        CampaignNativeCombatCancelledAssaultV21? combatCancelledAssault = null)
    {
        CurrentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ArgumentNullException.ThrowIfNull(history);
        CurrentHistory = history as ImmutableList<object> ?? ImmutableList.CreateRange(history);
        CurrentContacts = contacts ?? CampaignContactJournal.Empty(snapshot.CampaignId);
        if (combatSelection is not null && combatSelection.PriorSnapshot != snapshot)
            throw new ArgumentException("Combat selection must bind the session's prior snapshot.", nameof(combatSelection));
        if (noAttackCompletion is not null && (combatSelection is null
            || noAttackCompletion.PriorStateVersion != combatSelection.StateVersion
            || noAttackCompletion.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("No-attack completion must bind the owner decision.", nameof(noAttackCompletion));
        if (combatBarrage is not null && (combatSelection is null
            || noAttackCompletion is not null
            || combatBarrage.PriorStateVersion != combatSelection.StateVersion
            || combatBarrage.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("Barrage must bind a selected assault.", nameof(combatBarrage));
        if (combatRetreatDecline is not null && (combatBarrage is null
            || combatSelection is null
            || combatRetreatDecline.PriorStateVersion
                != (combatRetreatMove?.StateVersion ?? combatBarrage.StateVersion)
            || combatRetreatDecline.DecisionActionId != combatSelection.Decision.ActionId))
            throw new ArgumentException("RBA decline must bind the selected Barrage.",
                nameof(combatRetreatDecline));
        var moves = combatRetreatMoves?.ToArray()
            ?? (combatRetreatMove is null ? [] : [combatRetreatMove]);
        if (moves.Length > 0 && (combatRetreatMove != moves[^1]
            || combatBarrage is null
            || combatSelection is null
            || moves[0].PriorStateVersion != combatBarrage.StateVersion
            || moves.Skip(1).Where((move, index) =>
                move.PriorStateVersion != moves[index].StateVersion
                || move.DefenderElementId != moves[0].DefenderElementId
                || move.InitiallyAdjacentToKnownEnemy != moves[0].InitiallyAdjacentToKnownEnemy).Any()
            || combatRetreatMove.DecisionActionId != combatSelection.Decision.ActionId
            || combatRetreatWorld is null
            || combatRetreatPriorContacts?.AuthorityVersion != combatBarrage.StateVersion
            || combatRetreatWorld.Representations.All(value =>
                !value.BoundElementIds.Contains(combatRetreatMove.DefenderElementId)
                || value.CurrentLocationId != combatRetreatMove.DestinationLocationId)))
            throw new ArgumentException("RBA move must bind the selected Barrage and moved world.",
                nameof(combatRetreatMove));
        if (combatRetreatMove is null
            && (combatRetreatWorld is not null || combatRetreatPriorContacts is not null))
            throw new ArgumentException("RBA world and prior contact cut require a retained move.",
                nameof(combatRetreatWorld));
        var forceSeals = combatForceSeals?.ToArray() ?? [];
        if (forceSeals.Length > 2 || (forceSeals.Length > 0 && (
            combatRetreatDecline is null || combatSelection is null
            || forceSeals[0].PriorStateVersion != combatRetreatDecline.StateVersion
            || forceSeals[0].DecisionActionId != combatSelection.Decision.ActionId))
            || (forceSeals.Length == 2 && (
                forceSeals[1].PriorStateVersion != forceSeals[0].StateVersion
                || forceSeals[1].OwnerSide == forceSeals[0].OwnerSide
                || forceSeals[1].PriorSealHash is null)))
            throw new ArgumentException("Force seals must bind distinct owners and retained Combat authority.",
                nameof(combatForceSeals));
        if (combatForceCompletion is not null && (forceSeals.Length != 2
            || combatForceCompletion.PriorStateVersion != forceSeals[1].StateVersion
            || combatForceCompletion.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Force completion requires both retained owner seals.",
                nameof(combatForceCompletion));
        if (combatForceCancellation is not null && (combatForceCompletion is not null
            || forceSeals.Length > 1 || combatRetreatDecline is null
            || combatForceCancellation.PriorStateVersion
                != (forceSeals.Length == 0 ? combatRetreatDecline.StateVersion
                    : forceSeals[0].StateVersion)
            || combatForceCancellation.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Force cancellation requires an incomplete retained round.",
                nameof(combatForceCancellation));
        if (combatAntiArmor is not null && ((combatForceCompletion is null) == (combatForceCancellation is null)
            || combatAntiArmor.PriorStateVersion
                != (combatForceCompletion?.StateVersion ?? combatForceCancellation!.StateVersion)
            || combatAntiArmor.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Anti Armor must bind one retained Force outcome.",
                nameof(combatAntiArmor));
        if (combatCancelledAssault is not null && (combatAntiArmor is null
            || combatForceCancellation is null
            || combatCancelledAssault.PriorStateVersion != combatAntiArmor.StateVersion
            || combatCancelledAssault.CampaignId != snapshot.CampaignId))
            throw new ArgumentException("Cancelled Close Assault must bind retained Anti Armor.",
                nameof(combatCancelledAssault));
        CurrentCombatDecision = combatSelection;
        CurrentNoAttackCompletion = noAttackCompletion;
        CurrentCombatBarrage = combatBarrage;
        CurrentCombatRetreatDecline = combatRetreatDecline;
        CurrentCombatRetreatMove = combatRetreatMove;
        CurrentCombatRetreatWorld = combatRetreatWorld;
        CurrentCombatRetreatPriorContacts = combatRetreatPriorContacts;
        CurrentCombatRetreatMoves = Array.AsReadOnly(moves);
        CurrentCombatForceSeals = Array.AsReadOnly(forceSeals);
        CurrentCombatForceCompletion = combatForceCompletion;
        CurrentCombatForceCancellation = combatForceCancellation;
        CurrentCombatAntiArmor = combatAntiArmor;
        CurrentCombatCancelledAssault = combatCancelledAssault;
    }
    internal CampaignSnapshotV11 CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<object> CurrentHistory { get; }
    internal CampaignContactJournal CurrentContacts { get; }
    internal CampaignNativeCombatDecisionStateV12? CurrentCombatDecision { get; }
    internal CampaignNativeCombatNoAttackCompletionV13? CurrentNoAttackCompletion { get; }
    internal CampaignNativeCombatBarrageV14? CurrentCombatBarrage { get; }
    internal CampaignNativeCombatRetreatDeclineV15? CurrentCombatRetreatDecline { get; }
    internal CampaignNativeCombatRetreatMoveV16? CurrentCombatRetreatMove { get; }
    internal CampaignWorldSnapshotV6? CurrentCombatRetreatWorld { get; }
    internal CampaignContactJournal? CurrentCombatRetreatPriorContacts { get; }
    internal IReadOnlyList<CampaignNativeCombatRetreatMoveV16> CurrentCombatRetreatMoves { get; }
    internal IReadOnlyList<CampaignNativeCombatForceSealV17> CurrentCombatForceSeals { get; }
    internal CampaignNativeCombatForceCompletionV18? CurrentCombatForceCompletion { get; }
    internal CampaignNativeCombatForceCancellationV19? CurrentCombatForceCancellation { get; }
    internal CampaignNativeCombatAntiArmorV20? CurrentCombatAntiArmor { get; }
    internal CampaignNativeCombatCancelledAssaultV21? CurrentCombatCancelledAssault { get; }
    public override string ToString() => nameof(ExerciseSession);
}

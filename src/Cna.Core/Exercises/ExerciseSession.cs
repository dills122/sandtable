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
        CampaignContactJournal? combatRetreatPriorContacts = null)
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
        if (combatRetreatMove is not null && (combatBarrage is null
            || combatSelection is null
            || combatRetreatMove.PriorStateVersion != combatBarrage.StateVersion
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
        CurrentCombatDecision = combatSelection;
        CurrentNoAttackCompletion = noAttackCompletion;
        CurrentCombatBarrage = combatBarrage;
        CurrentCombatRetreatDecline = combatRetreatDecline;
        CurrentCombatRetreatMove = combatRetreatMove;
        CurrentCombatRetreatWorld = combatRetreatWorld;
        CurrentCombatRetreatPriorContacts = combatRetreatPriorContacts;
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
    public override string ToString() => nameof(ExerciseSession);
}

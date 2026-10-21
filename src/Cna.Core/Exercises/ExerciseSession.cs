using System.Collections.Immutable;
using Cna.Core.Campaigns;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public sealed class ExerciseSession
{
    internal ExerciseSession(CampaignSnapshotV11 snapshot, CampaignContentContext context,
        IEnumerable<object> history, CampaignContactJournal? contacts = null,
        CampaignNativeCombatDecisionStateV12? combatSelection = null,
        CampaignNativeCombatNoAttackCompletionV13? noAttackCompletion = null)
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
        CurrentCombatDecision = combatSelection;
        CurrentNoAttackCompletion = noAttackCompletion;
    }
    internal CampaignSnapshotV11 CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }
    internal ImmutableList<object> CurrentHistory { get; }
    internal CampaignContactJournal CurrentContacts { get; }
    internal CampaignNativeCombatDecisionStateV12? CurrentCombatDecision { get; }
    internal CampaignNativeCombatNoAttackCompletionV13? CurrentNoAttackCompletion { get; }
    public override string ToString() => nameof(ExerciseSession);
}

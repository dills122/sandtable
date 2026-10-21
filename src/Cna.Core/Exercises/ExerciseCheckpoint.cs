using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseCheckpoint
{
    public const int CurrentContractVersion = 1;

    internal ExerciseCheckpoint(CampaignSnapshotV11 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ContractVersion = CurrentContractVersion;
        CampaignId = snapshot.CampaignId;
        StateVersion = snapshot.StateVersion;
        RulesetHash = snapshot.RulesetHash;
        PositionId = CampaignCurrentActionExecution.CurrentPositionId(snapshot);
    }

    internal ExerciseCheckpoint(CampaignNativeCombatDecisionStateV12 decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ContractVersion = CampaignNativeCombatDecisionStateV12.CurrentContractVersion;
        CampaignId = decision.CampaignId;
        StateVersion = decision.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = decision.Decision.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatNoAttackCompletionV13 completion,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatNoAttackCompletionV13.CurrentContractVersion;
        CampaignId = completion.CampaignId;
        StateVersion = completion.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = completion.NextPositionId;
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
}

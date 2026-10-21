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

    internal ExerciseCheckpoint(CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatBarrageV14.CurrentContractVersion;
        CampaignId = barrage.CampaignId;
        StateVersion = barrage.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = barrage.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatRetreatDeclineV15.CurrentContractVersion;
        CampaignId = decline.CampaignId;
        StateVersion = decline.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = decline.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatMoveV16 move,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatRetreatMoveV16.CurrentContractVersion;
        CampaignId = move.CampaignId;
        StateVersion = move.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = move.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatForceSealV17 seal,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatForceSealV17.CurrentContractVersion;
        CampaignId = seal.CampaignId;
        StateVersion = seal.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = seal.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatForceCompletionV18 completion,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatForceCompletionV18.CurrentContractVersion;
        CampaignId = completion.CampaignId;
        StateVersion = completion.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = completion.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatForceCancellationV19 cancellation,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatForceCancellationV19.CurrentContractVersion;
        CampaignId = cancellation.CampaignId;
        StateVersion = cancellation.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = cancellation.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatAntiArmorV20.CurrentContractVersion;
        CampaignId = antiArmor.CampaignId;
        StateVersion = antiArmor.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = antiArmor.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatCancelledAssaultV21 cancelledAssault,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatCancelledAssaultV21.CurrentContractVersion;
        CampaignId = cancelledAssault.CampaignId;
        StateVersion = cancelledAssault.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = cancelledAssault.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatAssaultResultV22.CurrentContractVersion;
        CampaignId = result.CampaignId;
        StateVersion = result.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = result.NextPositionId;
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
}

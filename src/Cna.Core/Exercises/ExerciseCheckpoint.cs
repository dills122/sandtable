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

    internal ExerciseCheckpoint(CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (barrage.CampaignId != decision.CampaignId
            || barrage.PriorStateVersion != decision.StateVersion
            || barrage.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated Barrage must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatBarrageV14.CurrentContractVersion;
        CampaignId = barrage.CampaignId;
        StateVersion = barrage.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatDeclineV15 decline,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (decline.CampaignId != decision.CampaignId
            || decline.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated RBA decision must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatRetreatDeclineV15.CurrentContractVersion;
        CampaignId = decline.CampaignId;
        StateVersion = decline.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatMoveV16 move,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (move.CampaignId != decision.CampaignId
            || move.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated RBA move must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatRetreatMoveV16.CurrentContractVersion;
        CampaignId = move.CampaignId;
        StateVersion = move.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatForceSealV17 seal,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (seal.CampaignId != decision.CampaignId
            || seal.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated Force seal must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatForceSealV17.CurrentContractVersion;
        CampaignId = seal.CampaignId;
        StateVersion = seal.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatForceCompletionV18 completion,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (completion.CampaignId != decision.CampaignId
            || completion.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated Force completion must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatForceCompletionV18.CurrentContractVersion;
        CampaignId = completion.CampaignId;
        StateVersion = completion.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = completion.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRbaCancellationV33 cancellation,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatRbaCancellationV33.CurrentContractVersion;
        CampaignId = cancellation.CampaignId;
        StateVersion = cancellation.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = cancellation.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRbaCancellationV33 cancellation,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (cancellation.CampaignId != decision.CampaignId
            || cancellation.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated RBA cancellation must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatRbaCancellationV33.CurrentContractVersion;
        CampaignId = cancellation.CampaignId;
        StateVersion = cancellation.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = cancellation.NextPositionId;
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

    internal ExerciseCheckpoint(CampaignNativeCombatForceCancellationV19 cancellation,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (cancellation.CampaignId != decision.CampaignId
            || cancellation.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated Force cancellation must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatForceCancellationV19.CurrentContractVersion;
        CampaignId = cancellation.CampaignId;
        StateVersion = cancellation.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (antiArmor.CampaignId != decision.CampaignId
            || antiArmor.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated Anti Armor must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatAntiArmorV20.CurrentContractVersion;
        CampaignId = antiArmor.CampaignId;
        StateVersion = antiArmor.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatCancelledAssaultV21 cancelledAssault,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (cancelledAssault.CampaignId != decision.CampaignId
            || cancelledAssault.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated cancelled assault must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatCancelledAssaultV21.CurrentContractVersion;
        CampaignId = cancelledAssault.CampaignId;
        StateVersion = cancelledAssault.StateVersion;
        RulesetHash = initial.RulesetHash;
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

    internal ExerciseCheckpoint(CampaignNativeCombatAssaultResultV22 result,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (result.CampaignId != decision.CampaignId
            || result.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated result must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatAssaultResultV22.CurrentContractVersion;
        CampaignId = result.CampaignId;
        StateVersion = result.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = result.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatRetreatAfterAssaultV23.CurrentContractVersion;
        CampaignId = retreat.CampaignId;
        StateVersion = retreat.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = retreat.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatRetreatAfterAssaultV23 retreat,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (retreat.CampaignId != decision.CampaignId
            || retreat.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated retreat decision must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatRetreatAfterAssaultV23.CurrentContractVersion;
        CampaignId = retreat.CampaignId;
        StateVersion = retreat.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = retreat.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatSettlementV24.CurrentContractVersion;
        CampaignId = settlement.CampaignId;
        StateVersion = settlement.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = settlement.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatSettlementV24 settlement,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (settlement.CampaignId != decision.CampaignId
            || settlement.DecisionActionId != decision.ActionId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated settlement must bind accepted later-cycle selection.");
        ContractVersion = CampaignNativeCombatSettlementV24.CurrentContractVersion;
        CampaignId = settlement.CampaignId;
        StateVersion = settlement.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = settlement.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatCustodyDispositionV25 custody,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatCustodyDispositionV25.CurrentContractVersion;
        CampaignId = custody.CampaignId;
        StateVersion = custody.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = custody.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatCustodyDispositionV25 custody,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (custody.CampaignId != decision.CampaignId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated custody must bind accepted later-cycle campaign.");
        ContractVersion = CampaignNativeCombatCustodyDispositionV25.CurrentContractVersion;
        CampaignId = custody.CampaignId;
        StateVersion = custody.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = custody.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatSuccessorV26 successor,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCombatSuccessorV26.CurrentContractVersion;
        CampaignId = successor.CampaignId;
        StateVersion = successor.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = successor.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatSuccessorV26 successor,
        CampaignNativeRepeatCombatDecisionV31 decision,
        CampaignSnapshotV11 initial)
    {
        if (successor.CampaignId != decision.CampaignId
            || initial.CampaignId != decision.CampaignId)
            throw new ArgumentException(
                "Repeated successor must bind accepted later-cycle campaign.");
        ContractVersion = CampaignNativeCombatSuccessorV26.CurrentContractVersion;
        CampaignId = successor.CampaignId;
        StateVersion = successor.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = successor.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeReserveReleaseStateV27 reserveRelease,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeReserveReleaseEventV27.CurrentContractVersion;
        CampaignId = reserveRelease.CampaignId;
        StateVersion = reserveRelease.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = reserveRelease.Position.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeReserveReleaseStateV27 reserveRelease,
        CampaignSnapshotV11 initial)
    {
        if (reserveRelease.CampaignId != initial.CampaignId)
            throw new ArgumentException(
                "Repeated Reserve Release must bind accepted campaign.");
        ContractVersion = CampaignNativeReserveReleaseEventV27.CurrentContractVersion;
        CampaignId = reserveRelease.CampaignId;
        StateVersion = reserveRelease.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = reserveRelease.Position.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCycleControlV28 control,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeCycleControlV28.CurrentContractVersion;
        CampaignId = control.CampaignId;
        StateVersion = control.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = control.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCycleControlV28 control,
        CampaignSnapshotV11 initial)
    {
        if (control.CampaignId != initial.CampaignId)
            throw new ArgumentException(
                "Repeated cycle control must bind accepted campaign.");
        ContractVersion = CampaignNativeCycleControlV28.CurrentContractVersion;
        CampaignId = control.CampaignId;
        StateVersion = control.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = control.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeRepeatMovementV29 movement,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeRepeatMovementV29.CurrentContractVersion;
        CampaignId = movement.CampaignId;
        StateVersion = movement.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = movement.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeRepeatBreakdownV30 breakdown,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeRepeatBreakdownV30.CurrentContractVersion;
        CampaignId = breakdown.CampaignId;
        StateVersion = breakdown.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = breakdown.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeRepeatCombatDecisionV31 selection,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeRepeatCombatDecisionV31.CurrentContractVersion;
        CampaignId = selection.CampaignId;
        StateVersion = selection.StateVersion;
        RulesetHash = decision.PriorSnapshot.RulesetHash;
        PositionId = selection.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeRepeatCombatNoAttackV32 completion,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = CampaignNativeRepeatCombatNoAttackV32.CurrentContractVersion;
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

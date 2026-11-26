using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseCheckpoint
{
    public const int CurrentContractVersion = 1;

    internal ExerciseCheckpoint(CampaignGameTurn.EventInfo boundary, CampaignSnapshotV11 initial)
    {
        if (boundary.CampaignId != initial.CampaignId)
            throw new ArgumentException("Turn boundary belongs to another campaign.");
        ContractVersion = boundary.ContractVersion;
        CampaignId = boundary.CampaignId;
        StateVersion = checked(boundary.PriorStateVersion + 1);
        RulesetHash = initial.RulesetHash;
        PositionId = boundary.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignSourceTurnAuthority turn)
    {
        ArgumentNullException.ThrowIfNull(turn);
        ContractVersion = turn.World.CdSeamBound ? CampaignSourceTurnAuthority.CdSeamContractVersion
            : turn.World.MapIdentityBound ? CampaignSourceTurnAuthority.MapBoundContractVersion : CampaignSourceTurnAuthority.ContractVersion;
        CampaignId = turn.Source.Request.CampaignId;
        StateVersion = turn.StateVersion;
        RulesetHash = turn.Source.Request.RulesetHash;
        PositionId = turn.Position.PositionId;
    }

    internal ExerciseCheckpoint(CampaignSourceAuthorityState authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ContractVersion = authority.ProfileVersion;
        CampaignId = authority.Request.CampaignId;
        StateVersion = authority.StateVersion;
        RulesetHash = authority.Request.RulesetHash;
        PositionId = CampaignSourceAuthorityState.SetupPositionId;
    }

    internal ExerciseCheckpoint(CampaignPregameSourceState pregame)
    {
        ArgumentNullException.ThrowIfNull(pregame);
        ContractVersion = CampaignPregameSourceCreation.RequestVersion;
        CampaignId = pregame.Request.CampaignId;
        StateVersion = pregame.StateVersion;
        RulesetHash = pregame.Request.RulesetHash;
        PositionId = CampaignPregameSourceCreation.PositionId;
    }

    // MTS-TASK-003 (2026-11-23): the six-event Stage-entry preamble chain
    // (#518) had no ExerciseCheckpoint representation at all -- flagged as a
    // known gap by the MTS-TASK-005 Reserve slice's own doc. Each
    // constructor mirrors the CampaignNativeSecondReserveV43/initial shape
    // exactly: `initial` supplies only RulesetHash and a campaign-id
    // cross-check, never consulted for stage/turn, so these are safe for any
    // stage this chain is entered from.
    internal ExerciseCheckpoint(CampaignNativeInitiativeDeclarationV49 declared, CampaignSnapshotV11 initial)
    {
        if (declared.CampaignId != initial.CampaignId)
            throw new ArgumentException("Initiative Declaration belongs to another campaign.");
        ContractVersion = CampaignNativeInitiativeDeclarationV49.CurrentContractVersion;
        CampaignId = declared.CampaignId;
        StateVersion = declared.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = declared.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeWeatherDeterminationV50 determined, CampaignSnapshotV11 initial)
    {
        if (determined.CampaignId != initial.CampaignId)
            throw new ArgumentException("Weather Determination belongs to another campaign.");
        ContractVersion = CampaignNativeWeatherDeterminationV50.CurrentContractVersion;
        CampaignId = determined.CampaignId;
        StateVersion = determined.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = determined.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeNoObligationOrganizationV51 resolved, CampaignSnapshotV11 initial)
    {
        if (resolved.CampaignId != initial.CampaignId)
            throw new ArgumentException("Organization resolution belongs to another campaign.");
        ContractVersion = CampaignNativeNoObligationOrganizationV51.CurrentContractVersion;
        CampaignId = resolved.CampaignId;
        StateVersion = resolved.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = resolved.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeNoObligationNavalConvoyArrivalV52 resolved, CampaignSnapshotV11 initial)
    {
        if (resolved.CampaignId != initial.CampaignId)
            throw new ArgumentException("Naval Convoy Arrival resolution belongs to another campaign.");
        ContractVersion = CampaignNativeNoObligationNavalConvoyArrivalV52.CurrentContractVersion;
        CampaignId = resolved.CampaignId;
        StateVersion = resolved.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = resolved.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeNoObligationFleetAssignmentV53 resolved, CampaignSnapshotV11 initial)
    {
        if (resolved.CampaignId != initial.CampaignId)
            throw new ArgumentException("Fleet Assignment resolution belongs to another campaign.");
        ContractVersion = CampaignNativeNoObligationFleetAssignmentV53.CurrentContractVersion;
        CampaignId = resolved.CampaignId;
        StateVersion = resolved.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = resolved.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeNoObligationFleetRepairV54 resolved, CampaignSnapshotV11 initial)
    {
        if (resolved.CampaignId != initial.CampaignId)
            throw new ArgumentException("Fleet Repair resolution belongs to another campaign.");
        ContractVersion = CampaignNativeNoObligationFleetRepairV54.CurrentContractVersion;
        CampaignId = resolved.CampaignId;
        StateVersion = resolved.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = resolved.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeSecondBreakdownV46 completed, CampaignSnapshotV11 initial)
    {
        if (completed.CampaignId != initial.CampaignId)
            throw new ArgumentException("Second Breakdown belongs to another campaign.");
        ContractVersion = CampaignNativeSecondBreakdownV46.CurrentContractVersion;
        CampaignId = completed.CampaignId;
        StateVersion = completed.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = completed.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeSecondMovementCompletedV45 completed, CampaignSnapshotV11 initial)
    {
        if (completed.CampaignId != initial.CampaignId)
            throw new ArgumentException("Second Movement completion belongs to another campaign.");
        ContractVersion = CampaignNativeSecondMovementCompletedV45.CurrentContractVersion;
        CampaignId = completed.CampaignId;
        StateVersion = completed.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = completed.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeSecondMoveV44 movement, CampaignSnapshotV11 initial)
    {
        if (movement.CampaignId != initial.CampaignId)
            throw new ArgumentException("Second Movement belongs to another campaign.");
        ContractVersion = CampaignNativeSecondMoveV44.CurrentContractVersion;
        CampaignId = movement.CampaignId;
        StateVersion = movement.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = movement.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeSecondReserveV43 reserve, CampaignSnapshotV11 initial)
    {
        if (reserve.CampaignId != initial.CampaignId)
            throw new ArgumentException("Second Reserve belongs to another campaign.");
        ContractVersion = reserve.ContractVersion;
        CampaignId = reserve.CampaignId;
        StateVersion = reserve.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = reserve.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativePostCombatPassV42 passed, CampaignSnapshotV11 initial)
    {
        if (passed.CampaignId != initial.CampaignId)
            throw new ArgumentException("Post-combat pass belongs to another campaign.");
        ContractVersion = CampaignNativePostCombatPassV42.CurrentContractVersion;
        CampaignId = passed.CampaignId;
        StateVersion = passed.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = passed.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeTruckConvoyPassV41 passed, CampaignSnapshotV11 initial)
    {
        if (passed.CampaignId != initial.CampaignId)
            throw new ArgumentException("Truck Convoy pass belongs to another campaign.");
        ContractVersion = CampaignNativeTruckConvoyPassV41.CurrentContractVersion;
        CampaignId = passed.CampaignId;
        StateVersion = passed.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = passed.NextPositionId;
    }

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
        ContractVersion = decision.Decision.ContractVersion;
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

    // #394 gap 1/3: second-side analogs of the two constructors immediately
    // above, for the (World, Position)-keyed CampaignNativeSecondCombatDecisionStateV47
    // instead of the CampaignSnapshotV11-keyed CampaignNativeCombatDecisionStateV12.
    // RulesetHash still comes from TurnFacts -- a turn-wide, position-independent
    // fact that does not depend on which side's position CurrentSnapshot happens
    // to be frozen at (see that type's own remarks).
    internal ExerciseCheckpoint(CampaignNativeSecondCombatDecisionStateV47 decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ContractVersion = decision.Decision.ContractVersion;
        CampaignId = decision.CampaignId;
        StateVersion = decision.StateVersion;
        RulesetHash = decision.TurnFacts.RulesetHash;
        PositionId = decision.Decision.NextPositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCombatNoAttackCompletionV13 completion,
        CampaignNativeSecondCombatDecisionStateV47 decision)
    {
        ContractVersion = CampaignNativeCombatNoAttackCompletionV13.CurrentContractVersion;
        CampaignId = completion.CampaignId;
        StateVersion = completion.StateVersion;
        RulesetHash = decision.TurnFacts.RulesetHash;
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
        ContractVersion = retreat.ContractVersion;
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
        ContractVersion = retreat.ContractVersion;
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
        ContractVersion = custody.ContractVersion;
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
        ContractVersion = custody.ContractVersion;
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
        ContractVersion = reserveRelease.EventContractVersion;
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
        ContractVersion = reserveRelease.EventContractVersion;
        CampaignId = reserveRelease.CampaignId;
        StateVersion = reserveRelease.StateVersion;
        RulesetHash = initial.RulesetHash;
        PositionId = reserveRelease.Position.PositionId;
    }

    internal ExerciseCheckpoint(CampaignNativeCycleControlV28 control,
        CampaignNativeCombatDecisionStateV12 decision)
    {
        ContractVersion = control.ContractVersion;
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
        ContractVersion = control.ContractVersion;
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
        ContractVersion = selection.ContractVersion;
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

    private ExerciseCheckpoint(int contractVersion, string campaignId, long stateVersion,
        string rulesetHash, string positionId)
    {
        ContractVersion = contractVersion;
        CampaignId = campaignId;
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        PositionId = positionId;
    }

    // The second acting side's V14-V28 stages after its own V47 assault
    // selection: the same fields their later-cycle checkpoints expose, with
    // turn facts from the V47 selection's retained V11.
    internal static ExerciseCheckpoint ForSecondAssault(CampaignNativeRepeatAssaultRuntime runtime,
        CampaignNativeSecondCombatDecisionStateV47 decision)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(decision);
        if (!runtime.IsSecondSide || runtime.Events.Count == 0
            || runtime.Selection.Decision.ActionId != decision.Decision.ActionId)
            throw new ArgumentException("Second-side checkpoint needs an advanced second-side assault.",
                nameof(runtime));
        var ruleset = decision.TurnFacts.RulesetHash;
        return runtime.Events[^1] switch
        {
            CampaignNativeReserveReleaseEventV27 => new(runtime.Release!.EventContractVersion,
                runtime.Release.CampaignId, runtime.Release.StateVersion, ruleset,
                runtime.Release.Position.PositionId),
            CampaignNativeCycleControlV28 control => new(control.ContractVersion, control.CampaignId,
                control.StateVersion, ruleset, control.NextPositionId),
            CampaignNativeTruckConvoyPassV41 value => new(value.ContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativePostCombatPassV42 value => new(value.ContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatNoAttackCompletionV13 value => new(CampaignNativeCombatNoAttackCompletionV13.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatBarrageV14 value => new(CampaignNativeCombatBarrageV14.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatRetreatMoveV16 value => new(CampaignNativeCombatRetreatMoveV16.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.PositionId),
            CampaignNativeCombatRetreatDeclineV15 value => new(CampaignNativeCombatRetreatDeclineV15.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatRbaCancellationV33 value => new(CampaignNativeCombatRbaCancellationV33.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatForceSealV17 value => new(CampaignNativeCombatForceSealV17.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatForceCompletionV18 value => new(CampaignNativeCombatForceCompletionV18.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatForceCancellationV19 value => new(CampaignNativeCombatForceCancellationV19.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatAntiArmorV20 value => new(CampaignNativeCombatAntiArmorV20.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatCancelledAssaultV21 value => new(CampaignNativeCombatCancelledAssaultV21.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatAssaultResultV22 value => new(CampaignNativeCombatAssaultResultV22.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatRetreatAfterAssaultV23 value => new(value.ContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatSettlementV24 value => new(CampaignNativeCombatSettlementV24.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            CampaignNativeCombatCustodyDispositionV25 value => new(value.ContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.PositionId),
            CampaignNativeCombatSuccessorV26 value => new(CampaignNativeCombatSuccessorV26.CurrentContractVersion,
                value.CampaignId, value.StateVersion, ruleset, value.NextPositionId),
            _ => throw new ArgumentException("Second-side assault checkpoint has an unsupported event.",
                nameof(runtime)),
        };
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
}

using Cna.Core.Rules;

namespace Cna.Core.Exercises.Sequencing;

/// <summary>
/// Shadow derivation of <c>ExerciseSession.Pending</c> from the catalog cursor (sequence executor
/// S1; plan docs/work/plans/2026-11-24-pending-from-cursor-spike.md). Pure: no clock, Guid, RNG or
/// unordered iteration (DET-001/DET-002); tables are read by key only. Nothing in a production
/// path calls this; it exists for the S2 oracle and the later migration.
/// </summary>
internal static class PendingFromCursor
{
    private enum Mechanic { Reserve, MovementCompletion, Breakdown, CombatSelection, Combat, Truck, PostCombat }

    /// <summary>The PendingDecision subtype name for a session, or null when no decision is pending.</summary>
    internal static string? Derive(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var turn = Cna1979LandSequenceV4.CreateTurn(session.CurrentSnapshot.Setup.InitialGameTurn);
        return Derive(turn, CampaignExercises.QueryCheckpoint(session).PositionId, PendingLastEventClassifier.Classify(session));
    }

    internal static string? Derive(IReadOnlyList<LandSequencePosition> turn, string checkpointPositionId, PendingLastEvent last)
    {
        ArgumentNullException.ThrowIfNull(turn);
        var index = IndexOf(turn, checkpointPositionId);
        if (index < 0) throw new ArgumentException("Position is not in this turn.", nameof(checkpointPositionId));
        var at = turn[index];

        if (last == PendingLastEvent.MoveWithinPosition)
            return IsMovement(at) && PendingOccurrenceTable.Find(at.OperationStage, at.ActorRole) is { Prefix: { } movePrefix }
                ? movePrefix + "MovePending" : null;

        // Repeat returns the cursor to Movement while the cycle's Assault runtime is retained; the
        // occurrence's Assault arm owns it. UNVERIFIED for non-legacy occurrences: S0 can only drive
        // a Repeat on the legacy first side (Pending null), so the non-legacy answer is a hypothesis.
        if (last == PendingLastEvent.CycleRepeat)
            return IsMovement(at) && PendingOccurrenceTable.Find(at.OperationStage, at.ActorRole) is { Prefix: { } repeatPrefix, CombatArms: true }
                ? repeatPrefix + "AssaultPending" : null;

        // A Reserve event either designated an element (cursor stays on Reserve Designation) or completed
        // the phase (cursor advanced; the generic completed-position path below applies).
        if (last == PendingLastEvent.ReserveWithinPosition && at.PhaseId == LandPhaseIds.ReserveDesignation
            && PendingOccurrenceTable.Find(at.OperationStage, at.ActorRole) is { Prefix: { } reservePrefix })
            return reservePrefix + "ReservePending";

        if (index == 0) return null;
        var done = turn[index - 1];

        if (done.ActorRole is LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)
        {
            if (PendingOccurrenceTable.Find(done.OperationStage, done.ActorRole) is not { } occurrence) return null;
            if (MechanicOf(done) is not { } mechanic) return null;
            if (!occurrence.CombatArms && mechanic is Mechanic.CombatSelection or Mechanic.Combat or Mechanic.Truck or Mechanic.PostCombat)
                return null;
            if (occurrence.Prefix is null)
                return mechanic switch { Mechanic.Truck => "TruckConvoyPassPending", Mechanic.PostCombat => "PostCombatPassPending", _ => null };
            return mechanic switch
            {
                Mechanic.Reserve => occurrence.Prefix + "ReservePending",
                Mechanic.MovementCompletion => occurrence.Prefix + "MovementCompletionPending",
                Mechanic.Breakdown => occurrence.Prefix + "BreakdownPending",
                Mechanic.CombatSelection => occurrence.Prefix + "CombatSelectionPending",
                _ => occurrence.Prefix + "AssaultPending",
            };
        }

        return PendingPreambleTable.Find(done.OperationStage, done.PhaseId, done.SegmentId);
    }

    private static int IndexOf(IReadOnlyList<LandSequencePosition> turn, string positionId)
    {
        for (var i = 0; i < turn.Count; i++)
            if (turn[i].PositionId == positionId) return i;
        return -1;
    }

    private static bool IsMovement(LandSequencePosition p) =>
        p.PhaseId == LandPhaseIds.MovementAndCombat && p.SegmentId == LandSegmentIds.Movement;

    private static Mechanic? MechanicOf(LandSequencePosition p)
    {
        if (p.PhaseId == LandPhaseIds.ReserveDesignation) return Mechanic.Reserve;
        if (p.PhaseId == LandPhaseIds.MovementAndCombat)
        {
            if (p.SegmentId == LandSegmentIds.Movement) return Mechanic.MovementCompletion;
            if (p.SegmentId == LandSegmentIds.BreakdownDetermination) return Mechanic.Breakdown;
            if (p.SegmentId == LandSegmentIds.Combat && p.StepId == LandStepIds.PositionDetermination) return Mechanic.CombatSelection;
            return Mechanic.Combat;
        }
        if (p.PhaseId == LandPhaseIds.TruckConvoyMovement) return Mechanic.Truck;
        if (p.PhaseId == LandPhaseIds.CommonwealthRailMovement || p.PhaseId == LandPhaseIds.Repair || p.PhaseId == LandPhaseIds.Patrol)
            return Mechanic.PostCombat;
        return null;
    }
}

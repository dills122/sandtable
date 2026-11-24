namespace Cna.Core.Exercises;

// Overhaul doc (docs/work/plans/2026-11-23-codebase-overhaul-direction.md),
// Section 0.2/0.6 step 1: a discriminated union replacing the #394-onward
// Current*/*Predecessor field-pair cascade with a single, type-dispatchable
// value. Purely in-memory/in-process (EVT-001: never serialized, never part
// of the replay wire format -- see ExerciseSession.Pending's remarks).
//
// Scope: ONLY the mechanics introduced at #394 onward (one subtype per
// Current*/*Predecessor pair in ExerciseSession.cs today). The original
// pre-#394 Combat/Reserve-Release/Cycle-Control/Repeat chain (the dense
// hand-validated constructor's fields) is deliberately NOT represented here
// -- see the overhaul doc's 0.2 "Scope discipline" note.
internal abstract class PendingDecision
{
    internal abstract ExerciseSession? Predecessor { get; }
}

// --- This stage's own SECOND acting side (#394/MTS-TASK-006) ---

internal sealed class StageEntrySecondAssaultPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondNoAttackPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondCombatSelectionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondBreakdownPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondMovementCompletionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondMovePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntrySecondReservePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

// --- This stage's own FIRST acting side (#394/MTS-TASK-005) ---

internal sealed class StageEntryAssaultPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryNoAttackPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryCombatSelectionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryBreakdownPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryMovementCompletionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryMovePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryReservePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

// --- Prior cycle's second-acting side tail (#394 "gap 1/3") ---

internal sealed class SecondAssaultPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondNoAttackPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondCombatSelectionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondBreakdownPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondMovementCompletionPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondMovePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class SecondReservePending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

// --- Stage 1->2 entry-bridge preamble chain (MTS-TASK-002/003) ---

internal sealed class NoObligationFleetRepairPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class NoObligationFleetAssignmentPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class NoObligationNavalConvoyArrivalPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class NoObligationOrganizationPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class StageEntryWeatherPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class InitiativeDeclarationPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

// --- Prior stage's close-out (#394 base) ---

internal sealed class PostCombatPassPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

internal sealed class TruckConvoyPassPending(ExerciseSession? predecessor) : PendingDecision
{
    internal override ExerciseSession? Predecessor { get; } = predecessor;
}

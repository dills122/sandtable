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
//
// abstract record + one-line sealed-record subtypes: the same idiom already
// used for this exact shape (sealed-subtype union, one common property) in
// CombatStepsEffect (CampaignCombatSelectionStepsModels.cs) and
// CampaignPhasingContinuation/CampaignBreakdownFlow (CampaignBreakdownRecords.cs).
internal abstract record PendingDecision(ExerciseSession? Predecessor);

// The later-turn prelude reads its current cut from the last accepted history event.
internal sealed record TurnBoundaryPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Stage 3's own SECOND acting side (S3-6..S3-9: Reserve through Combat) ---

internal sealed record ThirdStageSecondAssaultPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondNoAttackPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondCombatSelectionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondBreakdownPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondMovementCompletionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondMovePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageSecondReservePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Stage 3's own FIRST acting side (S3-1..S3-4: Reserve, Movement, Breakdown, Combat Decision) ---

internal sealed record ThirdStageAssaultPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageNoAttackPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageCombatSelectionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageBreakdownPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageMovementCompletionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageMovePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageReservePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Stage 2->3 entry-bridge preamble chain (MTS-TASK-008) ---

internal sealed record ThirdStageNoObligationFleetRepairPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageNoObligationFleetAssignmentPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageNoObligationNavalConvoyArrivalPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageNoObligationOrganizationPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageWeatherPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record ThirdStageInitiativeDeclarationPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- This stage's own SECOND acting side (#394/MTS-TASK-006) ---

internal sealed record StageEntrySecondAssaultPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondNoAttackPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondCombatSelectionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondBreakdownPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondMovementCompletionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondMovePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntrySecondReservePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- This stage's own FIRST acting side (#394/MTS-TASK-005) ---

internal sealed record StageEntryAssaultPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryNoAttackPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryCombatSelectionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryBreakdownPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryMovementCompletionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryMovePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryReservePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Prior cycle's second-acting side tail (#394 "gap 1/3") ---

internal sealed record SecondAssaultPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondNoAttackPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondCombatSelectionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondBreakdownPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondMovementCompletionPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondMovePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record SecondReservePending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Stage 1->2 entry-bridge preamble chain (MTS-TASK-002/003) ---

internal sealed record NoObligationFleetRepairPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record NoObligationFleetAssignmentPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record NoObligationNavalConvoyArrivalPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record NoObligationOrganizationPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record StageEntryWeatherPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record InitiativeDeclarationPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

// --- Prior stage's close-out (#394 base) ---

internal sealed record PostCombatPassPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);
internal sealed record TruckConvoyPassPending(ExerciseSession? Predecessor) : PendingDecision(Predecessor);

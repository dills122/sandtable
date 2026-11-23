# MTS-TASK-005/006 (partial): the new stage's own Movement (2026-11-22)

**Status:** a real, test-verified slice of MTS-TASK-005/006 (generalize
Stage 2 Movement/Breakdown/Combat), not the whole checkpoint. Follows
[`2026-11-22-mts-task-005-stage-entry-reserve.md`](2026-11-22-mts-task-005-stage-entry-reserve.md)
(PR #519), which built this stage's own Reserve Designation and left the
rest of the checkpoint as "wiring repetition with a now-proven template,
not new design." This change is that template applied a second time, to
Movement + Movement Completion.

## What was built

**The new stage's own first-acting-side Movement + Movement Completion**,
reusing `CampaignNativeSecondMoveV44`/`CampaignNativeSecondMovementCompletedV45`
unchanged. Both were already role-as-data generic as of #518, and reading
their predecessor switches directly (`CampaignNativeSecondMoveV44.Source`'s
`predecessor switch` and `CampaignNativeSecondMovementCompletedV45.Create`'s
own) confirms they match on `CampaignNativeSecondReserveV43`/
`CampaignNativeSecondMoveV44` **by type**, not by which `ExerciseSession`
slot holds the value -- so a `CurrentStageEntryReserve` predecessor is
already accepted exactly like a `CurrentSecondReserve` one, with no
change needed to either native class. This confirms #519's own prediction
for the rest of the checkpoint.

- `src/Cna.Core/Exercises/ExerciseSession.cs` -- `CurrentStageEntryMove`,
  `CurrentStageEntryMoveWorld`, `StageEntryMovePredecessor`,
  `CurrentStageEntryMovementCompletion`, `StageEntryMovementCompletionPredecessor`
  fields, and `WithStageEntryMove`/`WithStageEntryMovementCompletion`, same
  `MemberwiseClone` shape and same validation as `WithSecondMove`/
  `WithSecondMovementCompletion`. Distinct slots are required for the same
  reason `CurrentStageEntryReserve` needed one: `CurrentSecondMove`/
  `CurrentSecondMovementCompletion` are already permanently bound to
  Stage 1's second acting side, and no field on this class is ever reset
  once set.
- `src/Cna.Core/Exercises/CampaignExercises.cs` -- `QueryStageEntryMovementSource`,
  `CompleteStageEntryMovement`, `QueryStageEntryMovementCut`,
  `SubmitStageEntryMovement` (mirroring `QuerySecondMovementSource`/
  `CompleteSecondMovement`/`QuerySecondMovementCut`/`SubmitSecondMovement`
  exactly, substituting `StageEntryOrders(session)` for
  `CurrentSnapshot.OperationStageOrders` the same way #519 did for Reserve);
  a `QueryCheckpoint` cascade branch for each of the two new fields,
  ordered chronologically ahead of the existing Reserve branch; a
  `Reconstruct` walk-back arm for each; and a `ReconstructOne` replay block
  for each, mirroring the `CurrentSecondMove`/`CurrentSecondMovementCompletion`
  blocks.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
  `AdvanceSecondMovementCompletion`/`AppendNativeSecondMove` already take
  the concrete event types directly, confirmed by reading both signatures.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryMovementTests.cs`
  -- new. Same "dormant, contract-level" testing depth as the Reserve
  file: no campaign in this suite can drive a real World whose element
  positions actually match this stage's Movement catalog step (that needs
  a real driven campaign, not a synthetic predecessor), so this file
  proves the **zero-elements-moved Movement Completion path** genuinely
  completes (`StageEntryMovementCompletesForTheNewStagesFirstActingSideWithNoElementsMoved`)
  -- a fully legal real-game choice, not a shortcut -- plus that the
  predecessor switch correctly rejects the opposite side
  (`StageEntryMovementRejectsTheOppositeStagesRole`). An actual
  element-moved `CampaignNativeSecondMoveV44.Create` call is not exercised
  at this depth, same limitation the Reserve file already documents.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning).
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `python3 .github/scripts/local_tests.py core --class "*StageEntry*"`:
  **75/75 passed** (73 prior + 2 new).
- `python3 .github/scripts/local_tests.py core --class "*Second*"`:
  **17/17 passed** (no regression).
- `python3 .github/scripts/local_tests.py core --class "*Reserve*"`:
  **183/183 passed**.
- `--class "*CampaignContactOwnerMovementCut*"`: **7/7 passed**.
- `--class "*CampaignContactJournal*"`: **7/7 passed**.
- `--class "*CampaignExercises*"`: **12/12 passed**.

## Honest re-scoping of what's left in MTS-TASK-005/006

Still left, same shape, now proven a third and fourth time:
Breakdown, Combat Decision/Assault/No-Attack for this stage's first
acting side -- each needs the identical `ExerciseSession` field slot +
`Query`/`Submit` pair + cascade/walk-back/replay wiring, mirroring
`WithSecondBreakdown`/`WithSecondCombatSelection`/`WithSecondAssaultEvent`/
`WithSecondNoAttack` exactly. The underlying native classes need no
changes, confirmed the same way as Reserve and Movement. MTS-TASK-006
(the new stage's second acting side) is the same repetition again, one
more time. Recommend continuing to slice one mechanic (or small related
group) per PR, consistent with this session's established practice.

Still not attempted: any Host/HTTP wiring (MTS-TASK-003/007), and the
System-audience auto-drain question for the preamble's no-obligation
mechanics -- both pre-existing open items, unchanged by this slice.

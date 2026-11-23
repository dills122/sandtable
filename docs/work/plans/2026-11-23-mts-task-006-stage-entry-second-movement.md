# MTS-TASK-006 (partial): this stage's own SECOND acting side's Movement (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-006. Follows
[`2026-11-23-mts-task-006-stage-entry-second-reserve.md`](2026-11-23-mts-task-006-stage-entry-second-reserve.md)
(PR #531), which began MTS-TASK-006 with this stage's second acting side's
Reserve Designation. This slice is that template applied a second time, to
Movement + Movement Completion -- confirmed, not assumed, to need no native-
class change.

## Checked first: does the Movement native class have Breakdown's problem? No.

`CampaignNativeSecondMoveV44.Source`, read in full before writing any
wiring, matches its predecessor by type alone
(`CampaignNativeSecondReserveV43 reserve when reserve.Completed`), not by
which `ExerciseSession` slot holds it -- the same confirmation every prior
Movement-reusing slice has made. A `CampaignNativeSecondReserveV43` object
sourced from `CurrentStageEntrySecondReserve` is accepted identically to one
sourced from any of the three prior slots. This is a sixth occurrence of the
same two event types, not a new one.

## What was built

- `src/Cna.Core/Exercises/ExerciseSession.cs` -- `CurrentStageEntrySecondMove`,
  `CurrentStageEntrySecondMoveWorld`, `StageEntrySecondMovePredecessor`,
  `CurrentStageEntrySecondMovementCompletion`,
  `StageEntrySecondMovementCompletionPredecessor` fields, and
  `WithStageEntrySecondMove`/`WithStageEntrySecondMovementCompletion`, same
  `MemberwiseClone` shape and guard style as every prior `WithStageEntryXxx`.
  A distinct slot is required for the same reason every prior Stage Entry
  slot needed one.
- `src/Cna.Core/Exercises/CampaignExercises.cs` -- `QueryStageEntrySecondMovementSource`,
  `CompleteStageEntrySecondMovement`, `QueryStageEntrySecondMovementCut`,
  `SubmitStageEntrySecondMovement`, mirroring the first-acting-side
  equivalents exactly, entered from this stage's own completed second-side
  Reserve Designation (`CurrentStageEntrySecondReserve`) instead of the
  first-acting-side chain; `QueryCheckpoint` cascade branches for both new
  fields, inserted ahead of the existing second-side Reserve branch
  (chronologically later, same ordering discipline as every cascade in this
  file); a predecessor-walk arm in `Reconstruct`'s native-successor switch
  for each; `ReconstructOne` replay blocks for each, inserted ahead of the
  existing second-side Reserve block for the same ordering reason.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
  `AdvanceSecondMovementCompletion`/`AppendNativeSecondMove` already take
  the concrete event types directly, confirmed by reading both signatures.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntrySecondMovementTests.cs`
  -- new, at the richer native-class-level depth
  `CampaignNativeStageEntryMovementTests` itself reaches (both native
  classes take typed predecessors directly, so no `ExerciseSession` is
  needed to exercise the real production logic). Proves the zero-elements-
  moved Movement Completion path genuinely completes for the `SecondSide`
  role, plus that the predecessor switch correctly rejects the opposite
  side.

## A test-construction correction made before landing (not a product bug)

The first draft of this slice's test helper tried to feed the six-event
preamble's terminal `CampaignNativeNoObligationFleetRepairV54` directly into
`CampaignNativeSecondReserveV43.Create` with `order.SecondSide`, mirroring
the *first*-acting-side Reserve test's own helper too literally. This threw
("Second Reserve requires its owner and settled World.") -- correctly, by
design: the preamble's terminal event's own `NextPositionId` resolves to
this stage's **first** acting side's Reserve position, never the second's,
exactly confirming this slice's own design finding (MTS-TASK-006 is entered
from the first acting side's own completed chain, not from the preamble).
Fixed by constructing a synthetic `CampaignNativePostCombatPassV42`
representing this stage's own `OperationStage == 2, FirstActingSide, Patrol`
completion directly (the same shape `QueryStageEntrySecondReserveSource`
itself consumes from `CurrentStageEntryAssault.PostCombatPasses[^1]` in
production), and a directly-constructed `CampaignOperationStageOrder`
instead of deriving one from the preamble. This was a test-setup mistake
caught before the PR landed, not a defect in the production guard or native
class, which both behaved correctly throughout.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). `Cna.Analyzers`'
  CNA001/CNA002 rules (live since #522) did not flag anything in this
  change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `--class "*StageEntrySecondMovement*"`: **2/2 passed** (the two new
  tests, after the test-construction fix above).
- `--class "*StageEntry*"`: **83/83 passed** (81 prior + 2 new).
- `--class "*Second*"`: **21/21 passed** (19 prior + 2 new; no regression).
- `--class "*Reserve*"`: **185/185 passed** (no regression).
- `--class "*CampaignExercises*"`: **12/12 passed** (no regression).
- `--class "*CampaignContactOwnerMovementCut*"`: **7/7 passed** (no
  regression).

## Honest re-scoping of what's left in MTS-TASK-006

Still left, same shape: Breakdown, Combat Decision/Assault/No-Attack, all
for this stage's second acting side. Breakdown specifically needs the same
"check the native class's actual predecessor-matching shape first" caution
MTS-TASK-005's own Breakdown slice established -- `CampaignNativeSecondBreakdownV46.CreateForStageEntry`
(built for MTS-TASK-005) reads `CurrentStageEntryMovementCompletion`/
`StageEntryMovePredecessor`/`CurrentStageEntryMoveWorld` directly, all
first-acting-side slots, so it will need its own third entry point
(`CreateForStageEntrySecondSide` or similar) for this stage's second acting
side, mirroring `Create`/`CreateForStageEntry`'s own relationship -- not
assumed here, left for that slice to confirm and build.

The Combat Decision/Assault piece should be cheaper than its MTS-TASK-005
counterpart: the Tier 1/Tier 2 central-dispatch-point discovery from that
slice means `CurrentRepeatAssault`/`WithRepeatAssaultEvent` need a
**fourth** branch (this stage's second acting side), not a from-scratch
investigation, and the 11 Tier 2 methods (including `QueryPostCombatPassSource`,
confirmed by reading it directly to already carry a 3-way ternary --
`CurrentStageEntryAssault` / `CurrentSecondAssault` / first-side fallback --
that will need to become 4-way) each need one more explicit branch
following the now twice-proven pattern.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics.

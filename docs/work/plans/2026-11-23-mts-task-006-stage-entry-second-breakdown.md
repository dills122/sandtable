# MTS-TASK-006 (partial): this stage's own SECOND acting side's Breakdown (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-006. Follows
[`2026-11-23-mts-task-006-stage-entry-second-movement.md`](2026-11-23-mts-task-006-stage-entry-second-movement.md)
(PR #532), which flagged exactly this slice's own finding ahead of time:
`CampaignNativeSecondBreakdownV46`'s `CreateForStageEntry` (built for
MTS-TASK-005) reads `CurrentStageEntryMovementCompletion`/
`StageEntryMovePredecessor`/`CurrentStageEntryMoveWorld` directly, all
first-acting-side slots, so it needs its own third entry point for this
stage's second acting side -- confirmed by reading `Create`/
`CreateForStageEntry` directly, not assumed.

## What was built

- `src/Cna.Core/Campaigns/CampaignNativeSecondBreakdownV46.cs` --
  `CreateForStageEntrySecondSide(ExerciseSession session)`, a third entry
  point. Every check, invariant, and the resulting event's contract are
  byte-for-byte identical to `Create`/`CreateForStageEntry` -- only which
  `ExerciseSession` slots are read differs (`CurrentStageEntrySecondMovementCompletion`/
  `StageEntrySecondMovementCompletionPredecessor`/
  `StageEntrySecondMovePredecessor`/`CurrentStageEntrySecondMoveWorld`
  instead of the first-acting-side or Stage 1 second-side equivalents).
  Same reasoning as `CreateForStageEntry`'s own remarks for choosing a
  duplicate method over parameterizing `Create`: mirrors this codebase's
  existing per-generation duplication convention rather than introducing
  new delegate-based parameterization for one call site.
- `src/Cna.Core/Exercises/ExerciseSession.cs` -- `CurrentStageEntrySecondBreakdown`,
  `StageEntrySecondBreakdownPredecessor` fields and
  `WithStageEntrySecondBreakdown`, same `MemberwiseClone` shape and guard
  style as every prior `WithStageEntryXxx`, guarded on
  `CurrentStageEntrySecondMovementCompletion` instead of the first-acting-side
  equivalent.
- `src/Cna.Core/Exercises/CampaignExercises.cs` -- `CompleteStageEntrySecondBreakdown`,
  mirroring `CompleteStageEntryBreakdown` exactly (same System-audience,
  no-owner-decision shape); a `QueryCheckpoint` cascade branch, inserted
  ahead of the existing second-side Movement Completion branch
  (chronologically later); a predecessor-walk arm; a `ReconstructOne`
  replay block, inserted ahead of the existing second-side Movement
  Completion block for the same ordering reason.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
  `AdvanceSecondBreakdown(CampaignNativeSecondBreakdownV46 completed)`
  already takes the concrete event type directly, confirmed by reading the
  signature.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntrySecondBreakdownTests.cs`
  -- new, same depth as `CampaignNativeStageEntryBreakdownTests` and for
  the same reason: `CreateForStageEntrySecondSide` takes the whole
  `ExerciseSession`, so reaching a genuine success path needs a real driven
  campaign and the Host/HTTP wiring that does not exist yet. What is
  tested directly: both `CreateForStageEntrySecondSide` and
  `CompleteStageEntrySecondBreakdown` correctly reject a real, legitimately
  constructed session that has not reached a completed second-side
  Movement Completion.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). `Cna.Analyzers`'
  CNA001/CNA002 rules (live since #522) did not flag anything in this
  change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `--class "*StageEntrySecondBreakdown*"`: **2/2 passed** (the two new
  tests).
- `--class "*StageEntry*"`: **85/85 passed** (83 prior + 2 new).
- `--class "*Second*"`: **23/23 passed** (21 prior + 2 new; no
  regression).
- `--class "*CampaignExercises*"`: **12/12 passed** (no regression).
- `--class "*CampaignCombatExerciseAssault*"`: **6/6 passed** (the real
  driven-campaign suite exercising Stage 1's Second Breakdown end to end
  via the unmodified `Create` path, confirmed unaffected).

## Honest re-scoping of what's left in MTS-TASK-006

Still left, same shape: Combat Decision/Assault/No-Attack for this stage's
second acting side. As flagged in the Movement slice's own doc: this piece
should be cheaper than its MTS-TASK-005 counterpart. `CampaignNativeSecondCombatDecisionStateV47.Select`
is already purely parameterized (confirmed when MTS-TASK-005's own Combat
Decision slice built it, no reason to expect a Breakdown-style deviation
here), and the Tier 1/Tier 2 central-dispatch-point discovery means
`CurrentRepeatAssault`/`WithRepeatAssaultEvent` need a **fourth** branch
(this stage's second acting side) rather than a from-scratch
investigation, with the 11 Tier 2 methods each needing one more explicit
branch following the now three-times-proven pattern.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics.

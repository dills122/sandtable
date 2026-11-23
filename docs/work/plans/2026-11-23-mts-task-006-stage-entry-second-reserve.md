# MTS-TASK-006 (partial): this stage's own SECOND acting side's Reserve Designation (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-006 (generalize this
stage's second acting side), not the whole checkpoint. Follows
[`2026-11-23-mts-task-005-006-stage-entry-assault.md`](2026-11-23-mts-task-005-006-stage-entry-assault.md)
(PR #529), which completed MTS-TASK-005: this stage's own first acting side
is now fully generalized through the entire Combat chain. This slice begins
MTS-TASK-006 -- the same repetition again, one more time, for this stage's
second acting side.

## The entry question, investigated first (not a design fork -- one correct answer by direct precedent)

Every prior Stage Entry mechanic was entered from an existing, already-named
`ExerciseSession` field representing "the immediately preceding step just
completed" (e.g. Reserve from `CurrentNoObligationFleetRepair`, Movement from
`CurrentStageEntryReserve`). MTS-TASK-006's own entry point -- "this stage's
first acting side has fully finished, time for the second acting side to
begin" -- had no existing precedent *within this stage's own chain*, since
nothing downstream of any prior "later acting side" occurrence had ever been
built before (Stage 1's own `#394` second side is the last side within Stage
1; nothing follows it there).

The real precedent lives one level up, in
[`2026-11-22-stage-entry-bridge-design-options.md`](2026-11-22-stage-entry-bridge-design-options.md)'s
"actually-relevant precedent" section: `CampaignNativePostCombatPassV42`'s own
`NextPositionId` is already fully generic and catalog-position-driven via
`Cna1979LandSequenceV4.GetNext`, and `CampaignNativeSecondReserveV43.Create`'s
predecessor switch already accepts a `CampaignNativePostCombatPassV42`
instance regardless of which `ExerciseSession` field it came from (confirmed
by reading both directly) -- that is exactly how `#394`'s own second side was
entered from the first side's singular `CurrentPostCombatPass`. The only
difference for MTS-TASK-006 is *where* the predecessor object lives:
`CurrentStageEntryAssault` (this stage's first acting side's own
`CampaignNativeRepeatAssaultRuntime`, confirmed by the Assault slice's own
Tier 2 wiring to retain a real `PostCombatPasses` list through Reserve
Release/Cycle Control/Truck Convoy/Post-Combat-Pass, the same tail `#394`'s
second side carries) rather than a singular field, since this stage's first
acting side is itself a "later occurrence" entered via `ReplaySecond`, not
the campaign's very first (pre-`#394`) chain.

This resolved to one sane answer, not several architecture directions to
weigh -- no separate design-options doc needed, same reasoning every prior
"investigate first, then just do the one correct thing" slice used
(Breakdown's session-slot finding, Combat Decision's tier split, Assault's
own Tier 1/Tier 2 discovery).

## What was built

- `src/Cna.Core/Exercises/ExerciseSession.cs` -- `CurrentStageEntrySecondReserve`,
  `CurrentStageEntrySecondReserveWorld`, `StageEntrySecondReservePredecessor`
  fields and `WithStageEntrySecondReserve`, same `MemberwiseClone` shape and
  guard style as every prior `WithStageEntryXxx`, guarded on
  `CurrentStageEntryAssault is { PostCombatPasses.Count: > 0 }` instead of a
  single terminal field. A distinct slot is required for the same reason
  every prior Stage Entry slot needed one: `CurrentStageEntryReserve` is
  already permanently bound to this stage's first acting side, and no field
  on this class is ever reset once set.
- `src/Cna.Core/Exercises/CampaignExercises.cs` -- `QueryStageEntrySecondReserveSource`
  (reading `CurrentStageEntryAssault.PostCombatPasses[^1]` as the predecessor
  and `CurrentStageEntryAssault.Release.World` as the carried-forward World,
  the direct analogue of how `QueryStageEntryReserveSource` itself carries
  forward `CurrentSecondAssault.Release.World` -- Stage 1's second side's
  settled world -- when entering Stage 2; same "carry forward whichever chain
  just finished" pattern, one stage-internal side-transition later) and
  `SubmitStageEntrySecondReserve`, mirroring `QueryStageEntryReserveSource`/
  `SubmitStageEntryReserve` exactly; `StageEntryOrders(session)` is reused
  unchanged (the stage's own `CampaignOperationStageOrder` does not change
  between this stage's two acting sides, only which side is currently
  acting, resolved inside the native class via `#518`'s `ActingRole`); a
  `QueryCheckpoint` cascade branch, inserted ahead of the existing
  `CurrentStageEntryAssault` block (chronologically later, same ordering
  discipline as every cascade in this file); a predecessor-walk arm in
  `Reconstruct`'s native-successor switch; a `ReconstructOne` replay block,
  inserted ahead of the existing `CurrentStageEntryAssault` block for the
  same ordering reason.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
  `AdvanceSecondReserve(CampaignNativeSecondReserveV43 reserve)` already
  takes the concrete event type directly, confirmed by reading the
  signature (same confirmation every prior Reserve-reusing slice made).
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntrySecondReserveTests.cs`
  -- new, same depth as every prior Stage Entry test file (see below).

## Testing depth: same limitation as every prior slice, and why

Reaching a session with `CurrentStageEntryAssault`'s own `PostCombatPasses`
genuinely populated needs this stage's own complete first-acting-side chain
(Reserve through Custody, including its own Reserve Release/Cycle
Control/Truck Convoy/Post-Combat-Pass tail) driven through a real campaign,
which needs the Host/HTTP wiring that does not exist yet (`MTS-TASK-003`/
`007`, the same pre-existing gap every prior slice has documented). What is
tested directly, with a real, legitimately-constructed `ExerciseSession` (a
fresh campaign genesis via `CampaignExercises.Begin`): both
`QueryStageEntrySecondReserveSource` and `SubmitStageEntrySecondReserve`
correctly reject a session that has not reached a completed first-acting-side
chain -- same depth and reasoning as every prior Stage Entry test file.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). `Cna.Analyzers`'
  CNA001/CNA002 rules (live since #522) did not flag anything in this
  change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `--class "*StageEntrySecondReserve*"`: **2/2 passed** (the two new tests).
- `--class "*StageEntry*"`: **81/81 passed** (79 prior + 2 new).
- `--class "*Second*"`: **19/19 passed** (17 prior + 2 new; no regression).
- `--class "*Reserve*"`: **185/185 passed** (183 prior + 2 new).
- `--class "*CampaignExercises*"`: **12/12 passed** (no regression).

## Honest re-scoping of what's left in MTS-TASK-006

Still left, same shape as MTS-TASK-005's own remaining pieces were before
each was built: Movement + Movement Completion, Breakdown, Combat
Decision/Assault/No-Attack, all for this stage's second acting side.
Recommend continuing to check each remaining native class's actual
predecessor-matching shape directly before assuming it follows this slice's
template rather than a Breakdown-style deviation (same caution every slice
since Breakdown's own finding has carried forward). The Combat
Decision/Assault piece should be cheaper than MTS-TASK-005's own version of
it: the Tier 1/Tier 2 central-dispatch-point discovery from the Assault
slice means `CurrentRepeatAssault`/`WithRepeatAssaultEvent` need a **fourth**
branch (this stage's second acting side), not a from-scratch investigation,
and the 11 Tier 2 methods each need one more explicit branch following the
now twice-proven pattern.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics.

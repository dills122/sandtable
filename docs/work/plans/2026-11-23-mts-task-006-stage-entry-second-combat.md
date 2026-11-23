# MTS-TASK-006: this stage's own SECOND acting side's Combat Decision/Assault/No-Attack (2026-11-23)

**Status:** the final slice of MTS-TASK-006, completing this stage's
second acting side generalization. Follows
[`2026-11-23-mts-task-006-stage-entry-second-breakdown.md`](2026-11-23-mts-task-006-stage-entry-second-breakdown.md)
(PR #533), which flagged this piece as cheaper than its MTS-TASK-005
counterpart -- confirmed true: no new design investigation was needed, the
Tier 1/Tier 2 central-dispatch-point pattern discovered in the first
acting side's own Assault slice
([`2026-11-23-mts-task-005-006-stage-entry-assault.md`](2026-11-23-mts-task-005-006-stage-entry-assault.md),
PR #529) extended to a fourth branch mechanically.

## Checked first: does the Combat Decision native class have Breakdown's problem? No.

`CampaignNativeSecondCombatDecisionV47.Select`, read in full before writing
any wiring, is already fully parameterized (`world`, `position`,
`turnFacts`, `randomState`, `context`, `contacts`, `side`,
`actingElementId`, `actionId`) -- it never reads an `ExerciseSession` field
directly, confirmed the same way MTS-TASK-005's own Combat Decision slice
confirmed it for a third occurrence. A fourth occurrence needs no new
native-class entry point.

## What was built

**`ExerciseSession.cs`:**
- `CurrentStageEntrySecondCombatDecision`/`StageEntrySecondCombatSelectionPredecessor`
  fields and `WithStageEntrySecondCombatSelection`, mirroring
  `WithStageEntryCombatSelection` exactly, guarded on
  `CurrentStageEntrySecondBreakdown` instead of the first-acting-side
  equivalent.
- `CurrentStageEntrySecondAssault`/`StageEntrySecondAssaultPredecessor`, the
  distinct runtime slot for this stage's second acting side (same reason
  every prior Stage Entry slot needed one), and `WithStageEntrySecondAssaultEvent`,
  mirroring `WithStageEntryAssaultEvent` exactly.
- `CurrentStageEntrySecondNoAttackCompletion`/`StageEntrySecondNoAttackPredecessor`
  and `WithStageEntrySecondNoAttack`, mirroring `WithStageEntryNoAttack`
  exactly.
- `CurrentRepeatAssault` extended from a three-way to a **four-way**
  null-coalesce: `CurrentStageEntrySecondAssault` (checked first,
  chronologically latest) `??` `CurrentStageEntryAssault` `??`
  `CurrentSecondAssault` `??` `firstSideRepeatAssault`.
- `WithRepeatAssaultEvent` extended with a `CurrentStageEntrySecondAssault
  is not null` branch ahead of the existing `CurrentStageEntryAssault` one.

**`CampaignExercises.cs`** -- every one of the sites the Assault slice's
own doc named as needing a fourth branch, confirmed and extended:
- `QueryStageEntrySecondCombatWorldAndPosition`, mirroring
  `QueryStageEntryCombatWorldAndPosition` exactly.
- `HasAdvancedCombatSelection` extended to a four-way check
  (`CurrentStageEntrySecondBreakdown` checked first).
- `QueryContactOwnerCombatCut` extended with a fourth branch ahead of the
  existing Stage Entry one.
- `SubmitContactOwnerCombatSelection` extended: the `isStageEntry`/
  `isSecondSide` pair became `isStageEntrySecondSide`/`isStageEntry`/
  `isSecondSide` (mutually exclusive by construction -- this stage's
  second acting side can only be reached after its own Breakdown, which
  itself requires the full first-acting-side chain to have resolved), with
  a new `if (isStageEntrySecondSide)` branch mirroring the existing
  `isStageEntry` branch's shape exactly.
- `CompleteStageEntrySecondCombatNoAttack`, mirroring
  `CompleteStageEntryCombatNoAttack` exactly.
- **Tier 1** (the ~10 methods that dispatch generically through
  `CurrentRepeatAssault`/`WithRepeatAssaultEvent`): zero additional
  changes needed beyond the two central-point extensions above --
  confirmed this is exactly the future-proofing payoff the Assault slice's
  own doc predicted ("every Tier 1 method Stage-Entry-aware with zero
  changes to the methods themselves").
- **Tier 2** (the 11 methods that bypass `CurrentRepeatAssault` on
  purpose): each of `QueryNativeReserveReleaseState`,
  `SubmitNativeReserveReleaseChoice`, `CompleteNativeReserveRelease`,
  `QueryPendingNativeCycleControl`, `TimeoutNativeReserveRelease`,
  `TimeoutNativeCycleControl`, `QueryPostCombatPassSource`,
  `SubmitPostCombatPass`, `QueryTruckConvoyPassSource`,
  `SubmitTruckConvoyPass`, and `SubmitNativeCycleControl` extended with a
  `CurrentStageEntrySecondAssault` branch checked first, mirroring each
  method's own existing `CurrentStageEntryAssault` branch exactly.
- `QueryCheckpoint`: three new branches (`CurrentStageEntrySecondAssault`,
  `CurrentStageEntrySecondNoAttackCompletion`,
  `CurrentStageEntrySecondCombatDecision`), inserted ahead of the existing
  second-side Breakdown block (chronologically latest, same ordering
  discipline as every cascade in this file).
- `ReconstructOne`: three new blocks mirroring the existing
  `CurrentStageEntryAssault`/`CurrentStageEntryNoAttackCompletion`/
  `CurrentStageEntryCombatDecision` blocks exactly, inserted ahead of them
  for the same ordering reason. No change to `Reconstruct`'s own
  native-successor predecessor-walk switch: confirmed (same way the
  first-acting-side Assault slice confirmed) that switch has never carried
  Combat Decision/Assault/No-Attack arms for any occurrence -- it stops at
  Breakdown by design.

`src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
`AdvanceSecondCombatSelection`/`AdvanceNativeCombatNoAttackCompletion`
already take the concrete event types directly.

## What was built: tests

`tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntrySecondCombatSelectionTests.cs`
and `CampaignNativeStageEntrySecondCombatAssaultTests.cs` -- new, same
depth as `CampaignNativeStageEntryCombatSelectionTests`/
`CampaignNativeStageEntryCombatAssaultTests` and for the same reason:
`WithStageEntrySecondAssaultEvent`, `CurrentRepeatAssault`'s new four-way
resolution, and `WithRepeatAssaultEvent`'s new four-way dispatch all
operate on a real `CampaignNativeRepeatAssaultRuntime`, which only accepts
a real `ExerciseSession`, not typed parameters -- there is no way to reach
a genuine success path without a real driven session, which does not
exist as a fixture yet. What proves those changes correct instead is the
full existing regression suite, which reads `CurrentRepeatAssault`/
`WithRepeatAssaultEvent` on every one of its own call sites today: a
defect in the new branch would surface as one of those tests failing, not
just as a missing new test. What's tested directly, same depth and
reasoning as every prior slice's test file: `WithStageEntrySecondCombatSelection`
correctly rejects a session that has not reached a completed second-side
Breakdown, and `WithStageEntrySecondNoAttack` correctly rejects a session
that has not reached a completed second-side Combat Decision.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). `Cna.Analyzers`'
  CNA001/CNA002 rules (live since #522) did not flag anything in this
  change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `--class "*StageEntrySecondCombat*"`: **2/2 passed** (the two new
  tests).
- Broad regression sweep across every test class touching any modified
  method, same classes the first-acting-side Assault slice swept
  (`Second*`, `*CombatSelection*`, `*CombatCycle*`, `*CombatRetreat*`,
  `*Timeout*`, `*ActingSide*`, `*CampaignCombatExerciseAssault*`,
  `*StageEntry*`, `*CampaignExercises*`, `*ForceSeal*`,
  `*ForceAssignment*`, `*Barrage*`, `*Custody*`, `*ReserveRelease*`,
  `*CycleControl*`, `*TruckConvoy*`, `*PostCombatPass*`, `*AntiArmor*`,
  `*CancelledAssault*`, `*Retreat*`, `*Settlement*`): run per-class since
  the local test runner's `--filter-class` does not accept comma-joined
  wildcard groups the way a single pattern does. All passed, zero
  regressions -- see the PR's own CI log / commit for the complete
  per-class counts.

## MTS-TASK-006 is now complete

**This stage's second acting side is fully generalized** through the
entire chain: Reserve Designation (#531), Movement (#532), Breakdown
(#533), and now Combat Decision/Assault/No-Attack. Combined with
MTS-TASK-005's completion of the first acting side, **this stage (Stage 2)
is now fully generalized for both acting sides** at the Core/`ExerciseSession`
layer.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics. Per the earlier Stage 3 scoping
verification (`2026-11-23-stage-3-scoping-verification.md`), Stage 3's own
preamble should reuse this exact pattern with no new design fork; its own
Movement/Breakdown/Combat will cost about the same as Stage 2's did until
the overhaul's `PendingDecision` refactor lands.

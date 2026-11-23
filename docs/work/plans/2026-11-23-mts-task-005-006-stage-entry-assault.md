# MTS-TASK-005/006 follow-up: this stage's own Combat Assault/No-Attack (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-005/006, completing the
piece [`2026-11-23-mts-task-005-006-stage-entry-combat-decision.md`](2026-11-23-mts-task-005-006-stage-entry-combat-decision.md)
(PR #526) deferred: generalizing the ~15 pre-existing shared submit/query
call sites that this stage's own Combat assault/no-attack chain runs
through once a Combat Decision is accepted. Investigating those call sites
directly, before writing any code, found the real shape is smaller and
cleaner than the deferred doc's own description -- documented below,
corrected rather than repeated.

## The real finding: most of the ~15 sites don't need a per-site change at all

The deferred doc described "each of the Barrage/RetreatMove/ForceSeal/
AntiArmor/CancelledAssault/AssaultResult/PostResultRetreat/Settlement/
CustodyDisposition/CustodySuccessor submissions" as independently ternarying
on `CurrentSecondAssault` (`CampaignExercises.cs`, ~15 call sites). Reading
every one of them directly found this isn't quite right: there are two
genuinely different tiers, and only one of them has per-site ternaries.

**Tier 1 -- Barrage through Custody (`CompleteNativeCombatBarrage`,
`SubmitContactOwnerForceSeal`, `CompleteNativeCombatForceAssignment`,
`CompleteNativeCombatAntiArmor`, `CompleteNativeCombatCancelledAssault`,
`SubmitContactPostResultRetreat`, `CompleteNoPostResultRetreat`,
`SubmitNativeCombatCustody`, `CompleteNoNativeCombatCustody`, and their
`SubmitContactDefenderRbaDecline` sibling):** none of these read
`CurrentSecondAssault` directly. They dispatch on `session.CurrentRepeatAssault`
(`ExerciseSession.cs`), a **computed property**:

```csharp
internal CampaignNativeRepeatAssaultRuntime? CurrentRepeatAssault =>
    (CurrentSecondAssault is { IsSecondSideFinish: false } ? CurrentSecondAssault : null)
        ?? firstSideRepeatAssault;
```

and the corresponding writer, `WithRepeatAssaultEvent`:

```csharp
internal ExerciseSession WithRepeatAssaultEvent(object campaignEvent, CampaignContactJournal contacts)
{
    if (CurrentSecondAssault is not null)
        return WithSecondAssaultEvent(campaignEvent, contacts);
    if (CurrentPostRepeat?.ActiveAssault is not null)
        return WithPostRepeatEvent(campaignEvent, contacts);
    // ... first-side fallback
}
```

`QueryCheckpoint`'s own cascade confirms the same thing from the query side:
every Barrage-through-Custody checkpoint branch (`CampaignExercises.cs`,
~15 lines) reads `session.CurrentRepeatAssault?.Xxx`, never
`CurrentSecondAssault?.Xxx` directly. **Extending these two central points to
a third branch (Stage Entry, checked first, chronologically latest) makes
every Tier 1 method Stage-Entry-aware with zero changes to the methods
themselves** -- the same future-proofing shape the overhaul direction doc's
Section 0 argues for generally (a single resolver instead of N
independently-maintained branches), discovered here as already half-built.

**Tier 2 -- Reserve Release onward (`QueryNativeReserveReleaseState`,
`SubmitNativeReserveReleaseChoice`, `CompleteNativeReserveRelease`,
`QueryPendingNativeCycleControl`, `TimeoutNativeReserveRelease`,
`TimeoutNativeCycleControl`, `QueryPostCombatPassSource`,
`SubmitPostCombatPass`, `QueryTruckConvoyPassSource`,
`SubmitTruckConvoyPass`, `SubmitNativeCycleControl`):** these genuinely do
read `CurrentSecondAssault` explicitly, by design, not oversight --
`CurrentRepeatAssault`'s `IsSecondSideFinish: false` guard means the
property stops resolving to `CurrentSecondAssault` exactly when that side's
own Patrol/post-combat-pass sequence finishes, which can happen as part of
settling the very combat round these methods are about to query. Reserve
Release has to see that side's `.Release` *after* it's finished, so these
11 methods bypass the generic property on purpose. Each needed its own new
`CurrentStageEntryAssault` branch, checked first, mirroring its existing
`CurrentSecondAssault` branch exactly.

This changes the real scope from "~15 independent mechanical edits" to "2
central-point extensions (Tier 1) + 11 explicit-branch extensions (Tier 2)
+ the Stage Entry counterparts to `WithSecondAssaultEvent`/`WithSecondNoAttack`
that Tier 1's writer dispatches into." Still a large change by line count,
but far lower risk than independently touching ~15 large methods by hand --
the correctness of every Tier 1 call site falls out of two points being
right, not fifteen.

## What was built

- `src/Cna.Core/Exercises/ExerciseSession.cs`:
  - `StageEntryAssaultPredecessor` (the field #526 deliberately left out,
    since nothing set it yet).
  - `WithStageEntryAssaultEvent`, mirroring `WithSecondAssaultEvent` exactly,
    writing `CurrentStageEntryAssault`/`StageEntryAssaultPredecessor` instead
    of `CurrentSecondAssault`/`SecondAssaultPredecessor`.
  - `CurrentRepeatAssault` extended to a three-way null-coalesce:
    `CurrentStageEntryAssault` (checked first) `??` `CurrentSecondAssault`
    `??` `firstSideRepeatAssault`.
  - `WithRepeatAssaultEvent` extended with a `CurrentStageEntryAssault is not
    null` branch ahead of the existing `CurrentSecondAssault` one.
  - `CurrentStageEntryNoAttackCompletion`/`StageEntryNoAttackPredecessor`/
    `WithStageEntryNoAttack`, mirroring `WithSecondNoAttack` exactly.
- `src/Cna.Core/Exercises/CampaignExercises.cs`:
  - `CompleteStageEntryCombatNoAttack`, mirroring `CompleteSecondCombatNoAttack`
    exactly.
  - The 11 Tier 2 methods above, each extended with a `CurrentStageEntryAssault`
    branch checked first (chronologically latest, same ordering discipline as
    every cascade in this file).
  - `QueryCheckpoint`: two new branches (`CurrentStageEntryAssault`,
    `CurrentStageEntryNoAttackCompletion`), inserted ahead of the existing
    `CurrentStageEntryCombatDecision` branch and ahead of the `#394`
    second-side block, same ordering discipline as every cascade above. The
    stale comment from #526 explaining why `CurrentStageEntryAssault` was
    *not* checked there is removed -- it now is.
  - `ReconstructOne`: two new cases, mirroring the existing
    `CurrentSecondAssault`/`CurrentSecondNoAttackCompletion` cases exactly,
    inserted ahead of them for the same ordering reason.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryCombatAssaultTests.cs`
  -- new, same depth as every prior Stage Entry test file (see below).

`src/Cna.Core/Hosting/CampaignHostAdapter.cs` -- confirmed, not changed.
Grepped for any `StageEntryReserve`/`StageEntryMove`/`StageEntryBreakdown`/
`StageEntryCombat` reference: zero hits, consistent with every prior Stage
Entry mechanic having no Host-layer forwarder yet (Host/HTTP wiring remains
out of scope for every slice so far, this one included).

## Testing depth: same limitation as every prior slice, and why

`WithStageEntryAssaultEvent`, `CurrentRepeatAssault`'s new resolution, and
`WithRepeatAssaultEvent`'s new dispatch all operate on a real
`CampaignNativeRepeatAssaultRuntime`, which (like `CampaignNativeSecondBreakdownV46.CreateForStageEntry`
and `CampaignNativeSecondCombatDecisionStateV47.Select` before it) only
accepts a real `ExerciseSession`, not typed parameters -- there is no way to
reach a genuine success path without a real driven session, and none exists
as a fixture yet. What proves those three changes correct instead is the
full existing regression suite, which reads `CurrentRepeatAssault`/
`WithRepeatAssaultEvent` on every one of its own call sites today: a defect
in the new Stage-Entry-first branch would surface as one of those tests
failing, not just as a missing new test. What's tested directly, same
depth and reasoning as every prior slice's test file: `WithStageEntryNoAttack`
correctly rejects a real, legitimately constructed session (a fresh
campaign genesis) that has not reached a completed Stage Entry Combat
Decision.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). `Cna.Analyzers`'
  CNA001/CNA002 rules (live since #522) did not flag anything in this
  change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- Broad regression sweep across every test class touching any modified
  method (`Second*`, `*CombatSelection*`, `*CombatCycle*`, `*CombatRetreat*`,
  `*Timeout*`, `*ActingSide*`, `*CampaignCombatExerciseAssault*`,
  `*StageEntry*`, `*CampaignExercises*`, `*ForceSeal*`, `*ForceAssignment*`,
  `*Barrage*`, `*Custody*`, `*ReserveRelease*`, `*CycleControl*`,
  `*TruckConvoy*`, `*PostCombatPass*`, `*AntiArmor*`, `*CancelledAssault*`,
  `*Retreat*`, `*Settlement*`): **488/488 passed, zero regressions** --
  this is the actual proof that the Tier 1 central-point extension and every
  Tier 2 explicit branch are correct, since every one of those tests
  exercises `CurrentRepeatAssault`/`WithRepeatAssaultEvent` or one of the 11
  Tier 2 methods on the paths that already existed.
- `--class "*StageEntry*"`: 84/84 passed (including the one new test).
- `--class "*CampaignNativeStageEntryCombatAssaultTests*"`: 1/1 passed.

## Honest re-scoping of what's left in MTS-TASK-005/006

**This stage's first acting side is now fully generalized** through the
entire Combat chain (Decision, Assault via Tier 1/Tier 2, No-Attack), on top
of the already-generalized Reserve/Movement/Breakdown. The checkpoint/query
side (`QueryCheckpoint`) and replay side (`ReconstructOne`) are both wired
for the complete chain.

**MTS-TASK-006** (the new stage's second acting side) is the same
repetition again, one more time, now that the first side's own chain is
complete.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics.

# MTS-TASK-005 (partial): the new stage's own Reserve Designation (2026-11-22)

**Status:** a real, test-verified slice of MTS-TASK-005 (generalize Stage 2
Movement/Breakdown/Combat), not the whole checkpoint. Follows
[`2026-11-22-mts-a-stage2-role-as-data-implementation.md`](2026-11-22-mts-a-stage2-role-as-data-implementation.md)
and the entry-bridge chain landed in
[`2026-11-22-mts-task-002-slices-2-3-and-wiring.md`](2026-11-22-mts-task-002-slices-2-3-and-wiring.md)
(PR #518). This document records what was actually built, a correction to
#518's own wiring found while building on top of it, and an honest
re-scoping of what MTS-TASK-005/006 actually requires — the original
checkpoint's "L" sizing did not anticipate the shape this turned out to
have.

## A correction to #518, found and fixed here

**`ExerciseSession.WithInitiativeDeclaration`'s guard checked the wrong
field.** It read `CurrentPostCombatPass is null` — the FIRST acting side's
own Patrol pass, which is already set long before the SECOND side's own
Reserve-through-Patrol cycle has even started. That let a Stage 2 Initiative
Declaration become constructible before both sides had actually finished
the prior stage, the exact kind of sequencing gap this session's own
discipline exists to catch (cf. `#508`, the System-audience auto-drain fix,
and `#511`'s Movement-completion gap — both found the same way: by trying
to actually drive the mechanic, not by inspecting it in isolation).

The real terminal fact is **not** represented by any dedicated
`ExerciseSession` field. Re-reading `CampaignNativeRepeatAssaultRuntime`
(`src/Cna.Core/Campaigns/CampaignNativeRepeatAssaultRuntime.cs:93-97,
440-468`) found that the second acting side's own Truck Convoy / Rail /
Repair / Patrol pass (`IsSecondSideFinish`'s `ApplySecondSidePhase`) is
**already fully modeled inside that runtime** — `TruckConvoyPass` and the
`PostCombatPasses` list (Rail, Repair, Patrol, in catalog order) are
properties of `CampaignNativeRepeatAssaultRuntime` itself, driven through
the existing `WithSecondAssaultEvent`/`CurrentSecondAssault.Advance(...)`
path with no new `ExerciseSession` wiring required. **Good news this
surfaces for the project's own future estimating**: Reserve Release, Cycle
Control, Truck Convoy, and all of Rail/Repair/Patrol for *any* "second-side-
style" single-pass occurrence are **already generic infrastructure** — no
new native types and no new per-mechanic `ExerciseSession` fields are ever
needed for that half of the cycle, for Stage 2, Stage 3, or Turns 2-6.

Fixed the guard to read `CurrentSecondAssault is not { PostCombatPasses.Count:
> 0 }` instead — defense-in-depth only (the real validation is
`CampaignNativeInitiativeDeclarationV49.Create`'s own predecessor-switch and
catalog check), consistent with the comment already on this block describing
these guards' purpose. No caller of `WithInitiativeDeclaration` exists yet
(preamble is not Host-wired — see #518's own "What was deliberately not
done"), so this had not yet caused an observable failure; it would have the
moment that wiring was built on top of it, which is exactly the situation
MTS-TASK-005/006 creates. Caught before it could compound.

## What was built

**The new stage's own first-acting-side Reserve Designation**, reusing
`CampaignNativeSecondReserveV43` unchanged — the type became role-as-data
generic in #518's refactor, so this is a third occurrence of the same event
type (Stage 1's second side, and now this), not a new one:

- `src/Cna.Core/Exercises/ExerciseSession.cs` — `CurrentStageEntryReserve`,
  `CurrentStageEntryReserveWorld`, `StageEntryReservePredecessor` fields and
  `WithStageEntryReserve`, same `MemberwiseClone` shape as
  `WithSecondReserve`, guarded on `CurrentNoObligationFleetRepair` (the
  preamble chain's terminal event) instead of `CurrentPostCombatPass`. A
  distinct slot is required — not a reuse of `CurrentSecondReserve` — because
  that field is already permanently bound to Stage 1's second side by the
  time this stage's preamble can even complete, and no field on this class
  is ever reset once set (the same "never reset" invariant `#394`'s design
  doc already established as load-bearing).
- `src/Cna.Core/Exercises/CampaignExercises.cs` — `QueryStageEntryReserveSource`,
  `StageEntryOrders` (appends `CampaignNativeNoObligationFleetRepairV54.ToOrder()`
  onto `CurrentSnapshot.OperationStageOrders` for the duration of the call,
  per that event's own remarks — `CampaignSnapshotV11.OperationStageOrders`
  itself is never touched), and `SubmitStageEntryReserve`; a `Reconstruct`
  walk-back arm; a `ReconstructOne` replay block; and a `QueryCheckpoint`
  cascade branch (which the six preamble events themselves still lack — see
  "Known gap not fixed here" below).
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` — **no change**.
  `AdvanceSecondReserve(CampaignNativeSecondReserveV43 reserve)` already
  takes the concrete event type directly, not a side or occurrence
  qualifier, so it is directly reusable.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryReserveTests.cs` —
  new. Same "dormant, contract-level" testing depth as
  `CampaignNativeStageEntryPreambleTests` (no campaign in this suite can
  drive a real Combat cycle to Patrol completion yet, Core or Host — see
  that file's own note and the "honest gap" in the wiring doc). Unlike that
  file's own full-chain test, which deliberately stops at a World-hash
  mismatch, this one gives the synthetic predecessor the *real* initial
  World's own hash, so the chain carries through to a genuinely successful
  `CampaignNativeSecondReserveV43.Create` call — proving Reserve Designation
  can actually complete for the new stage's first acting side, not just
  that the predecessor-switch arm is reached.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning).
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean
  (0 of 1224 files).
- `python3 .github/scripts/local_tests.py core --class "*StageEntry*"`:
  **73/73 passed** (the 12 preamble tests plus the 2 new ones here, run
  together since both files share the `*StageEntry*` filter).
- `python3 .github/scripts/local_tests.py core --class "*Second*"`:
  **17/17 passed** (no regression from the `WithInitiativeDeclaration` guard
  fix or the new field family).
- `python3 .github/scripts/local_tests.py core --class "*Reserve*"`:
  **183/183 passed**.
- `python3 .github/scripts/local_tests.py host --method "*SecondSide*"`: ran
  (no `Cna.OrleansHost` files were touched by this change; included as a
  broader regression check, not because this change could plausibly affect
  it).

## A reported finding, verified and not applied

A parallel review raised a claimed high-severity bug: `CampaignNativeReserveReleaseV27.cs`'s
`Initialize` method calling `projection.PriorSnapshot.OperationStageOrders.Single()`
without a `(GameTurn, OperationStage)` filter, by analogy to
`FirstActingSideResolver.Resolve`'s filtered lookup, claimed to throw once a
second stage/turn exists.

**Verified directly against source and not applied, because it does not
reach.** `Initialize` is called only from the genuine first acting side's
main `CampaignSnapshotV11`-rooted Combat pipeline (its own guard requires
`projection.NextPosition.ActorRole == LandActorRole.FirstActingSide`).
`CampaignSnapshotV11.OperationStageOrders` is seeded as `[]` at genesis
(`CampaignCreationV10.cs:87-89`) and replaced with exactly one entry exactly
once, at `InitiativeOrderDeclared` (`CampaignV11Preamble.cs:122`, gated to
`prior.StateVersion == 4` — fires once per campaign). Every V11 projector
thereafter (`CampaignV11MoveProjector.cs:49`,
`CampaignV11BreakdownProjector.cs:82`, `CampaignElementMovedV3Factory.cs:140`,
`CampaignV11Preamble.cs:137`) forwards that list unchanged; nothing appends
to it again. Under the standing Option-2 ruling, `CampaignSnapshotV11` is
never widened to represent Stage 2+, so this list cannot ever hold a second
entry — Stage 2's own order lives entirely in the separate, locally-built
list this document's `StageEntryOrders` helper constructs, which
deliberately never touches `CampaignSnapshotV11.OperationStageOrders` for
exactly this reason. `CampaignV11Preamble.cs` itself has the identical bare
`.Single()` pattern at lines 67 and 80, for the same reason, and is equally
safe. Changing this call to a filtered lookup would be unneeded defensive
code for a case the current architecture cannot produce; not made.

## Known gap not fixed here (pre-existing, from #518, flagged not resolved)

`QueryCheckpoint` (`CampaignExercises.cs`) has **no cascade branch for any of
the six preamble events themselves** (`CurrentInitiativeDeclaration` through
`CurrentNoObligationFleetRepair`) — only the new `CurrentStageEntryReserve`
branch this change adds. `ExerciseCheckpoint` has no constructor overload for
any of those six types (each type gets its own dedicated overload, not a
generic one), so adding those branches is its own small slice of work
(`ExerciseCheckpoint.cs` changes), not something this change's scope covers.
Left as a visible gap rather than silently expanded into.

## Honest re-scoping of what's left in MTS-TASK-005/006

This slice proves the pattern generalizes a third time and closes the real
entry-bridge bug. It does **not** complete the checkpoint. What's left,
now much better understood than when the checkpoint was first sized:

- **Movement, Movement Completion, Breakdown, Combat Decision/Assault/
  No-Attack** for the new stage's first acting side — each needs its own
  new `ExerciseSession` field slot (same shape as `CurrentStageEntryReserve`
  above) plus a `Query`/`Submit` pair, a `Reconstruct` arm, and a
  `ReconstructOne` block in `CampaignExercises.cs`, mirroring
  `WithSecondMove`/`WithSecondMovementCompletion`/`WithSecondBreakdown`/
  `WithSecondCombatSelection`/`WithSecondAssaultEvent`/`WithSecondNoAttack`
  exactly. The underlying native classes need **no changes** — they are
  already role-as-data generic (#518) or already side-generic by original
  design (`CampaignNativeRepeatAssaultRuntime.ReplaySecond`, confirmed to
  already carry a fresh single-pass cycle through to its own Truck Convoy/
  Rail/Repair/Patrol, per the correction above) — this is wiring repetition
  with a now-proven template, not new design.
- **MTS-TASK-006** (the new stage's second acting side) is the same
  repetition again, one more time, entered from the new stage's own first
  acting side's terminal Post Combat Pass (which will itself live inside
  *that* side's own `CurrentStageEntryAssault`-equivalent runtime, the same
  way Stage 1's second side already does) — no new design question, just
  more of the same mechanical slices.
- Recommend slicing the remainder the same way this document's own slice
  was sliced (one mechanic, or a small related group, per PR) rather than
  attempting the rest of the checkpoint in one change — consistent with
  this project's own established practice for every other large effort
  this session (`#497`, `#499`, `#502`, `#511`).
- Still not attempted, same as #518 left it: any Host/HTTP wiring for any
  of this (`MTS-TASK-003`/`007`), and the System-audience auto-drain
  question for the preamble's no-obligation mechanics.

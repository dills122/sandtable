# MTS-TASK-005/006 (partial): the new stage's own Combat Decision (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-005/006 (generalize
Stage 2 Movement/Breakdown/Combat), not the whole checkpoint. Follows
[`2026-11-23-mts-task-005-006-stage-entry-breakdown.md`](2026-11-23-mts-task-005-006-stage-entry-breakdown.md)
(PR #523), which built this stage's own Breakdown and found a real
deviation (session-slot hardcoding) in the native class it reused. This
slice checked the Combat-chain native classes for the same deviation
first, before assuming either template applied -- confirmed they do **not**
have that problem, but found Combat's own shape is a different kind of
larger, requiring an honest scope cut explained below.

## Checked first: does `CampaignNativeSecondCombatDecisionStateV47` have Breakdown's problem? No.

`CampaignNativeSecondCombatDecisionStateV47.Select`, read in full before
writing any wiring, already takes `world`, `position`, `turnFacts`,
`randomState`, `context`, `contacts`, `side`, `actingElementId`, and
`actionId` as explicit parameters -- it never reads an `ExerciseSession`
field directly. Its own remarks explain why: a direct `CampaignSnapshotV11`
pointing at the second side's post-Breakdown Combat position is rejected by
`Cna1979LandSequenceV4.IsSupportedCheckpoint`, so this type was built from
the start to never need one, following `CampaignNativeSecondMoveV44`'s own
precedent for the same architectural problem. `SecondAssaultFrame` (the
static helper that seeds the repeat-assault runtime's random cursor) is
likewise already parameterized by the decision/turnFacts/history passed in,
not by session field name. **Neither native class needed any new entry
point** -- unlike Breakdown (`V46`), Combat Decision (`V47`) was already
generic at the native-class layer.

## The real finding: Combat is a shared, multi-step state machine, not one owner-opening

Reserve, Movement, and Breakdown are each one clean, bounded mechanic: one
`ExerciseSession` field (or field pair), one dedicated `QueryStageEntryXxx`/
`SubmitStageEntryXxx` method pair, done. Combat Decision looks the same
shape on its own, but what it leads into does not: accepting a Combat
Decision immediately spins up `CampaignNativeRepeatAssaultRuntime` (the
multi-step Barrage -> Retreat-Before-Assault -> Force-Assignment ->
Anti-Armor -> Cancelled-Assault -> Assault-Result -> Post-Result-Retreat ->
Settlement -> Custody chain), and **every one of those ~15 steps is
submitted through a pre-existing, already-shared method** that ternaries
between the first side and the `#394` second side by field, not through a
dedicated per-side method the way Reserve/Movement/Breakdown have. For
example, `SubmitContactOwnerCombatSelection` itself (the Decision's own
submit entry point) is a single ~100-line method already branching on
`isSecondSide` for the #394 work; the Barrage/RetreatMove/ForceSeal/
AntiArmor/CancelledAssault/AssaultResult/PostResultRetreat/Settlement/
CustodyDisposition/CustodySuccessor submissions each do the same thing via
their own `session.CurrentSecondAssault is not null ? ... : ...` ternary,
confirmed by reading all of them directly (`CampaignExercises.cs`, ~15
call sites).

**Scope cut for this slice, and why:** generalizing Combat Decision itself
to a third (`isStageEntry`) branch is a bounded, mechanical change -- done,
below. Generalizing the ~15 Assault-advance call sites the same way is a
much larger, much riskier change, and **none of those 15 sites are
reachable today anyway**: like every prior Stage Entry mechanic, this one
has no Host/HTTP wiring yet (`MTS-TASK-003/007`), so there is no way to
drive a real session to a point where any of them would actually be
exercised by this stage's own assault runtime. Building untestable branches
across 15 already-large methods for zero present verification value runs
directly against this project's own standing discipline (verify against
real code and tests, don't guess). **Deferred to its own follow-up slice**,
sized and scoped the same way this one was: find the exact call sites,
extend each the same way, verify no regression to the #394/first-side
paths, same as this slice did for the three it touched.

This is a scoping decision, not a multi-option architecture fork -- there
was one sane call here (defer the untestable part, land the testable part),
not several design directions to weigh, so no separate design-options doc.

## What was built

- `src/Cna.Core/Exercises/ExerciseSession.cs` -- `CurrentStageEntryCombatDecision`,
  `StageEntryCombatSelectionPredecessor` fields; `WithStageEntryCombatSelection`,
  mirroring `WithSecondCombatSelection` exactly (same guard shape, same
  `CampaignNativeRepeatAssaultRuntime.ReplaySecond`/`SecondAssaultFrame`
  call, both reused unchanged); `CurrentStageEntryAssault`, the distinct
  slot the runtime is stored in (same reason every prior Stage Entry slot
  needed one: `CurrentSecondAssault` is permanently bound to Stage 1's
  second acting side). `StageEntryAssaultPredecessor` is **not** added in
  this slice: nothing here ever sets it -- that is
  `WithStageEntryAssaultEvent`/`WithStageEntryNoAttack`'s job, both part of
  the deferred follow-up -- so adding it now would be a dead field.
- `src/Cna.Core/Exercises/CampaignExercises.cs`:
  - `QueryStageEntryCombatWorldAndPosition`, mirroring
    `QuerySecondCombatWorldAndPosition` exactly, substituting this stage's
    own `CurrentStageEntryBreakdown`/`CurrentStageEntryMoveWorld`/
    `CurrentStageEntryReserveWorld`.
  - `HasAdvancedCombatSelection` extended from a 2-way to a 3-way ternary
    (Stage Entry checked first, chronologically latest, same ordering
    discipline as every cascade in this file).
  - `QueryContactOwnerCombatCut` extended with a Stage Entry branch ahead
    of the existing #394 second-side one.
  - `SubmitContactOwnerCombatSelection` extended: `isSecondSide` became
    `isStageEntry`/`isSecondSide` (mutually exclusive by construction --
    `CurrentStageEntryBreakdown` can only be set after the full #394
    second-side chain has already resolved, so Stage Entry is checked
    first), with a new `if (isStageEntry) { ... }` branch mirroring the
    existing second-side branch's shape exactly (same cut-building,
    `Select` call, `AdvanceSecondCombatSelection`, `WithStageEntryCombatSelection`,
    receipt construction).
  - `QueryCheckpoint`: one new branch, `CurrentStageEntryCombatDecision`,
    inserted ahead of `CurrentStageEntryBreakdown` (chronologically later).
    `CurrentStageEntryAssault` is deliberately **not** checked here (unlike
    the #394 `CurrentSecondAssault` branch above it): nothing in this slice
    ever advances it past zero events, so the branch would be dead code
    pending the follow-up slice.
  - `ReconstructOne`: one new case, mirroring the existing
    `CurrentSecondCombatDecision` case exactly, substituting
    `QueryStageEntryCombatWorldAndPosition` and the Stage Entry predecessor/
    field names. No timeout variant exists for this stage
    (`CampaignNativeSecondCombatSelectionTimeoutV48`'s equivalent is
    deferred along with the whole round-binding/System-audience timeout
    question, same as every prior slice's open items), so this case always
    calls `Select` directly -- no ternary, unlike the second-side case it
    mirrors.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` -- **no change**.
  `AdvanceSecondCombatSelection(CampaignNativeSecondCombatDecisionV47 selected)`
  already takes the concrete event type directly, confirmed by reading the
  signature.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryCombatSelectionTests.cs`
  -- new, same depth as `CampaignNativeStageEntryBreakdownTests` and for the
  same reason (see below), not the richer depth
  `CampaignNativeStageEntryMovementTests` reaches.

## Testing depth: same narrower depth as Breakdown, and why

`CampaignNativeStageEntryMovementTests` reaches a genuine success path
because `CampaignNativeSecondMoveV44`/`CampaignNativeSecondMovementCompletedV45`
are purely parameterized -- a synthetic chain can be built from typed
native-class calls alone, no `ExerciseSession` needed. `CampaignNativeSecondCombatDecisionStateV47.Select`
is *also* purely parameterized (confirmed above), which looked promising
for reaching that same richer depth -- but `Select`'s own `position`
parameter has to come from a completed `CampaignNativeSecondBreakdownV46`'s
`NextPositionId`, and that native class (both `Create` and
`CreateForStageEntry`) only takes a real `ExerciseSession`, the same
limitation Breakdown's own test file already hit. Hand-deriving the Combat/
Barrage catalog position independently, without the real Breakdown event's
own internal computation to check it against, would mean testing an
invented position rather than verifying the real one -- considered and
rejected as a false confidence, not a verification.

What the new test file proves instead, with a real, legitimately-constructed
`ExerciseSession` (a fresh campaign genesis via `CampaignExercises.Begin`):
`WithStageEntryCombatSelection` correctly rejects a session that has not
reached a completed Stage Entry Breakdown. This is a real, non-trivial
behavior assertion on the guard that actually ships, not a tautology -- but
it does not reach the method's success path, same honest limitation
Breakdown's own doc flagged.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). The `Cna.Analyzers`
  project's CNA001/CNA002 rules (live since #522) did not flag anything in
  this change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `--class "*StageEntryCombatSelection*"`: **1/1 passed** (the new test).
- `--class "*StageEntry*"`: **78/78 passed** (77 prior + 1 new).
- `--class "*Second*"`: **17/17 passed** (no regression to the #394 paths
  this slice's branches sit alongside).
- `--class "*CampaignExercises*"`: **12/12 passed**.
- `--class "*CombatSelection*"`: **7/7 passed**.
- `--class "*CampaignCombatExerciseAssault*"`: **6/6 passed** (the real
  driven-campaign suite exercising first-side Combat end to end via the
  unmodified-by-default path, confirmed unaffected).
- Every test class in the repo that calls `SubmitContactOwnerCombatSelection`/
  `QueryContactOwnerCombatCut`/`HasAdvancedCombatSelection` directly or
  indirectly, confirmed by grep and run individually: `CampaignCombatActingSideTests`
  (5/5), `CampaignContactCombatSelectionCutTests` (6/6),
  `CombatCycleAdmissionReachabilityTests` (3/3), `CombatCycleDisclosure*`
  (61/61 across all five files), `CombatRetreatUnseenEncounterReachabilityTests`
  (1/1), and the four `*TimeoutTests` classes (43/43 combined) -- **119/119
  passed**, zero regressions to the shared methods' existing first-side and
  #394 second-side behavior.

## Honest re-scoping of what's left in MTS-TASK-005/006

**This stage's first acting side, still left:** the ~15-site Assault/
No-Attack ternary wiring described above (`WithStageEntryAssaultEvent`/
`WithStageEntryNoAttack` on `ExerciseSession`, plus each of the Barrage/
RetreatMove/ForceSeal/AntiArmor/CancelledAssault/AssaultResult/
PostResultRetreat/Settlement/CustodyDisposition/CustodySuccessor submit call
sites). This is the one piece of MTS-TASK-005/006 that is not simply
"repeat the proven template" -- it genuinely needs its own slice(s),
possibly split further (e.g. Barrage/RetreatMove/ForceAssignment as one
slice, the remainder as another), given its size.

**MTS-TASK-006** (the new stage's second acting side) is the same
repetition again, one more time, after MTS-TASK-005 (including the
Assault/No-Attack piece above) completes.

Still not attempted, same as every prior slice: any Host/HTTP wiring
(`MTS-TASK-003`/`007`), and the System-audience auto-drain question for the
preamble's no-obligation mechanics.

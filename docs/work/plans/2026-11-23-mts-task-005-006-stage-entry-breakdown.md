# MTS-TASK-005/006 (partial): the new stage's own Breakdown (2026-11-23)

**Status:** a real, test-verified slice of MTS-TASK-005/006 (generalize
Stage 2 Movement/Breakdown/Combat), not the whole checkpoint. Follows
[`2026-11-22-mts-task-005-006-stage-entry-movement.md`](2026-11-22-mts-task-005-006-stage-entry-movement.md)
(PR #520), which built this stage's own Movement + Movement Completion and
confirmed the remaining mechanics were "wiring repetition with a now-proven
template, not new design." Breakdown turned out to be the one exception to
that prediction — not a new design question, but a real deviation in the
underlying native class that needed fixing before the established template
could even apply.

## A real deviation found and fixed: `CampaignNativeSecondBreakdownV46` is not like its siblings

`CampaignNativeSecondReserveV43`, `CampaignNativeSecondMoveV44`, and
`CampaignNativeSecondMovementCompletedV45` all take their predecessor as an
explicit, type-matched parameter (`Create(object predecessor, ...)` /
`Source(object predecessor, ...)`) — this is exactly why #519 and #520 could
reuse them unchanged: the predecessor switch matches by event *type*, not by
which `ExerciseSession` slot held it.

`CampaignNativeSecondBreakdownV46.Create`, read in full before writing any
wiring, does **not** follow that shape. It takes the whole `ExerciseSession`
and reads four named fields directly:
`session.CurrentSecondMovementCompletion`,
`session.SecondMovementCompletionPredecessor` (both at the top of the
method), and, inside its walk-back loop, `cursor.SecondMovePredecessor` and
`cursor.CurrentSecondMoveWorld`. None of these are parameters — they are
Stage 1's second-side slots, hardcoded. Calling `Create` with a session
whose equivalent data lives in `CurrentStageEntryMovementCompletion`/
`StageEntryMovementCompletionPredecessor`/`StageEntryMovePredecessor`/
`CurrentStageEntryMoveWorld` instead would read the wrong (null) fields and
throw immediately, regardless of whether this stage's own movement had
genuinely completed. This was not a bug in #518's role-as-data pass — #518
generalized the *side* (`LandSide`/`ActingRole`) hardcoding in all four of
these classes correctly; this is an orthogonal, previously-unflagged
*session-slot* hardcoding that #518 had no reason to touch, because nothing
used a Stage Entry slot at the time.

**Fix:** added `CampaignNativeSecondBreakdownV46.CreateForStageEntry`, a
second entry point whose body is the exact logic of `Create` with those
four reads substituted for their Stage Entry equivalents (and `orders`
threaded through as `CampaignExercises.StageEntryOrders(session)` instead
of `initial.OperationStageOrders` directly, same reason #519/#520 did the
same substitution at their own call sites). `Create` itself is **completely
untouched** — zero risk to the already-shipped, already-tested Stage 1
Second Breakdown path.

**Why a duplicate method instead of parameterizing `Create`:** this mirrors
the per-generation duplication this codebase already uses one layer up, at
the `CampaignExercises` layer (`QuerySecondMovementSource`/
`QueryStageEntryMovementSource` are two separate methods, not one generic
one) — the established, if architecturally-flagged-for-later, convention
(see `docs/work/plans/2026-11-23-codebase-overhaul-direction.md` Section 0,
which proposes collapsing exactly this family of duplication into a
`PendingDecision` union + handler registry, deliberately deferred until
Stage 2/3 land). Introducing a new delegate-parameterized `Create` for a
single call site would be a bigger, more novel change than this slice's
scope calls for; a plain duplicate is the conservative choice and carries
zero risk to the existing, security/determinism-critical Second Breakdown
path.

## What was built

- `src/Cna.Core/Campaigns/CampaignNativeSecondBreakdownV46.cs` —
  `CreateForStageEntry(ExerciseSession session)`, as described above. Every
  check, invariant, and the resulting event's contract are byte-for-byte
  identical to `Create` — only which `ExerciseSession` slots are read
  differs.
- `src/Cna.Core/Exercises/ExerciseSession.cs` — `CurrentStageEntryBreakdown`,
  `StageEntryBreakdownPredecessor` fields and `WithStageEntryBreakdown`,
  same `MemberwiseClone` shape and guard style as `WithSecondBreakdown`
  (guarded on `CurrentStageEntryMovementCompletion` instead of
  `CurrentSecondMovementCompletion`). A distinct slot is required for the
  same reason every prior Stage Entry slot needed one: `CurrentSecondBreakdown`
  is already permanently bound to Stage 1's second acting side, and no
  field on this class is ever reset once set.
- `src/Cna.Core/Exercises/CampaignExercises.cs` — `CompleteStageEntryBreakdown`,
  mirroring `CompleteSecondBreakdown` exactly (same System-audience,
  no-owner-decision shape — Breakdown is a verify-and-replay transition,
  not a submitted player action, same as Stage 1's); a `QueryCheckpoint`
  cascade branch, inserted ahead of the existing `CurrentStageEntryMovementCompletion`
  branch (chronologically later, same ordering discipline as every cascade
  in this file); a `Reconstruct` walk-back arm; a `ReconstructOne` replay
  block, calling `CompleteStageEntryBreakdown` the same way the existing
  `CurrentSecondBreakdown` block calls `CompleteSecondBreakdown`.
- `src/Cna.Core/Visibility/CampaignContactJournal.cs` — **no change**.
  `AdvanceSecondBreakdown(CampaignNativeSecondBreakdownV46 completed)`
  already takes the concrete event type directly, confirmed by reading the
  signature.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryBreakdownTests.cs`
  — new, at a different depth than the Reserve/Movement test files (see
  "Testing depth" below).

## Testing depth: narrower than Reserve/Movement, and why

`CampaignNativeStageEntryReserveTests`/`CampaignNativeStageEntryMovementTests`
test their native classes' `Create`/`Source` methods directly with
synthetic predecessor objects, because those methods take an explicit,
type-matched predecessor parameter — no real `ExerciseSession` is needed to
exercise the real production logic. `CreateForStageEntry` cannot be tested
that way: it takes the whole `ExerciseSession`, and reaching one with
`CurrentStageEntryMovementCompletion` genuinely populated needs the full
six-event preamble chain plus this stage's own Reserve/Move/MovementCompletion
driven through the real `ExerciseSession` `With*` chain — which itself
needs the Host/HTTP wiring that would let a real player (or a test) reach
that state, and that wiring does not exist yet (`MTS-TASK-003`/`007`, out of
scope here). This is the same pre-existing gap
`2026-11-22-mts-task-002-slices-2-3-and-wiring.md` already documents ("the
`ExerciseSession`/`CampaignExercises` wiring is not integration tested"),
now also applying to this new method specifically.

What the new test file does verify, with a real, legitimately-constructed
`ExerciseSession` (a fresh campaign genesis via `CampaignExercises.Begin`):
`CreateForStageEntry` correctly rejects a session that has not reached a
completed Stage Entry Movement, and `CompleteStageEntryBreakdown` correctly
translates that into a rejected `ExerciseStepResult` rather than letting an
exception escape. This is a real, non-trivial behavior assertion — not a
tautology — but it does not reach the method's success path. Flagged
honestly rather than silently narrowed.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (1
  pre-existing, unrelated `ASPIRE010` warning). The new `Cna.Analyzers`
  project's CNA001/CNA002 rules (merged in #522, after this slice's Movement
  predecessor) did not flag anything in this change.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `python3 .github/scripts/local_tests.py core --class "*StageEntryBreakdown*"`:
  **2/2 passed**.
- `python3 .github/scripts/local_tests.py core --class "*StageEntry*"`:
  **77/77 passed** (75 prior + 2 new).
- `python3 .github/scripts/local_tests.py core --class "*Second*"`:
  **17/17 passed** (no regression to the unchanged `Create` path).
- `python3 .github/scripts/local_tests.py core --class "*CampaignExercises*"`:
  **12/12 passed**.
- `python3 .github/scripts/local_tests.py core --class "*CampaignCombatExerciseAssault*"`:
  **6/6 passed** (the real driven-campaign suite that exercises Stage 1's
  Second Breakdown end to end via `Create`, confirmed unaffected).

## Honest re-scoping of what's left in MTS-TASK-005/006

Still left, same shape as before this slice: Combat Decision/Assault/
No-Attack for this stage's first acting side. Worth checking each
remaining native class's `Create`/predecessor-matching shape directly
before assuming it follows the Reserve/Movement template, rather than the
Breakdown template, given this slice's own finding — do not assume "generic
by #518" means "callable from any session slot" without reading the actual
method signature first. MTS-TASK-006 (the new stage's second acting side)
remains the same repetition again, one more time, after MTS-TASK-005
completes. Still not attempted, same as every prior slice: any Host/HTTP
wiring (`MTS-TASK-003`/`007`), and the System-audience auto-drain question
for the preamble's no-obligation mechanics.

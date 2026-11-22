# MTS-TASK-002 slices 2-3 and wiring (2026-11-22)

**Status:** completes the six-event native-chain Stage 2 preamble started in
[`2026-11-22-mts-task-002-slice-1-implementation.md`](2026-11-22-mts-task-002-slice-1-implementation.md),
extends `CampaignNativeSecondReserveV43`'s predecessor switch, and wires all six events into
`ExerciseSession`/`CampaignExercises`. Per the owner's instruction to continue straight through
slices 2-3, the predecessor-switch extension, and wiring in one pass.

## What was built

### Slice 2 — Organization, Naval Convoy Arrival

- `src/Cna.Core/Campaigns/CampaignNativeNoObligationOrganizationV51.cs` — predecessor
  `CampaignNativeWeatherDeterminationV50`. Carries no decision data of its own (same as the
  original V11 `NoObligationOrganizationResolved`); passes `FirstSide`/`SecondSide` forward
  unchanged.
- `src/Cna.Core/Campaigns/CampaignNativeNoObligationNavalConvoyArrivalV52.cs` — predecessor
  the Organization event. Reuses `CampaignV11Preamble.HasDueOrOverdueArrival` directly
  (already content/scenario-driven, not reimplemented) to fail closed exactly like the V11
  original does when a scheduled arrival is due or overdue — this release's admitted
  synthetic content never schedules one, so the positive-obligation path stays genuinely
  unbuilt, not silently bypassed.

### Slice 3 — Fleet Assignment, Fleet Repair

- `src/Cna.Core/Campaigns/CampaignNativeNoObligationFleetAssignmentV53.cs` and
  `CampaignNativeNoObligationFleetRepairV54.cs` — Commonwealth-specific phases (the real
  catalog hard-codes `LandActorRole.Commonwealth` for these, not an acting-side role, so this
  is not a role-as-data case the way `#518`'s classes were). Fleet Repair is the terminal
  event of the six-event chain; it carries `ToOrder()` (mirroring
  `CampaignNativeInitiativeDeclarationV49.ToOrder()`), returning the resolved
  `CampaignOperationStageOrder` for whatever calls `CampaignNativeSecondReserveV43.Create`
  next, per the owner's ruling that this value is self-contained on the terminal event rather
  than a dedicated `ExerciseSession` field.

### Predecessor-switch extension

`CampaignNativeSecondReserveV43.Create`'s predecessor `switch`
(`src/Cna.Core/Campaigns/CampaignNativeSecondReserveV43.cs:26-39`) gained one new arm
recognizing `CampaignNativeNoObligationFleetRepairV54` — additive, the existing two arms
(`CampaignNativePostCombatPassV42`, `CampaignNativeSecondReserveV43`) are untouched. The
caller is responsible for including `fleetRepair.ToOrder()` in the `orders` list it passes to
`Create`, consistent with every other value in this chain being caller-supplied rather than
self-discovered.

### Wiring

- `src/Cna.Core/Visibility/CampaignContactJournal.cs` — six new `AdvanceXxx` methods, one per
  event, each the same one-line shape as the existing `AdvanceTruckConvoyPass`/
  `AdvancePostCombatPass` (bump the authority cut; none of these six mechanics move an
  element or otherwise change observed contacts).
- `src/Cna.Core/Exercises/ExerciseSession.cs` — six new `CurrentXxx`/`XxxPredecessor` field
  pairs and `WithXxx` methods, same shape as `WithTruckConvoyPass`/`WithPostCombatPass`. Each
  guards idempotency (`CurrentXxx` must still be null) and chain order (the immediate
  predecessor's own `CurrentXxx` must already be set) — defense in depth; the native event's
  own `Create` method is what actually validates the predecessor object and catalog position.
- `src/Cna.Core/Exercises/CampaignExercises.cs` — six new walk-back arms in `Reconstruct`'s
  predecessor-chain loop, and six new replay/compare blocks in `ReconstructOne`, each
  following the exact template of the existing `CurrentPostCombatPass`/`CurrentTruckConvoyPass`
  blocks: re-run `Create` from the predecessor, compare serialized bytes, compare contacts,
  return the result. Weather's block additionally reconstructs the exact
  `RandomStreamState` passed to the original `Create` call from `genesisRandom.ContractVersion/
  AlgorithmId/Seed` (campaign-wide constants) plus the event's own stored
  `PriorRandomCursor` — the same technique used throughout this chain for `WorldHash`.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors (at each of the slice-2,
  slice-3, and wiring stages, not just the end).
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `python3 .github/scripts/local_tests.py core --class "*StageEntryPreamble*"`: **12/12
  passed** (the original 7 from slice 1, plus 5 new: Organization and Naval Convoy Arrival
  advancement/serialization, Fleet Assignment/Fleet Repair advancement, and a full six-event
  chain test proving the whole thing connects into the `CampaignNativeSecondReserveV43`
  predecessor-switch extension — see below).
- `python3 .github/scripts/local_tests.py core --class "*Second*"`: 17/17 passed (no
  regression from the predecessor-switch extension).
- `python3 .github/scripts/local_tests.py host --method "*SecondSide*"`: **29/29 passed** (no
  regression from the `CampaignNativeSecondReserveV43` predecessor-switch extension).

### The full-chain test, and what it actually proves

`FullSixEventChainResolvesIntoACampaignNativeSecondReserveV43PredecessorForTheNewStage`
builds the entire six-event chain from a synthetic `CampaignNativePostCombatPassV42`
predecessor through `CampaignNativeNoObligationFleetRepairV54`, then calls
`CampaignNativeSecondReserveV43.Create` with the resulting `fleetRepair` event as predecessor,
a real `CampaignWorldV6Factory.CreateInitial`-built World, and a real
`CampaignContentContext` (reusing the existing `BreakdownContentFixture`/`BreakdownSnapshotTests`
test fixtures rather than building new ones). It asserts the call fails with
`"Second Reserve requires its owner and settled World."` — **not**
`"Second Reserve requires an accepted predecessor."` — which is deliberate: the synthetic
`WorldHash` carried from the hand-built `SecondSidePatrolComplete` fixture at the start of the
chain was never going to match a real World's hash, so this call cannot fully succeed with
fixture data built this way. What it does prove, precisely, is that the new predecessor-switch
arm is reached and extracts `CampaignId`/`StateVersion`/`NextPositionId`/`WorldHash` correctly
from `CampaignNativeNoObligationFleetRepairV54` — i.e. the chain is wired, not that a real
Stage 2 Reserve Designation can be fully completed from genesis today.

## An honest gap: the `ExerciseSession`/`CampaignExercises` wiring is not integration-tested

The `WithXxx` methods, the `Reconstruct` walk-back arms, and the `ReconstructOne` replay
blocks added in this slice were **not** exercised by any test against a real `ExerciseSession`
in this pass. This was investigated directly, not assumed:

- Confirmed (again, more concretely than the prior slice's note) that **no existing test in
  this suite reaches a session state with `CurrentPostCombatPass` set** — a direct search for
  `CurrentPostCombatPass` across `tests/` returns zero hits. Reaching that state requires
  driving a full Reserve → Movement → Breakdown → Combat (all 6 steps) → Reserve Release →
  Cycle Control (Finish) → Truck Convoy → Rail → Repair → Patrol cycle for a real side, which
  no test fixture in this suite does yet, for any purpose.
- Confirmed `ExerciseSession`'s own constructor
  (`src/Cna.Core/Exercises/ExerciseSession.cs:39-75`) has no parameter for
  `CurrentPostCombatPass`/`CurrentTruckConvoyPass` or any later-chain field — they can only be
  reached by calling the real `WithXxx` methods in sequence, which themselves require real,
  validated predecessor objects (`CampaignNativeReserveReleaseStateV27`/
  `CampaignNativeCycleControlV28` with `Choice == Finish` for `WithTruckConvoyPass`, etc.).
  There is no way to construct a session in that state without genuinely driving the whole
  prior cycle — confirmed by reading the constructor's full parameter list, not assumed from
  the guard clauses alone.

Given this, the wiring in this slice is verified by: (a) exact, line-for-line correspondence
to an already-proven template (`WithPostCombatPass`/`WithTruckConvoyPass` and their
`Reconstruct`/`ReconstructOne` counterparts) with no novel control flow, and (b) a clean
build. It is **not** verified by driving a real session through it end to end. Building a
fixture that reaches "both sides' Patrol complete" is a real, separate, large undertaking
(the Combat cycle alone spans ~15 native event types) that no existing test in this suite has
attempted for any purpose — flagging this now rather than overclaiming "wired and tested."

## Open question surfaced, not resolved

The six no-obligation/decision mechanics in this chain correspond 1:1 to real candidate
action types in the original V11 preamble (`ActFirstAction`/`ActLastAction`,
`ResolveWeatherAction`, `ResolveNoObligationOrganizationAction`, etc.) — every one of them,
including the "no-obligation" ones, required an explicit accepted command in that original
design, not an automatic System-audience resolution. Whether that should still hold for the
native Stage 2+ chain, or whether some of these (the genuinely no-obligation ones
specifically) should instead auto-drain the way `CampaignActionAudience.System` steps do
(per `#508`, the engine-release session's own foundational fix) is a real design fork this
pass did not resolve — it belongs to the Host/HTTP Query/Submit wiring that this slice
deliberately did not build (see "What was deliberately not done" below), and should be ruled
on before that work starts rather than guessed at.

## What was deliberately not done in this slice

- No Host/HTTP wiring, no `CampaignOwnerOpeningKind` entries, no legal-action derivation for
  any of the six mechanics — and, as the open question above notes, the System-audience
  question for the four no-obligation mechanics needs a ruling before that work can start.
- No integration test driving a real `ExerciseSession` through the full chain (see the honest
  gap above).
- No change to `CampaignSnapshotV11`/`CampaignSnapshotV11Validator`, per the standing ruling.

## Next steps

- Rule on the System-audience open question above.
- Build a real end-to-end fixture reaching "both sides' Patrol complete" — needed both to
  close the honest gap above and to eventually prove Stage 2 is reachable over real HTTP;
  this is a large, separate undertaking in its own right (per the roadmap's own Combat-cycle
  task sizing conventions, likely multiple further implementation-sized slices).
- Host/HTTP wiring for the six mechanics, once the System-audience question is settled.

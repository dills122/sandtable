# MTS-TASK-002 slice 1 — Initiative Declaration + Weather Determination (2026-11-22)

**Status:** first code slice of the native-chain Stage 2 preamble (Option A, ruled in
[`2026-11-22-stage-entry-bridge-design-options.md`](2026-11-22-stage-entry-bridge-design-options.md)).
Two of the six new native events are built, built, and test-verified; the remaining four
(Organization, Naval Convoy Arrival, Fleet Assignment, Fleet Repair — slices 2-3) and all
`ExerciseSession`/`CampaignExercises` wiring are **not** part of this slice. Nothing in
this chain is reachable through the real Host/HTTP path yet — per this project's own
four-state status model (the roadmap), this slice reaches "Implemented" (focused C# tests,
replay-consistent construction) for these two event types, not "Activated and witnessed."

## What was built

- `src/Cna.Core/Campaigns/CampaignNativeInitiativeDeclarationV49.cs` — the first native
  preamble event. Predecessor: `CampaignNativePostCombatPassV42` (a side's own completed
  Rail/Repair/Patrol pass — concretely, the second acting side's Patrol phase, the last
  side-block of the ending stage). Validated against the real
  `Cna1979LandSequenceV4.CreateTurn`/`GetNext` catalog, never a literal `StateVersion`
  number (unlike `CampaignV11Preamble.Create`'s own handling of the same decision, which
  is gated on `prior.StateVersion == 4` and is not reusable past genesis). Carries
  `DeclaringHolder`/`FirstSide`/`SecondSide` directly as fields — per the owner's ruling on
  the `orders`-list sub-question, this is the "self-contained on the event" value every
  later mechanic (and, eventually, `CampaignNativeSecondReserveV43`) reads directly via
  `ToOrder()` rather than from a dedicated `ExerciseSession` field.
- `src/Cna.Core/Campaigns/CampaignNativeWeatherDeterminationV50.cs` — the second native
  preamble event, predecessor `CampaignNativeInitiativeDeclarationV49`. Wraps
  `Cna1979Weather.Resolve` and `WeatherEventFactory.GetSources` directly (both already
  stage-generic, needing only `gameTurn` and a `RandomStreamState` — confirmed by reading
  them, not assumed) rather than reimplementing the weather table. Carries
  `PriorRandomCursor`/`ResultRandomCursor` explicitly, since its predecessor carries no
  random-state field at all (Initiative Declaration never draws). Per the design doc's
  recorded decision, resolving *which* cursor value a future caller should supply (the
  `ExerciseSession.SecondAssaultFrame`-style "walk history" problem) is left to that future
  caller — this type only threads whatever cursor it's given.
- `tests/Cna.Core.Tests/Campaigns/CampaignNativeStageEntryPreambleTests.cs` — 7 focused
  tests: both-choice Initiative Declaration advancement, canonical serialization round
  trips for both types, predecessor/cut-version rejection paths, a same-stage catalog
  rejection (a real Repair/Maintenance position whose own successor is Patrol, not the
  next stage — proving the rejection is driven by the real catalog shape, not a hand-picked
  string), Weather's random-cursor threading and determinism (resolving twice from an
  identical prior cursor reproduces byte-identical output — the actual DET-001 property
  this type exists to preserve), and a foreign-random-algorithm rejection.

## A real bug this pass's own tests caught (worth recording, not hiding)

The first test run failed `InitiativeDeclarationAdvancesFromPatrolCompleteToWeatherForEitherChoice`'s
`Assert.Equal(1, actFirst.OperationStage)` — not a production bug, a wrong test
expectation: a Stage 1 second-side Patrol-complete predecessor correctly advances into
**Stage 2**'s Initiative Declaration (`OperationStage == 2`), and the test's own first draft
asserted `1`. Corrected to `2`. Recorded because it's a concrete, positive confirmation
that the new type does cross the stage boundary rather than silently staying parked at
Stage 1 — the exact property this whole effort exists to prove, caught by the test
actually exercising the real `Cna1979LandSequenceV4` catalog rather than a mock.

A second, build-time-only issue: the test fixture's own `SecondSidePatrolComplete` helper
initially matched more than one catalog position (`Patrol`/`SecondActingSide` appears once
per Operation Stage — 1, 2, and 3 — in `Cna1979LandSequence.CreateTurn`'s per-turn catalog,
confirmed directly by this failure), so the test needed its own `OperationStage == 1`
filter to pick Stage 1's occurrence specifically. This was a test-fixture bug, not a
production one, but is itself empirical confirmation that the underlying catalog really is
generic across all three stages, consistent with the scoping doc's Section 1 finding.

## Build and test verification

- `dotnet build Sandtable.slnx --no-restore`: succeeded, 0 errors.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: clean.
- `python3 .github/scripts/local_tests.py core --class "*StageEntryPreamble*"`: 7/7 passed
  (after the one test-expectation fix above).
- `python3 .github/scripts/local_tests.py core --class "*Weather*"`: 102/102 passed
  (existing Weather coverage plus the new tests — no regression, no naming collision).

## What was deliberately not done in this slice

- No `ExerciseSession` fields, no `CampaignExercises.Reconstruct`/`ReconstructOne`
  dispatcher branches, no Host/HTTP wiring. These two types are not yet reachable from a
  real session — wiring them in before the full six-event chain (and the Reserve
  Designation handoff) exists would be premature churn, and this project's own roadmap
  explicitly treats "implemented with focused tests, not yet wired" as a legitimate,
  named intermediate state rather than something to rush past.
- No extension of `CampaignNativeSecondReserveV43`'s predecessor switch yet — that's the
  terminal handoff, which needs the full six-event chain (specifically Fleet Repair's own
  event) in hand first.
- Organization, Naval Convoy Arrival, Fleet Assignment, Fleet Repair (slices 2-3) — not
  started.

## Next steps

- Slice 2 (Organization + Naval Convoy Arrival) and slice 3 (Fleet Assignment + Fleet
  Repair), same shape as this slice.
- Once all six exist, extend `CampaignNativeSecondReserveV43.Create`'s predecessor switch
  with one new `case` for Fleet Repair's event (additive, per the design doc's own
  analysis — no existing arm changes).
- `ExerciseSession`/`CampaignExercises` wiring and a real end-to-end HTTP test, once the
  chain is complete enough to actually reach Reserve Designation.

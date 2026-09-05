# Simulator Movement Cost Sensitivity

**Status:** Implemented and verified locally; integration remains provisional until merge

**Date:** 2026-08-30

> **Current compatibility note (2026-09-04):** These observations preserve the original
> pre-Reaction study. Under `ZOR-TASK-006C`, the stable-route arm opens Reaction after its cost-8
> move and fails closed after 11 actions, while the lowest-cost arm avoids the trigger and reaches
> Breakdown in 13 actions. Current paired fingerprint is
> `sha256:6a61e195d8c3eda656ff04b1b80e1ddbd2dd5e5ca56d194b71eedb74c69ada8b`;
> Runner Reaction selection remains `ZOR-TASK-007A`.

## Decision question

On the admitted synthetic non-contact setup, does selecting each element's lowest-public-cost legal
Movement route instead of the existing stable-ID route change the deterministic action, event, and
Capability Point trajectory while preserving equal declared inputs, exact repeatability, strict
bundle readback, reconstruction, and fresh-session re-adjudication?

This is a controller-sensitivity question. It is not a gameplay-balance, scenario-design, causal,
statistical-significance, or performance question.

## Competing hypotheses

- **No observed sensitivity:** both controllers choose the same legal routes, so this setup cannot
  distinguish stable-ID and lowest-cost selection.
- **Observed sensitivity:** the first route differs while equal inputs, terminal reachability, and
  replay evidence remain valid.
- **Invalid instrument:** exposing public exact cost to the trusted Runner weakens authority, leaks
  hidden state, or makes the evidence non-repeatable. This would reject the experiment design.

## Method and controlled variables

The checked
`scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json` manifest runs two isolated arms in
strict `serial-paired` order with root seed 0:

1. the existing `act-first-reserve-none-move-each-once-then-complete` control; and
2. the additive
   `act-first-reserve-none-move-each-once-by-lowest-cost-then-complete` instrument.

Both arms use the same admitted setup, content, scenario, ruleset, terminal boundary, maximum-step
bound, build mode, confidentiality/detail tier, pair key, pair-local ordinal, campaign creation
inputs, initial snapshot, and role/domain seed ledger. Only the versioned controller configuration
differs in behavior-bearing inputs; the arms necessarily use distinct exercise IDs as isolated-run
labels, and those IDs are not campaign creation inputs.

The trusted Runner candidate advances from v2 to v3 and carries the exact positive
`MoveElementAction.CostBreakdown.TotalCost` already present in the public legal action. Non-Movement
candidates cannot carry it. Core still owns legal-action derivation, cost calculation, submission,
adjudication, events, state, replay, and terminal truth. The lowest-cost policy keeps element order
stable, compares exact rational costs within the first eligible element, then uses destination,
origin, and action ID as deterministic tie-breakers.

## Observations

- Both arms accepted 13 actions, passed 94 checks, reached exact first-side Breakdown
  Determination, reconstructed exactly, and re-adjudicated exactly.
- The comparison retained equal creation-input hash
  `sha256:22d68c9755a113c587b1a82c5cc070b18430147adcec7149f7c784843c0783b7`,
  equal initial-snapshot hash
  `sha256:fe1a97265a581b971e5b3cb76cbdb698ece0130d5c2ae6abbeb165dbb79cf117`,
  and equal seed-ledger hash
  `sha256:0a970c85404176c70aab6ef9f50b6bd5b4360dd1b12f603a9e7aa34814a0c5f0`.
- The first accepted-action divergence is step ordinal 10, the first Movement route choice.
- The control moved Axis A `west -> center` for exact cost 8, then Axis B
  `north-west -> north` for exact cost 1: total accepted Movement cost 9.
- The lowest-cost arm moved Axis A `west -> north-west` for exact cost 1/2, then made the same
  Axis B cost-1 move: total accepted Movement cost 3/2.
- Accepted-step count delta is zero and both terminal outcomes are equal. The experiment therefore
  isolates an observed route/ledger sensitivity without changing boundary reachability here.
- Two fresh CLI runs into separate artifact roots produced the identical strict parent fingerprint
  `sha256:5f997e4d74d0f9b83e43d6d4c2c33ebedae03b188a7e30d64f57092a51edd1bc`.
- The complete `Cna.ExerciseRunner.Tests` project passed 378/378 tests with zero skipped after the
  instrument and checked fixture were added.

## Decision

Accept the lowest-public-cost controller as a bounded trusted simulator instrument. It demonstrates
that the current Movement fixture can distinguish a declared controller decision and that exact
cost propagation remains deterministic and replay-safe. Preserve the stable-ID controller as the
regression control; do not reinterpret the lowest-cost controller as an Umpire rule or recommended
player strategy.

More seed sweeps remain low value for this exact non-contact question because neither controller
uses randomness in route selection. The next useful simulator expansion should change one declared
setup or controller dimension at a time and use paired evidence where equal initial conditions are
required.

## Tuning gate and next experiments

The simulator is ready now for deterministic regression, contract hardening, controller-sensitivity
studies, and scenario-fixture debugging. Gameplay tuning should wait until the relevant outcome
surface exists: at minimum approved and implemented Breakdown, ZOC/Reaction or Contact behavior,
combat resolution, victory-relevant state, and representative published scenario data. Tuning
before those mechanics would optimize a synthetic non-contact slice rather than the game.

Candidate bounded follow-ons are:

1. repeat this paired route-cost comparison across deliberately admitted setup/topology variants;
2. add a separately declared highest-cost or route-diversity instrument only if it answers a named
   robustness question;
3. add seed sweeps when a covered rule or controller actually consumes the relevant RNG domain;
4. after contact/combat verticals exist, define outcome measures and tune only against retained,
   versioned scenario cohorts rather than one rules-laboratory fixture.

## Limitations

- One synthetic setup, one root seed, one initiative posture, and Reserve none are observed.
- The result is descriptive. It supports no causal, statistical-significance, gameplay-balance,
  recommendation, confidence-interval, or synchronized-post-divergence claim.
- Both arms stop at Breakdown Determination. No Breakdown action/adjudication, ZOC/Reaction,
  Contact/Engaged, combat, Reserve Release, second-side Movement, victory, campaign persistence,
  model controller, side-safe export, distributed execution, or performance threshold is covered.
- The parent fingerprint binds the paired report contract; detailed child truth still depends on
  strict bundle readback, semantic validation, reconstruction, re-adjudication, and identity
  reconciliation.

## Reproduction

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run \
  --manifest scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json \
  --artifact-root artifacts/exercises
```

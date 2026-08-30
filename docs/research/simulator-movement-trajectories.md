# Simulator Movement Trajectories

**Status:** Completed and merged in PR #78

**Date:** 2026-08-29

## Question

Does the bounded Movement controller repeat exact trusted evidence across deliberately selected seed
boundaries and interiors, and does seed variation change the supported non-contact routes selected
by the six act-first/act-last and Reserve none/one/all policies?

## Scope and method

`MovementSimulatorStudyTests` executes four root seeds—`0`, `1`, `ulong.MaxValue / 2`, and
`ulong.MaxValue`—against all six
`act-{first,last}-reserve-{none,one,all}-move-each-once-then-complete` controllers. Each combination
runs twice in process, for 48 total trajectories.

The seeds are boundary and interior determinism probes: zero, the adjacent low value, a large
interior value, and the unsigned-64 maximum. They are not a random or statistically representative
sample. Every run uses the predetermined rules-laboratory setup, the current public
observation-derived actions, ordinary submission, the same v2 manifest/controller configuration
contracts, maximum 13 steps, and exact first-side Breakdown Determination terminal.

For each repeat the test compares canonical accepted actions, events, step evidence, initial and
final snapshots, reconstruction proof, and fresh-session re-adjudication proof. It also derives a
Movement signature from element ID, origin, destination, and exact final CP expenditure for every
accepted `element-moved` event.

## Observations

- All 48 trajectories succeeded in 13 accepted actions and reached exact first-side Breakdown
  Determination; no Breakdown action or adjudication was added.
- Within every seed/controller combination, both runs produced exact equal accepted actions, events,
  step evidence, snapshots, reconstruction evidence, and re-adjudication evidence.
- Each controller produced one Movement signature across all four seeds. The observed supported
  routes were invariant under this seed set.
- Reserve `none` moved each eligible element once: Axis A `west -> center` or Commonwealth A
  `east -> center` at exact CP cost 8, then Axis B `north-west -> north` or Commonwealth B
  `south-east -> east` at exact CP cost 1.
- Reserve `one` moved only the eligible B element on its cost-1 route. Reserve `all` moved no
  element. These are 2/1/0 moves for the none/one/all policies.
- The checked six-child CLI fixture separately retains 13 actions/events and 94 passed checks per
  child. Its current local aggregate is 78 actions/events, six Reserve designations, six Reserve
  completions, six moves, six Movement completions, and exact final CP expenditure 20.
- Two clean CLI executions into separate artifact roots reconfirmed aggregate fingerprint
  `sha256:c1c20270dcd3402886931c28851bea7f23cd1e0778b45f94c43d85ed01d41c4b`.

## Interpretation

For this bounded synthetic setup and selected probes, the new policy is repeatable and its route
choice is insensitive to the tested seeds. This supports a deterministic regression claim only.
The study does not establish what other scenarios, policies, or later mechanics would do.

## Limitations and deferrals

- The aggregate report fingerprint does not bind detailed child event or CP/ledger bytes. Trusted
  evidence therefore still depends on strict child bundle semantics, reconstruction, fresh-session
  re-adjudication, and report reconciliation.
- Four deliberately selected seeds are boundary/interior probes, not a statistical sample. No
  causal effect, statistical significance, gameplay balance, recommendation, or performance
  threshold is claimed.
- No synchronized-post-divergence claim is made. The study is serial-unpaired and does not infer
  common-random-number behavior after trajectories differ.
- The result does not cover model controllers, side-safe export, distributed or parallel execution,
  full victory, enemy occupancy or ZOC/Reaction, Contact/Engaged behavior, Breakdown adjudication,
  combat, Reserve Release, repeated Movement cycles, or second-side Movement.
- PR #78 merged the checked fixture and study on 2026-08-30. The follow-on
  [Movement Cost Sensitivity](simulator-movement-cost-sensitivity.md) study now varies an explicit
  controller decision instead of multiplying seed probes that do not affect route selection.

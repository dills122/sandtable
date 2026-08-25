# Simulator Baseline 1: Reserve Preamble To Movement

**Status:** Completed; acceptance criteria passed

**Date:** 2026-08-25

## Question

Does the reviewed engine and harness produce stable, reader-validated results when the complete
implemented turn preamble is repeated across both admitted initiative setups, and what local
throughput does that path currently show?

## Method

Run the checked
`scenarios/maneuvers/rules-lab.reserve-designation.serial.v2.json` Maneuver 20 times with unchanged
inputs and current Debug assemblies. Each Maneuver contains the predetermined and contested setup,
uses the stateless designate-all Reserve controller, and must stop at first-side Movement.

For every strictly read-back report, retain:

- deterministic report fingerprint and overall status;
- requested, validated, succeeded, failed, and aggregation-failed counts;
- per-child terminal position, accepted action count, passed/failed check count, normalized manifest
  hash, and seed-ledger hash; and
- noncanonical Maneuver and per-child elapsed microseconds.

Aggregate timings as minimum, median, p95, and maximum. Fingerprints, counts, outcomes, and identity
hashes are correctness evidence. Timings are local diagnostics only.

## Acceptance criteria

- 20/20 Maneuvers and 40/40 child Exercises validate successfully.
- All Maneuvers have one identical deterministic report fingerprint.
- Every child reaches the exact Movement terminal in 12 accepted actions with zero failed checks.
- Each setup retains one stable normalized-manifest hash and one stable seed-ledger hash.
- No report or bundle admission failure occurs.

## Limitations

The workspace is dirty by design, the assemblies are Debug builds, and this is one local machine.
The experiment does not claim production performance. More importantly, the current engine stops at
Movement, so these results say nothing yet about movement decisions, combat, victory, game balance,
model behavior, or player-input quality.

## Results

### Correctness and repeatability

| Metric | Result |
| --- | --- |
| Maneuvers | 20/20 succeeded and were strictly read back |
| Child Exercises | 40/40 validated and succeeded |
| Failed/aggregation-failed/not-run children | 0/0/0 |
| Accepted actions | 480 total; exactly 12 per child |
| Checks | Exactly 87 passed and 0 failed per child |
| Terminal | All 40 reached `land.position.operation-1.first-player.movement-and-combat.movement` |
| Unique deterministic report fingerprints | 1 |
| Fingerprint | `sha256:7bf6a94e4beaa5b02f45b8c96588e65d54238076fd33fa46e1083d439b48f79b` |
| Failed artifact files | 0 |

The report fingerprint exactly matches the value frozen by the implementation tests and design.
For each setup, all 20 runs also retained one normalized-manifest hash, one seed-ledger hash, and one
artifact-manifest hash. The manifest-last bundle identities therefore remained stable in addition to
the aggregate report.

| Setup | Normalized manifest | Seed ledger |
| --- | --- | --- |
| Predetermined | `sha256:9624516973e0d765e191bb9eadb714fe420d818cf5de37c58bd778f9c8592264` | `sha256:0e95e7e975bfc96300ae508f16b4228dc35ad4f719399da9ef5837d1d39c6c8a` |
| Contested | `sha256:aeaf0357fa9903a5fd7c4c67140be320444d18dfe0d9124396f2a5b7def05bdc` | `sha256:94470b30ada496e1d7952c5b8e4a9c74caf7035a8c240d66f90df8f90ad8e0fc` |

### Local diagnostic timing

All values below are milliseconds and are excluded from simulation truth and report fingerprinting.

| Scope | Minimum | Median | p95 | Maximum | Mean |
| --- | ---: | ---: | ---: | ---: | ---: |
| Complete two-child Maneuver | 1,733.101 | 1,791.890 | 1,890.354 | 1,971.890 | 1,800.269 |
| Predetermined child, ordinal 0 | 1,075.914 | 1,153.816 | 1,177.856 | 1,270.699 | 1,158.615 |
| Contested child, ordinal 1 | 579.483 | 618.577 | 709.950 | 792.508 | 640.264 |

Across the 36.005 seconds of harness-reported Maneuver time, observed throughput was approximately
1.11 validated Exercises/second or 13.33 accepted actions/second. This includes evidence writing,
strict bundle readback, reconstruction, and fresh-session re-adjudication—not just adjudication.

The child timing difference is not a setup-performance conclusion. The predetermined child always
runs first in a fresh CLI process, so ordinal, cold-start, JIT, and initialization costs are
confounded with setup identity. A counterbalanced-order experiment is required before attributing
the difference to the engine path.

### Environment and artifact footprint

- macOS 26.5.2, arm64;
- .NET runtime 10.0.11, SDK 10.0.400;
- Debug exploratory assemblies;
- reviewed HEAD `ca3e3385e26551407b09ff17c0629ed7b7a7eaa3`;
- dirty/nonbaseline build identity, as required for this feature workspace; and
- ignored artifact root `artifacts/simulator/baseline-1`: 20 reports, 40 bundles, approximately
  6.6 MiB.

## Decision

Baseline 1 passes. The current engine/harness boundary is stable enough to support systematic
back-testing: identical inputs produced byte-stable deterministic reports and bundles, every
authority path reached the intended Movement checkpoint, and no semantic admission failure
occurred.

The next experiment should combine two improvements:

1. counterbalance child order or run in-process repetitions to separate engine cost from CLI/JIT
   startup cost; and
2. sweep contested-initiative root seeds, recording initiative outcomes, random cursor evidence,
   action/event trajectories, and terminal invariants.

Balance, decision quality, and outcome-distribution studies remain blocked on actual Movement,
combat, and victory mechanics.

# Simulator Baseline 2: Order And Contested-Seed Sweep

**Status:** Completed; acceptance criteria passed

**Date:** 2026-08-25

## Questions

1. Does the slower ordinal-0 timing from Baseline 1 follow the setup or the first child executed in
   a fresh process?
2. Across root seeds 0-31, does every contested-initiative trajectory remain deterministic,
   reader-valid, and Movement-terminal, and what initiative outcomes occur?

## Method

Generate ignored canonical Maneuver v2 manifests from the reviewed Reserve Designation fixture.
For the order experiment, reverse the two exercises and run 20 repetitions. Compare its ordinal
timings with the 20 original-order observations from Baseline 1.

For the seed experiment, materialize a single contested Exercise for each root seed 0-31. Run each
once through the normal CLI so every child bundle and parent report receives the same strict
manifest, build, replay, semantic, and readback validation as checked fixtures. Extract the exact
initiative event and report coordinates from accepted evidence.

## Acceptance criteria

- 20/20 reversed-order Maneuvers and 40/40 children succeed at Movement.
- 32/32 seed-sweep Maneuvers and Exercises succeed at Movement with 12 accepted actions and zero
  failed checks.
- Each seed has one internally consistent initiative outcome and deterministic report fingerprint.
- No failed or aggregate-invalid artifact is produced.

## Results

### Counterbalanced order

All 20 reversed-order Maneuvers and all 40 children succeeded, reached Movement in 12 actions, and
retained 87 passed/zero failed checks. Every reversed-order Maneuver had the same deterministic
fingerprint:
`sha256:cd63891f40706c31bf76064b8662e0f145bb8cbead38da1e6761ffb977735e81`.

The timing gap followed execution order, not setup identity. All values are noncanonical local
diagnostics in milliseconds.

| Order | Ordinal 0 median / p95 | Ordinal 1 median / p95 | Maneuver median / p95 |
| --- | ---: | ---: | ---: |
| AB: predetermined, contested | 1,153.816 / 1,177.856 | 618.577 / 709.950 | 1,791.890 / 1,890.354 |
| BA: contested, predetermined | 1,169.832 / 1,213.991 | 609.911 / 702.508 | 1,775.068 / 1,917.882 |

When contested moved from ordinal 1 to ordinal 0, its median increased by 551.255 ms. When
predetermined moved from ordinal 0 to ordinal 1, its median decreased by 543.905 ms. Within a fixed
ordinal, the two setup medians were within approximately 16 ms. The roughly 0.55-second cost is
therefore first-child/process initialization overhead in this experiment, not evidence that the
predetermined setup is intrinsically slower.

### Contested root seeds 0-31

All 32 Maneuvers and Exercises succeeded and were strictly read back. Every run reached the exact
Movement terminal in 12 accepted actions, recorded 87 passed and zero failed checks, designated two
eligible elements for the actual first side, and produced no failed artifact.

| Outcome | Count | Root seeds |
| --- | ---: | --- |
| Axis holds initiative | 13 | 4, 5, 9, 12, 13, 17, 19, 21, 25, 26, 28, 29, 30 |
| Commonwealth holds initiative | 19 | 0, 1, 2, 3, 6, 7, 8, 10, 11, 14, 15, 16, 18, 20, 22, 23, 24, 27, 31 |
| One initiative round | 29 | all except 2, 13, 25 |
| Two initiative rounds | 3 | 2, 13, 25 |
| Normal Weather | 7 | 0, 9, 10, 16, 17, 19, 23 |
| Hot Weather | 21 | 1, 2, 3, 7, 8, 11, 12, 13, 14, 15, 18, 20, 21, 22, 24, 25, 27, 28, 29, 30, 31 |
| Sandstorm | 4 | 4, 5, 6, 26 |

These 32 samples are descriptive coverage, not balance estimates. The initiative holder acted first
in all 32 runs, and the Reserve-designation side always matched that first side. This is expected
from the current first-by-action-ID fallback, but it exposes an important simulator coverage bias:
seed variation alone does not exercise the holder-acts-last branch, and the designate-all policy
does not exercise zero/one-element Reserve choices.

The complete seed-level holder, round/dice, cursor, Weather, first-side, and fingerprint evidence is
retained in [the Baseline 2 seed dataset](simulator-baseline-2-seeds.csv). Dice pairs are written as
`Axis:Commonwealth` for initiative and `first:second` for Weather.

Random-cursor evidence was internally consistent:

- one-round initiative consumed two draws and ended at cursor 2;
- two-round initiative consumed four draws and ended at cursor 4;
- normal/hot Weather consumed two further draws; and
- sandstorm consumed the additional location draw, ending at cursor 5 after a one-round initiative.

All 32 seed-specific manifests produced distinct report fingerprints, as expected because root seed
and Maneuver identity are fingerprint material. Seeds 0, 2, 4, and 13 were repeated to cover both
holders, both round counts, and sandstorm. Each repeat matched both its original report fingerprint
and its complete child artifact-manifest hash byte-for-byte.

The single-child sweep had a 1,167.370 ms median Maneuver time and 1,181.687 ms p95. This remains a
fresh-process Debug diagnostic, not an adjudication benchmark.

### Artifact inventory

- reversed order: 20 reports and 40 child bundles;
- primary seed sweep: 32 reports and 32 child bundles;
- representative seed repeats: 4 reports and 4 child bundles;
- failed artifact files: 0; and
- ignored Baseline 2 footprint: approximately 13 MiB.

## Decision

Baseline 2 passes. The engine and evidence pipeline remain deterministic across the first useful
randomized input sweep, and the apparent setup-performance gap from Baseline 1 is explained by
execution order. The next simulator investment should be policy/scenario coverage rather than a
larger seed count:

1. add explicit act-first and act-last deterministic controller profiles;
2. add Reserve none/one/all policy profiles; and
3. add an in-process batch/sweep entry point so engine timing can be separated from CLI startup and
   artifact orchestration.

Once Movement and combat exist, this policy matrix can become the foundation for trajectory,
outcome, and balance back-testing.

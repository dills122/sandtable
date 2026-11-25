# Scenario B sweep

`python3 tools/sweep/sweep.py scenarios/sweeps/<name>.json [--build] [--parallelism N] [--repeats N] [--out DIR]`

Generates one manifest per (policy, seed) from the sweep input, runs one `Cna.ExerciseRunner` process per
manifest in parallel (local processes only), then writes `aggregate.json` (identity only: exit codes, outcome and
failure categories, step counts, final positions, per-position visit counts, per-run bundle hash, determinism
verdict) and `timing.json` (wall-clock; not identity). Bundle hashes cover every file except `diagnostics.jsonl`,
and repeat k must equal repeat 0. The Runner manifest contract is unchanged. Default out dir
`artifacts/sweeps/<sweepId>` (gitignored); generated manifests go to `.planning/sweeps/<sweepId>` (gitignored).
Exit 0 ok, 1 determinism mismatch, 2 harness error.

## First dataset: `scenarios/sweeps/battalion-matrix-200.json` (VERIFIED, 2026-11-25, 10-core arm64 Mac, shared with other workers)

- 200 runs (100 seeds x `first-by-action-id` and `act-first-reserve-all-move-each-once-then-complete`), 8 parallel.
- Two repeats: 145.7 s (1.37 runs/s) and 136.7 s (1.46 runs/s). Lower than the plan's 2.4 runs/s; this machine was loaded.
- Determinism: 200/200 bundles byte-identical across repeats.
- Outcome: 200/200 exit 11, `no-unique-legal-action`, final position second-side `reserve-release`; 30 steps
  (`first-by-action-id`) and 32 steps (act-first policy). Terminal reached: 0. See
  `docs/work/2026-11-25-l8-second-side-reserve-release-exit-11.md`.

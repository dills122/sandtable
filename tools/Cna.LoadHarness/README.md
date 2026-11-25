# Cna.LoadHarness (L2)

Development-only load tool for the existing combat-infantry board-game lab.
It is deliberately outside `Sandtable.slnx`; the separate path-filtered
Load harness workflow builds it and runs its black-box tests.

```sh
dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --campaigns 16 --policy random-legal --root-seed 7 --output /tmp/load-a
python3 tools/Cna.LoadHarness/test_harness.py
```

Run again with a fresh output directory and compare `fingerprints.json`.
For a concurrency comparison, compare `campaigns/0.jsonl` with the same file
from a `--campaigns 1` run. The output directory must not already exist.

## Scope and semantics

- One in-process localhost Orleans silo, independent SQLite journals, real
  seat grant/redeem, read-only open-seat discovery, grain opening and submission
  calls. Reuses the LocalCampaignSimulator bootstrap pattern, without a web
  listener and without changing the HTTP access gate.
- Stage 1 only, fixed admitted setup and lab seed 12345 (as in L1). The harness
  stops at a verified Stage 2 initiative-declaration checkpoint, never submits
  the declaration. The decision budget is one hour to exclude timeout paths
  from determinism runs. `--max-steps-per-campaign` defaults to 50 (range 1–50).
- Seats and discovery entries are visited in stable L1 order. The last available
  opening is selected; each policy chooses its **index** within that opening:
  `first`, `last` (default), or `random-legal` (uniform SplitMix64, independently
  seeded for each role using its Runner seed ledger). Campaign identities are
  stable by ordinal, not process, concurrency, path, or run time. The root seed
  affects the controller streams, not the admitted lab seed.
- L1 fallback candidates are ordered data keyed by previous accepted stage;
  unavailable probes are counted separately. Missing/unsupported openings and
  max-step exhaustion are partial runs, not successful completions. Alternate
  legal paths are not guaranteed wired; no hidden state is used to choose actions.
- Exit 0 requires every campaign to reach the verified boundary; exit 1 means
  incomplete campaign(s); exit 2 means invalid CLI configuration. Ctrl+C stops
  at the next owner step and records incomplete campaigns where possible.

## Output (version 1)

| File | Contents |
|---|---|
| `run-manifest.json` | Assembly versions, configuration, scenario, lab seed, GC mode and CPU count |
| `seed-ledgers/<ordinal>.json` | Exact Runner `sandtable.exercise-seeds.v1` ledger, including per-role controller seed and digest |
| `campaigns/<ordinal>.jsonl` | Accepted owner decisions: zero-based ordinal, seat, stage, acting element, count/index, committed revision |
| `fingerprints.json` | Per-campaign terminal status and decision hash, aggregate decision hash |
| `diagnostics.jsonl` | Discovery/open/probe/submit outcomes and elapsed milliseconds |
| `summary.json` | Completion counts, throughput, per-campaign latency, per-call latency percentiles and outcome counts |

Hashes are SHA-256 of UTF-8 JSONL (fixed field order, LF terminator per row),
then SHA-256 of the ordered per-campaign hash strings (LF terminator per hash).
Timing, opaque refs, seat secrets and temporary paths are not fingerprinted or
written to artifacts. Decision fingerprints measure observed choices, not full
state identity. Summary throughput covers campaign creation through boundary
verification, excluding silo startup/shutdown. State fingerprint and the full
T1 gate are deferred to L5. Postgres, HTTP driver, 64/100-campaign nightly soak
and performance budgets belong to later slices.

## L2 baseline (2026-11-25)

Apple Silicon, .NET 10.0.400 Release, Server GC, shared development machine;
SQLite and no think time. Root seed 7, fixed lab seed 12345. All campaigns
reached the verified Stage 2 boundary; no Stage 2 action was submitted.

| Policy / campaigns | Accepted owner steps | Elapsed (s) | Steps/s | Submit p95 (ms) | Expected probe conflicts | Other call errors |
|---|---:|---:|---:|---:|---:|---:|
| first / 1 | 22 | 11.53 | 1.91 | 326 | 18 | 0 |
| last / 1 | 21 | 16.47 | 1.28 | 451 | 18 | 0 |
| random-legal / 1 | 22 | 12.78 | 1.72 | 313 | 18 | 0 |
| random-legal / 16 (A) | 342 | 52.83 | 6.47 | 2260 | 288 | 0 |
| random-legal / 16 (B) | 342 | 139.54 | 2.45 | 4955 | 288 | 0 |

Both N=16 runs produced byte-identical `fingerprints.json` (aggregate
`sha256:f87bd333e391701e585f1784946301d68eaca82e94c0ee5ddecb64ca1f21301d`).
Campaign 0's decision JSONL also matched the N=1 random-legal run exactly.
Individual N=16 campaigns had 19–24 accepted steps. The discovery tax was
716 discovery calls, 236 advertised opens, and 448 fallback probes per run.
The second run experienced scheduler/thread-pool degradation warnings under
shared-machine load (including a concurrent format check). These are observations,
not latency or throughput gates;
state fingerprints and Postgres measurements remain future slices.

# Cna.LoadHarness (L2/L3)

Development-only load tool for the existing synthetic board-game labs.
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

- One in-process localhost Orleans silo, independent SQLite journals (default)
  or the production Marten/Postgres journal, real seat grant/redeem, read-only
  open-seat discovery, grain opening and submission calls. Reuses the LocalCampaignSimulator bootstrap pattern, without a web
  listener and without changing the HTTP access gate.
- Default `--until stage2`: fixed L1 combat-infantry setup, lab seed 12345.
  Stops at a verified Stage 2 initiative-declaration checkpoint without submitting
  the declaration. Step limit defaults to 50 (range 1–50).
- Optional `--until turn2-reserve`: uses B6's fresh GT1 turn-boundary setup
  (same lab seed). Follows the synthetic discovery path through all three
  operation stages, verifies the End of Turn checkpoint before opening Turn 2's
  declaration, and stops at the verified, open but unaccepted Turn 2 Reserve.
  The step limit defaults to 112 (range 1–112). Both paths use a one-hour decision
  budget to exclude timeouts from determinism measurements.
- Seats and discovery entries are visited in stable L1 order. The last available
  opening is selected by default; each policy chooses its **index** within that opening:
  `first`, `last` (default), or `random-legal` (uniform SplitMix64, independently
  seeded for each role using its Runner seed ledger). Campaign identities are
  stable by ordinal, not process, concurrency, path, or run time. The root seed
  affects the controller streams, not the admitted lab seed.
- For the extended B6 fixture only, skip optional movement openings, require
  one discovered owner/stage, pick the ordinal-last acting element, and filter
  Reserve choices to the visible `complete` choice. Random-legal samples uniformly
  from the remaining eligible refs (including singleton Reserve completion).
  Recorded count/index refer to the original opening, not the filtered list.
  This exercises the supported synthetic checkpoint path, not every legal branch.
- L1 fallback candidates are ordered data keyed by previous accepted stage;
  unavailable probes are counted separately. Missing/unsupported openings and
  max-step exhaustion are partial runs, not successful completions. Alternate
  legal paths are not guaranteed wired; no hidden state is used to choose actions.
- Exit 0 requires every campaign to reach the verified boundary; exit 1 means
  incomplete campaign(s); exit 2 means invalid CLI configuration. Ctrl+C stops
  at the next owner step and records incomplete campaigns where possible.

## Postgres capacity runs (L3)

```sh
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --store postgres --until turn2-reserve --campaigns 16 --hot-pool 16 \
  --policy random-legal --root-seed 7 --output /tmp/load-pg-a
SANDTABLE_LOAD_POSTGRES_TEST=1 python3 tools/Cna.LoadHarness/test_harness.py
```

With no external configuration, Testcontainers starts `postgres:16-alpine`
and cleans it up after the run. Docker must be running. Set
`SANDTABLE_POSTGRES_HOT_CONNECTION_STRING` to an Npgsql connection string
for an external **test** server instead. The account must be able to create and
drop databases: the harness creates a unique fresh `load_<guid>` database,
uses Host's Marten factory/schema/backend, and removes only that database on
disposal. Do not point it at a production server. Orleans clustering/reminders
remain in-process and in-memory; only the campaign journal is Postgres-backed.

`--hot-pool 1..16` defaults to 16, limits concurrent campaign loops and caps
the Npgsql hot database pool; the monitor uses a separate unpooled connection
to the administrative database. Campaign loops waiting for a slot do not create
campaigns until admitted. This is not a grain activation-cache limit; the
production Marten store reconstructs a fresh working set per call.

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
verification, excluding silo startup/shutdown. Full T1 state fingerprint
verification is deferred to L5. HTTP driver, 64/100-campaign
nightly soak and performance budgets belong to later slices.

Default SQLite/Stage 2 retains the version 1 output layout and fingerprint
encoding (timestamps and measured latency are inherently variable).
Postgres, extended checkpoint, or non-default pool runs add `until` and
`hotPool` to the manifest and append metrics to the summary:
`postgresConnectionPeak` (sampled peak client backends for this run's database,
excluding the monitor; null if no samples succeed or SQLite),
`postgresConnectionSamples` and `postgresConnectionSampleFailures` (successful
and failed connection count samples; diagnostic failures do not discard workload
artifacts), `rssPeakBytes` (sampled process working
set), `metricSamples`, and `gc` (gen0/1/2 collection count deltas, total pause
milliseconds, sampled peak and final managed heap bytes). Samples are taken
before/after the timed workload and every 250 ms; peaks are lower bounds.
The process includes the in-process silo and harness, not the Postgres server.
Connection and GC/RSS measurements are diagnostic, not latency gates.

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
state fingerprints remain a future slice.

## L3 baseline (2026-11-25)

Apple Silicon, .NET 10.0.400 Release, Server GC, 10 logical processors,
Postgres 16 Alpine on Docker Desktop, pool cap 16, no think time. Each run used
a fresh database, `--store postgres --until turn2-reserve --policy random-legal
--root-seed 7`. Shared development machine; N=1 measurements overlapped
short focused validation runs. Latencies are observations, not gates.

| Run | Completed | Owner steps | Elapsed (s) | Submit p50/p95 (ms) | Discover p50/p95 (ms) | PG peak | Gen0/1/2 | GC pause (ms) | Heap peak/final (MiB) | RSS peak (MiB) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1-A | 1/1 | 58 | 47.55 | 139/549 | 125/418 | 1 | 693/20/3 | 4596 | 126.3/57.1 | 351.1 |
| 1-B | 1/1 | 58 | 56.58 | 151/836 | 115/636 | 1 | 692/16/2 | 7091 | 128.8/58.5 | 355.5 |
| 16-A | 16/16 | 928 | 176.05 | 672/1931 | 552/1118 | 16 | 11377/1520/342 | 70563 | 164.6/120.3 | 426.3 |
| 16-B | 16/16 | 928 | 187.39 | 701/2479 | 534/1352 | 16 | 11185/1484/334 | 80850 | 165.8/130.7 | 418.4 |

All campaigns completed the verified open Turn 2 Reserve checkpoint; zero
call errors or conflicts. Both pairs of runs had byte-identical
`fingerprints.json` and every per-campaign decision JSONL. Campaign 0 also
matched byte-for-byte across all four runs: decision hash
`sha256:025c5da9f27807e568af2c08551927b6ee546efedc0c0e47a83d2f483f5b193f`.
N=16 aggregate decision fingerprint:
`sha256:0824fedc0407209c99538615c018cbb464e01094b77eb74b499bca565b95eece`.
This passes the decision-fingerprint portion of T1, including the concurrency
comparison (only ordinal 0 exists at N=1); full state fingerprint verification
is L5. T2's 100-campaign paced workload and latency budgets were not tested.

A separate external-server smoke used two campaigns, a hot-pool cap of one,
and a one-step limit. Both stopped at the limit as expected, peak connection
count was one, and the external server had no leftover harness databases.

Monitor-only fault injection on an external test server (temporary connection
limit zero on the monitor database, then restored) recorded eight failed and
48 successful samples. The one campaign retained all 14 accepted owner steps
and every aggregate artifact; the expected step-limit exit and database cleanup
were preserved.

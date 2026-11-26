# Cna.LoadHarness (L2–L4)

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

## In-process scope and shared decision semantics

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
- In-process exit 0 requires every campaign to reach the verified boundary; exit 1 means
  incomplete campaign(s); exit 2 means invalid CLI configuration. Ctrl+C stops
  at the next owner step and records incomplete campaigns where possible.

## Full owner-action bot (Bot S1)

```sh
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --policy random-legal-full --until turn2-reserve --root-seed 7 \
  --require-coverage moves,attacks --output /tmp/full-bot
```

`random-legal-full` samples uniformly from the union of all currently advertised
owner refs, ordered by audience, action stage, acting element and original choice
index. It uses the campaign's System controller entry in the Runner seed ledger
with its own SplitMix64 stream (separate from HTTP pacing). Unlike the checkpoint
policies it does not skip optional movement, constrain Reserve choices, or prefer
no-attack combat. The accepted-step limit is 512 by default for this policy; an
explicit step limit bounds incomplete runs. Existing policies, defaults, manifest
and decision-fingerprint encoding are unchanged.

A submit refusal with code `rule-not-implemented` (HTTP 409, currently the known
later-stage Reaction gap #655) is counted as `reactionRefused`, not an error.
The bot removes that exact candidate from the current pool; completion/finish
choices remain eligible. It samples the remaining pool until one action succeeds
or the pool is exhausted (`wedge`). Other rejections, unsupported discovery, empty
discovery, checkpoint/replay failures, and step-limit exhaustion fail the run.
Locally launched full-policy campaigns also verify replay on exit, including
partial campaigns; this post-run diagnostic never drives owner choice. Attached
HTTP mode retains its public-checkpoint-only evidence boundary unless explicitly
using L5 `--verify-state`; that option adds a state fingerprint, not local
canonical journal position coverage.

Full-policy `summary.json` includes aggregate and per-campaign `coverage`:
accepted moves, visible move CP cost, attack selections, combat-step counts and
choice-kind counts, Reaction refusals, finish/completion actions, and distinct
turn-qualified owner-visible position IDs. The position count is an **observed
lower bound**: not every cut includes a position ID, and automatic/system-only
positions in the 112-position Turn 1 catalog and Turn 2 prelude are not owner
openings. For locally launched full-policy hosts, `journalPositionsVisited` and
`journalPositionIds` count explicit turn-qualified canonical event positions
(including recorded automatic edges), read only after campaign completion or
failure and verified replay. `journalCampaigns` identifies how many campaigns
contribute journal evidence; attached HTTP runs cannot provide it.
Aggregate positions are a union, not a sum. An accepted attack selection
is not proof that an assault resolved; force, retreat, custody and RBA are
counted separately. `zeroCoverage` identifies zero moves/attacks per campaign and
aggregate. `--require-coverage moves,attacks` additionally fails the process if
either aggregate count is zero, even if all campaigns reached the checkpoint.
These coverage fields are opt-in for existing policies via `--require-coverage`.

`attempts/<ordinal>.jsonl` records the deterministic owner choice immediately
before each submit (including refusals), followed by an outcome row after its
return. The accepted decision JSONL is flushed incrementally as well, so even
an interrupted or hung process leaves a replay prefix. Neither contains
credentials or opaque refs. The existing `campaigns/<ordinal>.jsonl` and
fingerprints still describe accepted submissions only.

Repeatable bug-hunt sweep (new output directories, Release binary already built):

```sh
python3 tools/Cna.LoadHarness/sweep.py --output /tmp/full-sqlite \
  --seeds $(seq 1 32) --parallel 4 --timeout 600
python3 tools/Cna.LoadHarness/sweep.py --output /tmp/full-http-pg \
  --driver http --store postgres --campaigns 16 --seeds 1 7 16 32 --timeout 1200
```

Each seed is run twice at the requested concurrency, and the script compares every
accepted-decision and attempt JSONL byte-for-byte. Per-run `result.json` records
the exact arguments, seed, ordinal, status, and prefix paths for each incomplete
campaign. A process timeout is classified as `hang-timeout`; missing summaries
are not counted as successes. Keep the output tree together when reproducing a
failure. The sweep is diagnostic, not part of the normal CI smoke gate.

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

`--hot-pool 1..16` defaults to 16, limits concurrent in-process campaign loops and caps
the Npgsql hot database pool; the monitor uses a separate unpooled connection
to the administrative database. In-process campaign loops waiting for a slot do not create
campaigns until admitted. This is not a grain activation-cache limit; the
production Marten store reconstructs a fresh working set per call.

## Real HTTP runs (L4)

```sh
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --driver http --store postgres --until turn2-reserve --campaigns 16 \
  --policy random-legal --root-seed 7 --output /tmp/load-http-a
```

`--driver inproc` remains the default. HTTP mode starts a real loopback Kestrel
listener and local Orleans silo in the harness process, with Development,
`CampaignJournal:EnableOutwardApi` and a generated operator secret. It uses the
existing Host v2 route map, not a mocked HTTP handler or newly added route.
SQLite uses the test journal backend (the production executable no longer
configures SQLite); Postgres uses the same factory as L3. Each campaign worker
owns one HttpClient and uses the real create, grant, redeem, discovery, opening
and submission routes. Requests carry the same-origin header. Operator secrets
are sent only on operator calls, never output, and redirects/proxies are disabled.

For an existing Development host on loopback with the outward API enabled:

```sh
# Set SANDTABLE_LOAD_OPERATOR_SECRET in your shell to the configured host secret.
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll \
  --driver http --base-url http://127.0.0.1:5080 --store postgres \
  --campaigns 2 --output /tmp/load-attached
```

Attachment never starts, modifies configuration, or stops the external host, and
uses fresh run-prefixed campaign IDs (controller seed identities remain
ordinal-based). Campaigns remain in the attached host's journal. Its actual
backing, version, pool, and resources cannot be inspected through the owner API;
`--store` is a caller-supplied description and GC/RSS describe only the harness.
PG connection metrics are null in attach mode.

HTTP-only options:

| Flag | Meaning |
|---|---|
| `--campaigns 1..100` | Number of campaign workers (inproc remains capped at 16) |
| `--ramp-seconds 0..3600` | Evenly stagger starts across this interval, first at zero and last at the end |
| `--think-ms-min/--think-ms-max 0..60000` | Inclusive uniform wait before each submit; defaults zero, independent System controller seed stream |
| `--max-inflight 1..100` | Global cap across **all** HTTP requests, including setup; default 16 |
| `--stop-submit-p95-ms <positive ms>` | Stop admission as soon as cumulative nearest-rank p95 of successful submissions exceeds limit |
| `--stop-error-rate <0..1>` | Stop admission when cumulative real request errors / completed HTTP requests exceeds limit |

Both cliff gates are opt-in and evaluated online without a warmup. In-flight calls
finish, pending/ramping/thinking owner loops stop, and optional read-only verification
still runs for created campaigns within the same concurrency cap. Partial artifacts are written
with `stoppedOnCliff`, `cliffReason`, and exit 1. This is a capacity test, not a
retrying client. Explicit user cancellation (Ctrl-C) also cancels verification.
`--hot-pool` continues to cap the launched Postgres pool;
it does not limit HTTP campaign workers. Keep paced runs within the one-hour
decision budget if comparing fingerprints.

HTTP summary adds peak in-flight requests, discovery/advertised-opening/fallback
probe counts, expected 409s, real error count/rate, and cliff fields. The Stage 2
ladder permits only 409 `unavailable` from opening probes; other 409s (including
submission conflicts) are errors unless explicitly handled as `reactionRefused` by
the full policy. B6 requires successful advertised opens.
Latency includes HTTP request/response, including decoding, but excludes time
waiting for admission and think time. Local metrics include Host and harness.

**Evidence boundary:** v2 returns only public opening revisions, not private
committed state versions. HTTP rows preserve L3's property order but encode
`resultingRevision: null`; the HTTP decision hash is reproducible across HTTP
repeats but is not the full L3 hash. Compare common fields by replacing
`resultingRevision` with null in the L3 JSONL before hashing. Stage 2 requires an attainable cut naming the Stage 2 position after other
discovered and fallback owner offers have been exhausted; Turn 2 Reserve requires four
accepted declarations and the observed open position, including Stage 2/3
and Turn 2 declaration cuts. This proves the public checkpoint path, not the
private End of Turn history or authoritative replay by itself. The optional
`--verify-state` flag adds a read-only operator verification after each created campaign
finishes, including partial or failed campaigns; it does not drive decisions or expose private committed revisions to
the owner driver.

## Output (version 1)

| File | Contents |
|---|---|
| `run-manifest.json` | Assembly versions, configuration, scenario, lab seed, GC mode and CPU count |
| `seed-ledgers/<ordinal>.json` | Exact Runner `sandtable.exercise-seeds.v1` ledger, including per-role controller seed and digest |
| `campaigns/<ordinal>.jsonl` | Accepted owner decisions: zero-based ordinal, seat, stage, acting element, count/index, committed revision |
| `fingerprints.json` | Per-campaign terminal status and decision hash, aggregate decision hash; optional verified state fingerprints |
| `diagnostics.jsonl` | Discovery/open/probe/submit outcomes and elapsed milliseconds |
| `summary.json` | Completion counts, throughput, per-campaign latency, per-call latency percentiles and outcome counts |

Hashes are SHA-256 of UTF-8 JSONL (fixed field order, LF terminator per row),
then SHA-256 of the ordered per-campaign hash strings (LF terminator per hash).
Timing, opaque refs, seat secrets and temporary paths are not included in
decision rows or stored directly in artifacts. Decision fingerprints measure observed choices, not full
state identity. Summary throughput covers campaign creation through boundary
verification, excluding silo startup/shutdown. The 64/100-campaign nightly soak
and capacity certification belong to later slices.

### Operator state verification (L5)

Add `--verify-state` to HTTP runs (including the example above). The harness
issues `GET /api/v2/campaigns/{id}/verification` with the operator capability
and existing same-origin header. The route is unavailable unless the Development
outward API is enabled, and rejects non-loopback or cross-origin calls. No grain
activation, durable append or timer processing occurs.

The `stateFingerprint` object in each campaign result and `fingerprints.json`
contains the verified persisted chain head, reconstructed snapshot hash, event
count (including genesis), canonical event byte count, checkpoint position and
game turn. The aggregate state hash is computed over the ordered fingerprint
objects as JSONL; a failed verification leaves that campaign fingerprint and
the aggregate null and fails the run. Verification failures are recorded separately
in `diagnostics.jsonl` (`operation: verify`), retaining any existing owner-loop failure
status. A successful owner loop with failed verification becomes `verification-failed`.
The existing decision hash calculation is unchanged.

Compare the reconstruction hashes independently of the persisted chain hashes:
record hashes bind opaque owner references, which use fresh host randomness.
Different chain heads across independent fresh databases therefore do not by
themselves establish a difference in reconstructed game state. Attached runs
also use unique campaign IDs, so are not suitable for exact state hash comparison.

Default SQLite/Stage 2 retains the version 1 output layout and fingerprint
encoding (timestamps and measured latency are inherently variable).
HTTP, Postgres, extended checkpoint, or non-default pool runs add `until` and
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

## L4 HTTP measurements (2026-11-25)

Same lab and policy inputs as L3: Postgres 16 Alpine via fresh Testcontainers,
Server GC, .NET 10.0.400, Apple Silicon, hot pool 16, in-flight cap 16, no
ramp or think time, random-legal root seed 7, `--until turn2-reserve`. Each run
observed the public Turn 2 Reserve checkpoint. Timings are indicative on a
shared machine; N=16 overlapped focused validation and showed thread-pool/GC
pressure, not a T2 capacity promise.

| Run | Completed | Steps | Elapsed (s) | Submit p50/p95 (ms) | Discover p50/p95 (ms) | Discover/open/fallback probes | Expected 409s/errors | PG peak | Gen0/1/2 | GC pause (ms) | Heap peak/final (MiB) | RSS peak (MiB) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1-A | 1/1 | 58 | 36.19 | 131/388 | 112/226 | 118/65/0 | 0/0 | 1 | 653/2/1 | 1768 | 132.8/120.7 | 371.0 |
| 1-B | 1/1 | 58 | 40.61 | 132/431 | 112/330 | 118/65/0 | 0/0 | 1 | 665/5/2 | 3263 | 134.5/101.1 | 366.0 |
| 16-A | 16/16 | 928 | 268.02 | 665/4876 | 546/3168 | 1888/1040/0 | 0/0 | 16 | 10926/1393/287 | 140448 | 179.6/151.7 | 431.1 |
| 16-B | 16/16 | 928 | 225.65 | 870/2741 | 687/2154 | 1888/1040/0 | 0/0 | 16 | 10945/1500/310 | 101306 | 173.0/116.1 | 427.4 |

Both HTTP repeat pairs had byte-identical `fingerprints.json` and per-campaign
JSONL. Ordinal 0 matched across both concurrency levels and all four runs.
The N=16 HTTP aggregate was
`sha256:0668d14a8e6d399cb29c1ce40d30ae37929fd9ad6e4b0a9f5b8a83c241cdd3a1`;
N=1 aggregate
`sha256:1ce1ec0cf36acddfffe15108c7200aec6ed6c7d73c50b046ce066a2c94b04ea7`.
Compared against the actual saved L3 A/B decision JSONL, whose aggregate matches
the documented L3 baseline:

| Ordinal | HTTP repeat identical | Exact L3 JSONL/hash | L3 with private revision set to null |
|---:|:---:|:---:|:---:|
| 0 | Yes (N=1 and N=16) | No | Yes |
| 1 | Yes | No | Yes |
| 2 | Yes | No | Yes |
| 3 | Yes | No | Yes |
| 4 | Yes | No | Yes |
| 5 | Yes | No | Yes |
| 6 | Yes | No | Yes |
| 7 | Yes | No | Yes |
| 8 | Yes | No | Yes |
| 9 | Yes | No | Yes |
| 10 | Yes | No | Yes |
| 11 | Yes | No | Yes |
| 12 | Yes | No | Yes |
| 13 | Yes | No | Yes |
| 14 | Yes | No | Yes |
| 15 | Yes | No | Yes |

The exact L3-hash criterion is **not met**: every HTTP row lacks the private
committed revision, by v2 contract. No other field differs. This passes
public-decision repeat/concurrency parity, not full T1 or L5 state verification.
No T2 100-campaign workload was measured. The earlier measurements preceded
final response-shape validation and cliff accounting refinements; the same
unpaced selection loop and identity encoding were used in all four runs. A
post-change in-process N=1 Postgres run also produced the exact original L3
ordinal 0 decision JSONL.

A SQLite HTTP run also reached Turn 2 Reserve with identical N=1 HTTP
fingerprints. A separate production Host process attachment using fresh local Postgres
completed one accepted owner step (expected step-limit exit), then the Host
and database container were stopped. No HTTP errors; attached metrics correctly
identified harness-only scope.

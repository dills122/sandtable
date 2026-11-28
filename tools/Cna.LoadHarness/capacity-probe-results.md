# T2 capacity feasibility — November 28, 2026

**Verdict: RED for full-shape interactive play at the current guard.** Synthetic content admission
is possible at 1,000 elements, but campaign creation is already too slow, before opening, movement,
or combat. At 250 elements on the same topology, even a genesis-only verified load and a separate
reconstruction are orders of magnitude over the two-second guard. Do not commit to full-shape
interactive play until these costs are removed and the same measurements rerun. This is a
capacity result, not a claim about rules fidelity or source-content admission.

## Method and limits

Baseline `651de7484504b56c7a2dda6633d1df856c646644`; local macOS 26.6.2 ARM64, .NET SDK
10.0.400, Release, server GC, real in-process Orleans Host with its SQLite durable journal.
Postgres and HTTP were not measured; the local Docker socket was inaccessible in the sandbox.
The in-process route uses the same durable Host discovery/submission interfaces as the load
harness. No production settings or event formats changed. The probe is excluded from ordinary
builds; the special compilation symbol exposes the generated synthetic IDs to the existing
resolver/setup lookup only. Pack generation and repeat generation/hash check precede Host
creation. Cold replay means a fresh journal handle, **not** a cold OS page cache. Warm discovery
means repeated calls at the same retained genesis. Samples are sequential; background machine
load was not controlled. The 250 creation overlapped the final shutdown of the 1,000 run for
about 30 seconds; subsequent 250 discovery and replay were isolated from this probe's other runs.

Times use `Stopwatch` monotonic elapsed time. Report nearest-rank p50/p95 only with at least 20
observations; single calls are observations, not percentiles. Allocated GB means cumulative
process-wide managed allocations during a call, not retained memory. Peak RSS is OS-reported
`time -l` maximum resident set size for each process; .NET peak working set returned zero on this
machine. Package, JSON response, retained evidence and RSS are different byte measures. Discovery
payload is JSON serialization of the in-process return object, not HTTP wire bytes. Advertised
drafts are successful returned element/stage pairs; failed/empty draft attempts are not counted
without instrumentation in the protected discovery code. Raw JSONL is retained separately as the
session artifact; it includes completion, start and external observation rows, but no payloads.

## Synthetic shapes

Invented axial-grid adjacency, two supported terrain categories, independent root battalions,
contiguous alternating-side occupied rows separated by empty rows. Every element has one root,
one occupied cell and synthetic origin. Each generated pack validated and produced the same
hash on regeneration. Real multiple-battalion stacks cannot be admitted by v6: the validator
limits a same-side combat/HQ stack to one battalion. This exercises dense adjacent occupied
cells at that limit, not multi-battalion stacks, source classes or arrival schedules.

| Elements | Tiles | Edges | Pack bytes | Generation(s) | Gen allocations (GB) |
|---:|---:|---:|---:|---:|---:|
| 60 | 400 | 1,121 | 419,385 | 0.10–0.20 | 0.034 |
| 250 | 7,000 | 20,661 | 6,427,747 | 0.91 | 1.337 |
| 1,000 | 7,000 | 20,661 | 7,444,748 | 2.03 | 4.506 |

Pack byte count is **not** the native journal evidence cap; genesis stores a content identity and
world evidence, not a copy of this pack. The 250/1,000 comparison holds topology fixed, unlike
60/250.

## Host timings and allocations (genesis)

| Operation | 60 elements | 250 elements | 1,000 elements |
|---|---:|---:|---:|
| Creation, first | 0.706 s / 0.846 GB | 20.11 s / 32.52 GB | 174–191 s to genesis commit (poll interval) |
| Creation, 20 calls incl. first | p50 0.235 s; p95 0.310 s | n=1, no percentile | n=1, no percentile |
| Issue+redeem seats | 0.876 s / 1.32 GB | 38.96 s / 65.04 GB | not completed |
| Discovery, first (two small runs) | 1.279–2.455 s / 3.49 GB | 127.73 s / 339.60 GB | not reached |
| Discovery, warm | n=20; p50 1.976 s; p95 3.055 s / median 3.49 GB | n=1 first only | not reached |
| New journal handle + load | n=21; p50 0.331 s; p95 0.494 s / median 0.580 GB | 24.34 s / 32.52 GB | not reached |
| Reconstruct (verified) | n=21; p50 0.709 s; p95 1.130 s / median 1.167 GB | 28.97 s / 65.44 GB | not reached |
| First discovery serialized bytes / advertised drafts | 194 / 1 | 194 / 1 | not reached |
| Peak RSS (maximum independently measured 60 run) | 356,581,376 B (340 MiB) | 475,987,968 B (454 MiB) | 434,946,048 B (415 MiB) |

Full-size journal file was created at 11:56:34 Toronto time; no genesis at 11:59:28, one genesis
at 11:59:45. The combined create-and-seat operation did not return before the timebox; after a
normal stop signal was intercepted by Host, the process was forcibly stopped at 285.64 s total.
This is **not** a validator rejection. No reliable allocated-byte delta for the unreturned call.
The 250 run was stopped after one completed discovery: a 20-call series at that observed rate
would take ~43 minutes for discovery alone, before separate replay. No p95 was fabricated.
The earlier 60 run was paused to isolate shapes; the subsequent Orleans timeout is a probe
artifact and excluded. An uninterrupted separate prefix run follows below.

## Evidence sizes and guards

| Genesis retained B | 60 | 250 | 1,000 | 1x / 2 MiB |
|---|---:|---:|---:|---:|
| Canonical creation event | 38,199 | 152,581 | 604,083 | 28.8% |
| Initial snapshot | 38,298 | 152,680 | 604,182 | 28.8% |
| Initial contacts | 51,304 | 222,304 | 891,004 | 42.5% |

- **GREEN (genesis evidence only):** all native creation/snapshot/contact records below 75% of
  the 2 MiB read limit on the 1,000 shape. The 24 MiB source limit is not exercised; the 65,536 B
  native Reaction codec limit and later-action evidence at full shape remain **unmeasured**.
- **RED (latency):** small-shape discovery p95 3.055 s; medium shape single creation/load/
  reconstruction/discovery all far exceed 2 s; full-size genesis commit takes nearly three
  minutes. Medium and large p95 unknown, not claimed from single observations.
- **RED (growth, indicative):** at fixed 7,000-tile topology, 4x elements takes ~8.65–9.50x
  genesis wall time, equivalent to 2.94–3.08x per doubling (target <=2.5x). This is coarse
  interval arithmetic with one sample per large shape, not a fitted complexity class; repeat
  isolated after fixes. Pack generation grows much less steeply (2.23x / 4x elements).

## Random-legal-full prefix (small shape, uninterrupted run)

15 owner submissions accepted; 23 retained accepted events plus genesis. System drainage adds
some retained events independently of owner submissions. The process was deliberately canceled at
425 seconds, not at an engine terminal or a rule rejection. Its peak RSS was 344,981,504 B;
the 20-creation process peaked at 311,640,064 B. This run did not reach movement, Reaction or
combat. The 100+ owner-step goal on the largest shape **was not met**: 1,000 could not finish
seat issuance within its timebox; 250's first discovery alone took 128 seconds. Running 100
or more full-policy steps on those shapes is not justified before fixing the measured REDs.
A separate 60-element sample path reached positive movement and combat and is reported below.

The plot is tabulated below. Discovery is the maximum measured seat call at that prefix;
submission is the owner call that reached the row; replay follows that submission. Times are
seconds; rows are selected from all 16 measurements, not only the fastest points. At prefix
15 only one seat discovery completed before the explicit stop.

| Owner steps | Accepted events | Discovery (max s) | Submit (s) | Cold load (s) | Reconstruct (s) | Last event B | Total canonical event B | Cold load allocations (GB) |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 1.414 | — | 0.675 | 0.872 | 38,199 | 38,199 | 0.579 |
| 1 | 4 | 2.306 | 1.053 | 0.946 | 1.047 | 948 | 41,463 | 2.558 |
| 2 | 10 | 2.380 | 1.929 | 1.828 | 1.896 | 958 | 46,803 | 5.318 |
| 5 | 13 | 3.059 | 2.584 | 2.395 | 2.374 | 958 | 49,677 | 7.112 |
| 10 | 18 | 4.149 | 3.438 | 3.367 | 3.034 | 958 | 54,467 | 10.102 |
| 12 | 20 | 14.904 | 4.356 | 4.415 | 3.697 | 958 | 56,383 | 11.298 |
| 15 | 23 | 9.226 | 8.364 | 8.496 | 7.032 | 958 | 59,257 | 13.092 |

31 discovery calls across changing prefixes: p50 3.453 s, p95 18.190 s (**RED**, not a
same-state percentile). Maximum individual accepted step evidence: command 262 B, canonical
event 1,271 B, receipt 288 B, checkpoint 38,868 B, contacts 4 B. Summing genesis and accepted
records for those five fields gives 1,052,102 B, excluding database/index/opening/receipt
storage overhead. Individual records remain comfortably below the native cap at this small
shape. Event records alone total 59,257 B; post-genesis average ~916 B/accepted event.

### Extrapolation (illustrative sensitivity, not validated prediction)

Fit a Theil–Sen line to all 16 paired replay samples (median pairwise slope, median residual
intercept) vs **retained accepted-event count**, not owner choices. Robust fitting is used
because machine wall times varied sharply even as allocations increased consistently. One
owner submission can add multiple events, so the event counts below are an optimistic prefix
basis if the intended target is 600 or 1,500 owner submissions. There is no validation at
those lengths, no positive combat/Reaction sample in this series, and no full-size slope.

| Metric, 60/400 shape | Median slope per retained event | Predicted at 600 | Predicted at 1,500 |
|---|---:|---:|---:|
| Cold new handle + load | 0.242 s | 144 s | 362 s |
| Core reconstruction | 0.191 s | 115 s | 287 s |
| Cold load allocation | 0.598 GB | 358 GB | 896 GB |

The increasingly large allocations with stable resident memory are consistent with repeatedly
reconstructing and validating prefix evidence. Do not multiply these slopes by the element
ratio to claim a full-size prediction; profile and rerun after addressing genesis cost first.

## Directed movement and positive combat smoke (separate 60-element run)

The `sample` mode uses an existing visible-choice classifier, opens no more than one advertised
element per stage and is not the random-full policy. It completed in 199.93 s with eight accepted
owner submissions, three actual moves and a positive combat selection followed by a retained
native combat barrage event. Replay verified. Peak RSS 356,581,376 B. No native Reaction record
was observed; the Reaction codec cap is untested. No custom combat-source file was supplied;
this only proves the reached positive selection/system step, not a completed combat chain.

| Operation at reached phase | Elapsed s | Allocated GB | Response / evidence |
|---|---:|---:|---|
| Movement discovery, active seat | 2.719 | 6.814 | 718 B, 31 advertised drafts |
| Movement owner opening | 2.223 | 5.387 | 111,664 visible B, 1 ref |
| First accepted movement submission | 2.244 | 5.685 | 3,737 event B; 40,997 checkpoint B; 902 contact B |
| Next two accepted movement submissions | 2.500 / 2.951 | 6.588 / 7.490 | 3,738 / 3,737 event B |
| Combat selection discovery, active seat | 4.449 | 11.512 | 560 B, 30 advertised drafts |
| Combat selection opening | 11.323 | 29.046 | 111,724 visible B, 2 refs |
| Positive combat selection submission | 11.461 | 29.313 | 600 event B, then 404 B native barrage event |
| Cold load after positive combat step | 3.523 | — | verified retained load |
| Reconstruct after positive combat step | 3.163 | — | verified |

23 retained records including genesis, 67,790 canonical event bytes, 1,034,165 bytes across
five evidence fields (not SQLite file size). Maximum accepted-step field lengths: 292 command,
3,738 event, 319 receipt, 40,997 checkpoint and 902 contacts, all in bytes. Every measured
positive movement or combat submission exceeded 2 s. These are individual observations, not
p95 estimates. Medium/full shapes did not reach owner movement or combat within their timeboxes.

## Where time goes / smallest next fixes

Timed spans isolate large work before discovery: on 250, verified load consumes 24.34 s, Core
reconstruct 28.97 s, discovery 127.73 s. Creation itself consumes 20.11 s with 32.52 GB
managed allocations; generation took only 0.91 s. Discovery creates a 194-byte result with
339.60 GB allocated; UI-only pagination cannot fix this. macOS native stack sample during
full-size creation showed continued CPU activity (physical footprint peak 392.5 MB at sample),
but unsymbolized managed frames; it is not line-level managed attribution. The following are
source inspection hypotheses to validate by adding fine-grained spans, not measured attribution:

1. `CampaignCreationExecution.Execute` validates content, creates the initial world, constructs
   content context and serializes/validates snapshot. `CampaignContentContext.Create` requires
   valid v6 content and rebuilds the legacy artifact. `ContentPackV6Validator.Validate` scans
   formations against all elements and each element against every tile's terrain. First profile
   genesis and load separately; cache immutable validated content/indices by exact pack hash and
   validate unique supported terrain/mobility pairs, maintaining identical canonical bytes.
2. `CampaignDurableAuthorityJournal.QueryOpenForSeat` loads/validates the journal, previews forced
   system steps, then invokes opening queries for each candidate stage/element. Add counters for
   attempted/successful drafts, replay passes and per-stage elapsed/allocations once that file's
   owner is available. Derive only requested legal actions and index adjacency/occupancy with
   stable ordering and unchanged seat visibility. Page **after** eliminating eager full derivation.
3. Track retained prefix and exact evidence by kind before proposing a verified snapshot or working
   set optimization. Preserve reconstruction verification and cap checks; never raise an evidence
   limit merely to admit a shape. Keep source projection/fidelity and UX scale decisions independent
   until a positive, measured end-to-end full-shape slice exists.

No changes to protected Core/Host files were made. No hot-path fixes were implemented here.

## Join spike S0: cheap source-table aggregate counts only

The checked-in map tables contain 7,073 hex entries and 4,365 feature-bearing hexside rows
(not every adjacency). Counts only, grouped by existing category vocabulary; no coordinates,
counter IDs, ratings or source values copied. Multi-feature rows mean feature counts can exceed
hexside row count. The most complete source authority test requires many staged activations;
placed/unplaced elements, elements by side/class, occupied/adjacent hexes and arrivals were not
cheaply computed here. Do not infer their values from synthetic shapes or sum overlapping packs.

| Terrain category | Count | Hexside feature category | Count |
|---|---:|---|---:|
| clear | 4,192 | track | 1,808 |
| desert | 677 | slope | 744 |
| rough | 655 | wadi | 703 |
| gravel | 463 | road | 355 |
| sea | 346 | escarpment | 216 |
| delta | 325 | minor-river | 205 |
| salt-marsh | 266 | ridge | 201 |
| mountain | 107 | railroad | 183 |
| swamp | 25 | major-river | 86 |
| major-city | 11 | unfinished-road | 85 |
| heavy-vegetation | 6 | unfinished-railroad | 55 |
| — | — | coast | 21 |

## Verification

- `dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release -p:DefineConstants=CAPACITY_PROBE --no-restore` — final opt-in build succeeded, zero warnings/errors.
- Final measurement-only smoke (`--capacity-probe 60 0 <new-jsonl-file> 0 1`) — exit 0, deterministic hash, verified reconstruction. Host emitted a watchdog warning on the busy local machine; no failed measurement.
- `dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release --no-restore` — ordinary harness build succeeded, zero warnings/errors; ordinary `--help` succeeded.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-restore --filter-class '*SyntheticContentCatalogTests*'` — 8 passed, 0 failed, 0 skipped (includes the 3 ordinary-build probe ID rejection cases).
- Full solution gate, Postgres/HTTP, larger-shape movement/combat, later-turn and Reaction coverage were not run locally.

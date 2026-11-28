# T9 — performance profile and cheap fixes (November 28, 2026)

## Scope and reproduction

Baseline: T2 probe head `f105f748b89e643d17239224157fdf8b81bb3409` (#681), with only
instrumentation. ARM64 macOS, .NET 10.0.400 SDK / 10.0.11 runtime, Release, server GC,
SQLite and in-process Host. All packs are generated synthetic content; see
[T2 measurements](../../tools/Cna.LoadHarness/capacity-probe-results.md) for the shape
construction and evidence limits. No authoritative byte formats, validators' acceptance criteria,
HTTP route gates, or protected exercise/session files are changed by the instrumentation.

```sh
dotnet build tools/Cna.LoadHarness/Cna.LoadHarness.csproj -c Release -p:DefineConstants=CAPACITY_PROBE --no-restore
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll --capacity-probe 60 5 /tmp/t9-before-60.jsonl 20 1
dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll --capacity-probe 250 0 /tmp/t9-before-250.jsonl 1 1
dotnet-trace collect --profile dotnet-sampled-thread-time,gc-verbose --providers Sandtable-Performance:0xFFFFFFFFFFFFFFFF:4 --output /tmp/t9-before-60.nettrace -- dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll --capacity-probe 60 1 /tmp/t9-trace-60.jsonl 1 1
dotnet-trace collect --profile dotnet-sampled-thread-time,gc-verbose --providers Sandtable-Performance:0xFFFFFFFFFFFFFFFF:4 --output /tmp/t9-before-250.nettrace -- dotnet artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll --capacity-probe 250 0 /tmp/t9-trace-250.jsonl 0 1
```

JSONL paths must not already exist. Allocations are decimal GB of cumulative process-wide
managed allocations; CPU seconds are differences of process `TotalProcessorTime` (all threads,
including GC). Neither is resident memory. Stopwatch elapsed time is secondary because of concurrent
machine load. No trace listener is enabled during timing runs. Each synchronous diagnostic span
reports inclusive elapsed Stopwatch ticks and executing-thread allocations; nested values must not
be summed to estimate process totals. Fixed operation/stage labels only, no state identifiers or
credentials. `Journal.DiscoveryDraft` counts every attempted draft, including exceptions. The
reconstruct scope is at the harness call site because T3 owns the Core exercise implementation.

The opt-in probe emits SHA-256 of retained canonical fields, reconstructed stream/snapshot hashes,
and exact serialized discovery bytes (including ordering), not payloads. Comparing equivalent
seeded paths before and after fixes tests byte identity independently of elapsed time.

## Baseline timings (listener disabled)

Per call: allocated GB / process CPU seconds. Same-genesis repeated rows use medians. One or two
observations are not a percentile.

| Operation | 60 elements / 400 hexes | 250 elements / 7,000 hexes |
|---|---:|---:|
| Creation | 0.846 / 1.164 | 32.518 / 33.851 |
| Seat issue + redeem | 1.525 / 2.792 | 65.044 / 60.502 |
| First discovery | 3.485 / 3.015 | 339.603 / 232.152 |
| Warm discovery | 3.490 / 2.403 (n=20) | 339.592 / 227.696 |
| Cold open + load | 0.581 / 0.440 (n=21) | 32.522 / 29.104 (n=2) |
| Reconstruct | 1.168 / 0.858 (n=21) | 65.439 / 49.924 (n=2) |

60-shape warm discovery elapsed p50 1.539 s, p95 1.625 s (n=20); cold load p95 0.351 s,
reconstruct p95 0.634 s (n=21). T2 observed a discovery p95 of 3.055 s on the same shape;
this illustrates the instability of wall time alone. Medium creation elapsed 45.162 s (n=1); first discovery elapsed 173.800 s. Its
194-byte reply has the same SHA-256 as the 60-shape reply (`1843cd71032d…`); warm
medium discovery elapsed 155.169 s and had the identical digest. Both medium replay samples
verified identical stream and snapshot hashes.

Five owner submissions on 60 produced prefix samples at 0, 4, 10, 11, 12 and 13 retained
post-genesis events, all verified. Pairwise median slopes using the first load at each prefix:

| Metric | GB / retained event | CPU seconds / retained event |
|---|---:|---:|
| Cold load | 0.503 | 0.371 |
| Reconstruct | 0.430 | 0.310 |

These slopes are not a fit to owner-choice count. Medium-prefix slope is not yet measured.

## Managed allocation and thread-stack profile

`dotnet-trace` 10.0.750501, EventPipe with managed method rundown; TraceEvent 3.1.23 used to
resolve allocation-tick and sample stacks (convert to TraceLog, enumerate `GCAllocationTick`, sum
`AllocationAmount64` by leaf method, select the largest weighted full stack as representative).
Sampled allocation weights estimate bytes; they are not exact GC allocation totals. Thread-time
samples (~100 Hz) on macOS include blocked/GC-suspended threads, not kernel on-CPU samples.
For thread hotspot ranking, restrict to stacks containing `Cna.Core` or `Cna.OrleansHost`, group
by leaf method and count samples. Keep GC safe-point samples visible rather than mislabeling them
as CPU instructions. Process CPU deltas in the separate timing runs are the CPU-time metric.

### 60 elements, one driven submission, two genesis discoveries

Trace allocation sample total 34.298 GB from 305,398 ticks. Five largest allocation leaves:

| Rank | Sampled GB | Leaf and representative caller chain (leaf first) |
|---:|---:|---|
| 1 | 17.252 | `LookupTerrain` ← `Cna1979ContentCompatibilityValidator.Validate` ← `Cna1979ContentV5CompatibilityValidator.Validate` ← `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` ← `CampaignSnapshotV11Validator.GetDiagnostic` |
| 2 | 7.632 | `SZArrayHelper.GetEnumerator` ← `LookupTerrain` ← `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` ← `CampaignSnapshotV11Validator.GetDiagnostic` |
| 3 | 1.213 | `Utf8JsonWriter.Grow` ← `CampaignObservationSerializer.WriteEdges` ← `CampaignContactOwnerVisibleCutV1.Serialize` ← owner-opening registry ← `QueryDiscoveryDraft` |
| 4 | 1.010 | `ContentPackValidator.ValidateOrigins` ← `ContentPackValidator.Validate` ← `ContentPackV5Validator.ValidateCore` ← `ContentPackV6Validator.Validate` |
| 5 | 0.860 | `ContentPackValidator.ValidateTopology` ← `ContentPackValidator.Validate` ← `ContentPackV5Validator.ValidateCore` ← `ContentPackV6Validator.Validate` |

13,371 domain-stack thread samples (234,667 total including idle/background threads):

| Rank | Samples | Leaf and representative caller chain |
|---:|---:|---|
| 1 | 5,605 | `LookupTerrain` ← `Cna1979ContentCompatibilityValidator.Validate` ← `Cna1979ContentV5CompatibilityValidator.Validate` ← `ContentPackV6Validator.Validate` |
| 2 | 4,447 | `Thread.PollGCWorker` ← `Buffer.BulkMoveWithWriteBarrier` ← `ImmutableDictionary.Values.MoveNext` (GC safe-point, not active CPU attribution) |
| 3 | 497 | `Cna1979ContentCompatibilityValidator.Validate` ← `ContentPackSerializer.SerializeCanonical` ← `ContentPackArtifact.Create` ← `CampaignWorldV6Validator.IsValidCore` |
| 4 | 313 | `ContentPackV6Validator.Validate` ← `ContentPackV6Serializer.SerializeCanonical` ← `ContentPackV6Artifact.Create` |
| 5 | 230 | `ContentPackValidator.ValidateOrigins` ← `ContentPackValidator.Validate` ← `ContentPackV5Validator.ValidateCore` ← `ContentPackV6Validator.Validate` |

Representative inclusive scope totals (over the entire traced run, not disjoint):

| Scope | Calls | Thread allocation GB |
|---|---:|---:|
| `ContentPackV6Validator.Validate` | 1,978 | 23.232 |
| `Journal.Load` | 14 | 15.694 |
| `Journal.QueryOpenForSeat` | 4 | 13.902 |
| `Journal.DiscoveryDraft` | 1,344 | 5.707 |
| `CampaignExercises.Reconstruct` (harness) | 3 | 5.213 |
| `Journal.PreviewDrainSystemSteps` | 4 | 4.496 |
| `Journal.ReadVerified` | 3 | 3.714 |
| `CampaignCreationExecution.Execute` | 15 | 3.285 |
| `CampaignContentContext.CreateV6` | 45 | 0.931 |

Movement and selection stages alone allocate 1.583 and 1.499 GB, respectively, over four
query calls—even though the observed genesis answer is a single initiative-order draft, and
only 194 serialized bytes. Thus optimizing only the outward JSON is insufficient.

### 250 elements, one genesis discovery

537.711 GB sampled across 4,947,589 allocation ticks. The trace (including setup, replay,
and one discovery) completed and its discovery and reconstruction fingerprints matched the
untraced baseline. Traced discovery took 308.204 s elapsed / 381.645 s CPU and allocated
339.607 GB; do not use the traced time as the performance baseline.

| Rank | Sampled GB | Leaf and representative caller chain (leaf first) |
|---:|---:|---|
| 1 | 359.447 | `LookupTerrain` ← `Cna1979ContentCompatibilityValidator.Validate` ← `Cna1979ContentV5CompatibilityValidator.Validate` ← `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` |
| 2 | 142.193 | `SZArrayHelper.GetEnumerator` ← `LookupTerrain` ← `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` ← `CampaignSnapshotV11Validator.GetDiagnostic` |
| 3 | 4.615 | `Utf8JsonWriter.Grow` ← `CampaignObservationSerializer.WriteEdges` ← `CampaignContactOwnerVisibleCutV1.Serialize` ← owner-opening registry ← `QueryDiscoveryDraft` |
| 4 | 4.551 | `ContentPackValidator.ValidateOrigins` ← `ContentPackValidator.Validate` ← `ContentPackV5Validator.ValidateCore` ← `ContentPackV6Validator.Validate` |
| 5 | 2.676 | `ContentPackValidator.ValidateTopology` ← `ContentPackValidator.Validate` ← `ContentPackV5Validator.ValidateCore` ← `ContentPackV6Validator.Validate` |

220,804 domain-stack thread samples (3,100,252 total including idle/background threads):

| Rank | Samples | Leaf and representative caller chain |
|---:|---:|---|
| 1 | 151,561 | `LookupTerrain` ← `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` ← `CampaignSnapshotV11Validator.GetDiagnostic` ← `CampaignContactOwnerMovementCut.Project` |
| 2 | 28,188 | `Thread.PollGCWorker` ← `Buffer.BulkMoveWithWriteBarrier` ← `ImmutableDictionary.Values.MoveNext` ← `Enumerable.TryGetSingle` ← `CampaignContactAdjacencySource.Derive` (GC safe-point) |
| 3 | 9,064 | `Cna1979ContentCompatibilityValidator.Validate` ← `Cna1979ContentV5CompatibilityValidator.Validate` ← `ContentPackV6Validator.Validate` |
| 4 | 6,639 | `SpanHelpers.SequenceEqual` ← `CampaignElementMovedV3Factory.FindEdge` predicate ← `CampaignContactAdjacencySource.Derive` predicate ← `Enumerable.Any` ← `CampaignContactAdjacencySource.Derive` |
| 5 | 5,024 | `ContentPackV6Validator.Validate` ← `CampaignWorldV6Validator.IsValidCore` ← `CampaignSnapshotV11Validator.GetDiagnostic` ← `CampaignContactOwnerMovementCut.Project` |

| Scope | Calls | Thread allocation GB |
|---|---:|---:|
| `ContentPackV6Validator.Validate` | 599 | 436.794 |
| `Journal.QueryOpenForSeat` | 1 | 339.597 |
| `Journal.DiscoveryDraft` | 1,286 | 223.636 |
| `Journal.Load` | 4 | 130.085 |
| `movement` stage | 1 | 93.825 |
| `combat-selection` stage | 1 | 92.274 |
| `Journal.PreviewDrainSystemSteps` | 1 | 83.439 |
| `CampaignExercises.Reconstruct` (harness) | 1 | 65.438 |
| `CampaignCreationExecution.Execute` | 5 | 60.396 |
| `Journal.ReadVerified` | 1 | 32.521 |
| `CampaignContentContext.CreateV6` | 5 | 5.720 |

Repeated static-content validations on a single genesis call sequence are not justified by changing
state: the same immutable pack is reused. Terrain lookup and its enumerator account for
501.640 GB (93.3% of sampled allocation) on medium. Adjacency scanning and visible cut
serialization are independently measured residuals to revisit after eliminating repeated content
work. Spans above overlap; they are not additive.

### Current decision

First target repeated validation of immutable content and rebuilding its legacy artifact. The
small trace directly identifies both, with terrain lookup and its enumerator accounting for
24.884 GB of sampled allocation (72.6%). World and snapshot validation must continue to run;
only immutable content facts should be reused. Do not change the protected snapshot validator.

The 250 trace confirms the same root cause at much larger scale. Post-fix measurements will
be added in subsequent PRs. No performance fix is included in this phase.

## Verification of instrumentation

- Opt-in Release harness build passed (zero warnings/errors); both traced and untraced probe
  runs exited 0, with verified reconstruction.
- Focused `CampaignPerformanceSpanTests`: 1 passed, 0 failed. Event payload includes only
  diagnostic name, ticks and thread allocations.
- `FrozenTurnBoundaryIdentityTests`: 1 passed, 0 failed; existing setup and event stream pins unchanged.
- No full solution or Postgres gate run locally. The 250 prefix slope and 1,000 genesis follow-up
  are deferred until the first cheap fix makes those probes reachable.

## Status before fixes

- **RED overall latency:** medium creation alone exceeds 2 s; medium p95 not measured. Small
  same-genesis discovery p95 was below 2 s in this particular run.
- **RED growth:** T2 fixed-topology 250→1,000 genesis was ~2.94–3.08x wall time per doubling;
  60→250 has different topology and cannot isolate element scaling.
- **RED prefix:** measured nonzero replay slopes; no evidence of bounded incremental verification.

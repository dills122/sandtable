# Codec canonicality cost during verified replay (#326)

**Status:** findings-only spike, 2026-11-24; base `8cc81446`. No codec, journal, hash,
trust-boundary or canonical-byte changes. This is evidence for a future design decision, not that
decision. Follows the history-growth workload in #451 (`CampaignCapacityBenchmarkTests`).

## 1. Results

Measured on an Apple M1 Max (8 performance + 2 efficiency cores, 64 GB RAM), macOS 26.6.2 arm64,
.NET SDK 10.0.400 / runtime 10.0.11, Release, default workstation GC. Other local workers were
active, so elapsed times are indicative rather than isolated-machine latency guarantees. MB below
means 1,000,000 bytes.

| Accepted journal steps | Mean verified read (ms) | Managed MB/read | Load thread samples | Decoder inclusive, sampled time | Re-encode subset, sampled time | Decoder inclusive, sampled allocation | Re-encode subset, sampled allocation |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 48.9 | 21.17 | 614 | 42 (6.8%) | 15 (2.4%) | 7.0% | 2.3% |
| 4 | 70.1 | 50.34 | 599 | 55 (9.2%) | 15 (2.5%) | 11.7% | 4.0% |
| 8 | 58.7 | 88.67 | 735 | 89 (12.1%) | 16 (2.2%) | 13.5% | 4.6% |
| 12 | 95.2 | 137.48 | 1,067 | 141 (13.2%) | 38 (3.6%) | 13.0% | 4.5% |

**Decoder inclusive** covers JSON parse, typed construction, validation, re-serialization and byte
comparison together. This is the measured share of full canonical read work, not a claim that all
that work can be removed. **Re-encode subset** counts samples with the matching serializer on the
stack below a decoder; this includes validation done by that serializer, excludes other replay-time
encodes, and is a conservative attribution of the re-encode cost. The byte comparison is often
inlined; the trace cannot separately quantify it. Do not add the two columns.

The allocation trace attributed approximately 1.51, 5.99, 12.12 and 18.21 MB/read to inclusive
canonical decode, respectively, of which 0.50, 2.08, 4.12 and 6.32 MB/read were visibly inside
re-encoding. `CampaignSnapshotV11Serializer.Deserialize` accounted for 317 of 327 inclusive
thread samples and ~99% of inclusive allocation weight (including nested decoders).

Canonical decode is material but not the dominant verified-replay expense in this workload.
Removing only the visible repeat encode would address a much smaller fraction than eliminating all
canonical decoding. Other work in `Load`, including reconstruction and journal verification,
continues regardless. The data do not support a blanket trust-boundary change across ~119 codecs.

## 2. Method and reproduction

`CampaignCodecReplayBenchmarkTests.cs` mirrors the existing capacity benchmark's truck lab history:
real accepted commands appended through the journal, not repeated synthetic payloads. At prefixes
1, 4, 8 and 12, reopen the SQLite journal fixture, warm one verified read, then time 20 synchronous
`ReadVerified` calls on one thread and record `GC.GetAllocatedBytesForCurrentThread` delta. Each
prefix separately verifies Core reconstruction of the warm and final reads and compares their
reconstructed snapshot bytes. It exercises the same `Load` body as the Marten working set; no
Postgres fetch/flush, Orleans, or concurrent callers are timed. Total per-read timing includes
connection/transaction setup, whereas the sampled denominator is specifically `Load`.

An initial exploratory run showed the 12th `Append` result's session version (13) differed from the
reopened verified journal (17). The timed witness is therefore the independently verified reload,
not the append result. Repeated reloads reconstruct identical bytes. No diagnosis or change to that
pre-existing behavior is part of #326.

```sh
SANDTABLE_CODEC_REPLAY_BENCHMARK=1 SANDTABLE_BENCHMARK_OUT=/tmp/codec-replay.jsonl \
  dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj \
    -c Release --filter-method '*CodecReplayBenchmarkHistoryGrowth*'
```

For attribution, use dotnet-trace 10.0.750501 with the compiled test runner as its child (no changes
to the product decoder). Run once with `PROFILE=dotnet-sampled-thread-time` and once with
`PROFILE=dotnet-sampled-thread-time,gc-verbose`, setting distinct output names. Only the thread-time
samples from the former and allocation ticks from the latter are used for the shares above.

```sh
PROFILE=dotnet-sampled-thread-time
SANDTABLE_CODEC_REPLAY_BENCHMARK=1 SANDTABLE_BENCHMARK_OUT=/tmp/codec-traced.jsonl \
  dotnet-trace collect --profile "$PROFILE" \
    --providers Sandtable-CodecReplayBenchmark:0xFFFFFFFFFFFFFFFF:4 \
    --show-child-io -o /tmp/codec-time.nettrace -- \
    dotnet artifacts/bin/Cna.OrleansHost.Tests/release/Cna.OrleansHost.Tests.dll \
      --filter-method '*CodecReplayBenchmarkHistoryGrowth*'
```

Analysis used TraceEvent (`TraceLog.CreateFromEventPipeDataFile`, then `TraceLog.Events` and
`event.CallStack()`):

1. Select events between `Sandtable-CodecReplayBenchmark` `/Start` and `/Stop` markers (payload
   is accepted step count). Retain stacks containing both `MeasureCodecReplay` and
   `CampaignDurableAuthorityJournal.Load` to exclude appends, setup, assertions and other threads.
2. Sum one per `Microsoft-DotNETCore-SampleProfiler/Thread/Sample`, or sum `AllocationAmount64` per
   `GC/AllocationTick`. Denominators include all retained stacks, not just JSON methods.
3. Count the union (not sum) of stacks containing any of these confirmed canonical read methods:
   `CampaignSnapshotV11Serializer.Deserialize`, `CampaignActionSubmissionSerializer.DeserializeCanonical`,
   `BreakdownPointAmountCodec.Deserialize`, `CapabilityPointAmountCodec.Deserialize`,
   `CampaignNativeReserveReleaseEventV27.Deserialize`, `CampaignSelectionTimeoutEvidenceV38.Deserialize`,
   `CampaignNativeCycleControlTimeoutV34.Deserialize` (observed in the exploratory combined trace).
   For the re-encode subset, require the same declaring type's `Serialize` or `SerializeCanonical`
   as a descendant (callee) of its `Deserialize` frame. Nested matches count only once.

Allocation ticks are sampled/weighted, not an exact per-method allocation ledger. The sampled
allocation denominators divided by 20 were 21.59, 51.30, 90.02 and 139.69 MB/read, within 0.1%
of that traced run's exact thread totals. The thread-time-only run measured 49.1, 52.5, 67.5 and
97.0 ms/read; the combined trace was slower (107.8, 83.5, 138.5 and 235.9 ms/read), hence the
separate profiles. Thread samples estimate elapsed stack occupancy, not hardware CPU cycles;
JIT inlining, unwinding, GC and allocation sampling can obscure attribution. These are short
accepted-command histories matching #451, not source-backed long-history/large-checkpoint evidence;
do not extrapolate percentages to the ~1024-step ceiling or to every codec. Recheck on longer
histories before any architectural change. No timing threshold enters CI; the test is opt-in.

## 3. Recommendation: preserve verification; prefer evaluating a bounded decode cache

Of the three directions in #326, a bounded, measured decode-cache investigation is the best next
candidate **if** longer-history profiling justifies it. It could address both parse and repeat
encode costs rather than only the smaller encode portion. This is not a recommendation to bypass
verification; first establish cache hit rate, retention cost and ownership/immutability. The current
numbers alone do not justify implementing any option.

| Direction | Hash-integrity / correctness risk |
|---|---|
| Verify once at append | A record hash checks retained bytes against its expected hash; it does not establish that those bytes were canonical or schema-valid. Future writers, imports, migrations and restores may not share the original validation path. Dropping read verification changes the integrity guarantee for such records. |
| Trust-boundary flag | A caller-controlled distinction between trusted and untrusted input is easy to misclassify as paths evolve. A storage read is not intrinsically proof of prior validation; a misplaced flag could admit noncanonical encoding despite matching a retained hash. |
| Decode cache | Cache entries must never substitute for hash-chain and replay verification. Incorrect identity/version binding, mutable objects, or stale entries could associate a previously validated object with different bytes or replay semantics. Retention also adds memory pressure. Establish and validate cache ownership and lifetime before considering any change. |

Keep `EVT-001`, `DET-001` and `DET-002` unchanged. In particular, no change to `SerializeCanonical`
output, verification of externally sourced bytes, or authoritative replay is proposed here. A
separate reviewed design decision is required.

# Capacity benchmark baseline (L0)

Date: 2026-11-25. Source plan: `concurrent-simulation-plan.md` §4 task L0. Raw data:
[`2026-11-25-capacity-benchmark-baseline/baseline.jsonl`](2026-11-25-capacity-benchmark-baseline/baseline.jsonl).

## How to run

```sh
just capacity-benchmark                      # all four benchmarks
scripts/capacity-benchmark.sh --only history --only parallel   # subset, any of history|parallel|stream|storm
```

The wrapper builds Release, sets `DOTNET_gcServer=1` (the test host otherwise runs Workstation GC and
under-reports throughput; the production Host gets Server GC from `Microsoft.NET.Sdk.Web`), sets
`SANDTABLE_CAPACITY_BENCHMARK=1` and `SANDTABLE_BENCHMARK_OUT`, and writes a first `{"kind":"env",...}`
line (cores, OS, dotnet, git SHA, dirty flag, shared-Postgres flag). Output defaults to
`artifacts/capacity-benchmark/<utc>.jsonl`. Needs Docker (Testcontainers) or `SANDTABLE_TEST_POSTGRES_URL`.
The benchmarks are `[Fact]`s that return immediately unless `SANDTABLE_CAPACITY_BENCHMARK=1`, so they
add no cost to normal test runs.

## Fix made in this change

`CapacityBenchmarkStreamLengthStorageCost` had bit-rotted: its raw `INSERT INTO accepted_steps SELECT ...`
omitted the `owner_side` column added later (`SQLite Error 1: table accepted_steps has 15 columns but 14
values were supplied`). Fixed by adding `owner_side` to the column list. It is the only code change.

## Environment (MEASURED, shared machine)

Apple M-series, 10 cores, Darwin 25.6, .NET 10.0.400, Server GC, Testcontainers `postgres:16-alpine` in
Docker Desktop. Machine was shared with other workers during the run; treat absolute numbers as noisy
(+/-30%). SHA `cc9c4d3d` plus the one-line fix above.

## Results

Per-call replay, Marten path (`readBody` = `ReadVerified`, ms):

| Steps | truck readBody | combat readBody | append flush |
|---:|---:|---:|---:|
| 1 | 36 | 29 | 14-47 |
| 4 | 83 | 86 | 16-17 |
| 8 | 136 | 75 | 16-18 |
| 12 | 199 | 88 | 18-24 |

The labs stop at 12 accepted steps (terminal), so the history benchmark cannot go beyond that.

Unit-of-work parallelism (12-step truck campaigns, calls/s, 3 rounds):

| Parallel | Marten UoW | Core replay only |
|---:|---|---:|
| 1 | 11.6-11.9 | 11.7 |
| 2 | 14.2-21.0 | 32.0 |
| 4 | 29.7-32.1 | 52.6 |
| 8 | 27.8-34.0 | 55.3 |
| 16 | 22.2-30.4 | 55.3 |

Saturation ~30 calls/s through Marten, ~55/s Core-only. Consistent with the plan's 30-35 calls/s.

Stream length, storage cost only (open = Marten fetch + SQLite copy; flush = Postgres transaction):

| Events | combat (7 KB/step) open / flush ms | checkpoint 961 KB/step open / flush ms |
|---:|---|---|
| 10 | 9 / 10 | 53-68 / 59-63 |
| 50 | 12-13 / 11-12 | 265-275 / 60-63 |
| 100 | 15-16 / 11-12 | 516-592 / 60-70 |
| 150 | - | 830-961 / 63-70 |
| 250 | 28-30 / 11-14 | - |
| 500 | 53-143 / 9-30 | - |
| 1000 | 91-97 / 8-9 | - |

Deadline storm (burst = all deadlines same instant; staggered = spread), hot pool as configured by the test:

| K | mode | setup ms | lateness p50 / p95 / max ms |
|---:|---|---:|---|
| 16 | burst | 10461 | 1475 / 1553 / 1553 |
| 32 | burst | 14240 | 1906 / 2010 / 2019 |
| 64 | burst | 17770 | 7608 / 7829 / 7930 |
| 128 | burst | 55725 | 13833 / 14044 / 14071 |
| 16 | staggered | 9767 | 726 / 910 / 910 |
| 32 | staggered | 16700 | 773 / 1272 / 1273 |
| 64 | staggered | 25940 | 565 / 792 / 953 |
| 128 | staggered | 54742 | 514 / 979 / 1089 |

The storm test took 6 m 32 s wall here (the plan measured ~1 m 48 s for K<=64 only; this run includes K=128).
Burst lateness at K=64 (7.6 s p50) is ~2x the plan's earlier run (4.1 s): noisy shared machine, not a
verified regression.

## Caveats

- Only a single run per benchmark; no variance estimate. Re-run on a quiet machine before gating anything.
- Per-campaign memory and Postgres server CPU are still not measured here (planned for L3 sampling).

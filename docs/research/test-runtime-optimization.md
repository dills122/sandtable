# Test runtime optimization

## Scope

Performance side quest based on `c22346d` (PR143). Preserve every existing test and assertion,
canonical rules/contract bytes, input rejection, authoritative replay and fog-of-war boundaries.

The latest completed main CI run before the change took 12m48s, including 10m27s in tests.
Core tests took 10m25s; ExerciseRunner took 5m55s concurrently. Restore took 9s, formatting 70s,
and build 52s. Source: [GitHub Actions run](https://github.com/dills122/sandtable/actions/runs/35940039678).
PR143's subsequent [verify job](https://github.com/dills122/sandtable/actions/runs/35945489592)
completed successfully in 14m08s against a 15-minute job timeout.

## Diagnosis and change

A ten-second managed sampling trace of the Release Core suite identified repeated
`Cna1979LandSequence.CreateTurn` construction as substantial work in inherited Combat replay.
Each call rebuilt the full catalog, including positions and defensively copied rule references.
A temporary benchmark measured roughly 1.6 GB allocated by 10,000 same-turn calls.

The catalog now retains only its last requested turn. The list, positions, and nested source
references are immutable. Volatile publication keeps each returned list coherent across threads;
concurrent misses can duplicate construction but never combine turns. Storage stays bounded to
one catalog even for arbitrary positive turn numbers. Invalid turns are rejected before lookup.
This cache does not contain campaign state or bypass replay authentication.

Regression tests cover allocation, cold/warm/evicted serialization for turns 1, 2, 6 and
`int.MaxValue`, caller mutation attempts, concurrent turn requests, successor rollover, overflow,
and forged positions. The allocation guard measures bytes rather than wall time and runs in an
isolated xUnit collection so other tests cannot evict its catalog during measurement.

CI retains the existing full-suite command and adds xUnit XML reports through the installed
xUnit MTP runner. The `test-results` artifact contains individual durations and remains available
for 14 days, including reports produced before a test failure. An interrupted process may not
finish its XML report. No tests are skipped, sampled, or moved off the PR gate.

## Verification

Measured on macOS arm64, SDK 10.0.400, Release configuration:

| Measurement | Before | After |
| --- | --- | --- |
| Full solution tests | 12m22.814s, 2,457 passed | 6m53.522s, 2,465 passed |
| Core test module | 12m22.154s | 6m53.295s |
| ExerciseRunner test module | 2m54.028s | 1m57.638s |
| Slowest Reaction completion mutation case | 506.432s | 271.804s |
| 10,000 warm same-turn catalog reads | 837.578ms; 1,602,027,832 allocated bytes | 6.835ms; 1,872 allocated bytes |

Observed full-suite reduction: approximately 44%. XML test-name/count reconciliation confirms
all 2,457 existing cases remain and pass, plus exactly eight new cache cases; zero failures or
skips. No test selection or parallelism setting changed. Build passed with zero warnings/errors;
21 focused sequence cases, 81 boundary cases, full format verification and workflow actionlint
all passed. The exact CI relative report directory was smoke-tested with all 10 contract tests.

The baseline included a ten-second profiler attachment and brief isolated build/format work;
the after run included a subsecond report-path smoke test. Benchmark allocation totals include
measurement formatting. These local observations are not a controlled benchmark or a promise of
an identical hosted-runner speedup. CI duration must be checked after publication.

To reproduce the suite measurement, build Release and run the unchanged full solution with XML:

```sh
dotnet build Sandtable.slnx --configuration Release --no-restore /bl:/tmp/test-runtime-build-{}.binlog
dotnet test --solution Sandtable.slnx --configuration Release --no-build --report-xunit-xml --results-directory artifacts/test-results /bl:/tmp/test-runtime-tests-{}.binlog
```

Compare complete runs on the same machine/configuration, preserving test identities and outcome
counts. XML durations overlap across parallel tests; do not sum them as elapsed suite time.

Profiler reference: [Microsoft dotnet-trace documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace).

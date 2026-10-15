# CI01 measurement and partition decision — 2026-10-15

Status: baseline complete; reversible static candidate implemented with Python TDD. Awaiting Brain selection, independent review and hosted budget. No hosted experiments or heavy local executions started. Base c24266a5d7dd12c05568c1788e472ad85a20c73e; branch `codex/ci-runtime-optimization`.

## Measurement method and limits

Read 20 most recent CI workflow runs and all job/step timestamps through read-only GitHub CLI with Keychain authentication, both token override variables removed. All attempts are 1. Runs span October 14 01:55 UTC through October 15 00:32 UTC and different code/suite/workflow versions. Thus aggregate figures describe history, not a matched performance experiment. Job queue is `created_at` to `started_at`; workflow wall is creation to last update; runner-minutes sum non-skipped job start/completion spans, without per-job billing rounding. No prices or billed usage claimed. Timestamps have one-second resolution. Negative skipped-job placeholder spans excluded. No statistically meaningful p95 claimed.

17 successes, 2 capacity/finalization cancellations, 1 discovery failure. Successful wall min/median/max: 1043/1191/1399 seconds. Runner-minutes min/median/max: 37.10/45.65/59.90. Across 146 non-skipped jobs, queue min/median/max: 2/3/118 seconds. Queue is separate from runner execution.

| Run | Head | Result | Workflow seconds | Runner minutes | Prepare seconds | B job seconds | B execution step seconds |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| [38658363876](https://github.com/dills122/sandtable/actions/runs/38658363876) | c24266a5 | success | 1384 | 57.68 | 143 | 1214 | 1114 |
| [38656131921](https://github.com/dills122/sandtable/actions/runs/38656131921) | 37b372a0 | success | 1311 | 59.90 | 108 | 1178 | 1080 |
| [38652809605](https://github.com/dills122/sandtable/actions/runs/38652809605) | 29636b9a | cancelled | 1410 | 61.62 | 182 | 1204 | 1112 |
| [38646785953](https://github.com/dills122/sandtable/actions/runs/38646785953) | 972e0c51 | cancelled | 1431 | 50.02 | 176 | 1220 | 1137 |
| [38635010687](https://github.com/dills122/sandtable/actions/runs/38635010687) | 2e18cfeb | success | 1394 | 44.15 | 169 | 1187 | 1098 |
| [38630698865](https://github.com/dills122/sandtable/actions/runs/38630698865) | 7be10e56 | success | 1379 | 45.65 | 194 | 1158 | 1064 |
| [38616471943](https://github.com/dills122/sandtable/actions/runs/38616471943) | 4521148a | success | 1268 | 46.30 | 140 | 1105 | 1015 |
| [38612754520](https://github.com/dills122/sandtable/actions/runs/38612754520) | 33120fe9 | success | 1288 | 45.77 | 162 | 1098 | 1009 |
| [38609798518](https://github.com/dills122/sandtable/actions/runs/38609798518) | 5a0dbc82 | failure | 188 | 3.23 | 166 | skipped | skipped |
| [38586167333](https://github.com/dills122/sandtable/actions/runs/38586167333) | f59ffcdb | success | 1191 | 44.18 | 174 | 995 | 898 |
| [38582156373](https://github.com/dills122/sandtable/actions/runs/38582156373) | d9db9e05 | success | 1171 | 45.77 | 156 | 981 | 889 |
| [38528562657](https://github.com/dills122/sandtable/actions/runs/38528562657) | 42503c57 | success | 1149 | 47.82 | 173 | 954 | 860 |
| [38526253465](https://github.com/dills122/sandtable/actions/runs/38526253465) | f2590be7 | success | 1043 | 44.68 | 162 | 786 | 709 |
| [38519497784](https://github.com/dills122/sandtable/actions/runs/38519497784) | b2acab89 | success | 1164 | 40.90 | 182 | 960 | 869 |
| [38517061509](https://github.com/dills122/sandtable/actions/runs/38517061509) | be149cb5 | success | 1160 | 45.73 | 177 | 958 | 868 |
| [38514134914](https://github.com/dills122/sandtable/actions/runs/38514134914) | 4191d5ef | success | 1174 | 45.67 | 183 | 966 | 875 |
| [38512285211](https://github.com/dills122/sandtable/actions/runs/38512285211) | 0973e404 | success | 1113 | 43.18 | 110 | 982 | 887 |
| [38506203076](https://github.com/dills122/sandtable/actions/runs/38506203076) | ed443bdf | success | 1399 | 37.15 | 174 | 1197 | 1109 |
| [38503950210](https://github.com/dills122/sandtable/actions/runs/38503950210) | 3491be42 | success | 1379 | 41.40 | 177 | 1176 | 1081 |
| [38498332221](https://github.com/dills122/sandtable/actions/runs/38498332221) | a2a21cdf | success | 1106 | 37.10 | 99 | 978 | 889 |

## Step measurements

Successful sample: prepare restore n17 min/median/max7/9/11s; format36/68/82s; build30/58/66s; row discovery6/11/13s; upload1/2/2s. Test jobs n84: restore6/9/18s; build31/60/67s; proof download0/1/3s; result upload1/1/2s. Aggregate download n17:0/2/4s; strict verification0/0/1s. Setup checkout/.NET and finalization are retained in job duration, not assigned to test execution.

Current-suite main run38658363876: prepare143s; test jobs A647/B1214/C410/D665/Exercise262/Contracts103s; verify17s. Core B test step1114s, restore9s, build65s. Main workflow1384s and57.68 runner-min. Previous PR38656131921: wall1311s and59.90 runner-min; prepare108s/B1178s/B execution1080s. These are two observations of current suite, not same-SHA repetitions.

## Failure ownership

- 38652809605: B cancelled at1204s; test and upload steps succeeded but job post-step did not. Required aggregate correctly failed on dependency outcome. Capacity/finalization, not demonstrated assertion variation. Owner: CI01/Brain; retain deadline headroom decision already made in base.
- 38646785953: B cancelled at1220s during test step; aggregate failed. Capacity; no final successful process claim. Owner: CI01/Brain.
- 38609798518: prepare row discovery failed `Empty/duplicate discovery`; tests skipped and aggregate failed. Discovery/report contract failure, not assertion flake. Owner: CI01; historical follow-up passed on new head, which alone does not establish flakiness.
- No controlled same-SHA intermittent assertion failure established. Diagnostic repeats must retain first failure and never replace required first-attempt proof. No retry/quarantine/skip introduced.

## Collection measurements and candidate

Locally read only XML, inventory and process-status members of aggregate artifacts11938342987 (main38658363876) and11937661511 (PR38656131921); discard binary/diagnostic members. No raw diagnostic export. Inventory SDK is10.0.402 in both reports; workflow uses ubuntu-24.04 and Release. Runtime reports identify .NET10.0.13 x64, xUnit4.0.2, collection-per-class, four collection threads. Exercise reports nonparallel collections. Core source contains one explicitly nonparallel `LandSequenceCacheTests` collection; proposed moved classes use default per-class collections and no shared collection declarations.

Collection elapsed is serial within that collection. Collections overlap, so their sum is **not** job wall time. Main/PR Core totals: A1233.75/1585.72, B3956.62/3764.57, C1074.90/1623.83, D600.13/489.68 collection-seconds. Assembly wall respectively A566.377/692.895, B1111.984/1077.886, C336.620/503.979, D577.883/469.628 seconds.

Recommended static complete-class moves, retaining six jobs and existing A policy:

| Full class suffix under Cna.Core.Tests.Campaigns | Rows | Main seconds | PR seconds | From | To |
| --- | ---: | ---: | ---: | --- | --- |
| CombatActualMovedContinuationContractTests | 66 | 479.780 | 459.702 | B | C |
| CombatActualRepeatedMovementContractTests | 12 | 181.151 | 169.926 | B | C |
| CombatActualPaidAssaultContractTests | 25 | 180.382 | 161.636 | B | C |
| CombatActualMovedAssaultContractTests | 74 | 283.380 | 263.314 | B | D |
| CombatActualPostAssaultContinuationRetryTests | 8 | 219.230 | 192.921 | B | D |

Predicted row counts A71/B2326/C233/D211/Exercise469/Contracts10, union3320. Main residual B collection sum2612.70s and longest collection762.672s; C sum1916.21s and longest479.780s; D1102.74s and longest577.664s. These are lower-bound/model inputs, not a promised schedule. Scheduler order, CPU contention and runner variation can change all durations. New/unmeasured classes remain in B; future methods in assigned classes follow their complete class. No class-name wildcard.

Reject consolidating existing RoundEntry A/B split: class combined serial elapsed1328.907/1509.430s exceeds current B wall. Preserve its proven eight transferred argument rows and retained-row predicate. A method+class include filters intersect, so route additional classes to C/D rather than adding ambiguous union logic to A.

Defer build-once: latest B restore+build74s is5.35% of workflow wall before artifact pack/download/unpack, well below20% target. It could reduce compute but adds provenance/runtime-closure complexity; no implementation justified in this bounded first candidate.

## Acceptance, gates and next steps

Manifest: `.github/scripts/test_partition.py`, `.github/scripts/test_partition_test.py`, this report and separate CI review packets. Workflow only if needed; no domain tests/fixtures or shared README/design/roadmap writes.

Preserve exhaustive/disjoint actual argument-bearing rows, fresh runner discovery versus predicate, exact SHA/attempt/SDK/Release, process exit zero, exact XML membership, report counters, always-upload and aggregate dependency success. No timeout/concurrency increase, selective required coverage, retries or service purchases.

Proposed experiment: at most three candidate hosted full runs plus required final/main gates only after Brain agrees budget. Target at least20% wall reduction in two comparable completed runs with at most10% runner-minute increase; report queue and all failed attempts. Current historical current-suite comparisons are provisional only. Fresh independent review and Brain merge remain mandatory. Heavy local build/test requires Brain lease; Python regression checks allowed.

Graph coverage limitation: indexed main graph generation2026-10-05 predates helper files; coverage reported helper paths missing. Used focused source reads for owned CI files; no graph completeness claim.

## Author self-review and executed checks

Changes are six lines in production helper: one provenance comment and five exact class entries. Existing class predicate and class filters consume the same constants, including fresh runner filter discovery and prepared partition reconstruction. No workflow change. Existing historical fixture expectations expand to match the complete C/D class policies; a fixed three-argument D command slice becomes `len(classes)+1`.

TDD: `python3 .github/scripts/test_partition_test.py MeasuredClassBalanceTests` before implementation: four tests ran, three failed (11 failing assertions/subtests), demonstrating missing moves, missing exact filters, and absence of selected-class guard. Unknown/lookalike fallback passed. First full candidate check:137 tests, one failure from obsolete fixed D command slice. Updated that assertion to exact expanded class list. Final command `python3 .github/scripts/test_partition_test.py`:137 pass. These are helper checks, not .NET execution proof.

Independent lightweight replay against inventory from run38658363876: candidate A71/B2326/C233/D211; exact Core union preserved and reverse input gives identical partition. Existing aggregate validator accepts original completed inventory/process/XML evidence; replacing only inventory with candidate layout rejects original XML as missing/unexpected rows. Thus prior successful reports cannot satisfy the new row layout.

Five-axis author review: exact-name assignment and complementary filters consistent; unknown classes route B; selected missing classes fail; A transfer policy untouched. Implementation reuses existing static policy and adds no dependencies, credentials, resource parallelism or cache state. Class totals are model inputs only; scheduler order/CPU contention remain unmeasured candidate risks. Entire solution restore/build, actual candidate runner discovery/execution, speed target and fresh independent review remain unexecuted. No merge readiness claim.

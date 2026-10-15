# CI01 final investigation — 2026-10-15

**Decision: measured rejection.** Brain rejected performance acceptance of the five-class mapping; do not merge PR191, launch a third unchanged-head run, or relax thresholds. Both candidate observations passed exhaustive coverage. Only one met both performance limits.

[PR191](https://github.com/dills122/sandtable/pull/191) remains open and unmerged on `codex/ci-runtime-optimization` at214d33b70f90b5a2686bc8078d147219846aa1e3 (implementation f586b82eed41ae7946783da5faf27223b31fa1cf; base c24266a5d7dd12c05568c1788e472ad85a20c73e). This report is preserved separately from the rejected mapping. The original report checkpoint is `cc81810c47250be8ab25716cf01d08867f31b2b1` on `codex/ci-runtime-investigation-handoff`; its parent contains the experimental mapping and must not be merged to retain this evidence.

## Scope and baseline

Candidate moved five complete classes from B to existing C/D using the existing exact class filters. A, six jobs, deadlines, concurrency, actual-row discovery and all provenance/process/XML gates stayed unchanged. No gameplay, fixture, workflow, cache, retry/quarantine/skip or selective-required-coverage change.

Read20 recent first-attempt CI runs spanning October14 01:55UTC through October15 00:32UTC:17successes,2capacity/finalization cancellations,1discovery defect. Historical successful wall min/median/max1043/1191/1399s; CI runner-min37.10/45.65/59.90. These runs include different suite/workflow versions and are context, not a matched performance distribution; no meaningful p95 inferred. Job queue metadata across146non-skipped jobs min/median/max2/3/118s.

Two current-suite controls and two candidate observations used the same3320actual row identities, SDK10.0.402, Release, .NET10.0.13x64 and xUnit4.0.2. Core/Contracts report four collection threads; Exercise nonparallel. Available prepare and critical B metadata agree on runner2.337.0, Ubuntu24.04 image20261012.334.1, provisioner20261012.601. Machine contention remains uncontrolled.

| Observation | Workflow / attempt | Source head | Tested event SHA |
| --- | --- | --- | --- |
| Primary main control | [38658363876](https://github.com/dills122/sandtable/actions/runs/38658363876),1 | c24266a5d7dd12c05568c1788e472ad85a20c73e | c24266a5d7dd12c05568c1788e472ad85a20c73e |
| Secondary PR control | [38656131921](https://github.com/dills122/sandtable/actions/runs/38656131921),1 | 37b372a07f743409e2efae6a44635b834b043a2c | 3509712155065622358eb144b2b674aac13884ae |
| Candidate1 | [38774737450/attempt1](https://github.com/dills122/sandtable/actions/runs/38774737450/attempts/1) | 214d33b70f90b5a2686bc8078d147219846aa1e3 | 0169c15c795b89baf859c79c1ff9630666170f0c |
| Candidate2 | [38774737450/attempt2](https://github.com/dills122/sandtable/actions/runs/38774737450/attempts/2) | 214d33b70f90b5a2686bc8078d147219846aa1e3 | 0169c15c795b89baf859c79c1ff9630666170f0c |

All2523tracked entries outside the exact five approved CI helper/test/report paths match both control source heads, candidate head and fetched candidate merge (canonical NUL-delimited tree-entry SHA2568e91d5d3057dbe0a56fe75d215a12ecdc9bccb35d68458a5d42df4df04658cd0). This excludes a content change as an explanation; it is not same-machine randomized A/B evidence.

## Coverage and performance evidence

Candidate counts: A71/B2326/C233/D211/Exercise469/Contracts10, total3320. Both fresh attempts independently passed prepared-partition reconstruction and unchanged strict process/status/XML verification: correct tested SHA, attempt, SDK, Release, all six exit-zero processes, complete unique argument-bearing rows, no failures/skips and successful dependencies. Complete identity union matches controls; canonical row JSON SHA256d9cb0e19215a32ac8e4f5d1479ecde7ebc7b24f0c2f181b0c856f306a4344026. Old reports reject the new row layout.

Prepared inventory artifacts11993007609/11994915924 and aggregate artifacts11994626482/11994858966 are attempt1/2 respectively. CodeQL, dependency review and observational links passed. Raw reports were read locally; no raw diagnostics or binary archives published.

Frozen performance acceptance: at least20%comparable whole-workflow execution reduction in two observations, with at most10%CI runner-minute increase. Primary control was selected before results; secondary remains prominent.

| Metric | Primary main | Secondary PR | Candidate1 | Candidate2 |
| --- | ---: | ---: | ---: | ---: |
| Whole workflow seconds | 1384 | 1311 | 1086 | 1156 |
| First job start→last finish seconds | 1380 | 1308 | 1081 | 1148 |
| Prepare+slowest leaf+verify seconds | 1374 | 1303 | 1075 | 1140 |
| CI runner-minutes | 57.6833 | 59.9000 | 62.7333 | 64.4500 |
| Wall reduction vs primary | — | — | 21.53% | 16.47% |
| Runner increase vs primary | — | — | 8.75% | 11.73% |
| Wall reduction vs secondary | — | — | 17.16% | 11.82% |

Candidate1 met both limits for one observation. Candidate2 missed both. Two-observation mean wall1121s is19.00%below primary and14.49%below secondary; mean runner63.5917min is10.24%above primary. Means do not replace frozen acceptance. No universal20%improvement, percentile or zero-flakiness claim.

Attempt2 ran17:28:21→17:47:37UTC; use attempt `run_started_at`, not original run creation17:07:47. Initial queue/handoff/final-update gaps were5/6/0s for candidate1 and7/8/1s for candidate2. Individual job queue spans2–4s and3–4s. Runner-minutes sum actual non-skipped CI job spans without billing rounding; companion workflows and currency costs are excluded.

## Why the target failed

| Job execution seconds | Primary | Secondary | Candidate1 | Candidate2 |
| --- | ---: | ---: | ---: | ---: |
| Prepare | 143 | 108 | 176 | 184 |
| Core A | 647 | 793 | 796 | 787 |
| Core B | 1214 | 1178 | 882 | 940 |
| Core C | 410 | 608 | 770 | 831 |
| Core D | 665 | 547 | 792 | 739 |
| Exercise | 262 | 232 | 249 | 251 |
| Contracts | 103 | 100 | 70 | 104 |
| Verify | 17 | 17 | 17 | 16 |

Versus primary, extra303/406runner-seconds comprise252/317s more aggregate test execution and51/89s more non-test work. Restore/build accounts for37/71s of the latter. Prepare build42→59/68s and format57→65/67s also vary despite identical workflow. Test-step deltas: B-328/-269s, C+340/+396s, D+113/+69s, unchanged A+139/+129s; Exercise/Contracts fall slightly. C/D growth exceeds B savings, with additional host/runtime variance. Against secondary, runner increase is4.73%/7.60%, but speed still misses20%; do not silently switch controls.

B's old ActualSelection tail shrank substantially. Candidate1 finishes with its44-row Selection collection at783.1s. Candidate2 instead reaches the retained151-row RoundEntry collection:818.212s serial work, starts near24s, finishes842.3s and determines assembly842.371s. A is independently dominated by26other RoundEntry rows, taking704.407/694.873s versus controls566.235/692.714s.

C now has seven collections. Short PaidAssault and RepeatedMovement contracts delay BreakdownCompletion until331/363s; that collection finishes676/732s. Original MovementCompletion also stretches from336/504s controls to593/645s candidates. D's added short classes finish early; the123-row CycleControlContract still determines assembly at691/647s versus controls578/469s. Collection values overlap and are not additive wall predictions. XML cannot identify whether stretch comes from CPU, GC, caches, order or host contention.

A fixed-observed-cost counterfactual illustrates the remaining margin: immediate B RoundEntry start would still yield≈1116s critical stages, above the1099.2s speed limit. Another class move might change contention, but no measured evidence establishes that benefit. Moving two short C classes to D could improve cost while leaving B critical and risking D scheduling. Consolidating full RoundEntry is worse: candidate2 A+B serial sum1513.085s exceeds B's1500s job cap before setup. No small whole-class correction has demonstrated margin to satisfy both targets.

## Failure classification and review

No assertion flake established. Historical38652809605 cancelled B at1204s after test/upload success during finalization;38646785953 cancelled B at1220s during execution. These remain capacity/incomplete gates, not pass/fail assertion variation. Required aggregation correctly failed.

Historical38609798518 attempt1 failed prepare on `Empty/duplicate discovery` at tested eventf635981bef9b33e94005f5b01cf10a6d907c5833 (PRhead5a0dbc82). Retained discovery listed2606records but2597unique names. A56-case theory had only47unique rendered names because xUnit shortened long distinct strings. Historical fix3bcaedc74a184a2adc28ad6e722787304c04e214 adds a leading integer ordinal; test body and gate unchanged. Next run38612754520 has2606/2606and56/56unique names. Already fixed in current base; no new CI code fix needed. Prepare stopped before a complete inventory, tests skipped and aggregate failed closed.

Candidate author evidence: genuine new-test RED (four checks, three failures with11assertions/subtests), then137Python checks passed. Real inventory replay preserves exact union and order independence. One obsolete fixed D command-length assertion was corrected. Fresh review1 found no code defect at214d33b per Brain; performance rejection remains decisive. No heavy local suite, retry-to-green, quarantine or test deletion used.

## Concrete next profiling plan

1. Stop this candidate: preserve branch/PR and both results; do not spend the final run on unchanged mapping. Budget2/3used,1reserved. A changed candidate needs two fresh observations plus review/final/main gates; one remaining run cannot certify it.
2. Under a new Brain-approved scope/lease, freeze one workload and measure full-solution versus test-project leaf builds with fresh outputs, same SDK/image/configuration and binlogs. Keep prepare's full-solution gate. Quantify dependency closure, cold/warm restore, actual critical-path savings and summed runner-seconds before selecting any workflow edit. Current B restore/build71–74s is an upper bound, not a measured narrower-build gain. Build-once remains deferred and would need hard artifact provenance.
3. With explicit gameplay/test-owner allocation, profile RoundEntry's eight transferred mutation rows (259–286s total across observations) and two lifecycle-clock rows (144–190s). Capture per-class start/finish plus CPU/GC/memory and immutable scenario/seed/configuration identities. Test whether repeated-prefix work dominates. Any reuse must prove authoritative state, RNG position, history, costs, replay and isolation equivalence; preserve every case/cut/assertion.
4. Only after measured design selection: genuine focused RED/GREEN, unchanged exhaustive/provenance negatives, author self-review, fresh review and a newly approved two-observation budget. No timeout increase, blanket parallelism, selective required coverage, paid runner purchase or threshold relaxation.

Detailed sanitized run/step ledgers and diagnosis remain in Brain's local planning evidence. This report contains safe code/test identifiers and derived measurements only; no raw logs, secrets, diagnostic binaries or blocked archive27d8293.

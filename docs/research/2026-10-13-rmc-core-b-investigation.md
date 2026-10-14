# PR182 Core-B timeout investigation

Status: bounded read-only root-cause investigation, no implementation or test suite run. Decision owner: Brain. Reviewed head `c20a84c5d84ca96852ba2bb9d7e1f88b6b668c4e`; native baseline `c43b9caec452c152a086412c779bb6495ef1a0ad`. CI38490841757 attempts1/2 tested merge `e0d67dcb054d4dc7490e433cccd91b18fd54d487`. Brain verified GitHub commit API parents exactly baseline+reviewed head, tree `f1b771af4383860b8b0859f4585e16d5a4fb2c73`. The merge object is not available in the research checkout; this ancestry verification is Brain-supplied evidence.

## Finding and recommendation

**The immediate failure mechanism is the existing15minute Core-B job budget expiring during continued successful execution of the unchanged2329-row shard.** Attempt2 shows no test stall, assertion failure, new reference fixture acquisition, or new native workload. It had2290passed/0failed/0skipped, one active row,38not-started rows when GitHub canceled the job. The same2290matched rows took18.915% more summed test duration than prior successful main CI38408833396. Independent heavy classes slowed16–24%; this is cumulative execution variance against an already narrow timeout margin.

Recommend an explicit **Core-B-only20minute job timeout**, keeping other shard/prepare/verify/dependency caps at their current values, exact row filters/workload unchanged, and exact-SHA/attempt/disjoint/exhaustive/process/coverage gates intact. This is a bounded capacity mitigation justified by the retained logs, not a generic workflow timeout increase or a claim that20minutes guarantees all future hosts. One subsequent exact-head CI run with the changed Core-B cap is the validating experiment. Do not issue another unchanged15minute retry.

The underlying source of slower effective execution—CPU scheduling, I/O, cache/GC, VM contention, or another host factor—was **not directly measured**. Runner-visible labels match; this does not prove identical effective resources. No evidence supports modifying product/contract behavior or bypassing physical dependency checks.

## Evidence and reproducible method

Skills: debugging-and-error-recovery, existing codebase-memory/Serena instructions. Graph project `sandtable-actual-selection-contract`, generation2026-10-05, is stale/missing the relevant newer symbols. Targeted search returned no matching ActualSelection nodes; coverage marked relevant source/CI paths changed or untracked. All material code reads therefore used immutable `git show c20a84c:<path>`. No reindex, author edits, GitHub writes, or tests ran.

Retained analyzer `/private/tmp/rmc-core-b-analyze.py`, result `/private/tmp/rmc-core-b-analysis.json`. Command `python3 /private/tmp/rmc-core-b-analyze.py` parses argument-bearing detailed execution rows, duration units, and timestamped diagnostic in-progress/completed nodes. It checks unique row identities and compares exactly matched rows; summed durations are explicitly **not wall time**, since classes execute concurrently. A separate read-only check asserted prior2329execution rows equal the prior prepared Core-B inventory and current2290completed rows are a subset.

Current artifacts:

- `/private/tmp/rmc-core-b-attempt2/core-b/execution.log`:4581lines,2290passed rows, no failed/skipped row.
- `/private/tmp/rmc-core-b-attempt2/core-b/diagnostics/Cna.Core.Tests_net10.0_x64_261014010027989.diag`:13775lines,2291started rows,2290finished, one active at cancellation.
- `/private/tmp/rmc-core-b-attempt1.log` and `attempt2.log`: job/step/cancellation timestamps. Attempt1 execution artifact is unavailable after rerun; do not infer its test position or row count.

Prior green artifacts supplied by Brain:

- `/private/tmp/rmc-prior-main-ci/ci-core-b-c43b9caec452c152a086412c779bb6495ef1a0ad-1/core-b/{execution.log,status.json,diagnostics/Cna.Core.Tests_net10.0_x64_261013135930972.diag}`.
- `/private/tmp/rmc-prior-main-ci/ci-inventory-c43b9caec452c152a086412c779bb6495ef1a0ad-1/inventory.json`:SDK10.0.401, prepared rows71/2329/469/10, prior Core-B status exit0.

The analysis JSON retains SHA256 of both execution/diagnostic pairs and per-row/per-class timing/state data. Preserve analyzer, result, this packet, and original artifacts with the CI disposition.

## Observations

| Metric | Prior green main | Attempt2 |
| --- | --- | --- |
| Completed Core-B rows | 2329pass,0fail/skip | 2290pass,0fail/skip |
| Started rows | 2329 | 2291 |
| First test start | Oct13 13:59:31.843UTC | Oct14 01:00:29.063UTC |
| Last observed completion | 14:11:58.087UTC | 01:14:02.715UTC |
| First→last completion wall | 746.243s | 813.651s, incomplete |
| Matched2290row duration sum | 2516.631s | 2992.662s (+18.915%) |
| Prior-only39row duration sum | 56.229s | incomplete |

Brain's prior job metadata reports Core-B13:58:26→14:12:01,815seconds including setup/build/reporting, only85seconds below900second cap. Core-A was13m15s. Attempt1 complete-row execution step00:37:44.865→cancellation00:51:32.611=827.746seconds; attempt2 step01:00:27.016→cancellation01:14:13.592=826.576seconds. Both are the same job-budget exhaustion pattern. Setup log's first timestamp is outside the exact platform job-budget start, so do not equate it with GitHub's15minute timer origin.

Class data independently demonstrate accumulated slowdown rather than one anomalous row:

| Class | Matched rows | Prior summed seconds | Current summed seconds | Increase |
| --- | --- | --- | --- | --- |
| CombatActualRoundEntryTests | 151 | 640.986 | 742.758 | 15.88% |
| CombatActualPostAssaultContinuationRetryTests | 8 | 180.281 | 216.719 | 20.21% |
| CombatActualRepeatedMovementContractTests | 12 | 152.609 | 187.717 | 23.01% |
| CombatActualRepeatedMovementTests | 16 | 146.581 | 181.848 | 24.06% |
| CombatActualPaidAssaultContractTests | 25 | 150.094 | 174.464 | 16.24% |
| CombatActualPostAssaultContinuationTests | 24 | 131.984 | 163.639 | 23.98% |

RoundEntry class's timestamped first-start→last-completion wall was641.113seconds prior versus742.883seconds current. It completed all151rows at01:13:23.820. The remaining expensive tail belonged to ActualSelection: class starts prior14:08:00.725 versus current01:10:39.527 (approximately101seconds later relative to first test). Current13completed class rows span203.187seconds, versus181.108seconds for those same rows prior (+12.19% summed duration).

Last completions were ActualSelection adversarial variants: commonwealth selection-unavailable01:13:42.037 (10.655s), finish01:13:52.594 (10.556s), opening-unavailable01:14:02.715 (10.120s). Then commonwealth/rba-expired started01:14:02.716; GitHub canceled01:14:13.592, only10.876seconds later. That same row passed in prior10.977seconds. The active row therefore had not exceeded its previous ordinary runtime when canceled. This does not prove it would pass on this run; it disproves attributing observed cancellation to a demonstrated long stall.

The39missing rows comprise31ActualSelection rows and8LandSequenceCache rows. Five remaining adversarial selection cases accounted for50.433of56.229prior seconds; all other missing cases total5.796seconds. Source method `EveryResignedEventAndProofLeafRejectsAgainstIndependentLedger` loops event/proof leaves, re-signs each forgery, and replays authenticated independent ledgers [test source281–302]; the lengthy workload is intentional repeated validation, not a new external inference wait.

Both diagnostic headers report MTP2.5.1samecommit,4processors,16GBavailable,Unix6.17.0.1022 and machine labelrunnervmmprz5. These labels cannot identify contention, CPU frequency, storage latency, or allocations; the causal host submechanism remains unknown.

## Why the new reference is not implicated by acquisition

`git diff --exit-code c43b9ca c20a84c -- .github/workflows/ci.yml .github/scripts/test_partition.py tests src global.json Directory.Build.props Directory.Packages.props` returned0/no changes. PR182 adds docs/private Python reference and evidence only; there are no additional native test rows, test source changes, project content-copy changes, dependencies, or partition changes.

Exact immutable citations:

- `.github/workflows/ci.yml:65` is the current15minute tests-job cap; `:69` four existing matrix shards; `:91` same complete-row execution; `:130` unchanged final exact-head coverage gate.
- `.github/scripts/test_partition.py:11` selected heavy-method list; `:74` existing8row transfer; `:78` complementary/disjoint complete Core partition; `:97` unchanged Core-B filter; `:158` successful dependency/process/exact-SHA/attempt/coverage checks; `:220` diagnostic/row report execution; `:285` prepared inventory binding and `:290` final process status. No private reference Python verifier is invoked by this .NET workflow.
- `tests/Cna.Core.Tests/Campaigns/CombatActualSelectionTests.cs:116` loads the explicitly named old selection fixture; `:281` adversarial row loop; `:423` dependency acquisition reads the supplied explicit path; `:424` repository lookup walks parents checking only `Sandtable.slnx`, without enumerating docs; `:430` Context loads the existing authority envelope.
- `src/Cna.Core/Campaigns/CampaignCombatActualSelection.cs:300` verifies the fixed static dependency dictionary, hashing only supplied named paths; dictionary begins`:309`. It does not enumerate new docs or acquire the completion fixture.
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` uses explicit existing fixture includes. It has no completion-reference fixture include and is unchanged; native repeated Movement/selection fixtures remain the previous files.

Thus the added RMC fixture is outside this test's acquisition map and outside native output-copy includes. Its existence may add a small checkout/artifact-repository overhead, but no evidence supports per-leaf acquisition growth, and it cannot explain changed native input work through a source path. Neither its bytes nor dependency pins should be changed to address this timeout.

##20minute bound and alternatives

Projection from the observed incomplete tail:

1. Current last completion is813.651seconds after first test.
2. Remaining39rows cost56.229prior summed seconds; they are predominantly one currently serial class tail. Multiplying by the matched Selection ratio1.122 gives63.084seconds, or by the worst listed independent class ratio1.241 gives69.758seconds. These are estimates, not proven ceilings, and conservatively summing different remaining classes may overestimate wall.
3. Expected test completion therefore roughly876.7–883.4seconds after first test, around01:15:06–01:15:12, approximately53–59seconds beyond current cancellation. Add setup/build/final process/report publication and expected whole job is about16minutes, with a17minute planning envelope based on observed24%variance and reporting allowance.
4. Core-B20minutes provides about3–4minutes beyond that envelope. A16minute cap would remain marginal;20minutes is a clear bounded mitigation rather than a minimally passing timer.

Changing only the Core-B cap preserves2329rows and current partition logic. Implement via a validated matrix-dependent tests-job timeout, with Core-B20and all other tests shards15; keep prepare/verify caps15. Record this causal rationale in CI evidence and obtain CI/configuration review. Validation still requires actual final2329row execution, process exit0and aggregate2879exact prepared rows, all owning dependencies successful, on the fresh tested merge SHA/attempt. Partial2290passed rows or discovery counts cannot establish success.

Partition split/rebalance is a legitimate later performance improvement, but broader now: Core-A's previous13m15runtime also has limited margin; transferring heavy methods without paired duration evidence could just move the timeout. A new Core-C changes MODULES, row predicates/filters, discovery proofs, coverage assumptions, matrix and helper tests. It needs a dedicated design/test/review gate. Neither is smaller than the proposed one-scope timeout correction for this run.

Residual risk: future execution can exceed the current16–24%variance, new rows can erode headroom, and synchronous detailed diagnostics/physical hash validation intentionally add cost.20minutes is not a permanent scaling solution or a latency promise. Retain per-class/row wall evidence from the validating run; if it still exceeds20or develops a stationary active row, stop and collect targeted CPU/I/O/allocation or hang evidence before any further increase. Plan later duration-based shard balancing with complete coverage proof; do not silently relax assertions, remove cases, memoize away required physical validation, or change reference contracts.

No product changes, broad local tests, retry, implementation, merge, or publication occurred during this investigation.

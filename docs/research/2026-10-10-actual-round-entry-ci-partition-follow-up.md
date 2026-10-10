# Actual round-entry CI partition follow-up

Date: 2026-10-10 America/Toronto. Status: bounded read-only recommendation, pending Brain decision and fresh research review. Investigation opened 17:35:11 Toronto with a 20-minute limit. Research3/9; DAY-A implementation1/9, recovery0/2; no counters reset. Product reference remains `64439f79356fd6303408e6a9b3579e530235b9d6`. Prior frozen research/reviews/dispositions remain unchanged.

## Recommendation and decision boundary

**Recommend a diagnostic-first experiment with the current two Core partitions, before choosing methods to move or adding a third shard.** The failed hosted A leaf supplies no per-test identities or timings. B supplies real budget evidence, but its spare time cannot establish which A method fits. The next run should add validated cancellation-retained test progress, combined with the already accepted retry/attempt isolation correction. This is a proposal for Brain, not permission to launch it, and it may still fail the existing cap.

Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422` owns selection and author authorization. This lane owns only this new note and its dated administrative handoff. It ran no .NET, CI, profiler or helper test; edited no product, workflow, helper, test or SDK setting; spawned no agents. DAY-A owns CI paths. A scheduling correction needs evidence and fresh review; timeout increases, weaker coverage and runtime/cache architecture changes remain excluded.

## Method and immutable source index

Read retained hosted artifacts, committed CI source and prior local evidence. Recomputed successful B method grouping from XML without executing tests. Codebase Memory project `sandtable-actual-selection-contract` is indexed2026-10-05; coverage reports workflow `metadata_changed` and both new Python paths `not_tracked`. Used immutable `git show` snapshots for those gaps; no reindex or fresh SDK process.

| Source | Identity and use |
| --- | --- |
| Experiment source | Head `6a598c8e7ba4df93c16a099c58417c2a5c8b9037`; [workflow](https://github.com/dills122/sandtable/blob/6a598c8e7ba4df93c16a099c58417c2a5c8b9037/.github/workflows/ci.yml), [partition helper](https://github.com/dills122/sandtable/blob/6a598c8e7ba4df93c16a099c58417c2a5c8b9037/.github/scripts/test_partition.py), [helper tests](https://github.com/dills122/sandtable/blob/6a598c8e7ba4df93c16a099c58417c2a5c8b9037/.github/scripts/test_partition_test.py). Snapshot hashes at `/private/tmp/actual-round-entry-ci-partition-follow-up/source-manifest.json`: workflow `924761989018c25a2c64332215132a9330af476f19b58713f6401be2199e64ba`; helper `5a5d0e9aeb60762dd18b5c4b3ac5bbd00d7842bd390622573707d6c4ea99d485`; tests `552d517ebad6c0430dcec797b16884568d8763bc029b610e705a8df879b2b9bb`. |
| Hosted experiment | [Run38086927828](https://github.com/dills122/sandtable/actions/runs/38086927828), attempt1, actual checkout/build event SHA `56b1382ca5e473a9096257ee8bd06db73fef37a8`, SDK10.0.401, ubuntu24.04. PR head and tested merge SHA are distinct identities. |
| Hosted raw evidence | `/private/tmp/native-round-entry-gates/ci-experiment-38086927828.log`, SHA256 `c5334aa3f486ad79fdc87df343c967c1d1c27db0a3b5816df5122e08471ceef7`; `ci-core-a-job.json`, `ci-core-a-annotations.json`, `ci-experiment-38086927828-timings.json` in the same directory. |
| Artifact inventory | `/private/tmp/native-round-entry-gates/ci-hosted-38086927828-artifact-hashes.json`, SHA256 `15a46cc931d6b7e3cf7052048fdd4d8730ea5a9ffbaca0aa7d639f56a8bc3ea1`: six artifacts,77 fingerprinted files. Download tree `/private/tmp/native-round-entry-gates/ci-hosted-38086927828/`. |
| B timings | `ci-experiment-38086927828-core-b-method-timings.json`, SHA256 `c3fab8719102ce9d0f600b3f98c57508d4d5f2c880c3f2e8a747e2adf2269aa1`,1182 methods/2212 rows; independently regrouped XML at `/private/tmp/actual-round-entry-ci-partition-follow-up/hosted-b-methods.json`. |
| Local retained evidence | [Prior budget note](2026-10-10-actual-round-entry-ci-budget-decision.md), including current Debug XML/method timings, exact12 A selectors, and their scope. Local Release discovery/control evidence is under `/private/tmp/native-round-entry-gates/ci-partition-release-*`; it is discovery/formatter evidence, not full A performance. |

## Hosted observations and missing A evidence

**Observation:** preparation succeeded in169 seconds. Complete Release discovery reconciled Core2291 rows as A79 across12 whole methods plus B2212; ExerciseRunner469 and Contracts10. A/B retain the same exact selectors as the prior budget note. B passes2212/2212 with zero failures/skips; ExerciseRunner469 and Contracts10 pass. Always-run `verify` fails correctly on the canceled dependency. This experiment did not solve the CI budget and cannot support implementation adoption.

| Leaf | Setup / test step / entire job | Retained outcome |
| --- | --- | --- |
| Core A | 80 /833 /918 seconds | Canceled; annotation says15m0s exceeded. No final process status or XML. |
| Core B | 71 /530 /604 seconds | Exit0; XML assembly527.959 seconds,2212PASS. |
| ExerciseRunner | 45 /73 /121 seconds | XML71.767 seconds,469PASS. |
| Contracts | 52 /1 /57 seconds | XML0.217 seconds,10PASS. |

A job [114315815136](https://github.com/dills122/sandtable/actions/runs/38086927828/job/114315815136) started21:18:32UTC; restore ran21:18:37–52; full Release build21:18:52–21:19:52; test step21:19:52–21:33:45. The always-run upload succeeded. Its `core-a/execution.log` is127 bytes, SHA256 `456e908f481e9ed4dff191991eabb0bdb20c2b20b651271a7ad2a262e7f7b1f4`, containing only the module-start line. No `status.json`, final XML, case start/completion identity or method duration survives. Binlogs survive; this lane did not analyze them and does not treat them as method profiles. The918-second API job interval includes cancellation/reporting overhead; the configured cap and annotation remain15 minutes.

**Unknown:** which A cases started, completed, waited, or were active at cancellation; whether time was dominated by one method, several collections, runner/report cleanup or another bottleneck. **No single-method hosted serial lower bound is established.** An833-second test-step interval is a censored process interval, not a method duration. A missing final report is not proof that all79 cases ran for that interval.

B's XML records collection-per-class, four collection threads, xUnit4.0.1 on.NET10.0.12. Assembly elapsed527.959 seconds differs from overlapping summed case durations1592.071 seconds. Major successful method sums:

| B method (class shorthand) | Rows | Summed seconds |
| --- | ---: | ---: |
| ActualSelection.EveryResignedEventAndProofLeafRejectsAgainstIndependentLedger | 16 |227.168 |
| ActualRoundEntry.IndependentLifecycleClockTableCoversEveryCommandAtEveryRepresentative | 2 |145.418 |
| ActualRoundEntry.CanonicalGrammarCapacitiesAndOrderedHistoriesRejectIndependentForgeries |34 |100.338 |
| ActualRoundEntry.EveryOriginalCutSuffixAndExactRetryMatchesIndependentLiteralBytes |34 |96.507 |
| InheritedSteps.FourActualHistoriesMatchAllEightyEightFrozenCommitmentsAndRestoreEveryCut |1 |53.453 |

B native143 rows sum442.318 seconds, spanning21:19:58.827–21:27:21.264UTC. ActualSelection collection ends21:28:33.013,26ms before assembly end, with248.157 summed seconds. **Inference:** B has about296 seconds nominal job margin in this one run. That is useful evidence for a future transfer hypothesis, not fungible CPU capacity or an assurance that an A method adds its local duration to B's wall time. Moving the34 native mutation rows back to B would reunite all177 native rows in its serial collection.

## Local evidence, process reuse and setup

**Observation, different environment:** current full Debug2770PASS took25m00.482s; Core2291PASS XML1499.630s. Core case sums6985.283; native177 sum1195.113, mutation34 sum664.536. Standalone native177 Debug took14m18.036s, collection856.993, mutation34 sum610.475. Other local A weights include reserve-completion deep558.305/2, reaction-completion458.201/2, actual-selection cuts376.784/16 and reaction-fallback319.389/2. These are selection weights with overlapping collections. They neither establish a hosted single-method cap violation nor provide a Release/Linux conversion factor. The local Release discovery/formatter controls do not fill the missing A timing gap.

**Documented source fact:** `test_partition.py` lines196–211 launches one `dotnet test --project ... --configuration Release --no-build` process per leaf, appends one include/exclude filter with all12 method values, redirects output to a file, then writes status after `subprocess.run` returns. There is no fresh-process loop per A method. A and B have independent hosts/processes. **Inference:** their existing in-process memo state cannot cross that boundary; splitting another method or diagnosing methods in isolated invocations can change warm reuse. Within a leaf the existing reuse remains possible. No measured hit/miss or lost-reuse cost is retained, so do not claim it caused A's timeout or sum isolated cold timings into the original batch forecast.

Setup consumes the same leaf cap: A80 seconds, including15-second restore and60-second full build; B71 seconds. A third leaf repeats setup, adds a cold process and changes contention. A theoretical900−80=820-second test budget is only a planning envelope; shutdown/upload variability requires margin. Moving binaries/restored project state between jobs would introduce a separate compatibility/correctness change; it is not needed to answer the missing-progress question.

## Comparison and minimum next experiment

| Option | Evidence and limit | Disposition |
| --- | --- | --- |
| Move a whole A method to B | Small selector edit; B actually finishes. Which transfer fits is unmeasured; native transfer increases a known serial collection. | Plausible later experiment, no specific transfer certified now. |
| Third Core shard | Can separate costly collections and change queueing. Unknown A bottleneck; extra setup/cold process; cannot cure a single unsplittable method if one eventually exceeds its effective budget. | Defer automatic shard increase. |
| Current partitions plus cancellation-retained progress | Directly addresses the distinguishing evidence gap while keeping coverage and process grouping. Adds diagnostic overhead and may fail again. | **Minimum recommended diagnostic experiment.** |

After Brain authorizes, the author should combine its accepted attempt-binding fix `78927cbc8a35149b0b4918ad2d5f58076120007b` (reported65 helperPASS/self-review complete, local/push held) with a narrowly validated logging change. That reported fix is not reviewed or certified by this lane.

1. Keep the exact current selectors, runner concurrency, full Release preparation/build/format gates, unfiltered other modules, fail-fast:false, complete row reconciliation and15-minute caps. Bind every artifact to actual tested SHA, run and attempt; never ingest prior aggregate artifacts. No retry masking or ignored failure exits.
2. First prove that the pinned.NET10/MTP2/xUnit application supports the chosen options and that diagnostic files survive interruption. Candidate platform flags are `--diagnostic --diagnostic-verbosity Trace --diagnostic-synchronous-write --diagnostic-output-directory <shard-artifact-directory>`, with `--output Detailed` as supplemental result output. These are proposal flags, not an executed/validated command. Preserve the existing `--project` invocation and keep any narrow control output separate from acceptance XML.
3. Require cancellation-retained, timestamped case identities and state transitions sufficient to distinguish completed, running and not-started rows. Validate on a narrow control before spending another hosted leaf budget. Official flags do not themselves promise that complete case lifecycle evidence is emitted. If supported logging provides only completions, report that limitation; it cannot establish started-to-canceled lower bounds for unfinished cases. If the control fails this requirement, return the logging gap to Brain before launching; do not silently add a custom observer, new dependency, runner upgrade or per-method process loop.
4. Retain raw progress/diagnostic files with always-run upload. Report elapsed setup, process/assembly and per-method/collection times separately, including logging overhead. Synchronous logging intentionally changes cost, so its durations are diagnostic observations, not an uninstrumented performance guarantee. Preserve B baseline evidence.
5. Reconcile full same-head Release inventory and final execution rows: currently Core2291=A79+B2212, ExerciseRunner469, Contracts10; refresh actual identities/counts if the head changes. Require no missing/unexpected/duplicate/deferred unaccounted rows, skips/NotRun/errors, absent reports or failed/canceled/skipped dependencies. Partial diagnostic progress can inform a next decision but cannot replace final passing XML/status or turn `verify` green. Adoption still requires all exact-head required checks plus fresh implementation review.

[Microsoft's MTP CLI reference](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-cli-options) documents diagnostic file logging and MTP2 synchronous writes, with an execution-cost penalty. [SDK test documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp) documents Detailed output; newer live-progress behavior and JSON listing have.NET11 requirements. Do not rely on those features or MTP2.4 result selectors under this pin. [xUnit's options](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) expose diagnostics and long-running detection, but [configuration semantics](https://xunit.net/docs/config-xunit-runner-json) say long-running warnings occur while the runner waits idle; that is not a complete lifecycle trace. This note makes no claim that any proposed switch produces per-test start/finish records without the control proof.

## Confidence, stop and next owner

High confidence in the failure, B success, setup cost and absence of A timing; moderate confidence that a diagnostic-only change is the smallest decision-resolving next experiment. Low confidence in a particular rebalance or shard count until A lifecycle evidence exists. A completed A method approaching the effective leaf budget, or a known started unfinished case with a long elapsed interval, would change the choice; whole-method transfers then need actual cohosted validation. No percentage speedup, optimal split or single-method lower bound is inferred from censored output.

Stop at this packet. Brain decides whether to authorize the diagnostic gate, prioritizes any cheaper supported alternative, and commissions fresh review. DAY-A implements only the accepted CI scope. No experiment starts from this research lane; prior review counters and frozen documents remain intact.

# Actual round entry: required CI budget decision

Date: 2026-10-10 America/Toronto. Status: bounded read-only recommendation for Brain, not implementation approval or independent review. Investigation started16:16 Toronto, maximum25 minutes; stopped at this decision packet. Product snapshot PR169 `64439f79356fd6303408e6a9b3579e530235b9d6`. Research checkpoint remains2/9; DAY-A implementation review0/9, recovery0/2. This continuation resets no counters. Previous frozen research/dispositions remain unchanged.

## Decision and scope

**Recommend a reversible, complete-coverage CI experiment with two complementary Core method partitions, unfiltered ExerciseRunner/Contracts, and the existing required `verify` identity as a fail-closed gate.** This is the smallest candidate investigated that both cuts the measured serial native collection and distributes existing heavy work across independent hosts without changing product authority or increasing within-collection concurrency. Two shards are an initial experiment, not a proven sufficient count or runtime guarantee. Do not replace the current gate until discovery/row coverage and failure propagation are demonstrated.

Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422` owns selection and implementation authorization. Brain relays that the user's Combat-delivery authorization includes necessary routine CI fixes. No additional human scope confirmation is implied for this routine candidate; Brain still must reconcile and authorize it. Current code configures **15 minutes per job**. No user whole-pipeline15-minute requirement is recorded, and this note invents none. Architectural changes, new admission caches and weaker coverage remain excluded. This lane edits no workflow, runtime, test or governing contract and launches no .NET process/probe/CI experiment.

Success means the complete currently required test inventory executes with zero failures/skips/missing rows, all existing build/format obligations and other required checks pass, every leaf fits its unchanged15-minute cap, and the existing `verify` check passes only after all evidence reconciles. A canceled/partial run is failed evidence, not a reason to reduce the inventory.

## Method and source index

Source hierarchy: immutable committed code/configuration; retained author logs/XML; official runner/Actions documentation; labelled inference. Codebase Memory project `sandtable-actual-selection-contract` is ready but indexed2026-10-05. Coverage returned7/7 paths: metadata match for CI/global/Core runner config; packages/new engine/codec/tests are `not_tracked`. Direct `git show 64439f7:...` snapshots cover the gaps. No reindex or agent was started. Native MTP/xUnit detection used platform-detection and run-tests skills; a new SDK process was prohibited, so SDK identity comes from the retained CI log.

| Ref | Immutable source / observed artifact |
| --- | --- |
| C1 | [CI at64439f7](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/.github/workflows/ci.yml), lines25–59: one15-minute verify job, sequential restore/format/Release build, then whole solution; artifact upload excludes cancellation. |
| C2 | [global.json](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/global.json), [packages](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/Directory.Packages.props), [Core project](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/tests/Cna.Core.Tests/Cna.Core.Tests.csproj): SDK10/latestFeature, native MTP, SDK-style net10 executable, xunit.v3.mtp-v2 4.0.1. Solution contains exactly three test projects. |
| C3 | [Core runner config](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/tests/Cna.Core.Tests/xunit.runner.json) uses defaults; [ExerciseRunner config](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/tests/Cna.ExerciseRunner.Tests/xunit.runner.json) explicitly disables collection concurrency. [LandSequenceCacheTests](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/tests/Cna.Core.Tests/Rules/LandSequenceCacheTests.cs) explicitly disables its collection parallelism. |
| C4 | [New engine](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs), replay/result/key/preflight paths; [codec](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs), Parse15–28/Object37/readers46–59/shape114; [tests](https://github.com/dills122/sandtable/blob/64439f79356fd6303408e6a9b3579e530235b9d6/tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs), warm public shared-memo callback count142–154 and explicit local-memo concurrency334 onward. |
| O1 | `/private/tmp/native-round-entry-gates/ci-stage1b-64439f7.log`, module starts/PASS/cancel253–258; accompanying manifest binds run/job/annotations/head/artifacts. ANSI-colored PASS lines must not be discarded by narrow matching. |
| O2 | `stage1b-full-solution.log`, `stage1b-reports/Cna.Core.Tests.xunit.xml` and `stage1b-reports/solution-method-timings.json` under the same gates directory. Complete current-head evidence, supplied by the author and independently parsed here. |
| O3 | `stage1b-native-acceptance.log`, `stage1b-native-reports/Cna.Core.Tests.xunit.xml` and `method-timings.json`; existing prepared/cancelled profile logs. Standalone timings and full-run coactive timings are distinct. |
| O4 | Cheap Python partition/name checks against complete current Debug XML; temporary `/private/tmp/actual-round-entry-ci-budget/current-head-partition-rehearsal.json`. No test execution. |

## Observed budget and work

**Observation:** required Release run38082003227/job114300669399 canceled with15m0s timeout annotation; job20:00:13–20:15:28UTC, Test20:02:41–20:15:26. About148s elapsed before testing and about765s testing survived. Those intervals include reporting/termination overhead; they are not measured CPU allocations. All seven other exact-head checks succeeded. Hosted Contracts passed900ms, ExerciseRunner3m56.603s, Core unfinished. Upload was skipped and artifact API returned `[]`; no hosted Core XML/overall PASS exists. SDK log reports10.0.401, selected via latestFeature; do not silently assume10.0.302. GitHub repository visibility was verified public; the [official hosted-runner table](https://docs.github.com/en/actions/reference/runners/github-hosted-runners) documents4CPU/16GB for public ubuntu-24.04. Effective process scheduling/utilization was not measured.

**Observation:** unchanged head/DLL `c836f5e521eca026e0fb2a86a32680b4babb034b92022a8a489cd93ac293bc69` completed full Debug2770/2770 PASS, zero failures/skips,25m00.482s. Core2291 passed25m00.306s (XML assembly1499.630s); ExerciseRunner469 passed2m32.805s (XML152.325s); Contracts10 passed593ms. Standalone native177 passed14m18.036s, collection856.993s; mutation34 summed610.475s. Prior full Debug2764 passed29m56.179s. These observations support improvement, not a controlled cross-platform conversion factor or CI forecast.

**Current full-run XML:** native177 summed1195.113s; mutation34 summed664.536s. Native's first test20:04:38.942505UTC starts304.330s after Core19:59:34.612741; its last test20:24:34.226356 finishes26ms before Core20:24:34.252196. This directly identifies a queued serial collection ending the local run. Starting it earlier would change contention;304s is not a guaranteed saving.

Other large current method sums: reserve completion deep558.305s/2, reaction completion458.201s/2, actual-selection cuts376.784s/16, reaction fallback319.389s/2, reaction-history302.268s/14, actual-selection re-signed282.620s/16, native grammar166.407s/34, native original-history141.093s/2. All Core case durations sum6985.283s across overlapping collections. **These are selection weights, not wall shares or CPU time.** Do not divide by host cores, add across collections into a CI budget, or infer native speed from hosted ExerciseRunner's different duration.

**Documented fact:** xUnit4 defaults to collection parallelism, serializing tests within a collection; `all` also permits tests inside collections concurrently. Core defaults therefore leave all177 native rows in one collection, confirmed by XML. [xUnit configuration](https://xunit.net/docs/config-xunit-runner-json).

Remaining product costs include obligatory physical pin/hash work, canonical source/input/claim parsing, context/evidence copying, and original selection replay on genuine misses. Existing Stage1b profile medians: prepared/cancelled preflight4.312/4.206ms; original replay21.738/22.746; warm replay6.037/5.987; correct proof read12.226/12.041; input miss37.404/37.173; event miss37.096/37.579. They overlap and cannot be subtracted for attribution. The same-entry result shortcut already avoids read-only hydration. Shape metadata splitting and redundant parse/string/clone allocation are remaining new-family candidates, but their aggregate saving is unmeasured; guard/error-order regressions require the full matrix. Another product tweak has no evidence of sufficient budget margin. New caches/earlier guard bypasses are outside scope.

## Option comparison

| Option | Evidence / benefit | Limitation and disposition |
| --- | --- | --- |
| New-family parsing/shape/copy refinement | Identifiable repeated work; authority guards could remain exact. | No component share/CI margin; cold original histories remain expensive. Defer before another product change. |
| Module limit/order or projects on independent hosts | Removes ExerciseRunner/Core contention; separate projects retain complete inventories. | Core still has the same serial native class. Lowering module concurrency can lengthen total work. Useful adjunct, insufficient demonstrated fix alone. |
| Class-only Core split | Simple class selectors/complement, same test internals. | Entire native class remains14m18s local standalone before setup; no hosted proof it fits. Defer as first candidate. |
| Longest collection first | Observed304s native start delay suggests ordering opportunity without simultaneous native methods. | Requires a test-ordering change; contention and19m55s coactive native duration prevent a fit claim. Smaller alternative if Brain prioritizes a single-host experiment; not a guaranteed remedy. |
| Explicit `parallelMode: all` / more threads | Can shorten serial method/row tails. | Shared bounded static memo can be evicted between warm-up and exact58-callback assertion; current concurrent tests use their own memos. Global override also changes intentional ExerciseRunner isolation and other shared-state assumptions. Oversubscription/aggressive scheduling cannot reduce CPU work. Do not enable broadly as first experiment. |
| **Two complementary Core method partitions** | Splits native mutation from other native methods and distributes heavy existing methods; complete theory methods remain intact. Separate processes preserve current per-project concurrency rules. | Extra host/build cost and cold process caches; Release discovery/rows and actual budgets need proof. **Recommended minimum complete experiment.** |

The [MTP driver](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp) controls module concurrency separately from xUnit collection concurrency. Its default module maximum is processor count. Simple xUnit method/class include/exclude filters support multiple values; use exact fully qualified methods and never mix simple/query syntax. [xUnit MTP options](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform).

## Exact candidate selector and coverage rehearsal

Let `S` contain the following12 fully qualified methods. Shard A includes S; B excludes the same S and otherwise runs **all Core tests**. Native's34 mutation rows stay in A and143 other native rows stay in B. Every newly added method automatically goes to B. If a selected method gains rows, all of them stay in A; manifests/counts must be refreshed, not frozen to old counts. Filters compare names case-insensitively, so grouping/checks use that same equivalence.

1. `Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.EveryResignedEventInputProofBaseAndControlLeafRejects`
2. `Cna.Core.Tests.Campaigns.CombatActualSelectionTests.EveryFrozenCutSuffixRetryAndProofMatchesOriginalBytes`
3. `Cna.Core.Tests.Campaigns.CombatClosureTests.EveryCutRetriesAndDiscardRemainStableIncludingClosedAndFreshCommandsReject`
4. `Cna.Core.Tests.Campaigns.CombatInheritedCycleControlTests.ReadbackRejectsDeepMutationAndResignedEvents`
5. `Cna.Core.Tests.Campaigns.CombatInheritedReserveMovementCompletionTests.DeepReadbackAndResignedHistoryForgeriesReject`
6. `Cna.Core.Tests.Campaigns.CombatInheritedReserveMovementTests.DeepReadbackMutationsAndResignedForgeriesReject`
7. `Cna.Core.Tests.Campaigns.CombatReactionCompletionTests.EveryEventAndEveryCutCacheLeafRejectsRawAndResigned`
8. `Cna.Core.Tests.Campaigns.CombatReactionFallbackTests.EveryEventAndCacheLeafRejectsRawAndResigned`
9. `Cna.Core.Tests.Campaigns.CombatReactionHistoryReplayTests.EveryOccurrenceRejectsMissingDuplicateReorderedForeignAndUnsupportedTails`
10. `Cna.Core.Tests.Campaigns.CombatReactionSecondMoveTests.EveryEventAndCacheLeafRejectsRawAndResigned`
11. `Cna.Core.Tests.Campaigns.CombatSettledControlTests.FullSourceScopeOrdinalPrefixProgressAndOriginalDescriptorRejectTampering`
12. `Cna.Core.Tests.Campaigns.CombatStepsHistoryReplayTests.SyntheticC3aAndReactionForksCannotSupplyActualSelectionProvenance`

The temporary rehearsal uses actual current-head full Debug rows:2291 cases,1194 method groups. A has79 cases, B2212; their normalized `(module,type,method,argument-bearing display name)` sets are disjoint and union exactly equals all2291. The12 methods were selected using summed duration as a balancing heuristic, at most one chosen method per class; proxy weights3490.263/3495.020s are **not predicted elapsed times**. Every theory/generated row in this retained XML remains with its whole method. No code or runner filter was executed. Rehearsal SHA256 `00b930f3d00d30ac88a58a87b963a4295611704cd2d7f7f8ed41b2996d3ca698`.

All2291 full Debug argument-bearing names are unique. All177 standalone/current-full native names match; raw XML IDs match0/177 because IDs are regenerated each run, as the [xUnit XML specification](https://xunit.net/docs/format-xml-v2) documents. **Do not compare cross-run XML IDs as stable coverage identities.** Observed unique names are a usable retained baseline, not a guarantee about future data providers or Release discovery.

## Minimum experiment and fail-closed acceptance

After Brain authorizes, use one reversible CI-only change with unchanged product/test semantics: preserve current restore/format/full Release build in a required preparation gate, unfiltered Contracts, two Core method leaves, unfiltered ExerciseRunner, then existing named `verify` as the final gate. Keep15-minute leaf caps and all other checks. The first version may independently restore/build on each leaf to avoid introducing artifact/obj-transfer complexity; repeated setup time is part of measured budget. If later distributing binaries, bind every checkout/build to the same tested event SHA and matching SDK/artifacts; `--project --no-build` requires restored project state. Do not mix PR merge-checkout artifacts with a head-only checkout.

Commands are **proposal forms, not executed**. Define Bash array `selector_methods` with one exact quoted S entry per element:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --filter-method "${selector_methods[@]}" --report-xunit-xml --results-directory artifacts/test-results/core-a
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --filter-not-method "${selector_methods[@]}" --report-xunit-xml --results-directory artifacts/test-results/core-b
```

Before test execution, compare actual unfiltered and both filtered **Release** discovery under the same build/SDK/configuration. Use supported text discovery and theory pre-enumeration only after validating effective options. Native SDK10 does not support the newer `--list-tests json` form (documented for.NET11 Preview7); do not upgrade SDK to obtain it. A flat discovered-method count cannot prove deferred theory rows. [MTP discovery/version notes](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp).

Coverage procedure: establish required discovery inventory D, including each enumerated theory row; verify each method routes exactly once by predicate, `D_A ∩ D_B = ∅` and `D_A ∪ D_B = D_Core`, with unfiltered inventories for both other modules. For deferred/non-serializable/generated rows, require their complete expected argument-bearing row manifest from current same-head full execution/source data, and validate every actual generated row against it; a method-only discovery placeholder is not a row. If row identities are ambiguous, duplicated or cannot be reconciled across discovery/execution, fail the experiment and resolve that evidence gap before adopting partitioning. Do not silently deduplicate or accept counts alone. Current Debug2770 is a reference; Release/platform differences must be explained through actual inventory, not assumed away.

After execution, reconcile every row and per-method count, no missing/unexpected/duplicate rows, no skips/NotRun, environment errors or failures, and all nonzero process exits. Fresh filenames/artifact names must include shard identity and tested SHA; absent/truncated reports fail. Strict per-shard minimum counts can help catch emptiness but are not proof of exact coverage and do not replace skip/row checks. Do not ignore an empty-run or failure exit. Finish all shards with matrix `fail-fast: false`, no `continue-on-error`, no failure masking. [Actions matrix failure policy](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idstrategyfail-fast).

The existing `verify` identity must run after **all direct preparation/test dependencies**, including failures, and succeed only when every expected dependency reports success and all coverage/evidence checks pass. Fail/cancel/skip/missing statuses fail it. GitHub's [needs context](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#needs-context) exposes these statuses; [workflow dependency conditions](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idneeds) permit the gate to run after failure. A skipped required check can satisfy protected-branch rules, so never let a failed prerequisite merely skip this gate. [Required-check semantics](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging).

Accept only a complete exact experiment-head Release run where all leaf budgets, unchanged build/format/other checks and coverage proof pass. Retain observed SDK, binary/source hashes, complete reports, leaf elapsed/start/finish and failure-propagation checks. If a leaf still times out, keep it failed and return measured constraint to Brain; choose more partitions or another scoped option only from that evidence. No extra experiment was launched here.

## Confidence and preservation

High confidence: configured budget, functional full pass, current collection tail, remaining guarded work, exact retained row partition and XML-ID caveat. Moderate: method partition as the next experiment. Unknown: hosted Core per-method costs, cold shard-cache effect, stable Release discovery/row mapping, sufficient shard count/headroom and reproducible failure propagation. This packet closes diagnosis at those gates; it does not authorize an implementation or certify Ready. Prior ExerciseRunner aggregation failure remains unexplained/unwaived despite later passes; retained inherited verifier failures are not waived by this research.

Self-review checked current-head versus prior/standalone evidence, summed versus elapsed time, source/index limitations, default versus overridden concurrency, raw-ID versus row identity, generated theories/new methods, exact filter complement, SDK-version limits, current required-check identity, and user authorization as relayed by Brain. Saved-note links/whitespace, source/artifact hashes and research-only boundary are verified. Brain owns the next fresh research review and counters.

## Retained fingerprints

| Artifact | SHA256 |
| --- | --- |
| CI workflow at64439 | `5b4f124d553bce0b5ff26df8fb98feb081f6635f2c59bafd06b3df77aca807ae` |
| New engine at64439 | `00f30d4abd24598ebea9e12319aef6ba7c7285e64565ed9eff76ac37f4d04fcf` |
| Codec at64439 | `e224bb57183b1b0febf9b90a297744e9ce99cf3b4124ce5d320eb3fb60e48065` |
| Tests at64439 | `bd353386516b9c6456f2b014aaa2c88382d9ece4242d656bb8019ae951461c2c` |
| CI log | `3e979d262933d7d8031253d61fe0d87b49a19bcaace594412dd8dc2dcd22d2b3` |
| CI manifest | `53d5c95a35eba7167e0006dca8cc0f8d0878a152730440dee2f0f846a79c0018` |
| Complete full log | `633ee08040aef7024749f7739df82e62d814280583cb0c88f24753e776931706` |
| Complete Core XML | `b73d8c37b344716e089f4366fb9b985e3e626614300da43d09e676cf25dcce33` |
| Full method timings | `b788552e233cde40637884abd79bc91be6f9276ded44955eefcb2ab06840f9f1` |
| Standalone native XML | `2c709769a536e10415263e6855a006d2db3f16c39ea13532a4edb04737dc708c` |
| Prepared profile | `90d5627f4e31b15a47828e9f49e5997a7329c8cf8414868293803c936f01658d` |
| Cancelled profile | `637faf1c256d7363bef75c7377b895dc85e35680a40abfb20204a32dc6d4f615` |
| Partition rehearsal | `00b930f3d00d30ac88a58a87b963a4295611704cd2d7f7f8ed41b2996d3ca698` |

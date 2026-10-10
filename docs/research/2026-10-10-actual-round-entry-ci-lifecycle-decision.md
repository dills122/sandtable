# Actual round-entry CI lifecycle scheduling decision

Date: 2026-10-10 America/Toronto. Status: bounded read-only recommendation for Brain, pending fresh review and scope decision. Started18:38:40 Toronto; maximum20 minutes. Research4/9; native implementation1/9, recovery0/2; no counter reset. Research branch `codex/actual-round-entry-performance-research`, checkpoint `1e1eacc19d630c4e8030459e0207b9c53dd26119`. All prior frozen notes/reviews/dispositions remain unchanged.

## Recommendation and authority

**Recommend a CI-only two-leaf row-selection experiment transferring eight fixed native mutation rows from A to B.** Preserve complete assertion bodies, existing collection concurrency and the other11 selected A methods. Prove supported filters and exact same-head Release discovery first; Brain must authorize any implementation/run. No third Core leaf, test rewrite or runtime/cache change is needed for this candidate. Minimum refers to the CI-only surface and existing two-leaf topology, not a proven minimum number of transferred rows.

The new hosted trace establishes the scheduling bottleneck more narrowly than the [frozen diagnostic recommendation](2026-10-10-actual-round-entry-ci-partition-follow-up.md). It does not establish a full mutation-method duration or prove that isolation cannot improve contention. Keeping the whole method serial and merely starting it earlier has little evidence of sufficient savings: it already starts2.834 seconds after the test step. A full-method isolation experiment could help, but would rely on unmeasured contention savings and a home for45 other A rows.

Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422` owns authorization and review. DAY-A owns CI paths. This research lane writes only this new note and a dated administrative handoff; it runs no .NET/CI/profiler/helper test/SDK process and edits no code or workflow. Prior runtime authorization does not authorize another experiment. No product/cache changes, timeout increase, skips, fewer assertions or synthetic admission are proposed.

## Sources, method and evidence identity

Hosted [run38090888897](https://github.com/dills122/sandtable/actions/runs/38090888897), attempt1, ubuntu24.04 Release/SDK10.0.401, branch head `fc80df1a57689d88c4d6af4b68e1030a617b9992`; actual tested event/merge SHA `31d9d3bd9b2d9874f2166ac779d1d94d7e84897a`. These distinct identities are retained; branch snapshots are not silently relabelled as merge source bytes.

Author source/evidence manifest `/private/tmp/native-round-entry-gates/ci-diagnostic-review-2-manifest.json`, SHA256 `0fc4bc8cc451b2bdb98e138d5392f2703055789dd1d8c84866cbc6b0d237efe3`, binds five CI source snapshots,105 evidence files and five product pins. Primary retained inputs in that directory: `ci-diagnostic-38090888897-lifecycle.json`, `ci-diagnostic-38090888897-method-lifecycle.json`, `ci-diagnostic-38090888897-timings.json`, `ci-diagnostic-38090888897-report.md`, raw `.log`, A annotations and the85-file `ci-hosted-38090888897-artifact-hashes.json`. Actual pinned runner help is `/private/tmp/native-round-entry-runner-control/runner-help.log`, from the earlier authorized control; no fresh help process was run here.

Read-only derivations and immutable source snapshots are under `/private/tmp/actual-round-entry-ci-lifecycle-decision/`: `source-manifest.json`, `mutation-publication-spans.json`, `raw-trace-check.json`, `row-selection-rehearsal.json`; neutral `review-manifest.json` indexes the complete packet. Independently parsed all155 A producer records from the raw Trace and matched the supplied lifecycle sequence exactly. Reconciled the proposed partition against retained actual Release inventory; parsed B XML. No runtime filter proof is claimed.

Codebase Memory is ready but indexed2026-10-05; native mutation symbol search returns no node. Coverage marks new test/helper/packages untracked and workflow changed. Used immutable `git show fc80df1:...` for six cited source scopes, not stale graph absence as proof. [Mutation test](https://github.com/dills122/sandtable/blob/fc80df1a57689d88c4d6af4b68e1030a617b9992/tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs#L425) lines425–462 has one integer-index theory row per fixture, preserving all event/input/proof/control mutation loops and rejection assertions. `Cases()` yields indices0–33. [Helper](https://github.com/dills122/sandtable/blob/fc80df1a57689d88c4d6af4b68e1030a617b9992/.github/scripts/test_partition.py) currently proves method predicates and executes one batch process per leaf; its predicate/discovery/execution must change together for row selection. The native mutation theory is not one of its deferred providers.

## Observed publication spans and ordering

**Observation:** A has155 node publications:78 starts,77 Passed, no unexpected identities or unsupported state records. All11 other selected methods complete45/45. Mutation completes32/34 with33 starts. Index12 has only an InProgress publication22:35:40.4330369UTC; index28 has no observed publication. A has no final XML/status; required `verify` fails correctly. Surviving A Trace is219714 bytes, SHA256 `5e4548908782cbee5b990488a77bc6aff08b0604bb8a65dbd82350eab636c285`.

| Measured scope | Observed value |
| --- | --- |
| A setup / test step / job |72 /841 /917 seconds; configured15m0s cancellation |
| Native first publication |22:21:53.8341511UTC; near test launch22:21:51 |
| Native last completed publication |22:35:40.4313900UTC; first-to-last826.597 seconds |
|32 paired native start→Passed publication intervals |Sum826.529 seconds; median22.756; range19.686–50.862 |
| Latest publication of all other11 A methods |22:27:48.7863072UTC;354.952 seconds after first native publication |
| Native publication tail after other A methods finish |471.645 seconds through last completed native row |
| B XML / setup / test step / job |541.071 /78 /544 /627 seconds;2212PASS, no failures/skips |
| B native143 rows |XML sum448.689 seconds;22:22:13.315–22:29:42.119UTC |

Native observed start order is `11,10,9,24,26,0,5,1,15,6,18,33,16,13,31,21,30,2,22,27,7,8,20,3,17,19,32,4,23,25,29,14,12`;28 is unobserved. Each of the32 completed rows publishes Passed before the next native start. The combined sequence is consistent with the configured serial native collection. These are producer publication timestamps, not exact test-body starts, CPU work or authoritative body durations. Do not assign the841-second test interval to a case, treat index12 as a slow hotspot, or infer a duration for index28. Index12 starts only11.567 seconds before the test-step cancellation timestamp; that observed publication age is not a completed-case duration.

**Inference:** the native sequence is the surviving A tail and is already scheduled early. Its32 completed intervals nearly consume the nominal900−72=828-second test allowance before the two missing completions and final reporting. Preserving these observed spans would not fit the full method at that setup cost. **Unknown:** isolation's effect on the first coactive rows, cold/warm reuse, fixture complexity, host contention and unfinished work. Whole-method isolation cannot be certified to fit from this trace; the trace also cannot prove it must fail after removing peers. Later completed rows are mostly around20–23 seconds while early coactive rows are longer, but that correlation does not identify a causal contention saving.

## Minimum row candidate and exact coverage algebra

Let D be the complete same-head Release Core row inventory; M the native mutation method's34 rows; O the45 rows belonging to the other11 currently selected A methods. Original A=O∪M and B=D\(O∪M). Use exact fully qualified argument-bearing display names. Transfer set T contains mutation indices **0,1,2,3,17,18,19,20**, all observed Passed. Let Q=M\T.

Proposed **A′=O∪Q=(O∪M)\T** and **B′=D\A′=B∪T**. Thus A′∩B′=∅ and A′∪B′=D; current counts **71+2220=2291**. Native rows remain26 in A plus151 in B, totaling177 across all native methods. ExerciseRunner469 and Contracts10 remain unfiltered. This is a small, fixed index block rather than a claim of an optimal split or a fastest-row selector. Eight changes enough observed work to test meaningful margin without moving half of the mutation method into B's known serial native collection.

Retained `row-selection-rehearsal.json` contains every exact proposed row, the eight full display names and inventory hash. All2291 names are unique under case folding; native names contain no wildcard. Rehearsal is set algebra only, not execution. Implement from fresh unfiltered discovery: require all eight T identities exist exactly once; derive Q from actual M rather than a stale26-name list. New rows of M outside T enter Q/A and are excluded from B; newly added methods outside the selected12 enter B. Refresh counts from D. Unknown/custom/duplicate/ambiguous names, missing selected methods or unmatched T cause failure before runtime.

[Official xUnit4.0 release notes](https://xunit.net/releases/v3/4.0.0#Microsoft-Testing-Platform) document `--filter-display-name` and `--filter-not-display-name` specifically for individual theory-row selection. The existing4.0.1 pin satisfies that version requirement; no runner upgrade is needed. [Official4.0.1 filter API](https://api.xunit.net/v3-aot/4.0.1/v3-aot.4.0.1-Xunit.Runner.Common.XunitFilters.AddExcludedDisplayNameFilter.html) describes display-name filters and only beginning/end wildcard support. Retained application help confirms both flags, positive OR and negative AND semantics. Use literal names without wildcards, and simple filters only.

Candidate syntax forms below are **not executed or validated here**. Define `selected_methods` as the existing12, `other_selected_methods` as those11 excluding M, `transferred_names` as T's eight names, and `retained_mutation_names` as fresh Q's display names:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --filter-method "${selected_methods[@]}" --filter-not-display-name "${transferred_names[@]}"
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --filter-not-method "${other_selected_methods[@]}" --filter-not-display-name "${retained_mutation_names[@]}"
```

Retain existing report, Trace, artifact and process-status arguments. Prefer the helper's structured Python argument list. The actual pinned runner's combined simple-filter behavior must be proven through complete unfiltered/A′/B′ Release discovery before any test runtime; do not assume documentation substitutes for this gate. Keep all deferred-provider expansion/control proofs intact. Method-only counts cannot prove row selection. An ignored row filter would fail the explicit expected sets rather than pass a count check.

## Conditional budget scenarios and alternatives

Transferred eight paired publication intervals sum **189.220 seconds**; retained completed24 sum **637.309**. They overlap peer work and include scheduling/logging effects. They are neither CPU budgets nor portable duration weights.

**Planning inference only:** write A scenario as `setup + max(other-method span,637.309 + U) + launch/report reserve`, where U represents all unobserved residual work and changes from order, contention, memo reuse and instrumentation. U is unmeasured and has no established finite upper bound. If U lies0–100 seconds, peers still finish before the native sequence, setup72–80 and reserve5–15, the scenario is roughly714–832 seconds. This is a conditional envelope, not a forecast or assigned duration for either unfinished case. To retain60 seconds nominal margin at setup80/reserve15, the model needs U≤107.691 seconds; the experiment must falsify or validate the scheduling hypothesis, not assume that inequality.

For B, mechanically adding the189.220 publication weight to the two observed job totals604–627, with an extra0–15-second reserve, gives a conditional793–831-second scenario. Co-hosted native151 rows, changed order and memo warming can invalidate additivity in either direction. There is no statistical confidence interval. B's current nominal273-second margin gives room for this hypothesis; transferring17 rows with observed half-method weights around386–440 seconds has much weaker margin in the same two-leaf layout.

| Candidate | Evidence and risk | Disposition |
| --- | --- | --- |
| Whole M alone, peers removed | May reduce early contention; native already starts promptly.32 completed spans already consume nearly the allowance. Moving45 peers to B may exceed B's margin; a third host repeats setup. | Not certified to fit; less informative than a bounded row-transfer hypothesis. |
| **Eight argument rows A→B on existing leaves** | Supported4.0.1 row filters;71/2220 exact row algebra; changes no assertion/test identity/concurrency setting. B's serial native tail grows. | **Recommended minimum candidate after filter proof.** |
| Two17-row mutation batches on independent extra leaf | Large margin hypothesis, no individual-case split, but adds setup/process/inventory topology and loses reuse. | Escalate only after Brain reviews a failed smaller candidate; not automatic. |
| Rewrite theory into multiple methods/classes or enable intra-collection parallelism | Can expose more scheduling concurrency but changes identities/shared-state assumptions and proof mapping. | Larger test change; defer while supported row filters are available. |

Separate A/B process caches already exist. Moving T can lose A warm context and gain B warm context; cost is unmeasured. B native cases remain serial in one class collection, but T's relative order may change and affect warming. Keep each mutation row's complete loops intact; never split leaf assertions or select representative paths. No global `parallelMode: all`, thread-count increase, seed/order override or cache-cap adjustment is included. Extra fresh processes per row would change the measured reuse model and are excluded.

## Falsifying experiment, acceptance and stop

After Brain scope authorization and fresh research review, DAY-A should use TDD for helper row predicates and failure tests, same-head actual Release discovery of D/A′/B′, then one complete hosted experiment at unchanged15-minute caps. All existing restore/build/format/security/docs checks, exact tested event SHA/run/attempt binding, fail-fast:false, always retention and required fail-closed `verify` remain. Require complete final status/XML with exact identities, no missing/unexpected/duplicate/deferred unaccounted rows, skips/NotRun/errors or unsuccessful dependencies. Retain full lifecycle traces to diagnose failure; progress never replaces acceptance reports.

The hypothesis is falsified if actual filters do not realize71/2220 sets, any row/assertion is lost, either leaf times out, a failure/skip appears, or margins collapse under new grouping. Report actual setup/test/job and collection spans separately; one passing run proves that run, not a stable performance distribution. Seek nominal margin rather than a cap-edge pass; the60-second scenario target is a research preference, not an invented user requirement or waiver. Any scheduling fallback requires another Brain decision with preserved counters. No run, code edit, commit/push or new agent is authorized from this research lane. Stop at the note and neutral manifest.

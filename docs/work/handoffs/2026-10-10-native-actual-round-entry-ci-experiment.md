# Native actual round entry CI scheduling experiment

Status: preserved WIP. The bounded two-shard experiment failed its hosted budget. The accepted retry-isolation correction is committed locally with SELF_REVIEW_COMPLETE; push is held by Brain pending a bounded scheduling decision. Draft PR169 remains Not ready for delivery; corrected-head CI and Brain-owned independent implementation review2 are still required.

The product checkpoint is 64439f79356fd6303408e6a9b3579e530235b9d6. All five primary hashes in the existing native actual round entry hash manifest remain frozen. Independent implementation review 1 found no actionable semantic defects, but returned Not ready for delivery because the 15-minute CI job cancelled before Core completed. Its original report is preserved verbatim alongside a separate response. Implementation review counter is 1/9; recovery 0/2.

Brain authorized only this scheduling experiment after the separate research review 3/9 returned Ready. Approved files are the CI workflow, Python partition helper/tests, BCL-only ReleaseTestRows helper, dated review packets and this handoff. No engine, codec, native tests, solution, dependency or runner concurrency settings change. Two complementary whole-method Core partitions use the twelve reviewed selectors; ExerciseRunner and Contracts run unfiltered. Each leaf retains the existing 15-minute cap. Matrix fail-fast is false. The required check identity remains verify, with always-run fail-closed aggregation. No third shard or broader optimization is authorized if this experiment exceeds budget.

## Discovery and completeness

The same-head SDK10 Release discovery gate passed locally at 92d2076ca4c9815d59170cc601c87339cc43c8e4, SDK10.0.400. Actual unfiltered Core CLI discovery lists 2250 serializable rows plus five deferred provider placeholders. Reading those five reviewed compiled Release factories supplies 41 actual argument-bearing rows, giving Core2291. Actual filtered discovery matches Core A79 and B2212, disjoint and exhaustive. ExerciseRunner469 and Contracts10 give total2770. Raw XML UUIDs are excluded from identity.

The tracked BCL-only helper loads this host's compiled Release test assembly and installed xUnit4.0.1 assemblies. It checks the compiled runner's display-method signature and formatter call, default runner configuration, known MemberData bindings, row overrides, argument counts, duplicate rows and repeated factory output. It invokes no test bodies or delegates carried by rows. The formatter is checked against actual runner output using the 34 native case rows and five quoted-JSON InlineData rows from BreakdownRecordTests. Unknown deferred providers or display semantics fail. Binary fingerprints and helper binary fingerprint are retained. CI rediscovers dynamically at github.sha; no Debug inventory is used as authority.

Coverage requires every discovered module/method/argument-bearing name once, passing, and from successful processes at the tested SHA, SDK and Release configuration. Failed, cancelled, skipped or missing dependencies; missing/duplicate/malformed reports; unexpected/missing/duplicate/skipped/NotRun/error rows; invalid process exits; and SHA/SDK mismatch fail verify. XML display text is JSON-unescaped exactly once after XML parsing, matching the installed writer. A hard timeout may prevent status/report/artifact creation; evidence absence fails the aggregate and is not treated as completion.

## Local evidence before publication

Evidence directory: /private/tmp/native-round-entry-gates.

- ci-partition-helper-red.log: permissive baseline failed 44 of45 coverage checks.
- ci-partition-discovery-red.log: expanded60 checks failed the duplicate/wrong formatter control cases before tightening validation.
- ci-helper-final-tests.log:60/60 helper checks pass,0.126s.
- ci-row-helper-final-build.log: Release helper build passes,0warnings/errors.
- ci-row-helper-final-format.log: helper format verification passes.
- ci-tracked-provider-proof.log / ci-tracked-provider-proof.json: compiled Release data provider and formatter control proof. The earlier control attempt depended on AppContext fixture paths and failed; replaced with fixture-independent InlineData. No fixture copies or product edits.
- ci-tracked-inventory-proof.json: actual local Release inventory reconciled to79/2212/469/10.
- ci-provider-config-negative.log: changed runner display configuration rejected by the compiled helper.
- ci-gate-real-xml-reconciliation: parser accepts actual retained xUnit XML names against Release inventory using simulated partition reports. These are parser evidence, not new execution evidence.
- Workflow YAML parses via Ruby YAML; git diff --check passes.

Earlier product execution evidence remains177/177 native and2770/2770 full Debug tests passing; no new heavy product test execution is claimed here. Existing four inherited Python oracle failures and the original isolated route timeout remain explicitly unwaived. Hosted source644 run38082003227 cancelled at15minutes: Contracts and ExerciseRunner completed, Core did not; artifact upload was skipped. No wall-budget forecast is inferred from local summed row durations.

## Initial publication plan (superseded by the budget stop below)

Commit and push the clearly labelled unreviewed CI experiment to existing draft PR169 under existing publication authorization. Run same-head Release discovery before push. Collect hosted leaf durations, raw logs, status, discovered inventory, reports and aggregate outcome, including failures and missing evidence. If two-shard budget fails, report Brain and stop scope expansion. After actual evidence, complete the author five-axis review and neutral bootstrap/separate author rationale, then request Brain-owned fresh independent implementation review2/9. Do not self-merge or promote readiness on passing tests alone.

## Author-found retry isolation correction

The first experiment at6a598c8 cleared hosted discovery under SDK10.0.401 and tested PR merge SHA56b1382ca5e473a9096257ee8bd06db73fef37a8. Hosted Core inventory remains79/2212 and providers5/13/6/4/13, with34 numeric and5 quoted JSON controls. Prepare took169s. ExerciseRunner469/469 passed in71.767s (whole job121s), Contracts10/10 in0.217s (whole job57s), with exact discovered/executed identity matches. Core and final aggregate outcome are pending at this writing.

Author review found that the initial aggregate artifact prefix also matched the leaf download pattern. A retry could import old aggregate copies, concealing absent leaf artifacts. Brain accepted a narrow correction without interrupting the current measurement. The aggregate prefix is now coverage-aggregate, outside ci-* input discovery. Artifact names/download pattern, inventory and leaf status all bind to github.run_attempt as well as tested SHA/SDK. Prior same-SHA attempt evidence fails; SHA alone is not freshness. Download is scoped to the current workflow run by the action. Partial reruns intentionally fail if all discovery/execution artifacts do not belong to the same current attempt; use Re-run all jobs to produce a complete new attempt. No overwrite or cross-attempt fallback is permitted.

TDD: ci-artifact-pattern-red.log failed1/61, then ci-artifact-pattern-green.log passed61/61. ci-attempt-binding-red.log failed4/65 for inventory/status/stale aggregate copy freshness, then ci-attempt-binding-green.log passed65/65. The original hosted run remains scheduling evidence only. Corrected-head CI and Brain-owned review2 are required for delivery; no product, partition, timeout or collection setting changed.


## Final hosted outcome and local correction disposition

CI run38086927828 (https://github.com/dills122/sandtable/actions/runs/38086927828) completed cancelled at initial experiment branch head6a598c8, tested merge/event SHA56b1382ca5e473a9096257ee8bd06db73fef37a8. Required verify job114318761245 ran always and failed with “Failed/cancelled/skipped/missing dependency”. The cap is unmet; no delivery readiness is claimed.

| Job | Outcome | Setup before test | Test step | Full job | Completed report |
| --- | --- | ---: | ---: | ---: | --- |
| prepare | success | — | — |169s | actual Release discovery proof |
| Core A | cancelled |80s |833s |918s | absent |
| Core B | success |71s |530s |604s |2212/2212 pass;527.959s XML time |
| ExerciseRunner | success | retained in timing JSON | retained in timing JSON |121s |469/469 pass;71.767s XML time |
| Contracts | success | retained in timing JSON | retained in timing JSON |57s |10/10 pass;0.217s XML time |

Core A check114315815136 annotations explicitly state maximum execution time15m0s and cancellation. Its always-run artifact upload succeeded and retained restore/build/test binlogs plus a127-byte execution log containing only the module-start message. There is no status.json, XML, completed/partial per-test identity list or row timing evidence. Do not infer a dominating A case from Debug sums or B data. Core B has1182 observed method records and2212 actual row durations; method duration sums overlap and are not wall forecasts. Exact twelve A selectors are retained alongside these records.

Evidence directory /private/tmp/native-round-entry-gates contains ci-experiment-38086927828.log, ci-experiment-run-status.json, ci-core-a-job.json, ci-core-a-annotations.json, ci-experiment-38086927828-artifacts.json, ci-experiment-38086927828-timings.json, ci-experiment-38086927828-core-b-method-timings.json, ci-hosted-38086927828 artifacts and ci-hosted-38086927828-artifact-hashes.json (77 files). Successful B/ExerciseRunner/Contracts argument-bearing identities match their actual hosted Release inventory. Aggregate failure is retained, not waived.

Brain acknowledged the budget stop and directed local commit of the accepted retry correction/evidence, holding push briefly to avoid a knowingly redundant scheduling run. Product frozen; no third shard, timeout increase, collection-concurrency change or runtime optimization authorized. Final local checks:65/65 helper tests pass; YAML parses with always-run verify; diff whitespace check passes; all five product primary hashes and preserved review1 bytes unchanged. The earlier helper build/format gates remain applicable because its C# source is unchanged by retry isolation. SELF_REVIEW_COMPLETE means author correctness/reliability/security/maintainability/performance review is complete, including the unresolved measured performance blocker. It is preserved WIP, not Ready. Independent implementation counter1/9; recovery0/2.

Next: send corrected local head and evidence to Brain, hold push for its bounded scheduling disposition. Any delivery candidate still needs corrected-head Release CI and fresh independent implementation review2 before merge. The research chat is authorized to analyze retained evidence only; no new local heavy test lease is assigned.

# Native actual round entry CI scheduling experiment

Status: bounded experiment prepared; hosted timing and independent implementation review 2 remain pending. Draft PR169 remains unready for delivery until the required verify check and Brain-owned review complete.

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

## Next gate

Commit and push the clearly labelled unreviewed CI experiment to existing draft PR169 under existing publication authorization. Run same-head Release discovery before push. Collect hosted leaf durations, raw logs, status, discovered inventory, reports and aggregate outcome, including failures and missing evidence. If two-shard budget fails, report Brain and stop scope expansion. After actual evidence, complete the author five-axis review and neutral bootstrap/separate author rationale, then request Brain-owned fresh independent implementation review2/9. Do not self-merge or promote readiness on passing tests alone.

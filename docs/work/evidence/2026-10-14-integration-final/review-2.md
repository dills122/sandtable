# Independent N3 CI capacity review

Review instance: 2 of 9 total N3; pass 2 of 3 current round; recovery 0 of 2. No counter reset or further review instance initiated.

Target HEAD: `29636b9a5bcdd14219b4fc936a9c687f147efa3c`.
Product commit: `d5c1cba0423fa2ba0f4f9bee75f1ad75289f6372`.
Base/native baseline: `972e0c513132afd5a92fcbbb5afdfe85f11732d0`.
Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
Observed branch: `codex/actual-moved-cycle-control`, consistent with bootstrap's stated publication fast-forward destination. Bootstrap describes authoring branch `codex/n3-ci-capacity`; exact supplied HEAD matches. Tracked tree clean; only excluded untracked `.serena/` present.

Applied independent-review and code-review-and-quality skills. Read neutral bootstrap, applicable AGENTS, canonical capacity section, pre-edit implementation plan, diagnosis, tests, helper and workflow before separate author/self-review/evidence packets. Preliminary ledger written first at `/private/tmp/n3-ci-independent-review-2-preliminary.md`. No inherited implementation rationale beyond neutral initiating scope. No fixes, commits, pushes, subagents, heavy .NET execution, or raw diagnostic export.

## Findings

No actionable findings in this bounded correction.

Exact scope: seven product paths (helper, helper tests, CI workflow, README, canonical implementation plan, capacity implementation plan, capacity diagnosis), plus four review packets. Product-to-review delta contains exactly those four packets. No native/product/protocol/test source, pins, fixtures, or ReleaseTestRows formatter changes. Git diff of `src`, `tests`, `reference`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `.github/scripts/ReleaseTestRows` against baseline is empty; complete changed-path list contains only the eleven declared paths.

Correctness and integrity: Core-D owns exactly both complete N3 classes. B excludes D and remains complement; A's selectors/eight transferred rows and C's class policy remain intact. CORE_SHARDS is used for prepared Core reconstruction, per-shard policy checking and execution filters; MODULES requires six leaves. Discovery actually enumerates all MODULES and compares each filtered result against full inventory predicates, requiring pairwise disjointness and exhaustive union. Future exact-class methods follow D; similar classes remain B. `load_partition` rejects swapped assignments even with unchanged union. The existing `verify_coverage` and `expand_core` bodies are byte-identical to baseline, and their loops now include required D through MODULES.

Workflow: six matrix leaves; no fail-fast cancellation; B20/default15 unchanged, prepare/verify15/dependency5 unchanged. Exact event SHA checkout and attempt-scoped inventory/artifact downloads remain. Aggregate excludes its own artifact name and requires successful prepare/tests dependencies. Every leaf must provide process0, matching SHA/attempt/SDK/Release/module/shard, exactly one XML report, correct target framework, no failures/skips/not-run/errors, and exact unique argument-bearing row identities. Progress logs cannot satisfy acceptance.

Architecture/readability/security/performance: small extension of existing complete-class shard pattern, centralized Core enumeration, no dependencies or domain boundary changes. Added deterministic tests exercise missing classes, future/similar classes, conservation, bad discovery, misassignment, missing D evidence, stale/nonzero/Debug processes, and skipped/failed/duplicate/foreign XML. New parallel leaf addresses measured workload without weakening budgets. No material maintainability concern found.

## Plan Review

Plan is proportionate and faithful to accepted seven-path capacity correction. It preserves native N3 baseline and prior review without treating prior review as CI correction evidence. Dedicated D is supported by workload measurements and avoids placing approximately another 9.5-minute class on C's existing load. A timeout-only workaround, dropped tests, wildcard slicing, and product changes are absent.

Implementation checkpoint leaves metadata/review/publication checkboxes open. Author/evidence packets explicitly complete later local metadata verification on exact product commit and retain hosted/merge/main gates. This historical checkpoint ordering is clear and not a completion overclaim. No heavy pivot or separate workstream is needed.

## Author-Claim Reconciliation

| Claim | Independently inspected evidence | Status / consequence |
| --- | --- | --- |
| Four policy tests failed genuinely before implementation;132 green afterward | Local RED log:4 failures/4 tests/0.003s, assertion failures for missing D; retained GREEN:132/0.401s; reviewer rerun132/0.433s | Confirmed retained test evidence and current green; chronology not independently reproduced |
| 3320 actual Release metadata rows, A71/B2511/C130/D129/Exercise469/Contracts10 | Rebuilt inventory from all seven retained actual discovery logs and deferred provider payload using reviewed helper; normalized JSON exactly equals inventory | Confirmed retained discovery data, not execution |
| Four Core shards exhaustive/disjoint and D exact129 N3 rows | Reconstructed 2841 full Core identities; all pairs disjoint; union equals full discovery; exact two D classes | Confirmed |
| Existing assignments preserved | Each original five N2 row list exactly equals corresponding new inventory list; prior N3 A/C/Exercise/Contracts unchanged; prior N3 B equals new B union D | Confirmed |
| Same policy filters used for execution | Inspected discover/execute paths and checked execution_command suffix equals partition_filters for each Core shard; actual retained filtered discovery equals predicate | Confirmed |
| SHA/SDK/Release/local metadata binding and hashes | Inventory contains product SHA/SDK10.0.400/Release/local; inventory SHA2566236e68ffe5e01381658666a1fc7baff69fa277e6776b93ffa93b531f0ceb1cd; console SHA25659d99bd44284677bbb5b438f9bd019698ccfb49072de04a2456cc93b55994a5f; seven material hashes match packet | Confirmed retained receipt/material consistency; no fresh .NET discovery or binary-provenance authentication by reviewer |
| Measured workload caused timeout; final coverage unavailable | Local failed execution:2608 passing lines,0 failed/skipped;105 N3 completions including99 contract rows totaling480.675s; last20 durations3.4–3.958s; no final XML/status | Confirmed workload evidence, not accepted coverage or guarantee against other failures |
| Existing B remains tight; D15 expected enough | N2 retained XML B2511 pass/process0/time1061.977s; C130 pass/process0/time370.267s; remaining24 parity rows at last20 observed rates project81.6–94.992s | Measured rationale credible; hosted budget success remains unverified forecast |
| No source/schema/pin/fixture/formatter change or gate weakening | Exact git scope; baseline body comparisons; workflow diff only matrix D addition;132 tests and actionlint | Confirmed |
| New-head hosted execution/merge/main pending | Evidence packet explicitly lists these gates and executionPerformed=false | Confirmed limitation; no merge/main readiness inferred |

## Verification Performed

- `python3 .github/scripts/test_partition_test.py`: PASS132 tests in0.433s. Includes real lightweight Python subprocess failure/success-without-XML controls; no .NET tests executed.
- `actionlint .github/workflows/ci.yml`: exit0, no diagnostics.
- `git diff --check 972e0c513132afd5a92fcbbb5afdfe85f11732d0..29636b9a5bcdd14219b4fc936a9c687f147efa3c`: exit0.
- `git rev-parse HEAD`, status, branch, log, full diff, product-to-review changed paths and excluded-path empty diff: supplied exact scope verified.
- Read-only Python reconstruction of `/private/tmp/n3-ci-capacity-discovery/*-discovery.log` and deferred.json with build_inventory/expand_core/load_partition: exact reconstructed inventory, six counts, identity conservation, prior N2/N3 equality checks, command/filter equality and material hashes all pass. Initial reviewer comparison compared Python tuples to JSON lists and failed; corrected only comparison through JSON normalization, with no repository edit, then all assertions passed.
- Read-only XML/status parsing of retained N2 B/C and duration/count parsing of failed N3 execution: measurements above. Raw artifacts remain local.
- Byte comparisons of verify_coverage and expand_core bodies with baseline: unchanged.

## Open Questions And Residual Risks

New exact-head hosted execution is not reviewed as passing here. Author's local metadata was on product commit; descendant review commit changes only four text packets. Required hosted run must bind its own tested event SHA and current attempt. Brain must reconcile all3320 actual XML identities, six process0 receipts, SDK/Release and dependency success, then enforce tested-tree match/merge and verified-main CI/CodeQL.

B retains historical20-minute tightness, and D's10–13-minute prediction is environment-sensitive. Added prepare discovery and a sixth concurrent build/job may experience hosted contention. Existing bounded gates must expose those failures. No timeout guarantee or WIP completion claim made.

Review verified retained actual discovery data and measurements, not a fresh metadata enumeration or .NET/native execution. No heavy execution lease was acquired, used or remains held. No raw archive27d8293 evidence exported. No actualRepeat/public/full72/downstream authorization implied.

## Verdict

**Ready** for this bounded CI capacity correction at exact supplied HEAD. No actionable implementation or plan finding. This verdict completes independent code/plan review; it does not satisfy the separately required hosted execution, merge, tested-tree, or verified-main gates.

## Recommended Next Actions

Brain reconcile this report and retain remaining exact-head hosted/main gates. Preserve labelled WIP if budgets or session deadlines fail. No new review instance or recovery dispatched by this reviewer.

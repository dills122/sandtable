# Native paid-assault independent review 2

Review instance: 2 of 9. Round1 pass2; research recovery0/2. No counter reset.

Exact target: PR177, branch `codex/native-paid-assault-consumer`, base `6334a7c87446ef5c7694ac325f7650819d891413`, frozen head `ec0c6d813eea23866bba73ad882d801d4af41348`, worktree `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.

HEAD and branch verified at start and finish. Only unrelated `.serena/` is untracked. Source/test diff from correction freeze `31f445c988d404d07b07041874d04beeda69303b` to frozen head is empty. Full base-to-head implementation and the focused correction delta from review1 head `661ba68d46c77c17bf599e449cc7cc5d9efd70e4` were inspected separately. No checkout edits, commits, merges, publication, additional reviewers or workstreams.

## Preliminary review

Recorded `/private/tmp/native-paid-review-2-preliminary.md` before reading either author explanation, review1 rationale or disposition. Neutral bootstraps supplied scope and correction cases; they did disclose that this was rework, so this was independent inspection of code/plan before author rationale, not ignorance of the existence of the prior correction.

Read canonical paid spec/schema, predecessor input grammar, NP01–NP10 plan, all four new production modules and both test modules. Preliminary conclusion: no confirmed actionable defect. Pending checks were current focused execution, pin/schema comparisons and author/prior-finding reconciliation. Those checks are now complete within the bounded review scope.

Applied independent-review, code-review-and-quality and codebase-memory instructions. Serena manual read before code exploration. Codebase Memory project `sandtable-actual-selection-contract` generation `2026-10-05T14:03:21Z` is stale: `search_graph` for `.*PaidAssault.*` returned zero; coverage classified all six changed source/test files `not_tracked`. Used direct focused source reads and Git diffs, with no absence/completeness inference from the graph. No reindex or source mutation performed.

## Findings

**No actionable findings remain.** Review1's accepted P2 is resolved.

`CampaignCombatActualAssaultAdmission.OwnLedgers` (lines39–55) owns copies, validates closed selection `Input/Command` and `RoundInput/RoundCommand` grammar, enforces count16, predecessor depth32 and aggregate1MiB limits, then supplies validated bytes to inherited replay. `CampaignCombatActualPaidAssault.Replay` validates those ledgers before paid input replay; the adapter repeats bounded validation for direct callers. Codec command field primitives are checked before command version comparison. Coherent predecessor/source mismatches retain004 through the existing replay boundary; physical dependency009 remains the first public ingress gate.

Executed regression rows test boolean admittedAt→001, negative admittedAt→002 and wrong command version→003 for both predecessor ledgers through ReadSource, ReadProof, ReadControl and ApplyTrustedSource. They also test dependency009 first. Two coherent-but-mismatched-time rows preserve004 through those same APIs. Existing genuine paths, original World provenance, paid debit, ordered RNG, isolated mechanical outcomes, windows, replay and ownership checks still pass.

A source-observed corner was considered: AAD reference `ledgers()` checks both ledger containers/counts before typing records, while OwnLedgers checks/parses selection before checking round count. Thus multiply invalid inputs may choose different errors (earlier malformed record versus later oversized collection). The frozen prose mandates each individual diagnostic and named source/dependency/claim boundaries, but does not expressly order those two competing independent-ledger errors. No required standalone diagnostic mismatch or acceptance bypass was identified from this observation, so it is not elevated into a finding. No external-entry reproduction of that mixed-invalid combination is claimed.

## Plan Review

| Plan item | Evidence and assessment |
| --- | --- |
| NP01–NP03 | Native AAD derives authentic ARE Prepared25 proof, normalized native rules,16616-record catalogue and unchanged false capability flags. Paid source and independent ledgers supply authority. All four genuine histories match literal event/source/final proof bytes, versions26–33 and cursor10; disabled-fresh recovery/retries remain valid. Correction closes predecessor syntax acceptance. Original four-root semantic RED is retained as documentary evidence, not re-executed. |
| NP04 | CertifySupport precedes fresh debit; exact ordered catalogue, fixed handlers/capacities and resource checks fail before publishing changed state. Summarized equivalent mechanical outcomes cover the complete catalogue. Failure-sensitive handler/capacity/catalogue/resource tests execute. |
| NP05 | Both9728 isolated settlement matrices exercise every applicable projected cut. Tests independently assert asymmetric losses, capture subset, refusal/DP, conservation, custody duties, component/origin/ammunition/readiness preservation, movement CP/excess DP and capped RP. Literal genuine outputs independently anchor identities. Synthetic probes never become ingress authority. |
| NP06–NP07 | Native RNG uses eight purposes and conditional ninth; twelve literal vectors cover rejection/block crossing and atomic U64 exhaustion. Actual ingress fixes seed1/cursor2. Both owners/captor roles, mandatory choices, deadline equality, fallback, context-before-fallback, retries/closure and local-versus-audit clocks execute. |
| NP08–NP09 | Replay regenerates events from independent causal inputs, handles recovery, disabled-fresh progression, retries and valid stale timers; outputs clone bytes/trees. Closed shape/null/version/union grammar, preallocation/depth/array/byte/scalar/canonical gates and malformed retries remain tested. Added predecessor input grammar matches unchanged inventories exactly. |
| NP10 | Static comparison confirms exact62 wire pins,59 AAD pins and2 delivery pins; every physical hash matches. Runtime dependency tests observe64 external paths and cover changed/missing/null/unreadable and after-first-read failure. Fixtures remain test-only. |

README, tech-design and Task019F6 describe the same private immediate slice and preserve production ingress, Release execution, later/repeat/finish/consumed Reserve, public/player/host/Exercise, persistence and parent continual-cycle obligations. Naming is unchanged, so naming-overview needs no terminology edit. No shared/predecessor source, reference spec/schema/oracle/fixture, package or workflow changes occur in the full diff. The fixture-copy project edit is one additive unchanged paid-fixture link. Size is substantial but implements one accepted private contract slice with coherent admission/codec/support/transition responsibilities; no heavy pivot or new workstreams warranted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Review1 P2 accepted and fixed by independent ledger validation | Correction diff, OwnLedgers, Replay, codec and all8 added rows | Confirmed | Prior P2 closed; no forwarding-only workaround |
| Dependency009 before001/002/003; coherent mismatches004 through all four APIs | Executed regression assertions, public ingress call chain | Confirmed | Frozen single-error and named precedence behavior restored |
| Primitive-before-version grammar; owned depth32/count16/aggregate1MiB | Codec Write/Parse/Bounds and OwnLedgers; exact predecessor shape comparison | Confirmed | No authority expansion or shared codec edits |
| Full native implementation and literal parity preserved | Executed8 genuine +25 contract rows; full source/test inspection | Confirmed |33 passed,0 failures/skips |
| Six semantic RED failures against old production, then33 GREEN | Retained primary RED/GREEN logs and receipts; log SHA256 values independently checked | Documentary evidence confirmed | RED shows expected001/002/003 versus actual004; GREEN receipt source hashes match current six files |
| Correction source/test freeze31f445c; head changes delivery docs only | Empty source/test comparison31f445c..ec0c6d8 | Confirmed | Exact candidate scope preserved |
|40 paid shapes,62 wire/59 predecessor/2 delivery closure with unchanged inventories | Independent Ruby JSON/regex comparison and physical SHA256 validation | Confirmed | No pin refresh, fixture regeneration or undisclosed external acquisition found |
| Complete Release discovery71/2253/469/10=2803; no deferred factory | Inventory/receipt and added InlineData source | Documentary evidence only for discovery counts | Discovery not re-executed or represented as execution |
| Build/format and exact-head CI | Retained build/format receipts; CI not inspected by this reviewer | Local receipts only; CI unverified here | Brain retains full.NET and merge gate |
| Private scope and historical failures unwaived | Canonical documents and internal APIs | Confirmed | No activation/parent completion inferred |

## Verification Performed

SDK10.0.400; SDK-style net10.0; global.json selects native Microsoft.Testing.Platform; xunit.v3.mtp-v2 4.0.2 from central packages. Read run-tests/platform-detection/binlog-generation skills before invocation. Each test process bounded with Perl `alarm120`, no build/restore and unique binlog. Tests ran sequentially.

1. `git status --short`, `git rev-parse HEAD`, `git branch --show-current`, base-to-head stat/diff/log, focused review1 correction diff. Exact target verified; only excluded `.serena/` untracked.
2. `git diff --name-only 31f445c988d404d07b07041874d04beeda69303b ec0c6d813eea23866bba73ad882d801d4af41348 -- src tests`: empty. `git diff --check 6334a7c87446ef5c7694ac325f7650819d891413 ec0c6d813eea23866bba73ad882d801d4af41348`: exit0. Frozen reference/shared/package/workflow scope comparison empty.
3. `perl -e 'alarm 120; exec @ARGV' dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPaidAssaultContractTests' --no-ansi '/bl:/private/tmp/native-paid-review-2-contract-{}.binlog'`: sandbox invocation exit134, named-pipe bind Permission denied; no verification credit. Authorized escalated identical invocation exit0,25 passed/0 failed/0 skipped,56.447s. Successful binlog `/private/tmp/native-paid-review-2-contract-20261011-210307--29570--1ZtjI0-dotnet-test.binlog`; failed harness binlog `/private/tmp/native-paid-review-2-contract-20261011-210301--29376--RDfYiO-dotnet-test.binlog`.
4. `perl -e 'alarm 120; exec @ARGV' dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPaidAssaultTests' --no-ansi '/bl:/private/tmp/native-paid-review-2-genuine-{}.binlog'`: authorized escalated invocation exit0,8 passed/0 failed/0 skipped,44.034s. Binlog `/private/tmp/native-paid-review-2-genuine-20261011-210429--31074--xQr7ql-dotnet-test.binlog`.
5. `shasum -a 256` on six source/test files: all match correction GREEN receipt. Retained RED log SHA256 `51255c75a75b1749248453e478749946ed9dc041a333a215c5b74ea8ca97c255`; GREEN log `e87787fb0e0aa23393097de8571fab7ed21848159034de0fd37b9ed19f7e3d3a`, both match receipts.
6. Read-only inline `ruby -rjson -rdigest` script parsed C# shape/pin literals and frozen JSON inventories, compared40 paid object shapes +4 predecessor input/command shapes, exact62/59 maps +2 delivery paths, and hashed all64 physical files. Exit0,2.005s, all comparisons passed. No Python oracle, broad suite or fixture regeneration invoked.

## Open Questions And Residual Risks

Full exact-head CI, full-suite execution and complete discovery were not rerun by this reviewer; Brain owns those gates. No rebuild was requested in this pass; existing Release outputs execute the added regression rows, and current source/test hashes match the retained correction build/GREEN freeze. The original four owning failures and ExerciseRunner aggregation/CoreB139 remain unwaived.

The new private modules duplicate closed grammar and fixed hashes and repeatedly reconstruct bounded state/catalogue. This creates documented versioning and CPU/allocation costs, but no concrete correctness/security/performance regression was established in the scoped review. Isolated outcomes and current passing tests establish mechanical coverage, not alternate campaign provenance, production authentication or full-cycle reachability. No production/private API activation or rollout is approved by this report.

## Verdict

**Ready** for the reviewed private native slice. Prior P2 resolved, no additional actionable findings, all33 focused rows passed. This verdict covers code and accepted plan at the exact frozen target; it is not a full-CI result, merge instruction or parent-completion verdict.

## Recommended Next Actions

Return to Brain. Brain should retain this review2/9 report, verify exact-head full.NET CI and its remaining delivery gates, then decide publication/merge. No new independent instance, pivot, source change or external action is dispatched by this reviewer.

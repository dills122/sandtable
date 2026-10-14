# N1 independent review

Review instance: 1 of 9 total; round 1 of 3; recovery 0 of 2.
Verdict: **Not ready**. The new theory row display identities collide, preventing the required hosted inventory and test gate.

## Findings

### P1 — Give each fixture theory a unique untruncated discovery identity

File: /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable/tests/Cna.Core.Tests/Campaigns/CombatActualMovedAssaultContractTests.cs:17 (provider), :21 (theory signature).

Cases returns only the long fixture name. xUnit4.0.2 truncates string arguments in displayed theory identities. Independent scoped discovery of the frozen head produces98 records but89 unique display names, with five collision groups. The largest group has six rows displayed as `EveryRetainedCutHasExactSourceControlProofAndEventParity(name: "commonwealth-scripted-unavailable-attacker-selecte"···)`.

This is a delivery blocker: .github/scripts/test_partition.py:70 intentionally rejects duplicate discovery records. Passing the independently captured scoped output to its discovery_rows function reproduces `ValueError: Empty/duplicate discovery`. Brain additionally reports hosted CI38609798518 attempt1 failed at prepare discovery on tested mergef635981bef9b33e94005f5b01cf10a6d907c5833; hosted test jobs were skipped. No completed inventory exists, so no complete disjoint row reconciliation or full hosted test coverage can be claimed. The98 passing focused tests establish behavior, but cannot satisfy that gate.

Smallest correction: add a stable unique compact ordinal as the first theory argument, retaining the full name for fixture lookup (provider emits ordinal,name; theory accepts ordinal,name). Native formatting then keeps the distinguishing ordinal even when the name is truncated. Preserve all56 cases and798 cut expectations, and verify unique pre-enumerated discovery before another hosted gate. No shared CI change, scope expansion, heavy pivot or workstream split is required. This reviewer made no fix.

No other actionable P0–P3 finding identified.

Reviewed the full base-to-product range from f59ffcdbcbcadfbdb20b7e7f8796d477533b792f to c0b6a6345e9946bf69ee4215815792c1f902df46 at exact review HEAD 5a0dbc82e7046cd9dc4a8f42a2fc48913099fc75, branch codex/actual-moved-assault, checkout /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable. The product-to-review delta contains only the four designated review packet paths. Git status before and after checks contains only the documented untracked .serena/. No product or metadata edit, commit, publication, reviewer spawn or full-suite run occurred.

Independence: bootstrap was read first; contract, plan, tests, implementation/codec, reference verifier and design diff were inspected before author packets. Preliminary concerns were recorded in /private/tmp/n1-review-preliminary.md before opening author.md, self-review.md or evidence.json. This context contained no author implementation history.

Correctness/data integrity: Replay owns all six ledgers and authenticates the I01 parent before composing staged state. Every suffix event is regenerated from the independently retained ledger and compared byte-for-byte. Claims are parsed/compared after replay. Apply replays the prospective Source before returning; buffers and exported ledgers are defensive copies. Commit uses the actual Prepared CP4/0 World, adds5/3 and consumes ammunition once without a draw. Resolve/projection uses the cumulative paid World. Command/actor recovery retains current state and original event bytes; primitive/context gates precede duplicate/live gating. Source-derived proof tails preserve cold/retry parity. Timer, actor and admission-disabled behavior are covered by negative/recovery tests.

Architecture/readability: own contextual Selection/Round/Paid ports avoid modifying frozen predecessors or pretending an old registered proof is authentic ingress. PaidAssaultFrame is a factual carrier with owned PreparedBase. Frozen pure arithmetic, catalogue, Resolve, Project and MakeWindow are reused through that carrier. The duplicated phase mechanics and literal grammar are a real maintenance cost, but are explicit and justified by immutable contract boundaries. No new service/package/public surface was added. Security review found no new model/remote path, hidden-state disclosure or bypass of trusted ingress. Performance work is bounded by fixed ledgers, dependency pins, catalogue and finite projections; this is private full replay and does not claim production latency.

## Plan Review

The plan's contract-first ordering is present in Git: missing-reader stub/tests4819927, cumulative-debit expectations a33c944, contract/reference freeze5a4482e, native product c0b6a63, then metadata-only review packet5a0dbc8. Historical stubs explicitly throw NotSupportedException, so they are executable missing-behavior tests rather than unresolved-symbol compilation failures. The historical executions were not rerun in this review.

The resulting unit covers authentic I01 entry18, Selection/RBA, sealed Round/Prepared30, cumulative commit31 and mandatory settlement to same-cycle Reserve Release ENTRY. Twelve paid contexts cover both owners, all three closure reasons and both seal orders. Forty-four fallback branches preserve prior movement costs and RNG. README, tech-design, naming-overview and combat-cycle implementation plan describe the same private endpoint and exclusions. The accounting/lineage test additions are coherent with the delivery unit. No predecessor path is modified in the product diff.

Support and ancestry remain properly distinguished: the 16,616 catalogue rows and projection/window certification are support over the same cumulative World; the nine optional handler tests modify isolated arithmetic state and explicitly do not create authenticated Sources. Genuine ingress remains the specified seed1 runtime roots. Reserve continuation/PAC/finish/public/Runner/general RNG and full72 closure are excluded. The coordinator CI gate has now failed on theory discovery; actual-row reconciliation/merge/verified-main remain blocked and were not waived by this review.

## Author-Claim Reconciliation

| Claim | Independent evidence | Status/consequence |
| --- | --- | --- |
| Frozen target and metadata-only packet commit | Git HEAD/status/diff/log before and after checks | Confirmed |
| Six-ledger replay, byte ownership and replayed claims | Replay/Apply/ReadControl/ReadProof and codec Own/SnapshotDependencies; ownership/lineage negative tests | Confirmed |
| Cumulative CP4+5=9/0+3=3, ammo consumed once, no commit draw, Reserve entry | CumulativePaidWorld/PaidTransition; literal accounting tests; independent native/reference reruns | Confirmed |
| 56 authentic fixture cases,12 paid/44 fallback,798 cuts | Independent Python verifier and native byte-parity tests | Confirmed |
| 16,616 support records and isolated optional cases | CertifyMovedSupport full ordered comparison/counts, compressed projection-equivalent facts; reference arithmetic/coverage checks; isolated nine handler tests | Confirmed as support, not extra ancestry |
| Bounds and source quotation domination | Codec preparse/copy/source checks; syntactic max quoted wire test;13 capacity justbelow/exact/larger tests | Confirmed within stated scope; not an authenticated near-limit stress claim |
| Runtime/delivery pin identity | Parsed native83 pins equal schema83 pins; all97 grammar objects match; compiled delivery hashes and file hashes checked | Confirmed; author prose says96 shapes but actual schema/native both97, with no behavioral consequence |
| Final author98/98 and reference evidence | Author console hashes match retained logs; independently reran98/98 and56/798 | Confirmed as focused behavior, insufficient for full CI because discovery collides |
| Capacity RED/GREEN | Retained logs' hashes match evidence; RED1failed Assert.Throws/no exception; GREEN1passed; current all13 capacity regression checks pass | Confirmed |
| Original RED and later intermediate execution history | Historical executable stubs inspected; intermediate binlogs not decoded/rerun | Execution history only partly independently verified; no effect on current behavioral GREEN |

## Verification Performed

SDK10.0.400, SDK-style net10.0 project, global native Microsoft.Testing.Platform runner and xunit.v3.mtp-v2 identified from repository configuration. Run-tests/binlog skills read before execution.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedAssault*' '/bl:/private/tmp/n1-review-{}.binlog'`: exit0;98 total,98 succeeded,0failed,0skipped;2m31.597s. First sandbox attempt failed before tests because artifacts writes were denied; authorized escalation of the identical focused command succeeded. Reviewer console /private/tmp/n1-review-tests.log SHA256 aa575de004f68e7874e573605fcbe58103baa7f68c4894b4fa0a3a149471849d. Successful binlogs /private/tmp/n1-review-20261014-175113--87476--BXWV07.binlog and /private/tmp/n1-review-20261014-175125--87476--W4Pbaz-dotnet-test.binlog.
2. `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-actual-moved-assault-v1.py`: exit0;56 cases,12positive,44fallback,798cuts;Source/Control/Proof/event/literal-cost/Reserve-entry checks pass. Reviewer log /private/tmp/n1-review-python.log SHA256 a0b2c1a541b0b66f1e876731193efc69cc4e4e4fba68db190cf1cb3aeb4c1f73.
3. `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include` followed by the two new native files and all four CombatActualMovedAssault test files: exit0;empty /private/tmp/n1-review-format.log, SHA256 e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855.
4. `git diff --check f59ffcdbcbcadfbdb20b7e7f8796d477533b792f..c0b6a6345e9946bf69ee4215815792c1f902df46`: exit0.
5. Independent inline Python comparison of schema/native grammar and pins, all six material size/hash pairs and three packet hashes: all equal;97object shapes,83runtime pins.

6. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --list-tests --pre-enumerate-theories on --no-ansi --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedAssault*' '/bl:/private/tmp/n1-review-discovery-{}.binlog'`: process exit0,98 records/89unique names. Independently feeding the output to unchanged test_partition.discovery_rows raises `ValueError: Empty/duplicate discovery`. /private/tmp/n1-review-discovery.log SHA2562e4f9230f0bb2a56450c0053411c23d1f48eeaa9a94cb50da68fd3072890bd8b. This is discovery-only, no test bodies executed.

## Open Questions And Residual Risks

Codebase-memory list/search/coverage were used. Its generation is2026-10-05 and all eight cited source/test paths are not tracked, so focused rg/source reads supplied evidence; no graph completeness claim is made. Reindexing was not performed because this pass is read-only.

No full local suite, hosted CI, CodeQL or link checks were run by this reviewer. The coordinator reported the hosted discovery failure after my initial no-defect pass; I independently reproduced the specific N1 collision and partition rejection without executing a full suite. No general seed/runtime optional ancestry or public campaign behavior is established. Structural wire domination and isolated support sizing remain distinct from maximal authenticated sources. Full replay/copy/catalogue cost is bounded but unprofiled for production. Frozen mechanics/grammar duplication demands deliberate future contract updates. These limits are accurately retained by the contract and author packet.

## Verdict

**Not ready.** Focused native/reference checks substantiate the current private runtime behavior, but the N1 theory identities prevent the mandatory complete hosted inventory/test gate. Correct the bounded test-data identity issue before delivery. No heavy pivot is needed.

## Recommended Next Actions

Return the focused native execution lease to Brain. Author should accept or dispute the discovery finding with evidence, make the smallest theory-identity correction, and verify scoped discovery uniqueness. Brain owns any subsequent independent review/immutable-head CI dispatch under the existing1/9 review count and recovery budget; no review or CI retry was spawned here. Preserve excluded continuation/public/general-seed gates.

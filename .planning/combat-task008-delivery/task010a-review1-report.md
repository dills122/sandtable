# Task010A independent review

Review instance: 1 of 3.

## Findings

No actionable findings. Source review, frozen-contract comparison, both independently executed contract oracles, and21 focused Core tests support the bounded Task010A implementation. No P0–P3 defect established. Limits below remain outside this verdict.

Review boundary: base `6f52d81c5d0ac95a9f0b9799545f304444661dac`; branch `codex/combat-task008-reaction-lifecycle`; five primary files frozen by `task010a-source.sha256`. Initial HEAD equaled base. Root committed candidate during review as `2eda2edca9f56d8b0d736cf7b8259598255c61ef`; all five hashes matched before and after that commit and before closeout. Reviewer made no source edits, fixes, commits, agent dispatches or additional reviews. Only this report was written as a review artifact; test tooling produced its ordinary output and `/tmp` binlogs.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:875` explicitly splits010A actual empty selection/traversal,010B timed provisional C3a mechanisms,010C cumulative history routing and011 Prepared. Current implementation satisfies010A without claiming parent010 completion, positive admission, public activation or extended Snapshot12 persistence. Dependencies on009A admission and actual G2 replay are used directly rather than replaced with synthetic state.

Five primary paths match bootstrap: two new transition modules, existing identity codec, new focused test class and one fixture link in Core test project. Supporting design/naming documentation accurately describes immutable nested selection, authoritative outer traversal closure, unchanged material state and deferred router work. README/roadmap status changes remain conservative. Contract/schema/fixture/oracle paths have no diff against base.

Implementation assessment:

- `CampaignCombatInheritedSelection.cs:42` reconstructs actual admission through `CampaignCombatCertification.Admit`, validates bounded local events and compares complete re-emitted canonical bytes. `Apply` and `ReadControl` always replay. Neither state construction nor receipt count provides an alternate command-authority entry point; transition is private.
- `CampaignCombatInheritedSelection.cs:71` checks command grammar, System actor, segment, position and opening-receipt combination before retry lookup. Exact input hash returns owned retained event bytes and current replayed state; changed input must pass current version/lifecycle checks.
- `CampaignCombatInheritedNoAttack.cs:39` reconstructs closed selection before step replay. `Apply` bounds/clones/validates optional cache, replays independently, then requires exact cache equality. Retry semantics preserve current state while returning the original event.
- `CampaignCombatInheritedNoAttack.cs:95` derives seven catalog positions from the admitted first-slot Combat position. Six private transitions advance only outer version/prefix/position and local receipts. First previous-step receipt names selection opening; disposition names selection closure. Nested selection stays byte-identical; Reserve Release arrival produces no release, material reset, progress or extra completion event.
- `CampaignCombatIdentityCodec.cs:353` onward implements schema-order inputs/events/Controls, exact effect tags, receipt domains, prefix hashing and closed recursive grammar. New local arrays retain semantic order; inherited Boundary ordering remains unchanged. Whole-byte comparison supplies semantic validation after syntax checks.
- `CombatInheritedStepsTests.cs:16` builds all four actual Core G2 histories, compares28 selection plus60 traversal frozen hash/length commitments, restores40 control cuts and exercises96 prior-input retries across those cuts. Remaining tests reject incomplete/cross-history/duplicate/reordered tails, rehashed event forgeries, changed authority/position/version/receipt, forged caches, malformed bytes and capacity overflow, and check owned buffers plus adversarial Created mutation during list access. Golden values are commitments, not fixture-stored event JSON.

Repeated bounded predecessor replay has an intentional computational cost but preserves authority. Descriptor duplication across frozen schema and codec is a maintenance obligation mitigated by byte commitments and independent oracle checks. No unrelated architecture or gameplay expansion found.

## Author-Claim Reconciliation

Preliminary blind ledger was persisted before reading `task010a-author.md` or `task010a-checks.md`; its original text is retained below. No prior review reports, aggregate-evidence files, execution-history artifacts or cross-session retrieval were used. CCE remained unused as instructed. Historical status prose appearing beside relevant documentation was not used as current verification evidence.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Every operation reconstructs creation-rooted authority | Both Replay/Apply/ReadControl paths; Certification.Admit and actual G2 trace builder | Confirmed | Cache/count/family tag cannot authorize transitions. Pure input helpers and serializers are not authority entry points. |
| Two empty-selection events, six exact no-attack completions | Private transitions, catalog route, both frozen schemas/oracles,7 focused tests | Confirmed | Exact contract bytes and terminal position verified. |
| Exact retries retain original bytes without advancing | Input hash lookup after identity/actor checks; retry loops at every cut | Confirmed | Changed/stale/foreign inputs reject. |
| Nested selection and World/RNG remain unchanged | Selection reference retained; complete nested serialization equality in every traversal cut;88 commitments | Confirmed | Outer closure does not rewrite predecessor or release anything. |
| Hostile grammar/bounds precede trusted replay; buffers are owned | Codec validation, bounded capture/cache clone, sentry and mutation tests | Confirmed for exercised boundaries | No claimed exhaustive concurrency guarantee. |
| Four histories and88 commitments,40cuts,96retry invocations | Test loops and fixture cardinalities; independent7/7 run | Confirmed | Genuine runtime output checked against frozen commitments. |
| Prior RED, build, format and full-gate facts | Author/checks testimony only; no historical logs consumed | Unverified independently | Not relied on for verdict. Focused tests and oracle executions below are independent evidence. |
|010B/010C/011 and extended persistence remain outside010A | Canonical refinement, source scope, design text | Confirmed | Parent010 stays open. |

## Verification Performed

Clearance marker `/tmp/task010a-after-boundary-20260920` was observed to exist before the first .NET invocation. No clearance inferred from elapsed time. Used `login:false`; test commands used approved local IPC execution. SDK-style project, native MTP and xUnit v3 verified from `global.json`, project and central build/package files. `dotnet --version` returned `10.0.400`, allowed by checked-in `latestFeature` roll-forward.

| Exact command | Independent result |
| --- | --- |
| `shasum -a 256 -c .planning/combat-task008-delivery/task010a-source.sha256` | All five entries OK at initial inspection, after root commit and before closeout. |
| `git diff --check` | Exit0; no whitespace errors. |
| `git diff --name-only 6f52d81 -- docs/specs` | Empty; frozen contracts unchanged. |
| `python3 -B docs/specs/verify-combat-inherited-selection-v1.py` | Exit0:4traces/8events,12cuts,8retries,1,126mutations,46raw,72boundaries,6source pins. |
| `python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py` | Exit0:4traces/24events,28cuts,24retries,956mutations,36raw,208boundaries,7,483fixture bytes,7source pins. |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-review1-{}.binlog'` | Exit0;7passed,0failed,0skipped;35.950s total. |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '-bl:/tmp/task010a-review1-identity-{}.binlog'` | Exit0;14passed,0failed,0skipped;12.399s total. |

Binlogs verified present:

- `/tmp/task010a-review1-20260920-083701--17137--mlbZo8-dotnet-test.binlog`
- `/tmp/task010a-review1-identity-20260920-083751--17180--1pKIDI-dotnet-test.binlog`

Both Python and both .NET test processes reached exit0; no reviewer-started process remains active. Root's later modification to `task010a-checks.md` was visible in status but not reread or treated as independent evidence. Reviewer did not run solution-wide build/tests, format, Boundary suite or remote CI; those remain root-owned gates. Native test runs intentionally reused built output per bootstrap.

## Open Questions And Residual Risks

No unresolved correctness question within010A scope. Runtime corruption suite samples meaningful mutations rather than reproducing every Python oracle mutation; passing Python oracles alone would not prove C# rejection behavior. Focused runtime tests and whole-byte replay comparison supply that additional evidence here.

Cumulative router integration, timed C3a mechanisms, Prepared continuation, positive campaign composition, full extended Snapshot12 persistence, public actions and actual publication proof are not delivered by this slice. Full-suite/format/CI completion is not independently asserted. This report is review1 of3 and does not bypass root's remaining acceptance gates.

## Verdict

**Ready** — bounded frozen Task010A candidate. Parent010 remains open.

## Recommended Next Actions

Root retains acceptance and remaining review/gate ownership. No code correction requested by this review. Preserve the five-file frozen boundary and010B/010C/011 exclusions when recording acceptance.

## Appendix — Preliminary Blind Ledger

The following ledger was persisted before author/checks access and before independent oracle/test execution. Pending statements describe that earlier checkpoint, not current status.


Boundary: base `6f52d81c5d0ac95a9f0b9799545f304444661dac`, branch `codex/combat-task008-reaction-lifecycle`, frozen working candidate defined by all five entries in `task010a-source.sha256`. Initial HEAD equals base; five hashes verified. Source/Git read-only; CCE and cross-session retrieval unused. No prior review reports, aggregate evidence or execution-history artifacts read. Relevant canonical Task009/010 refinement and contracts inspected; historical status text appearing alongside relevant documentation diffs is not verification evidence.

No actionable defect established in blind pass. Pending independent execution and author reconciliation.

| Concern | Blind source/test evidence | Preliminary disposition |
| --- | --- | --- |
| Cached/tagged state replacing authority | Both Replay methods reconstruct predecessors; selection calls Certification.Admit, which requires actual G2 projection from HistoryReplay. Apply and ReadControl invoke Replay. | Satisfied by source inspection. |
| Event forgery or changed retry | Canonical input extraction, private deterministic Transition, full event-byte comparison; authenticated System identity checked before input-hash retry lookup. | Satisfied; execute focused rejection tests. |
| Frozen nested selection versus terminal outer position | Traversal retains selection object, derives catalog route from admitted position, counts six receipts and closes only at six. | Satisfied; frozen fixture comparisons cover route bytes. |
| Ownership/capacity | Created captured before hostile local indexing; bounded retained event capture and defensive result/event copies. Cache syntax checked before replay. | Focused sentry and mutation tests present; execute independently. |
| Canonical bytes and tests | Closed typed schemas match inventory; input/event/control canonical round-trip required. Four actual G2 traces feed 28 selection and 60 traversal hash/length commitments; all cuts and prior retries checked. | No synthetic positive path used. Runtime negative suite is selected coverage, not entire Python deep-mutation matrix. |
| Plan overclaim | Canonical refinement reserves timed C3a for010B, cumulative router for010C, Prepared for011; design explicitly excludes extended Snapshot12 claim. | Task010A only; parent010 remains open. |

No .NET command executed yet. Clearance requires actual existence of `/tmp/task010a-after-boundary-20260920`.


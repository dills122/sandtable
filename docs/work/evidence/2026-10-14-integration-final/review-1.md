# Independent N3 review

Review instance: 1 of 9 total; pass 1 of 3 current round; recovery 0 of 2.
Reviewer mode: fresh neutral context; read-only product review. Preliminary ledger recorded at `/private/tmp/n3-independent-review-1-preliminary.md` before opening author, self-review, or evidence packets. No additional agents or workstreams created.

Exact review HEAD: `972e0c513132afd5a92fcbbb5afdfe85f11732d0`.
Product HEAD: `7e9d1600ff88680ce585d6b800e255c69e977e9e`.
Base: `2e18cfebf33a69ea9204fccd96cf7df86a9aa26e`.
Branch: `codex/actual-moved-cycle-control`.
Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.

## Findings

No actionable findings from the independent code and plan audit. Fresh scoped native execution passed all 129 rows, reference regeneration matched all 112 cases, and scoped discovery/format/diff checks passed.

The base-to-product diff contains exactly 14 product paths: eight primary artifacts, one fixture-copy project change, and five documentation/plan paths. Product-to-review delta contains only the four same-date review packets. The only visible untracked item is unrelated `.serena/`, preserved. Frozen predecessor implementations, schemas, pins and CI helpers have no changes in the product diff.

## Plan Review

The accepted private capability is implemented coherently. Trusted N2 replay and eight separately supplied ledgers admit authentic paid/armed assessed parents. Control opens one current-occurrence window, then either closes ordinal 1 into same-slot Truck Convoy or produces a typed non-executable pending repeat intent. There is no ordinal-2 publication, progress reset, actual repeat consumer, public activation, general-seed expansion, or full72 completion claim.

I inspected canonical spec/schema, dated implementation ledger, retained design-plan addition, tests before implementation detail, both new native modules, Python reference and surrounding predecessor leaf/certification behavior. `CampaignCombatActualMovedCycleControl.cs:81-96` owns all ledgers before dependency callbacks, strictly authenticates N2 and recomputes each appended event. Lines 108-115 validate primitive command context, actor and nullability before duplicate/no-op recovery. Lines 129-153 implement window-local timing, unchanged early expiry, System fallback provenance retaining owner input, and distinct closure/intent receipts. Lines 180-189 preserve authenticated private v5 successor metadata and change only the three phase-position fields. The result wrapper owns bytes and cloned views; claims compare with fully replayed results. Codec limits and closed grammar bound parsing before dependency acquisition.

Fresh admission invokes unchanged complete N2 certification followed by N3's class-independent retention/closure law. Isolated effects are never appended to authoritative Source, ledgers, Proof or RNG ancestry. This fits the accepted partial boundary without a new execution engine or service. Nested replay, allocation and certification costs are real and explicitly bounded; this review makes no production-latency or peak-memory claim.

Process deviation remains explicit: immutable checkpoint `07a5153` was committed after first native GREEN. Git preserves missing-reader stubs and reader/finish/intent tests; retained RED log shows 2 failed, first native log 6 passed. These support genuine reader RED and the acknowledged checkpoint contents. They do not independently prove the wall-clock claim that local API/capacity text existed at 21:24 before native implementation. Neither packet claims isolated disposition-only RED. Current acceptance evidence can be assessed without rewriting that history; I found no need for a heavy pivot or plan replacement.

Exact-head hosted CI with actual-row/process/XML reconciliation, merge, and verified-main checks remain explicit delivery gates owned by the initiating task. This review does not mark them complete.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Exact accepted product scope and preserved predecessors | Git status, branch, full base/product path diff and product/review path diff | Confirmed; no unrelated product expansion. |
| Both paid/armed, 56 authentic parents, finish and pending-intent outcomes | Fixture census: 12 paid + 44 armed parents, 112 cases; 224 event authors/dispositions inspected; native parity/test and reference checks below | Confirmed by census, fresh native parity and normal reference regeneration. |
| Eight-ledger/callback/input/result ownership and strict claims | Replay/Apply/ReadControl/ReadProof/result code plus ownership, ledger and claim negatives | Confirmed by implementation and fresh scoped execution. |
| Timing/retry/no-op and System provenance | Transition checks at lines 108-153; timing/retry tests | Confirmed by implementation and fresh scoped execution. |
| 60 shapes, 98 runtime pins, 93 N2 pins unchanged | Script extracted native grammar and hashes and compared schema/current files/parent schema | Confirmed: 60/60 exact shapes, all 100 runtime+delivery hashes valid, all 93 inherited values unchanged. |
| Material evidence identifies this product | Recomputed all 14 material-file lengths/SHA256 values | Confirmed; zero mismatches. |
| Actual Release discovery has 129 unique/112 parity rows | Fresh scoped discovery; unchanged `.github/scripts/test_partition.py.discovery_rows` | Confirmed: 129/112; console SHA256 `101ef61542b558e3c2179fd88aef0fca405eef5a80666e01851377b6aaacc815` matches packet. |
| Author final focused log is exact retained evidence | Recomputed `/private/tmp/n3-final-closed-focused.log` SHA256 | Confirmed; matches packet; fresh execution reported separately. |
| Freeze chronology and RED specificity are disclosed | Historical checkpoint and current canonical/delivery docs | Disclosure confirmed; local pre-code chronology remains historical testimony. |
| Capacity stress proves framing, not maximum authenticated roots | MaximumQuotedWireCarrier test and capacity schema/spec | Confirmed; no inflated maximum-root claim. |

## Verification Performed

Environment: SDK 10.0.400; SDK-style net10.0 xUnit v3/MTP v2 test project; global native Microsoft.Testing.Platform mode. Read run-tests, platform-detection and binlog-generation guidance before .NET invocation. No full local suite ran.

- `git diff --check 2e18cfebf33a69ea9204fccd96cf7df86a9aa26e..7e9d1600ff88680ce585d6b800e255c69e977e9e`: exit 0.
- Static grammar/dependency/evidence/fixture checks described above: passed.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --list-tests --pre-enumerate-theories on --no-ansi --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedCycleControl*' /bl:/private/tmp/n3-independent-review-1-discovery-{}.binlog`: exit 0; unchanged discovery validator accepted 129 unique rows and 112 parity rows.
- Initial focused build attempt failed before test execution because sandbox denied writing worktree build caches. Authorized escalation succeeded in launching the same scoped command; this environmental failure is not a product test failure. Logs retained separately from product evidence.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatActualMovedCycleControl.cs src/Cna.Core/Campaigns/CampaignCombatActualMovedCycleControlCodec.cs tests/Cna.Core.Tests/Campaigns/CombatActualMovedCycleControlTests.cs tests/Cna.Core.Tests/Campaigns/CombatActualMovedCycleControlContractTests.cs`: exit 0, empty log `/private/tmp/n3-independent-review-1-format.log`.

Graph tooling was queried first. The index generation was 2026-10-05 and all four new native/test paths were untracked by that index. Exact-path coverage check reported `not_tracked`; the review therefore used focused source/diff reads and does not claim graph completeness. Serena instructions were read, but its activated project was the primary checkout; no semantic-tool results from the wrong worktree were used.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedCycleControl*' /bl:/private/tmp/n3-independent-review-1-{}.binlog`: authorized rebuilt run exit 0; **129 passed, 0 failed, 0 skipped; 7m 14s 148ms**. Log `/private/tmp/n3-independent-review-1-tests.log`; SHA256 `dd65575f325943889f6406e9f361f4848d3456315a2be9b639be64d4c8b12b34`. Binlogs verified present, including `/private/tmp/n3-independent-review-1-20261014-221925--95833--NpMmDj-dotnet-test.binlog` and `/private/tmp/n3-independent-review-1-discovery-20261014-222046--99439--3_4QDg-dotnet-test.binlog`. Focused native lease returned to initiating agent after completion; no further native runs planned.

- `python3 docs/specs/verify-combat-actual-moved-cycle-control-v1.py`: exit 0; `{"status": "pass", "parents": 56, "cases": 112, "cuts": 336, "events": 224}`. Normal regeneration/compare, no draft-freeze flag. Log `/private/tmp/n3-independent-review-1-reference.log`; SHA256 `609387176c7bc27036babfe88e2b41e459290f2c8662c4219b824df9e4cc75fd`.
- Final scope check: HEAD remains `972e0c513132afd5a92fcbbb5afdfe85f11732d0`; only unrelated untracked `.serena/` remains. No source, tests, plans, contracts or predecessor artifacts edited by reviewer.

## Open Questions And Residual Risks

No blocking open question. Historical local freeze-before-code ordering is only partly reconstructible and its delayed immutable checkpoint is explicitly retained as a process deviation. Capacity framing stress is intentionally unauthenticated; no authenticated maximum-root, exhaustive mutation-space, general-seed, production performance or public-loop assurance is inferred. The tests cover the supplied authentic parent family and focused negatives; full hosted integration/actual-row reconciliation remains to be executed by the initiating task. Graph coverage was stale/missing and not treated as proof.

## Verdict

**Ready** for the accepted private N3 implementation scope at review HEAD `972e0c513132afd5a92fcbbb5afdfe85f11732d0` / product `7e9d1600ff88680ce585d6b800e255c69e977e9e`. No actionable findings and no heavy pivot required. This is an independent code-and-plan readiness verdict, not a claim that hosted CI, merge or verified-main gates have completed.

## Recommended Next Actions

Initiating task should retain the disclosed process chronology, complete exact-reviewed-head hosted CI actual-row/process/XML reconciliation, then perform its planned merge and verified-main checks. No product remediation or extra review instance is requested by this report. Focused native lease has been returned; reviewer started no additional stream or reviewer.

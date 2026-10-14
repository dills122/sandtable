# N2 independent review

Review instance: 1 of 9 total N2; pass 1 of 3 current round; recovery 0 of 2.

## Findings

No actionable findings established in the accepted private N2 scope. No heavy pivot required.

Reviewed exact head `7be10e5600a1833a2aa7cfcc68dfa883bf59992b`, product `7c8f496b2829196fd16ecfda4194f2c69596b8a5`, base `4521148a1d2faabf3b3e24ecc21f8b8ef3502761`, branch `codex/actual-moved-continuation`, worktree `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`. Product-to-head delta contains only the four named review packets. Initial and final Git checks show only excluded untracked `.serena/`; tracked review target unchanged. No product edits, commits, pushes, spawned reviewers or new workstreams.

Neutral preliminary ledger was recorded at `/private/tmp/n2-independent-review-1-preliminary.md` BEFORE reading author explanation, self-review or evidence JSON. It recorded four investigation areas and no confirmed defect. Reconciliation and executed checks resolved those areas without identifying a required correction.

Material implementation observations:

- `src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuation.cs:95` parses/copies the source and ancestor carrier, owns all seven ledgers before dependency acquisition at line 100, calls genuine N1 replay at line 101 and I01 replay at line 104. Lines 106–119 enforce the bounded occurrence, exact original NONE membership/current resources and original end/progress preservation. Guards never become original members.
- Same file lines 62–77 owns current input before callbacks, recovers original event with current source/proof on retry, and replays an extended source before publishing its result. Transition lines 135–153 validates actor/context before duplicate recovery, requires current phase/version for new effects and permits only two System Release transitions. Fresh opening certification is separate from accepted replay/already-open completion. There is no clock/control/Repeat/finish action.
- Assessment lines 158–192 uses original MovementEnd eligibility and current World representation/TOE/relationships/occupancy. CP is cumulative; break-off uses maximum Contact/Engaged cost; incremental DP is computed against current spent CP. Reaction-dependent destinations are retained separately. Assessment binds current World/members, original occurrence and both completion receipts, carries nextOrdinal 2, and grants no execution. State retains original World/RNG/future duties.
- Certification lines 197–240 checks all 16,616 selected records against expected immutable rows, invokes N1 complete cumulative support, projects terminal custody/disposition classes and assesses them with refreshed member CP. Armed CP4/ammo10 support is separately checked. These are isolated factual carriers, with owned Base metadata; no registered predecessor proof or fabricated future ancestry is introduced.
- Codec bounds, strict shapes, pins and result copy semantics match the frozen contract. Smaller semantic cardinalities follow source bounds plus authenticated derivation/replay and cannot be waived by the generic 512-item grammar. Structural quoted-source stress is explicitly unauthenticated and rejects when replayed. No authority claim rests on measured near-limit roots.

## Plan Review

Implementation follows the coherent eight-primary-file scope plus the authorized fixture-copy entry, plan and synchronized navigation/design text. Git history retains pre-edit plan and real missing-reader stub RED (`8e8d6a9`), contract/reference freeze (`01fa6eb`), whitespace-only correction (`ea216dd`) and native consumer (`7c8f496`). Read the canonical contract/schema and ordered implementation plan before implementation review.

Source-specific release/assessment ports and reuse of existing internal pure occupancy/catalogue/projection helpers fit the agreed boundaries. No predecessor source/schema/pin changes, shared visibility changes, package additions, service expansion or harness/workflow weakening occur in the product diff. All 83 N1 runtime pins retain their values within N2's 93-pin closure. The schema and codec agree on all 55 shapes.

Plan steps for implementation, focused native/reference/discovery/format and author self-review are evidenced. This report supplies the first independent review. Exact-head hosted CI, complete actual-row/process/XML reconciliation, Brain merge and verified main remain outstanding workflow gates, explicitly acknowledged by the implementation; they are not claimed complete here. Public/host/Exercise/Runner, active cycle control, Repeat execution, finish and full72 remain excluded. No plan or architecture pivot is necessary.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Genuine paid and armed authority, seven independent ledgers and original membership/end | Consumer replay lines 95–120, both test classes, native 56-parent parity | Confirmed within bounded N1 profile |
| Two Release events preserve World/resources/RNG/progress, with immutable assessment | Transition/Assessment, literal tests and all 168 native/reference cut hashes | Confirmed; no further execution inferred |
| Retry recovers original event plus current proof; fresh-disabled completion remains possible | Apply/Transition and paid/armed retry tests | Confirmed |
| Complete selected support and terminal projections precede fresh opening | Certify, direct predecessor support/project source reads, 56 genuine fresh openings in native and reference | Confirmed; isolated projections remain support only |
| Strict ownership, claim comparison, dependency/delivery revalidation | Codec SnapshotDependencies/Own, result copying and mutation/forgery tests | Confirmed |
| Structural capacities dominate; measurements are not authentic near-limit admission | Spec arithmetic, fixed cardinalities, stress/just-over tests and ancestor replay rejection | Confirmed distinction; no maximal authentic campaign claim |
| 55 shapes, 93 pins, 83 unchanged ancestor pins, material/packet hashes | Independent Python comparison of schema/codec/current files/base schema | Confirmed: 8 material hashes and 3 packet hashes match |
| Author Release70/discovery/reference/format evidence | Native and RED historical log hashes match; independently repeated current native/reference/discovery/format | Confirmed; native duration differs normally |
| Full local suite absent; hosted exact-head gates pending | Author packets and this review's executed command scope | Confirmed, remains a delivery gate |

## Verification Performed

All commands ran from the worktree above, against the frozen head.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedContinuation*' /bl:/private/tmp/n2-review-{}.binlog`
   - Initial sandbox invocation failed before tests with MSB3491: artifact-cache temporary file write denied. This is an environment failure, not behavioral RED.
   - Authorized escalated rerun passed: **70 total, 70 succeeded, 0 failed, 0 skipped; 2m38.835s**.
   - Log: `/private/tmp/n2-review-1-native-escalated.log`, SHA256 `ef28bdd9928cd90711fe7721b603c9cd79fa72c7202b325bc28897ef941d65a3`.
   - Retained build binlog: `/private/tmp/n2-review-20261014-201902--66059--ajzlZP.binlog`; native test binlog: `/private/tmp/n2-review-20261014-201915--66059--An4HWL-dotnet-test.binlog`. Initial failed-build binlog also retained.
2. `python3 docs/specs/verify-combat-actual-moved-continuation-v1.py`
   - Passed: **56 cases, 12 paid, 44 armed, 168 cuts, 112 events**. Maxima Source137450/Control29127/Proof29702/Event1345/Input285 bytes.
   - Log: `/private/tmp/n2-review-1-reference.log`, SHA256 `e5d27cf21e1251afa0dd1855448417c9061430eb1be9b161ad2d22dc85ac53d4`, equal to author evidence.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --list-tests --pre-enumerate-theories on --no-ansi --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedContinuation*' /bl:/private/tmp/n2-review-discovery-{}.binlog`
   - Exited 0. Fed actual output to unchanged `.github/scripts/test_partition.py::discovery_rows(text, 'Cna.Core.Tests', {})`: **70 unique rows accepted, 56 parity rows**.
   - Log `/private/tmp/n2-review-1-discovery.log`, SHA256 `fcef93cfbc94590c594043e186ba7a5e2013d5e647769e9d010c43769332f73c`, equal to author evidence.
4. `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuation.cs src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuationCodec.cs tests/Cna.Core.Tests/Campaigns/CombatActualMovedContinuationTests.cs tests/Cna.Core.Tests/Campaigns/CombatActualMovedContinuationContractTests.cs`
   - Exited 0, empty log `/private/tmp/n2-review-1-format.log`.
5. `git diff --check 4521148a1d2faabf3b3e24ecc21f8b8ef3502761..7c8f496b2829196fd16ecfda4194f2c69596b8a5` and same check through review head: exited 0.
6. Independent Python checks: all material/packet hashes; all 93 physical runtime hashes; all 83 base N1 pin values; exact schema/codec grammar equality. Passed.

No full local suite was run. Sole focused native test lease returned to Brain after native/discovery completion; no further native checks planned.

## Open Questions And Residual Risks

No unresolved correctness question identified within this finite private profile. Full ancestry replay and complete certification have real allocation/runtime costs; this review establishes focused correctness, not production latency/peak-memory suitability. Accepted real sources remain seed1 and the specified first-stage ordinal1 profile. Structural size stress proves framing rather than authenticated near-limit ancestry. Larger scope requires its own gates.

Codebase Memory project listing/query/coverage were used. The selected worktree index generation is stale (2026-10-05); four new/relevant paths report not_tracked and CertifyMovedSupport has no indexed symbol. Direct focused source reads supplied missing coverage; no negative/exhaustive graph claims made. A sandbox `ps` probe was unavailable; it was unnecessary to test outcome and no process inventory claim relies on it.

## Verdict

**Ready** for the accepted private N2 implementation scope. No actionable findings or heavy pivot. This verdict does not claim completion of the outstanding hosted-CI/merge/verified-main gates.

## Recommended Next Actions

Brain may proceed with the already planned immutable-head hosted CI and full actual-row/process/XML reconciliation, then its merge/verified-main gates. Preserve the bounded private scope. No product remediation or further reviewer instance is requested by this review.

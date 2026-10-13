# CS01 independent review

Review instance: 1 of 9. Round 1, pass 1; recovery 0/2.
Reviewer mode used independent-review, code-review-and-quality, codebase-memory and run-tests. Serena initial instructions were read. No author implementation conversation was supplied; the separate author explanation was first read after preliminary ledger was written.

## Findings

No blocking implementation or plan defect established.

**P3 — Optional: directly protect retry of an older accepted command after later acceptance/closure.** `tests/Cna.Core.Tests/Campaigns/CombatActualPostAssaultContinuationTests.cs:101` retries only `all[cut - 1]` at each cut. This exercises the original receipt of the latest command, including a terminal command, but does not exercise a release-open/release-complete/control-open command against a later or terminal Source. PAC01 explicitly promises retry of old command+actor even after later acceptance/closure. The implementation appears correct: `CampaignCombatActualPostAssaultContinuation.cs:215–219` scans all receipts before closed/version checks. A future refactor could accidentally preserve only the latest retry while all current tests still pass. Smallest follow-up: at each later cut, retry every previously accepted command, checking its own original receipt, unchanged Source/Control/ledger and no event; include both terminal outcomes. This is test hardening, not an observed runtime failure or merge blocker.

## Target and scope verification

- Checkout: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
- Branch: `codex/native-post-assault-continuation`.
- Base: `44cf98656350b29e2f1074f2a9a1b1e634234faa`.
- Actual and requested published HEAD: `b8a2a2eb9aee6f3d89c2bd1be25d4f6fd41bf7fc`.
- Code freeze: `c7f2622a5e1ccdafa52f14ccee2238d28edfbe42`.
- Commits: semantic RED `8888a1e`, native consumer `c7f2622`, administrative freeze `b8a2a2e`.
- Base-to-head: 21 paths. Principal runtime/test changes are two additive native source files, two additive test files, one fixture-copy link. Canonical plan/navigation/roadmap and retained review evidence accompany them. Large inventory JSON is administrative discovery evidence, not runtime code.
- Freeze-to-head changes only author-plan/review/inventory evidence. Source/tests, README, tech-design and canonical combat plan match freeze. Final status still only unrelated untracked `.serena/`.
- No predecessor source or frozen spec/fixture/oracle changes. All 228 baseline `docs/specs` files were independently compared byte-for-byte to base.

## Preliminary ledger

Written to `/private/tmp/cs01-independent-review-1-preliminary.md` before author explanation:

1. No actionable defect established after requirement/test/engine/codec inspection.
2. Literal parity and negative/clock tests must execute; discovery is insufficient.
3. Compare handwritten policy to frozen oracle: occupancy/terrain, prospective cost, original Movement locations/exclusions and progress receipt.
4. Verify later-cut retries and actor-before-fallback ordering.
5. Graph generation Oct5 does not include new files; use source fallback.
6. Exact-head full CI remains a separately owned delivery gate.

Reconciliation: parity/negative tests executed successfully; pure policy and clock transition matched the frozen reference in inspected paths. Actor/context/family validation precedes receipt lookup and fallback. The older-command retry invariant is correctly implemented but only partially directly tested, producing the optional P3 above. No preliminary suspicion became a blocking finding.

## Implementation review

`Replay` checks dependency bytes, own Source grammar, original creation context and independent predecessor ledgers before producing a Frame. Native paid Replay owns predecessor bytes, authenticates original assault admission and regenerates every paid event. Continuation events regenerate from separately supplied continuation inputs and compare complete canonical bytes. ReadProof/ReadControl call full ingress before parsing claims. This preserves the authority boundary; supplied claims/World/synthetic helper Frame cannot supply Source authority.

`Derive` retains original entry member/history and Movement evidence, checks complete original own membership using pinned content side facts, and only updates spent CP from authenticated paid World. Progress uses the exact actual-paid commitment event type, canonical event hash and receipt membership, with attack-history linkage. Own admission stays ordinary NONE, first-side/turn1/stage1/ordinal1 and closed paid33. It does not widen positive Reserve/Engaged/capture admission.

`Assessment` uses original Movement proximity/exclusions and current occupied locations; compares frame World/members/Movement before computing witnesses. Clear terrain2, maximum Contact2/Engaged4 breakoff, cumulative 150% CPA and incremental excess-CPA DP agree with frozen pure kernels. CP9 is prospective: the World retains CP5. Future duties and resource/RNG/history state are copied, not activated or debited.

`Transition` preserves empty Release34/35, one control36 decision and terminal37. Independent local clock floor and original configuration budget do not borrow paid high-water. Deadline equality rejects; null/regressed/unavailable clocks finish with System authorship after owner/context checks. Receipt lookup precedes stale-version/closed checks. Repeat derives ordinal2 authority from pre-event prefix/result version and resets only cycle-local targets/progress. Finish reaches same-slot Truck Convoy. Neither new phase executes.

Codec follows the closed ordered schema, rejects duplicate properties/noncanonical bytes, validates type/range before own version comparison, bounds raw bytes/depth/arrays before publication, and remaps inherited diagnostic families. Returned byte arrays and JsonElements are copied. No new package, cache, host registration, external runtime process or shared refactor appears. Compact style matches neighboring private consumers; no concrete architecture/performance/security regression established in this bounded scope.

## Plan review

NC01–NC06 map coherently to scope, semantic RED, source/codec, literal core, negative/recovery and physical/bounds work. Dependencies place admission before effects and verification before publication. NC07 retained receipts accurately distinguish focused execution, full build/format, static integrity and discovery. NC08 correctly remains pending for independent review/exact-head CI/Brain merge. README/tech-design/roadmap consistently distinguish private native boundary from public gameplay and broader campaign certification. No migration/rollback mechanics are needed for additive private code without ingress activation. Full selected-branch campaign, actual repeated Movement, later/consumed Reserve, persistence and parent acceptance remain explicitly open. No heavy pivot is needed.

## Author-claim reconciliation

| Claim | Evidence inspected | Status/consequence |
| --- | --- | --- |
| Full native actual ancestry and four independent ledgers precede claims | Replay/Derive, paid Replay/Admit call chain, malformed/foreign-ledger and source-first claim tests | Confirmed; no synthetic authority introduced |
| 8 traces, 40 control frames, 32 events and complete Source/Proof parity | AllGenuineEventsFramesAndFinalProofMatchFrozenContract loops plus reviewer execution | Confirmed |
| World/RNG preservation and prospective CP9 | Initial/Assessment/Transition, PreservedFields and cost assertions | Confirmed |
| Original Movement/history/progress receipt retained | Derive and frozen oracle derive/assessment | Confirmed |
| Native regenerated inherited seal timestamp supported | ValidInheritedRoundTimestampVariantRegeneratesNativePaidAndContinuation | Confirmed for exercised variant; no exhaustive profile claim |
| Primitive/context/actor before retry/fallback; old receipt before stale/closed checks | Transition:208–219, clock/forgery tests | Confirmed implementation; older-command-after-later-event direct coverage follow-up remains |
| 20 shapes, 64 wire pins plus 2 physical delivery pins; predecessor immutable | Independent schema/codec/hash/base byte comparisons and physical-fault test | Confirmed |
| Final 40/40, retained source/log identities | Retained hashes independently verified; reviewer rerun 40/40 | Confirmed |
| Build zero warnings/errors and format pass | Retained author receipts/plan inspected | Retained evidence only; not independently rerun |
| Discovery2843 is execution evidence | Author explicitly denies this | Correct separation; no full-suite pass inferred |
| Full exact-head CI/merge complete | Author leaves pending | Pending, externally owned; not verified here |

## Verification performed

Native .NET10 MTP and xUnit v3 verified from global.json, SDK10.0.400 error stack/retained receipt, project SDK/net10/runner settings, Directory.Build.props and central xunit.v3.mtp-v2 version4.0.2.

Exact focused command:

```text
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*CombatActualPostAssaultContinuation*' --no-ansi '/bl:/private/tmp/cs01-review-1-focused-escalated-{}.binlog'
```

A subprocess wrapper imposed120s termination for the process group. First sandbox attempt exited-6 in0.659s before test execution: local MTP NamedPipeServer bind Permission denied. It is not a test failure or pass. Approved escalated retry exited0 in65.721s: **40 succeeded, 0 failed, 0 skipped**; test-run duration65.200s. Log `/private/tmp/cs01-review-1-focused-escalated.log`, unique binlog stem as above. Sequential focused execution only; no broad suite or Python owning chain.

Independent static checks: 66 physical hashes valid;64 wire pins equal schema;20 ordered shapes exactly equal schema; all4 retained source/test hashes match; retained focused log hash matches;228 baseline specs byte-identical; `git diff --check base..HEAD` exit0. Git target/status verified before and after execution.

Graph list_projects/index_status/search_graph/check_index_coverage used. Generation2026-10-05T14:03:21Z; new4 paths freshness `not_tracked`, search no matches. No absence claim made; focused `rg` and source reads supplied structural evidence. Serena active project is primary checkout, so no source-target assumptions were made from it.

## Open questions and residual risks

- Full exact-head CI is Brain-owned and must still be checked before merge; this report does not approve bypassing it.
- Focused run used existing Release build output with retained frozen-source build evidence, not an independent rebuild.
- Tests establish bounded ordinary roots and one authentic timestamp variant, not every upstream clock variation or broader campaign reachability.
- Repeated full lineage replay/66 physical validations have CPU/I/O cost; no production performance or activated-ingress claim exists.
- Historical owning failures/aggregation/CoreB139 remain unwaived and outside this change's completion claim.

## Verdict

**Ready with non-blocking follow-ups.** No runtime or plan blocker identified. The optional P3 adds direct protection for a documented retry invariant already correctly implemented. Exact-head CI remains the separate mandatory merge gate.

## Recommended next actions

Brain should reconcile the optional test follow-up and verify exact-head CI for published b8a2a2e before merge. No repairs, commits, publication, additional reviewers or new workstreams were performed by this reviewer.

# Fresh Review Bootstrap

Review instance: 2 of3. Frozen candidate; fresh context required.

## Review Objective
Independent implementation and plan review of Task017B native settled Combat → empty Release adapter.

## Repository And Worktree
Integration /Users/dsteele/.codex/worktrees/8fcc/sandtable. Create own clean detached clone under /tmp.
Original /Users/dsteele/repos/sandtable working tree remains read-only. No presumption of readiness.

## Base, Head, Branch, And Dirty State
Base201395c3e3faf06735424a8bc70658a2af75f081; candidate3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67.
Branch codex/combat-reserve-release-lifecycle. Source frozen; root may add evidence-only metadata.
Confirm exact SHA and clean review clone before review. Scope diff base..candidate only.

## In-Scope Commits And Paths
CampaignCombatResultRelease.cs, CampaignCombatReserveReleaseCodec.cs under src/Cna.Core/Campaigns;
CombatResultReleaseTests.cs and Cna.Core.Tests.csproj under tests/Cna.Core.Tests; root implementation
plan and README/tech-design/naming-overview/roadmap plus Task017B delivery artifacts.
Inspect plan and tests before implementation. Do not read author/check/dev/audit/worker or prior
review reports until preliminary ledger written. No prior acceptance verdict should prime review.

## Canonical Requirements And Plan
Read AGENTS and docs/design/combat-cycle-implementation-plan.md Task017 refinement, plus canonical
docs/specs/combat-result-cycle-finish-v1.md/schema/fixture/oracle; native result-settlement-v2 and
combat-reserve-release-v1 contracts as needed. Target32source contexts/32native base literals/
64native event literals/96native recovery cuts. Source must be fully replay-derived from native
history; settled World retained. Wrapper state hashes are not native Release state expectations.

## Explicit Exclusions
Full wrapper bridge/cycle advancement, Movement certificates/execution, actual positive Reserve
lineage, public APIs, Snapshot/publication and parent017 acceptance. Upstream boundary is synthetic.
No CCE/context_search/session_recall/memory/prior-review access; restriction supersedes retrieval
instructions. No source edits, commits, subagents or extra review instances/workstreams. Not alone;
preserve concurrent integration work. Own only task017b-review2-preliminary.md and
 task017b-review2-report.md in integration .planning/combat-task008-delivery; logs in own clone/tmp.

## Verification Commands Available To Reviewer
Use MTP --project/--solution, login:false. Restore/IPC escalation as needed. Repeated --filter-class
for focused suites; Boundary=UserSpace is Core trait. No global build-server shutdown; close owned
commands. Run proportionate focused checks. Reviewer3 independently rebuilds full solution.

## Author Explanation Location Or Delivery Step
Integration .planning/combat-task008-delivery/task017b-author.md, only AFTER preliminary ledger saved.
Use $independent-review in reviewer mode. Review instance 2 of3. Work from neutral bootstrap first,
record preliminary review before author explanation; then verify explanation against code and plan,
run proportionate non-mutating checks and return evidence-backed verdict and exact command results.
Do not implement fixes, create further reviews or split into new workstreams.

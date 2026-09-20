# Fresh Review Bootstrap

Review instance: 2 of 3. Resumed 2026-09-20; interrupted prior round1 does not count.

## Review Objective
Review Task015 custody implementation and plan against canonical requirements. Return an independent evidence-backed verdict; do not presume readiness.

## Repository And Worktree
Integration worktree `/Users/dsteele/.codex/worktrees/8fcc/sandtable`. Review in a separate clone under `/tmp`, with a fresh context and no inherited conversation. Original `/Users/dsteele/repos/sandtable` checkout is read-only and excluded from review execution.

## Base, Head, Branch, And Dirty State
Immutable review base `d00a230`, candidate `00a8b68c580a7efc38070571750c9a973e0fa579`. Integration branch `codex/combat-custody-closure-session` starts at refreshed main `ba54efd720b38627b473cfd5d2e2ba4ee638d6b2`. Source/test tree matches candidate; verify before review. Integration-only evidence updates are outside candidate diff. Review clone must be detached at candidate.

## In-Scope Commits And Paths
Reconstruct complete `d00a230..00a8b68` diff, including candidate documentation. Seven physical source/test paths:

- `src/Cna.Core/Campaigns/CampaignCombatResolution.cs`
- `src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs`
- `src/Cna.Core/Campaigns/CampaignCombatObligations.cs`
- `src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs`

## Canonical Requirements And Plan
Read AGENTS.md; `docs/design/combat-cycle-implementation-plan.md` Task015/checkpointG; `docs/specs/combat-result-settlement-v2.md` and corresponding schema, fixture and oracle; relevant World settlement requirements. Inspect tests before implementation and inspect smallest relevant context.

## Explicit Exclusions
No source fixes, commits, further reviews, or subagents. Task016 relationship/round closure is excluded. Public activation, actual positive creation-rooted history, Snapshot successor and durable host publication remain open. Do not read prior reviews, execution history, aggregate evidence or author/worker/dev rationale before blind ledger. Do not call ANY CCE/context_search/session_recall or cross-session memory tools; this explicit independence boundary supersedes repository retrieval instructions.

## Verification Commands Available To Reviewer
.NET is cleared in the isolated clone. Use native MTP `--project`/`--solution`, login:false and unique binlogs. Restore/build as required; run proportionate focused C# checks and report exact commands/results. Reviewer3 must independently rebuild. Close all owned processes. Write only own preliminary ledger/report to integration `.planning/combat-task008-delivery/task015-review2-{preliminary,report}.md`; generated checks/logs stay in clone or `/tmp`.

## Author Explanation Location Or Delivery Step
First persist blind preliminary ledger. Then read candidate `.planning/combat-task008-delivery/task015-author.md` and verification/worker evidence, treating claims as testimony. Reconcile stale pending statuses using supplied evidence only after blind assessment; never infer a pass from author prose.

Use $independent-review in reviewer mode. This is review instance 2 of 3. Work from the Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

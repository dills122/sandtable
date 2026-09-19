# Fresh Review Bootstrap

Review instance: **2 of 3**. User reserves one additional final round only if blockers remain after
round3 and one bounded research/experiment is performed; do not dispatch further review instances.

## Review objective

Independent engineering review of Task008 A1c implementation and plan. Reconstruct requirements
from canonical files; record preliminary findings before reading author explanation.

## Repository and worktree

`/Users/dsteele/repos/sandtable`, branch `codex/combat-task008-creation-binding`.
Applicable root AGENTS.md and skill instructions apply. CCE available but some queries stale;
use focused local fallback if retrieval/expansion fails.

## Base, head and dirty boundary

Base and HEAD: `e64bed9` (merged PR #123). Target is explicit uncommitted working-tree delta.
Include untracked new source/test files. No commits or staging needed for review.

## In-scope paths

- `src/Cna.Core/Campaigns/CampaignCombatCreationRequest.cs`
- `src/Cna.Core/Campaigns/CampaignCreatedV11.cs`
- `src/Cna.Core/Campaigns/CampaignCreatedV11Serializer.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatCreationBindingTests.cs`
- `docs/design/combat-cycle-implementation-plan.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `README.md`, `tech-design.md`, `naming-overview.md`

Local `.planning/combat-task008-delivery/` is intentionally ignored execution/review evidence;
it is present and not hidden dirt. Read author/evidence only after preliminary review.

## Canonical requirements and plan

- `docs/design/combat-cycle-implementation-plan.md`, Task008 A1c execution row.
- `docs/specs/combat-authority-envelope-v1.md`, schema and fixture alongside it.
- `docs/specs/verify-combat-authority-envelope-v1.py`.
- Predecessor Setup7/configuration/World7 contracts linked from those files.

## Exclusions

A2 Snapshot12, inherited handlers/restore, provider selection and publication implementation,
gameplay and public activation. Do not treat contract-only oracle as runtime replay proof.

## Verification commands

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationBindingTests' '-bl:/tmp/a1c-review2-{}.binlog'`

`python3 docs/specs/verify-combat-authority-envelope-v1.py`

`git diff --check`

Read-only review; proportionate tests allowed. Do not modify implementation, stage, commit, or spawn.
Write only `a1c-review2-report.md` in this planning directory, including preliminary findings before
author reconciliation and final severity-backed verdict. Return findings to lead before next round.

## Author explanation location

After preliminary pass, read `.planning/combat-task008-delivery/a1c-author.md` and
`a1c-evidence.md`. Verify claims, not just tests. Review both code and plan.

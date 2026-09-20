# Fresh Review Bootstrap

Review instance: **2 of 3** for B1. Only initiating lead may invoke one final fourth review after
remaining round3 blockers and one bounded experiment. Do not initiate additional reviews.

## Objective and repository
Review Task008 B1 implementation and plan independently in /Users/dsteele/repos/sandtable.
Branch codex/combat-task008-opening-preamble; base/HEAD b67b4f9 (accepted A2). Explicit working-tree
boundary includes untracked source and tests. Read applicable AGENTS and independent-review skill.
Inspect requirements/tests/code and record preliminary ledger BEFORE author explanation.

## Scope
src/Cna.Core/Campaigns/CampaignOpeningPreamble.cs, CampaignOpeningPreambleCodec.cs,
CampaignOpeningPreambleModels.cs; tests/Cna.Core.Tests/Campaigns/CombatOpeningPreambleTests.cs;
tests/Cna.Core.Tests/Cna.Core.Tests.csproj; README.md, tech-design.md, naming-overview.md;
docs/design/combat-cycle-implementation-plan.md; docs/roadmap/pre-alpha-roadmap.md;
.planning/combat-task008-delivery execution/B1 evidence. Do not read prior review reports.

## Canonical requirements
Task008 execution index; docs/specs/combat-opening-preamble-v1.md and frozen fixture/schema/verifier;
combat-authority-envelope-v1.md, combat-cycle-sequence-v1.md; combat-weather-v1.md and
combat-stage-entry-v1.md for dependency boundaries (locate exact Weather name if different).

## Exclusions
Weather implementation, stage-entry, generic noninitial Snapshot12, public activation, actual provider
publication or durability. Verify exclusions do not conceal a current B1 requirement.

## Verification
Focused: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatOpeningPreambleTests' '-bl:/tmp/b1-review2-{}.binlog'
Oracle: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-opening-preamble-v1.py
Lead full suite may run concurrently. No builds or code edits; no-build focused checks permitted.
Only write .planning/combat-task008-delivery/b1-review2-report.md. No staging, commits or delegation.

## Author explanation
After preliminary ledger read b1-author.md and b1-evidence.md in same planning directory.
Return findings, plan review, claim reconciliation, exact checks, residual risks and evidence-based
Ready / Ready with non-blocking follow-ups / Not ready / Unable to verify verdict.

# Fresh Review Bootstrap

Review instance: **1 of 3** for Task008 B2 stage-entry. Only lead may invoke conditional fourth
review after unresolved round3 blockers and one bounded experiment. No reviewer delegation.

## Objective and target
Independent implementation AND plan review in /Users/dsteele/repos/sandtable.
Branch codex/combat-task008-stage-entry; base/HEADa0babdb (accepted C Weather PR127).
Explicit working-tree delta includes untracked source/tests. Read AGENTS and independent-review
skill; inspect canonical requirements/tests/code and record preliminary ledger BEFORE author.

## Scope
src/Cna.Core/Campaigns/CampaignCombatStageEntryModels.cs, CampaignCombatStageEntry.cs,
CampaignCombatStageEntryCodec.cs; tests/Cna.Core.Tests/Campaigns/CombatStageEntryTests.cs;
tests/Cna.Core.Tests/Cna.Core.Tests.csproj; README.md, tech-design.md, naming-overview.md;
docs/design/combat-cycle-implementation-plan.md; docs/roadmap/pre-alpha-roadmap.md;
.planning/combat-task008-delivery execution/B2 evidence. Prior accepted slices are base context.
Do not read prior review reports.

## Canonical requirements
Task008 B/B2 execution rows; docs/specs/combat-stage-entry-v1.md/schema/fixture/verifier;
combat-weather-v1.md; combat-opening-preamble-v1.md; combat-authority-envelope-v1.md;
combat-cycle-sequence-v1.md. Existing StageEntry policy/sources and sequence catalog unchanged.

## Exclusions
Positive obligations, Reserve execution/firstcycle opening, generic Snapshot12, public activation,
provider publication/durability. Verify exclusions do not conceal B2 requirements. Parent B includes
B1+B2 only; parent008/H remain larger obligations.

## Verification
Focused: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStageEntryTests' '-bl:/tmp/b2-review1-{}.binlog'
Oracle: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-stage-entry-v1.py
No builds while lead integration may run. Only write b2-review1-report.md in planning directory;
no source/doc edits, staging, commits or additional agents. Focused no-build checks permitted.

## Author explanation
After preliminary ledger, read b2-author.md and b2-evidence.md. Return findings, plan assessment,
claim reconciliation, exact checks/residual risk and evidence-backed Ready / Ready with non-blocking
follow-ups / Not ready / Unable to verify verdict.

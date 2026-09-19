# Fresh Review Bootstrap

Review instance: **1 of 3** for Task008 C Weather. Only lead may invoke conditional fourth review
after unresolved round3 blockers and one bounded experiment. Do not start additional reviewers.

## Objective and target
Independent implementation and plan review in /Users/dsteele/repos/sandtable.
Branch codex/combat-task008-weather; base/HEAD22f8da6 (accepted B1 PR126). Explicit working-tree
delta includes untracked source/tests. Read AGENTS and independent-review skill. Inspect canonical
requirements/tests/code and record preliminary findings BEFORE reading author explanation.

## Scope
src/Cna.Core/Campaigns/CampaignCombatWeatherModels.cs, CampaignCombatWeather.cs,
CampaignCombatWeatherCodec.cs; tests/Cna.Core.Tests/Campaigns/CombatWeatherTests.cs;
tests/Cna.Core.Tests/Cna.Core.Tests.csproj; README.md, tech-design.md, naming-overview.md;
docs/design/combat-cycle-implementation-plan.md; docs/roadmap/pre-alpha-roadmap.md;
.planning/combat-task008-delivery execution and C evidence. Prior accepted slices are base context,
not new diff. Do not read previous independent reports.

## Canonical requirements
Task008 C row; docs/specs/combat-weather-v1.md/schema/fixtures/verifier;
combat-opening-preamble-v1.md; combat-authority-envelope-v1.md; combat-cycle-sequence-v1.md;
combat-stage-entry-v1.md for next-family handoff. Existing Weather Rules artifact and RNG are
unchanged authority; verify source/artifact binding and genuine predecessor reconstruction.

## Exclusions
Stage-entry/Reserve, positive immediate effects, generic noninitial Snapshot12, public activation,
provider publication/durability. Evaluate whether exclusions conceal an actual C requirement.

## Verification
Focused: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatWeatherTests' '-bl:/tmp/c-review1-{}.binlog'
Oracle: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-weather-v1.py
No builds while lead integration may run; no-build focused checks permitted. Only write
.planning/combat-task008-delivery/c-review1-report.md. No implementation/doc edits, staging,
commits, delegation or further reviews.

## Author explanation
After preliminary ledger, read c-author.md and c-evidence.md in same planning directory.
Return findings, plan assessment, claim reconciliation, exact checks, residual uncertainty and
Ready / Ready with non-blocking follow-ups / Not ready / Unable to verify verdict.

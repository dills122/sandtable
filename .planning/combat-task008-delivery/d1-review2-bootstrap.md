# Fresh Review Bootstrap

Review instance: **2 of 3** for Task008 D1 Reserve designation. Conditional one experiment/final
fourthreview belongs to lead only if blockers remain afterround3; do not delegate or start reviews.

## Objective and target
Independent implementation AND plan review in /Users/dsteele/repos/sandtable.
Branch codex/combat-task008-reserve-designation, base/HEAD3ded1eb (accepted B2 PR128).
Explicit working-tree delta includes untracked source/tests. Read AGENTS/independent-review skill,
canonical requirements/tests/code and preliminary ledger BEFORE author explanation. No priorreports.

## Scope
src/Cna.Core/Campaigns/CampaignCombatReserveModels.cs, CampaignCombatReserveDesignation.cs,
CampaignCombatReserveCodec.cs; tests/Cna.Core.Tests/Campaigns/CombatReserveDesignationTests.cs;
tests/Cna.Core.Tests/Cna.Core.Tests.csproj; README.md, tech-design.md, naming-overview.md;
docs/design/combat-cycle-implementation-plan.md; docs/roadmap/pre-alpha-roadmap.md;
.planning/combat-task008-delivery execution/D1scope/evidence. Predecessors remain base context.

## Canonical requirements
Task008 execution index D1/D2/019A refinement; docs/specs/combat-reserve-designation-v1.md/schema/
fixture/verifier, combat-stage-entry-v1.md, combat-reserve-release-v1.schema.json member/history shapes,
combat-inherited-successors-v1.md/schema and combat-cycle-sequence-v1.md for completion ownership.
Inspect bounded typed World mutation/serialization and complete real predecessor provenance.

## Exclusions and plan review
D1 only precompletion state10/optionaldesignation11, owner none→I. Completioncodec D2 then019A must
apply SAME completion2 event with atomiccycle1, no extraevent. Full terminalReserve/readback/retry
and Movement handoff pending019A. Generic World/Snapshot restore, publicactivation and provider
publication excluded. Verify execution split preserves all original contract requirements.

## Verification
Focused: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d1-review2-{}.binlog'
Unchanged oracle baseline: /tmp/d-reserve-baseline.log. Inspect canonical source+retained baseline;
rerun only if concern warrants: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-reserve-designation-v1.py
No builds while leadintegration may run. Only write d1-review2-report.md in planning directory;
no code/docs edits/staging/commits/delegation. Focused no-build checks permitted.

## Author explanation
After preliminary ledger read d1-author.md and d1-evidence.md. Return actionable findings, plan review,
claim reconciliation, exact verification/residual risk and evidence-based Ready / Ready with non-blocking
follow-ups / Not ready / Unable to verify verdict. Preserve review independence.

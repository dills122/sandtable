# Fresh Review Bootstrap
Review instance: 1 of 9; round1 maximum3, recovery0/2. Brain owns dispatch/counts.

## Review Objective
Review the additive actual Reaction-closure -> Movement lifecycle -> Breakdown -> certified positive Combat-entry contract and native consumer, implementation plan and tests against repository requirements. Read-only review.

## Repository And Worktree
/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable. Applicable AGENTS/steering apply. No shared writers permitted.

## Base, Head, Branch, And Dirty State
Base42503c570294e5e4823ff1995bbfdcadc298bf07. Product freeze d3332dbf89bfa378872330ccdd84beebb45b89af. Branch codex/actual-reaction-combat-entry.
Exact final review head is supplied in Brain's dispatch after text evidence commit; verify checkout equals it. Later review/evidence text commits do not alter product hashes in evidence JSON. Untracked .serena/ preserved unrelated. No product edits while target frozen.

## In-Scope Commits And Paths
1508764 behavioral RED; ec8a0a4 contract freeze; d3332db native/reference GREEN and docs; associated initial test/plan commit38887a9. Resolve complete base..dispatchhead diff.
Primary files: docs/specs/combat-actual-reaction-combat-entry-v1.{md,schema.json}; verify-combat-actual-reaction-combat-entry-v1.py; fixtures/combat-actual-reaction-combat-entry-v1.json; src/Cna.Core/Campaigns/CampaignCombatActualReactionCombatEntry.cs/Codec.cs; tests/Cna.Core.Tests/Campaigns/CombatActualReactionCombatEntryTests.cs/ContractTests.cs; exact fixture-copy csproj entry; README/tech-design/naming-overview; implementation plan and review/evidence artifacts.

## Canonical Requirements And Plan
docs/specs/combat-actual-reaction-combat-entry-v1.md/schema; docs/work/plans/2026-10-14-actual-reaction-combat-entry-implementation.md; AGENTS architecture/trust/determinism/contracts. Parent TASK017-019 and full72 remain distinct requirements; scope does not close them.

## Explicit Exclusions
Existing frozen predecessors/pins read-only. No generic composer, public/host/Exercise/Runner integration, selected paid/finish downstream joins, laterII/consumed/Engaged/capture/general seed support, all72 or firstrelease claim. Prior blocked rawarchive export remains out of ancestry/scope.

## Verification Commands Available To Reviewer
python3 docs/specs/verify-combat-actual-reaction-combat-entry-v1.py
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class 'Cna.Core.Tests.Campaigns.CombatActualReactionCombatEntry*' /bl:/private/tmp/reaction-entry-review-{}.binlog
Scoped format verify and git diff --check. Coordinate focused/heavy lease with Brain; no broad suite without grant. Native tests require .NET10/MTP/xunitv3. No mutation/nativefixture regeneration.

## Author Explanation Location Or Delivery Step
FIRST inspect requirements/code/tests and record preliminary concerns. THEN read docs/work/reviews/2026-10-14-reaction-combat-entry-author-explanation.md and self-review/evidence JSON as claims to verify.

Use independent-review in reviewer mode. Work from bootstrap first, record preliminary review before author explanation, then verify explanation/code/plan and run proportionate non-mutating checks. Return evidence-backed verdict. Do not implement fixes/create other review instances/split workstreams.

# Fresh Review Bootstrap

Review instance:2 of9 total N1 budget; round2 of3; recovery0 of2. Review1 is consumed. Brain dispatch assigns and tracks this fresh instance.

## Review objective
Review the actual moved-assault successor and its implementation plan against the canonical requirements. Establish repository truth before reading the author explanation; do not infer readiness from test claims.

## Repository and worktree
Repository:dills122/sandtable. Author worktree:/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable. Branch:codex/actual-moved-assault. A separate read-only worktree at the frozen product commit may be used by the coordinator.

## Base, head, branch and dirty state
Base:f59ffcdbcbcadfbdb20b7e7f8796d477533b792f. Frozen product head:3bcaedc74a184a2adc28ad6e722787304c04e214. Final published review head is supplied by Brain dispatch; any later commit before dispatch may contain only the six review packet/report/disposition paths named below, and no product change. Verify actual Git state/range and stop if that condition does not hold. Unrelated untracked .serena/ is outside scope and preserved.

## In-scope commits and paths
Review the full base-to-product range, including genuine missing-reader tests4819927/a33c944 and contract freeze5a4482e. Product paths: the four docs/specs/combat-actual-moved-assault-v1 artifacts (contract, schema, verifier, fixtures); CampaignCombatActualMovedAssault.cs/Codec.cs; four CombatActualMovedAssault test files; fixture-copy entry in Cna.Core.Tests.csproj; README.md,tech-design.md,naming-overview.md,docs/design/combat-cycle-implementation-plan.md; docs/work/plans/2026-10-14-actual-moved-assault-implementation.md. Metadata paths: this bootstrap, separate author explanation, self-review, evidence JSON, immutable review1 report and its disposition in docs/work/reviews with the same date/slug.

## Canonical requirements and plan
Read applicable AGENTS.md, docs/specs/combat-actual-moved-assault-v1.md and .schema.json, and docs/work/plans/2026-10-14-actual-moved-assault-implementation.md. The correction since prior review target5a0dbc82e7046cd9dc4a8f42a2fc48913099fc75 is confined to theory row identity; verify actual full scope and delta independently. Read predecessor requirements only as surrounding context; predecessor implementations and their frozen hashes were not authorized for modification.

## Explicit exclusions
No PAC/Release continuation/finish/public/host/Exercise/Runner/general seed delivery. Isolated catalogue support is not new authenticated history. Blocked raw archive27d8293, external diagnostics and I01 metadata-preservation branch are excluded. Do not implement fixes, mutate worktrees or create further review instances/workstreams.

## Verification commands available to reviewer
Python:python3 docs/specs/verify-combat-actual-moved-assault-v1.py.
Native:dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class 'Cna.Core.Tests.Campaigns.CombatActualMovedAssault*' /bl:/private/tmp/n1-review-{}.binlog.
Formatting:dotnet format Sandtable.slnx --verify-no-changes --no-restore --include <the two new native files and four new test files>.
Diff:git diff --check f59ffcdbcbcadfbdb20b7e7f8796d477533b792f..3bcaedc74a184a2adc28ad6e722787304c04e214.
Use a coordinator-granted execution lease for native checks; do not start an unleased full suite.

## Author explanation location or delivery step
After inspecting implementation/tests and recording preliminary concerns, read 2026-10-14-actual-moved-assault-author.md, 2026-10-14-actual-moved-assault-self-review.md, 2026-10-14-actual-moved-assault-evidence.json and the separate review1 report/disposition.

Use $independent-review in reviewer mode. This is review instance2 of9, at most3 in this round. Work from the Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both implementation and plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

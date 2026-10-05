# Fresh Review Bootstrap

Review instance:1 of3. Use independent-review in reviewer mode. Read-only.

## Review objective

Assess CMB-019D1 native implementation and frozen manifest against019D0. Establish facts from
requirements/tests/code before any author explanation. Record preliminary concerns first.

## Repository and worktree

/Users/dsteele/.codex/worktrees/native-settled-continuation/sandtable
Latest user policy: /Users/dsteele/repos/sandtable/AGENTS.md (Codebase Memory supersedes older
committed CCE policy). Read Serena manual; activate exact worktree and verify retrieval.
Unique graph sandtable-cmb019d1; call index_status/check_index_coverage before structural use,
source fallback for new untracked paths. Do not index as main sandtable.

## Base, head, branch and dirty state

Base and current HEAD:f33c78cdb4aa44c694e0baf3aeb963d98819ce7b.
Branch:codex/native-settled-continuation. Explicit working-tree boundary; no commits yet.
New engine/codec/tests are untracked. Modified test project and canonical plan.
Administrative README, tech-design, naming-overview, roadmap, movement delivery plan and contract
implementation-status prose. This bootstrap and later author/report/handoff are administrative.
Untracked .serena tooling is excluded. Inspect actual status/diff; do not ignore untracked source.

## In-scope paths

src/Cna.Core/Campaigns/CampaignCombatSettledContinuation.cs
src/Cna.Core/Campaigns/CampaignCombatSettledContinuationCodec.cs
tests/Cna.Core.Tests/Campaigns/CombatSettledContinuationTests.cs
tests/Cna.Core.Tests/Cna.Core.Tests.csproj
docs/design/combat-cycle-implementation-plan.md (Task019D1)
Administrative documentation paths above are allowed. All upstream engines and frozen
schema/fixture/oracle files must remain unchanged.

## Canonical requirements and plan

docs/specs/combat-settled-continuation-v1.md and its ordered schema, fixture and oracle.
docs/design/combat-cycle-post-movement-dispatch.md (019D0 and following native-proof slice).
docs/design/combat-cycle-implementation-plan.md (Task019D1 manifest and Checkpoint H).
docs/design/combat-cycle-movement-delivery-plan.md; docs/roadmap/pre-alpha-roadmap.md.

## Explicit exclusions

No public/transport/Snapshot registration, new gameplay predecessor, parent017–019 closure,
settled control, spend/repeat/exception authority, actual creation-rooted positive Combat history,
later-II/consumed expansion. Synthetic earlier Movement provenance remains explicit.

## Verification commands

Native .NET10 MTP/xUnit v3:
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatSettledContinuationTests' /bl:/private/tmp/cmb019d1-review-focused-{}.binlog
Proportionate non-mutating verification allowed outside sandbox as needed for worktree/build files.
Author full just check and seven oracle gate logs are under /private/tmp/cmb019d1-*.
Do not execute concurrent full solution rebuilds; author gate is running. Focused --no-build is
available after author build completion; ask parent before independent rebuilding if needed.

## Author explanation delivery

Not included in this bootstrap. Send preliminary findings to parent first; parent then supplies
separate author packet. Return evidence-backed implementation AND plan verdict. Do not implement
fixes, create further review instances or split into workstreams. Max3 instances total.

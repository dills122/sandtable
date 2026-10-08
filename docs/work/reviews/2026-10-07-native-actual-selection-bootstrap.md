# Fresh Review Bootstrap

## Review Objective

Independently review S4 / Task019F1 / REL-AUD-01 private native actual selection against
the frozen contract, accepted research and canonical Combat plan. Inspect code/tests first,
record a preliminary review, then read the separately stored author explanation.

Review instance:1of9 proposed next S4 instance (set1/pass1); dispatch and ledger belong to
the coordinator. No independent reviewer or verdict was created by the implementation task.
Configured flow is bounded3sets×3, with coordinator research recovery between failed sets.

## Repository And Worktree

Repository/worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
Use this exact existing checkout; no new worktree. Branch: `codex/native-actual-selection`.
Activate this exact checkout with Serena and read its manual before coding-tool use. Read
applicable AGENTS and use codebase-memory status/coverage; stale/missing graph evidence must
fall back to focused source reads. Review remains read-only and coordinator-owned.

## Base, Head, Branch, And Dirty State

Base: `1dcbb5e6f27437ad17a98a732e31748d2b493fd7` (merged PR164).
Implementation/review head: `458e49229e29315ff13e52035d2a1b168b79a752`.
Branch: `codex/native-actual-selection`.
Implementation is committed; the following administrative-only commit adds this bootstrap,
the separate author explanation and handoff. Runtime/project/plan bytes must still match the
implementation head. Final branch head and clean-state evidence are returned in the task's final
message; packet pins implementation head explicitly to avoid a self-referential commit hash.
Verify `git status --short`, `git diff 1dcbb5e6f27437ad17a98a732e31748d2b493fd7..458e49229e29315ff13e52035d2a1b168b79a752 --name-only` and the later
administrative-only diff before review. No PR/publication/CI/merge is claimed.

## In-Scope Commits And Paths

Implementation commit: `458e49229e29315ff13e52035d2a1b168b79a752`. Five primary paths only:

- `src/Cna.Core/Campaigns/CampaignCombatActualSelection.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualSelectionCodec.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatActualSelectionTests.cs`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` (fixture link only)
- `docs/design/combat-cycle-implementation-plan.md` (Task019F1 addition)

Following dated administrative paths are review metadata, not runtime/spec changes:

- `docs/work/reviews/2026-10-07-native-actual-selection-bootstrap.md`
- `docs/work/reviews/2026-10-07-native-actual-selection-author.md`
- `docs/work/handoffs/2026-10-07-native-actual-selection.md`

File SHA256 manifest and exact gate evidence are in the handoff.

## Canonical Requirements And Plan

Read these from the verified checkout, not from the author's testimony:

- `docs/specs/combat-actual-selection-v1.md`
- `docs/specs/combat-actual-selection-v1.schema.json`
- `docs/specs/fixtures/combat-actual-selection-v1.json`
- `docs/specs/verify-combat-actual-selection-v1.py`
- `docs/research/combat-actual-selection-bridge-feasibility.md`
- `docs/work/plans/2026-10-05-actual-selection-dependency-disposition.md`
- `docs/design/combat-cycle-implementation-plan.md` (final prior reconciliation and Task019F1)

Two original actual owners; positive FA20 after decline19 without FA completion; seven fallback
variants per owner to Reserve Release; full source/proof ownership, separate caller-trusted
ledger, all frozen16 dependencies, byte parity/cuts/retries/canonical grammar/capacity and
systematic combined-error precedence. No mutable sharing or semantic causal reordering.

## Explicit Exclusions

No old contract/schema/fixture/pin/runtime-admission edits; no synthetic C3a/Round2/Result2
promotion; no actual round/result/all32 reachability, later-II/consumed/repeat, full Snapshot,
outward/public/host/AI/UI/transport, Archives restart, Maproom/Runner activation or parent017–019
completion. No production seat/clock/store authentication. No new worktree, automation, auth
retry loop, publication or merge. Historical pin failures remain failures, never waivers.

## Verification Commands Available To Reviewer

Use proportionate non-mutating verification; the handoff retains exact completed commands,
statuses, timings, stdout hashes and binlogs for focused/Boundary/build/full/format and unchanged
oracles. Full-suite lease was granted to this implementation task; coordinate any new full-suite
execution. Repository is .NET10 native MTP/xUnit v3: use `--project`/`--solution`, MTP filters and
unique `/bl` for MSBuild-based calls. No fixture regeneration or predecessor pin maintenance.

## Author Explanation Location Or Delivery Step

After recording a preliminary code/plan review, read
`docs/work/reviews/2026-10-07-native-actual-selection-author.md`.
Operational evidence is in `docs/work/handoffs/2026-10-07-native-actual-selection.md`.

Use $independent-review in reviewer mode. This is review instance1of9, set1/pass1, unless the
coordinator supplies a later verified ledger position. Work from the Fresh Review Bootstrap
first and record a preliminary review before reading the Author Explanation. Then verify the
explanation against the repository, review both implementation and plan, run proportionate
non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create
further review instances, split workstreams, publish or merge. Return findings to coordinator;
only coordinator owns reconciliation, recovery and subsequent bounded review dispatch.

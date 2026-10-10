# Fresh Review Bootstrap — native actual round entry

Review instance: 1 of 9, to be assigned by Brain. Cumulative implementation reviews remain 0 of 9; recovery spikes 0 of 2. Diagnostic research reviews are separate.

## Review objective

Review the private native implementation of the merged `combat-actual-round-entry-v1` contract and Task019F3 of its implementation plan. Establish scope and acceptance from repository evidence before consulting author testimony. Author self-review is complete; required Release CI has not passed.

## Repository and worktree

Repository: `dills122/sandtable`.
Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
Branch: `codex/native-actual-round-entry`.
Draft: [PR169](https://github.com/dills122/sandtable/pull/169).

## Base, head, branch and dirty state

Base: `34a494c46ab7b8665e381f9e531c7b8365cb3efe` (merged PR167).
Frozen primary target: `64439f79356fd6303408e6a9b3579e530235b9d6`.
The accompanying freeze message supplies the later administrative commit head. Verify that all five primary paths still match the [primary hash manifest](2026-10-10-native-actual-round-entry-primary-hashes.json) before reviewing that head.
Only `.serena/` is excluded untracked tool state. Dated review/handoff files are administrative additions. Verify actual status and the complete base-to-head diff; do not silently review another checkout or mutable branch tip.

## In-scope commits and paths

Primary implementation checkpoints are `a37fbd8ea0fa9a138c369eea1d55929bee41b951`, `f836a04c7314dd6fdb028e7bc4f077dac6a39a98` and the frozen target above. Resolve the entire base-to-target diff, not only the latest commit.

- `src/Cna.Core/Campaigns/CampaignCombatActualRoundEntry.cs`
- `src/Cna.Core/Campaigns/CampaignCombatActualRoundEntryCodec.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatActualRoundEntryTests.cs`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` (fixture content link)
- `docs/design/combat-cycle-implementation-plan.md` (Task019F3)

The primary plan retains checkpoint-time pending statements. The later completed outcomes below and in the handoff supplement those historical statements without editing the frozen primary target.

## Canonical requirements and plan

Read `AGENTS.md`, [contract](../../specs/combat-actual-round-entry-v1.md), [schema](../../specs/combat-actual-round-entry-v1.schema.json), [fixture](../../specs/fixtures/combat-actual-round-entry-v1.json), [verifier](../../specs/verify-combat-actual-round-entry-v1.py) and [Task019F3](../../design/combat-cycle-implementation-plan.md#task019f3--private-native-actual-round-entry-rel-aud-02c--day-a).
Consult original actual-selection requirements and implementation as needed. All four round-entry artifacts and the 57 dependency pins are unchanged inputs. Their hashes are retained in the handoff and physical-pin manifest.

## Explicit exclusions

No predecessor/shared/transport/host/public activation, paid commitment/results/settlement, full Snapshot/Archives restart, repeated Movement, later-II/consumed lineage, Exercise/Runner activation or parent017–019 completion. No separate Base memo, cached Apply outcomes, timeout increase or test/assertion reductions. Four inherited Python failures remain unwaived. Brain owns independent review, next scope and merge.

## Verification commands and evidence available

Native MTP uses `--project`/`--solution`. Coordinate with Brain before any heavy .NET run; the author lease is idle, not automatically assigned to the reviewer.

```sh
dotnet build Sandtable.slnx --no-restore
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests --report-xunit-xml
dotnet test --solution Sandtable.slnx --no-build --report-xunit-xml
```

Use a unique binlog for any assigned build/test. Full retained logs/binlogs/XML are under `/private/tmp/native-round-entry-gates`.

- Native: 177/177 passed, zero failures/skips, 14m18.036s.
- Full Debug solution: 2770/2770 passed, zero failures/skips, 25m00.482s; Core2291, ExerciseRunner469, Contracts10.
- Build: zero warnings/errors. Format and 57 physical hashes: passed.
- Required [Release verify run38082003227](https://github.com/dills122/sandtable/actions/runs/38082003227): cancelled at its unchanged 15-minute limit. Core unfinished; report upload skipped and artifacts absent. Seven other PR checks succeeded. This gate remains unmet.

Original failures, earlier passes and timing caveats are in the separate evidence handoff. No passing claim applies to an unexecuted check.

## Author explanation location and delivery step

Record a preliminary review of requirements, tests, code and plan before reading `2026-10-10-native-actual-round-entry-author.md` in this directory. The author explanation is testimony, not a readiness recommendation.

Use independent-review in reviewer mode. This is review instance 1 of 9 when Brain dispatches it. Work from this bootstrap first and record preliminary concerns, then verify the separate author explanation against source and executed evidence. Review both implementation and plan with proportionate non-mutating checks. Return one evidence-backed verdict. Do not implement fixes, dispatch further reviewers, split work or reset the counters. Keep the unmet exact-head CI gate explicit.

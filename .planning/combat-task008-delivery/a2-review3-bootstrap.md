# Fresh Review Bootstrap

Review instance: **3 of 3** for A2. One final fourth round only if blockers remain after round3 and
one bounded research/experiment; reviewer must not dispatch further rounds.

## Review objective

Independent implementation and plan review of Task008 A2: creation-only Snapshot12 and publication
evidence boundary. Inspect requirements/tests/code before author explanation; record preliminary ledger.

## Repository and worktree

`/Users/dsteele/repos/sandtable`, branch `codex/combat-task008-creation-snapshot`.
Base/HEAD `a8eed35`, accepted A1c checkpoint/PR124. Explicit working-tree delta including untracked
source/tests. No staged edits expected. Applicable root AGENTS and independent-review skill apply.

## In-scope files

- `src/Cna.Core/Campaigns/CampaignCreationSnapshotV12.cs`
- `src/Cna.Core/Campaigns/CampaignCreationSnapshotV12Codec.cs`
- `tests/Cna.Core.Tests/Campaigns/CombatCreationSnapshotTests.cs`
- `docs/specs/combat-authority-envelope-v1.md`
- `docs/design/combat-cycle-implementation-plan.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `README.md`, `tech-design.md`, `naming-overview.md`
- Execution/evidence in `.planning/combat-task008-delivery/` (some ignored, intentionally present).

## Canonical requirements

Task008 A2 row; C2 `combat-authority-envelope-v1.md`, its schema/fixture/verifier; D1
`combat-cycle-sequence-v1.md` and verifier; `docs/research/orleans-publication-feasibility.md`.
Use Git base to distinguish pre-existing requirements from proposed documentation changes.

## Exclusions

Noninitial causal readers, inherited event families, public registration, actual provider/publication
implementation, storage selection and full parent Task008 acceptance. Do evaluate documentation
against those remaining requirements; exclusions cannot waive a requirement.

## Available verification

`dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCreationSnapshotTests' '-bl:/tmp/a2-review3-{}.binlog'`

`PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-authority-envelope-v1.py`

`git diff --check`

Read-only except own `a2-review3-report.md` in planning directory. Do not modify implementation/docs,
stage/commit, spawn or start other reviews. Proportionate no-build focused checks permitted;
coordinate any build because lead integration may be running. Do not read prior review reports.

## Author explanation

After preliminary pass, read `a2-author.md` and `a2-evidence.md` in same planning directory.
Reconcile claims and return findings, plan assessment, exact tests, residual risks and verdict.

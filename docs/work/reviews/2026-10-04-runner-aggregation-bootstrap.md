# Fresh Review Bootstrap

Review instance: set 1/pass 1, total 1 of maximum 9 across 3 sets (at most 3 passes per set; recovery requires coordinator). No self-reset or reviewer spawning.

## Review Objective

Review the R2 bounded Runner aggregation diagnosis and its plan against repository source, retained historical evidence and overnight authorization. Determine whether conclusions and proposed next gate are supported. Do a blind preliminary pass before reading the separate author explanation.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/runner-aggregation-research/sandtable`.
Read latest primary AGENTS.md and overnight session-plan/run-state for coordination only. Reviewer read-only; no fullsuite/heavy process without coordinator lease.

## Base, Head, Branch, And Dirty State

Branch `codex/runner-aggregation-research`; base and prepublication HEAD `907f41403ed159d65024f193f3e1f730a23b9bbb`.
Working-tree review boundary: four untracked Markdown paths listed below. Local `.serena/` tooling is untracked and excluded; no source/test/config changes. Inspect actual status and hashes; reject scope drift.

## In-Scope Commits And Paths

No implementation commits. These four retained working-tree files:

- `docs/research/runner-aggregation-failure.md`
- `docs/work/reviews/2026-10-04-runner-aggregation-bootstrap.md`
- `docs/work/reviews/2026-10-04-runner-aggregation-author.md`
- `docs/work/handoffs/2026-10-04-runner-aggregation-research.md`

A separate final reviewer report may be retained after judgment. Publication metadata updates must not alter reviewed research conclusions without reconciliation.

## Canonical Requirements And Plan

R2 section of `/Users/dsteele/repos/sandtable/.planning/combat-overnight/session-plan.md` (ignored execution ledger); AGENTS.md architecture/reliability/branch/quality rules.
Historical source checkpoint `e8fbaefd9c73edb198103ef1b4e9423ec89befe1`, handoff/review at `docs/work/{handoffs,reviews}/2026-10-04-native-settled-continuation*.md`.
Evidence logs `/private/tmp/cmb019d1-just-check-final.log`, `/private/tmp/cmb019d1-runner-repro.log`, `/private/tmp/cmb019d1-just-check-repeat.log`.
No new product truth, architecture change or executable fix is authorized.

## Explicit Exclusions

Production/test/fixture/oracle/config changes; primary or other-worktree edits; fullsuite reruns/stress loops; retry/timeout/assertion weakening; new behavior; claims that a passing rerun diagnoses cause. Do not introduce a fix, change scope, or spawn another reviewer.

## Verification Commands Available To Reviewer

`git rev-parse HEAD`; `git status --short`; `git diff --check`; `git diff e8fbaefd9c73edb198103ef1b4e9423ec89befe1 HEAD -- src/Cna.Core src/Cna.ExerciseRunner tests/Cna.ExerciseRunner.Tests scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json`.
Read bounded report and cited source symbols, retained logs and hashes. Validate relative Markdown links. New fullsuite is disproportionate to this prose-only scope. If a temporary control was run, inspect exact command/build/binlog/input/hash/result evidence in the report; never treat it as historical reproduction.

## Author Explanation Location Or Delivery Step

Only after preliminary concerns/ledger: `docs/work/reviews/2026-10-04-runner-aggregation-author.md`.

Use $independent-review in reviewer mode. This is review instance 1 of maximum 9 under the approved three-set state machine, currently set 1/pass 1. Work from the Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

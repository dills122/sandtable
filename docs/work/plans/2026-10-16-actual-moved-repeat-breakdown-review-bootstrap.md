# Fresh Review Bootstrap

Review instance: 1 of 3.

## Review Objective

Review the provisional, private authentic R02 moved-repeat Breakdown completion contract and its plan for correctness, authority preservation, bounds, adversarial coverage and readiness to freeze for a native consumer. This is not a public Combat, full72, Runner or release-candidate review.

## Repository And Worktree

Repository: `/Users/dsteele/repos/sandtable`.
Review worktree: `/Users/dsteele/.codex/worktrees/ci-runtime-optimization/sandtable`.

## Base, Head, Branch, And Dirty State

Branch: `codex/combat-moved-repeat-breakdown`, base `5d0417b150a7b022bf79ea3ecfc3dfe3fa2d1c5d`, WIP head recorded by `git rev-parse HEAD` in the review worktree. The proposed change consists of the nine files named below plus this bootstrap in one labelled WIP commit. `.serena/` remains untracked local tool state and is excluded. The primary checkout is dirty and behind main; do not use it as the review tree. Verify Git status afresh before reviewing.

## In-Scope Commits And Paths

Review the labelled WIP commit against the base. Its in-scope paths are:

- `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown.md`
- `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-author-review.md`
- `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.md`
- `docs/specs/combat-actual-moved-repeat-breakdown-completion-v1.schema.json`
- `docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py`
- `docs/specs/test_combat_actual_moved_repeat_breakdown_completion_v1.py`
- `docs/specs/verify-combat-actual-moved-repeat-breakdown-groups-v1.py`
- `docs/specs/verify-combat-actual-moved-repeat-breakdown-physical-v1.py`
- `docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py`

This bootstrap is the tenth committed review-artifact path, not part of the executable contract.

## Canonical Requirements And Plan

Read `AGENTS.md`; `docs/specs/combat-actual-moved-repeat-movement-completion-v1.md` and its schema/verifier; the separate `docs/specs/combat-actual-repeated-breakdown-completion-v1.md` as a semantic precedent; and the in-scope plan/spec above. The new contract must consume authentic R02 terminal lineage and eleven independent ledgers, preserve inherited authority, make one adjacent System transition, reject unsupported/nonterminal states and all missing physical material, and remain fail-closed while provisional.

## Explicit Exclusions

No native implementation, public admission, Runner integration, full72 closure, RC claim, CI performance claim, `.serena/` state, old B02 lineage transformation, or publication of blocked raw archive `27d8293`.

## Verification Commands Available To Reviewer

From the review worktree:

```sh
python3 -B docs/specs/test_combat_actual_moved_repeat_breakdown_completion_v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-physical-v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-groups-v1.py --group paid-axis-m0
```

The complete 18-group local gate already ran sequentially and passed 300 disjoint rows in 711.221 seconds; rerun only if proportionate to the review. The physical gate passed 540 injected faults and the capacity script passed its provisional model. The exact command/output record is summarized in the author review. No hosted CI has run for this WIP candidate.

## Author Explanation Location Or Delivery Step

First inspect the plan, spec, schema, verifier and tests and record preliminary concerns. Then read `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-author-review.md` as author testimony and reconcile each material claim against the files and executed checks.

Use `$independent-review` in reviewer mode. This is review instance 1 of 3. Work from this Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

# Fresh Review Bootstrap

Review instance: 2 of 3.

## Review Objective

Independently review the revised provisional, private authentic R02 moved-repeat Breakdown completion contract and delivery plan for readiness to freeze its wire layout for a native private consumer. Judge the whole branch diff, including whether review-1 corrections are complete. Public readback and `closureFrozen` remain off.

## Repository And Worktree

Repository: `/Users/dsteele/repos/sandtable`. Exact review worktree: `/Users/dsteele/.codex/worktrees/ci-runtime-optimization/sandtable`.

## Base, Head, Branch, And Dirty State

Branch `codex/combat-moved-repeat-breakdown`; base `5d0417b150a7b022bf79ea3ecfc3dfe3fa2d1c5d`. The initiating chat will provide an exact committed head in its review request. Verify that `git rev-parse HEAD` matches it. `.serena/` is untracked local tooling and excluded. The primary checkout is dirty and stale; do not review it. Stop if the exact target cannot be verified.

## In-Scope Commits And Paths

Review all commits and paths in `base..HEAD`. The executable and human contract consists of the schema, verifier, focused/group/physical/capacity programs and spec under `docs/specs/combat-actual-moved-repeat-breakdown-*` and `docs/specs/verify-combat-actual-moved-repeat-breakdown-*`, plus the plan under `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown.md`. The review/bootstrap and disposition documents under the same plan prefix are context artifacts. Verify the actual path inventory from Git instead of assuming this list is exhaustive.

## Canonical Requirements And Plan

Read `AGENTS.md`, R02 moved-repeat Movement completion spec/schema/verifier, the separate B02 repeated Breakdown completion spec only as a semantic precedent, and the in-scope plan/spec. The candidate must consume genuine R02 terminal lineage with original CreationRequest and eleven independent ledgers; emit exactly one adjacent System Breakdown event; preserve inherited authority; reject unsupported and missing material; and remain fail-closed while provisional. Wire-layout freeze precedes native private implementation; `closureFrozen` requires later native capacity/semantics and exact-head gates.

## Explicit Exclusions

No native implementation, public Combat admission, Runner, full72, RC claim, hosted CI claim, `.serena/` state, transformation of B02 lineage, or publication of blocked raw archive `27d8293`.

## Verification Commands Available To Reviewer

From the exact worktree:

```sh
python3 -B docs/specs/test_combat_actual_moved_repeat_breakdown_completion_v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-physical-v1.py
python3 -B docs/specs/verify-combat-actual-moved-repeat-breakdown-groups-v1.py --group paid-axis-m0
git diff --check 5d0417b150a7b022bf79ea3ecfc3dfe3fa2d1c5d HEAD
```

The initiating chat's exact-head test evidence will be in the updated plan and author packet. The complete 18-group run takes about twelve local minutes; use proportionate non-mutating checks and distinguish execution from inspected historical evidence.

## Author Explanation Location Or Delivery Step

First inspect implementation, tests, contract and plan independently and record preliminary concerns. Then read `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-author-review.md` and `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-review-1-disposition.md` as author testimony. Reconcile the author claims against source and checks.

Use `$independent-review` in reviewer mode. This is review instance 2 of 3. Work from this Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

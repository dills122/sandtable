# Fresh Review Bootstrap

Review instance: **10 of 10**. The user explicitly authorized one new read-only capacity review after the prior nine-review milestone limit. Review the capacity research addition; do not treat this as a release or closure review.

## Review Objective

Assess whether the provisional moved Assault event-size calculator is mathematically conservative for every **reachable** paid/armed R02-to-Breakdown ancestry under the pinned content pack, and whether the next experiment addresses the remaining native replay memory and latency gate. Report incorrect cardinality assumptions, missing event variants, unsafe byte recurrences, and plan gaps. Keep `closureFrozen=false` and publication/merge blocked.

## Repository And Worktree

Repository: `/Users/dsteele/.codex/worktrees/ci-runtime-optimization/sandtable` on local host. Branch: `codex/combat-moved-repeat-breakdown`.

## Base, Head, Branch, And Dirty State

Base `60004a5`; review target `758ebc71b853cc0dffeb966e695536006fb053b0`. At handoff, tracked files were clean; these two review packets and `.serena/` are untracked and excluded from the code diff. Verify actual Git state before review.

## In-Scope Commits And Paths

Commit `758ebc7` modifies `docs/work/plans/2026-10-16-actual-moved-repeat-breakdown-capacity-evidence.md` and adds `docs/specs/verify-combat-actual-moved-repeat-breakdown-event-bound-v1.py` and `docs/specs/test_combat_actual_moved_repeat_breakdown_event_bound_v1.py`.

## Canonical Requirements And Plan

Start with `AGENTS.md`, the frozen `docs/specs/combat-actual-moved-assault-v1.schema.json` and five successor schemas named by the calculator, the pinned `docs/specs/fixtures/combat-content-v7.canonical.json`, and the capacity evidence packet. Inspect actual transition and codec code, especially `CampaignCombatActualMovedAssault`, `CampaignCombatActualPaidAssaultCodec`, `CampaignCombatActualReactionCombatEntry`, and `CampaignCombatCertification`. The source cap is 268,435,456 bytes. A byte model does not establish native resource safety.

## Explicit Exclusions

No runtime C#, schema, fixture, closure flag, hosted workflow, or public branch changed in this commit. Do not review or publish the blocked raw archive `27d8293`. Do not interpret prior review 9's private-checkpoint verdict as capacity approval.

## Verification Commands Available To Reviewer

`PYTHONDONTWRITEBYTECODE=1 python3 -m unittest docs/specs/test_combat_actual_moved_repeat_breakdown_event_bound_v1.py` passed 4 tests. `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-actual-moved-repeat-breakdown-event-bound-v1.py` reported a provisional 207,807,680-byte Source model. `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-actual-moved-repeat-breakdown-capacity-v1.py` passed its existing structural and sample checks. The focused native replay class completed 38/38 locally in 5m20s; it is fixture replay evidence, not near-bound or hosted resource proof.

## Author Explanation Location Or Delivery Step

Read `docs/work/reviews/2026-10-16-mrb-capacity-review10-author.md` **after** a blind first pass of the changed calculator/tests and a preliminary findings ledger.

Use `$independent-review` in reviewer mode. Work from this Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

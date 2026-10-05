# Fresh Review Bootstrap

Review instance: N4 set1/pass1,total1of9; maximum3passes per set. This is a fresh
GPT-6.1 medium reviewer task, read-only. No author verdict is supplied here.

## Review Objective

Review the integrated overnight plan, status reconciliation, evidence reuse and durable
restart handoff against canonical requirements and repository truth. Reconstruct the scope
before consulting author rationale. Assess whether completion language stays within accepted
product boundaries, and whether verification/publication claims identify their actual provenance.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/overnight-core-sync/sandtable`.
Primary `/Users/dsteele/repos/sandtable` is protected user state; inspect read-only if needed.
Read applicable AGENTS.md and use $independent-review in reviewer mode.

## Base, Head, Branch, And Dirty State

Base `origin/main`: `e90eef556bde6bbde4fd6b3e17064ba613868ee6`.
Branch `codex/overnight-core-sync`. Exact committed head is supplied in coordinator dispatch
after author freeze; verify it before review, and stop/report mismatched scope.
Generated `.serena/` is untracked/excluded. No runtime/spec/test changes are authorized.

## In-Scope Commits And Paths

Reconstruct the complete `git diff <base> <head>` scope, including retained coordinator checkpoints
`ea86b4127a3cb381d49f4d23d147db45b1dd1816`,
`477744308791ceb1347427ae79f627a2c3bfdae7`,
`a9a51f9d21c497b4e1935941a910f27571cd7cad`, main merge
`4660093ebad34b3e8b806fe62a71f34f9c24fcf7`, and the final N4 commit.
Inspect substantive documentation/evidence before reading the separate author packet; reconcile
that final file after recording the preliminary ledger. Expected eight Markdown paths:

- docs/design/combat-cycle-implementation-plan.md
- docs/roadmap/pre-alpha-roadmap.md
- docs/work/plans/2026-10-05-combat-overnight.md
- docs/work/handoffs/2026-10-05-overnight-session.md
- docs/work/reviews/2026-10-05-overnight-plan-review.md
- docs/work/reviews/2026-10-05-overnight-integration-evidence.md
- docs/work/reviews/2026-10-05-overnight-integration-bootstrap.md
- docs/work/reviews/2026-10-05-overnight-integration-author.md

## Canonical Requirements And Plan

Roadmap; Combat implementation plan019D3/019E0/019E1; post-Movement dispatch;
settled-control and positive-entry v1 specs/schema/fixture/oracles; approved overnight plan.
R1 research decision, R2 diagnosis, child independent reports and handoffs are supporting evidence.
Primary ignored `.planning/combat-overnight/run-state.md` records coordinator publication observations.

## Explicit Exclusions

No new product behavior, contracts, source pins, test changes or synthetic provenance promotion.
No parent017–019 closure, C3a/Result2 actual consumption, all32actual-reachability, repeat execution,
later-II/consumed or public activation. Do not change primary user files, push, create PRs, merge,
implement fixes, expand workstreams or dispatch reviewers. Coordinator retains those decisions.

## Verification Commands Available To Reviewer

```sh
git status --short --branch
git rev-parse HEAD origin/main
git diff --name-status e90eef556bde6bbde4fd6b3e17064ba613868ee6 HEAD
git diff --check e90eef556bde6bbde4fd6b3e17064ba613868ee6 HEAD
git diff --name-status 5c847895bc269d06e9d4e8d86bf90f9acd473f31 HEAD
```

Inspect literal logs/hash inventory and byte-equivalence before deciding on proportionate
checks. Full-suite reuse is a review claim to verify, not an instruction to accept it.
Original Breakdown command remains failed; a passing supplement must not be substituted.
Native MTP test commands require explicit --project/--solution and unique binlogs.
No heavy gate lease is granted to this reviewer; request coordinator disposition if needed.

## Author Explanation Location Or Delivery Step

First record preliminary findings from the diff, plan and evidence. Then read the separate
`docs/work/reviews/2026-10-05-overnight-integration-author.md` and verify its claims.
The evidence report is factual verification data; author rationale is separated.

Use $independent-review in reviewer mode. This is review instance N4 set1/pass1,total1of9,
maximum3per set. Work from the Fresh Review Bootstrap first and record a preliminary review
before reading the Author Explanation. Then verify the explanation against the repository,
review both the implementation and its plan, run proportionate non-mutating checks, and return
an evidence-backed verdict. Do not implement fixes, create further review instances, or split
the work into new workstreams. Return the report to the coordinator.

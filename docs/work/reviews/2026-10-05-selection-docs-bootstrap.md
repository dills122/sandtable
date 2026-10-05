# Fresh Review Bootstrap: D2 selection documentation

## Review Objective

Verify minimal user-facing reconciliation after merged S3 PR161: private executable contract is separate from native execution and production authentication; remaining product gates and known pin/link failures remain explicit. Coordinator assigns counted fresh medium review; author does not dispatch reviewers.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/sandtable-user-docs-refresh/sandtable`, reused D1 worktree.
Branch `codex/sandtable-selection-docs-reconciliation`.

## Base, Head, Branch, And Dirty State

Base `2143e25a553bc927b0fe4bd504379df54828babb`; product commit `00eaf9c73a4827c778fa74acdf08a2906beb778c`.
Final administrative head and push outcome supplied by final response/dispatch; resolve branch. Only generated `.serena/` untracked/excluded.

## In-Scope Commits And Paths

Product: `README.md`, `docs/README.md`, `site/index.html`.
Administration: this bootstrap, `docs/work/reviews/2026-10-05-selection-docs-author.md`, `docs/work/handoffs/2026-10-05-selection-docs-reconciliation.md`.

## Canonical Requirements And Plan

Read AGENTS, roadmap and Combat implementation plan, then `docs/specs/combat-actual-selection-v1.md` authority/scope and trusted input precondition. Merged contract reaches FA after defender decline, with seven fallback variants per owner to no-attack Reserve Release. Native S4 deferred; production actor/clock/store authentication and actual round/result/repeat/public gates remain open.

## Explicit Exclusions

Roadmap/Combat plan editing, runtime/contracts/fixtures/oracles/pins, CI/packages, .NET full suite, redesign, deployment and author self-review/merge. Preserve other agents' work.

## Verification Commands Available To Reviewer

```sh
git diff --check 2143e25a553bc927b0fe4bd504379df54828babb HEAD
/private/tmp/d1-tools/lychee-aarch64-apple-darwin/lychee --config .lychee.toml --root-dir "$PWD" README.md docs/README.md site/index.html
node --check site/app.js
python3 /private/tmp/d1-site-check.py
```

Temporary pinned Lychee0.24.2 verified during D1. Logs `/private/tmp/d2-links-*.log`; desktop render `/private/tmp/d2-render/`. Static site has no build step. Author stops preview4173 before handoff.

## Author Explanation Location Or Delivery Step

Record preliminary findings before reading `docs/work/reviews/2026-10-05-selection-docs-author.md`.
Use $independent-review in reviewer mode, with coordinator-supplied counted instance. Inspect implementation first, then verify author testimony; return evidence-backed verdict. Do not fix/stage/commit, self-dispatch or merge.

# Fresh Review Bootstrap: D1 user-facing documentation

Review instance: coordinator assigns D1 set/pass before dispatch; maximum three sets of three.

## Review Objective

Assess documentation accuracy, onboarding commands, public/private capability boundaries and link/render regressions. This is a documentation-only delivery unit in approved session2.

## Repository And Worktree

`/Users/dsteele/.codex/worktrees/sandtable-user-docs-refresh/sandtable`.
Branch: `codex/sandtable-user-docs-refresh`. The app could not attach it because it is owned by another task; the coordinator explicitly assigned shell reuse. Do not create another checkout.

## Base, Head, Branch, And Dirty State

Base: `96596dde066b0d8c9a0110eba50fcfcb01d99a46` (merged PR159).
Product documentation commit: `e885228c7cf466248c3db5bd7eb1fbe2894a8a2c`.
The final administrative head is supplied by coordinator dispatch; resolve the branch and review base through that head. Only local generated `.serena/` is untracked and excluded. No staged or other dirty product files at packet creation.

## In-Scope Commits And Paths

Product commit above: `README.md`, `docs/README.md`, `CONTRIBUTING.md`, `site/index.html`.
Administrative follow-up: this bootstrap, `docs/work/reviews/2026-10-05-user-docs-author.md`, `docs/work/handoffs/2026-10-05-user-docs-refresh.md`.
No executable behavior changed.

## Canonical Requirements And Plan

Read `AGENTS.md`, `docs/roadmap/pre-alpha-roadmap.md` (status owner), and `docs/design/combat-cycle-implementation-plan.md` (task/evidence owner). Inspect merged PR152–159 changes, especially019D3 and019E1, and current verification/bridge research. Session2 dependency disposition is at `/Users/dsteele/.codex/worktrees/combat-session-two-sync/sandtable/docs/work/plans/2026-10-05-actual-selection-dependency-disposition.md`.

Acceptance: summaries match merged capabilities; public Rules9 entry, synthetic/private settled evidence and actual entry are distinguished; selection research/in-flight S3 is not advertised as merged; future product gates remain explicit; verification limitations are honest; setup/link/render evidence is proportionate. Reconcile any newly merged S3 before publication through coordinator.

## Explicit Exclusions

Runtime, contracts, scenario files, fixtures, oracle/pin repair, CI/package changes, redesign and deployment. S3 exclusively owns the Combat implementation plan and actual-selection spec/schema/fixture/oracle. Coordinator owns roadmap reconciliation and session ledgers. Do not modify historical research/review records into live claims. No full .NET suite lease.

## Verification Commands Available To Reviewer

```sh
git diff --check 96596dde066b0d8c9a0110eba50fcfcb01d99a46 HEAD
node --check site/app.js
/private/tmp/d1-tools/lychee-aarch64-apple-darwin/lychee --config .lychee.toml --root-dir "$PWD" README.md CONTRIBUTING.md docs/README.md site/index.html
python3 /private/tmp/d1-site-check.py
python3 -m http.server 4173 --bind 127.0.0.1
```

Lychee is temporary pinned0.24.2, official release archive checksum verified. Full corpus uses `git ls-files -- '*.md'` with `.lychee.toml`; historical link errors require baseline comparison. Static Pages workflow uploads `site/` directly; no website build/package task exists. Preview port may be in use by D1; inspect before starting another server.

## Author Explanation Location Or Delivery Step

Record preliminary findings before reading `docs/work/reviews/2026-10-05-user-docs-author.md`.

Use $independent-review in reviewer mode. Coordinator supplies the counted review instance. Work from this bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both implementation and plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

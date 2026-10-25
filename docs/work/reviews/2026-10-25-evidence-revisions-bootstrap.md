# Fresh Review Bootstrap

Review instance: 6 of 6 (human-approved limit).

## Review Objective
Assess complete dependency-revision compatibility implementation and evidence reconciliation for correctness and plan fidelity. No release/admission claim.

## Repository And Worktree
/Users/dsteele/.codex/worktrees/3d3c/sandtable

## Base, Head, Branch, And Dirty State
Base and HEAD: 3e2d4336b1505d9f308529841361e5413cf70f74. Branch codex/rc-reserve-crash-recovery. Explicit dirty working-tree review; tracked patch and SHA256 inventory including new files captured in artifacts/rc-source-pins/review6-start.patch and review6-start-manifest.json. No changes staged or committed for review.

## In-Scope Commits And Paths
All listed changes. Native CampaignCombatActual* dependency readers/replay, CombatDependencySnapshot, CombatEvidenceRevisions, affected Core tests, retained legacy archives, schemas/fixtures/oracles and project docs. Start with fixed source/test state. Downstream B2/B3 generation and focused native checks remain active; final artifact delta and results must be reviewed before verdict. Do not treat generation as validation.

## Canonical Requirements And Plan
AGENTS.md; docs/specs/combat-evidence-revisions-v1.md; docs/work/plans/2026-10-24-rc-checklist.md. Contract is new and must itself be challenged against existing replay contracts. Original scoped source-pin reconciliation expanded with human approval to preserve exact legacy evidence alongside explicitly frozen refreshed manifests. No arbitrary manifest acceptance.

## Explicit Exclusions
Prior committed crash-recovery work; unrelated Snapshot and Outward pin repair; public certification; admission of absent roots; external persistence migration or hosting support. These remain open, not implicit passes.

## Verification Commands Available To Reviewer
Focused dotnet tests via --project and --filter-method/--filter-class; original Python oracle groups; git diff; archive/hash comparisons. Existing native test batch running: avoid concurrent build/output mutation. Read-only checks preferred.

## Author Explanation Location Or Delivery Step
Do not read prior implementation rationale/review reports yet. Inspect source/requirements/tests independently and send preliminary concerns first. Author explanation and final artifact delta supplied afterward. Write final report only to docs/work/reviews/2026-10-25-evidence-revisions-independent-review.md.

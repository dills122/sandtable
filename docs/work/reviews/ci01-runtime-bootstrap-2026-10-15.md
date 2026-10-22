# Fresh Review Bootstrap — CI01

Review instance: pending Brain allocation, 0 of9 used; round1, pass0 of3; recovery0 of2. Do not start an unallocated extra review instance.

## Review objective

Assess correctness, plan fidelity, coverage/provenance preservation, maintainability and measurement sufficiency of the CI partition candidate. A review may assess code readiness separately from the pending hosted performance experiment. No merge authorization.

## Repository and worktree

`/Users/dsteele/.codex/worktrees/ci-runtime-optimization/sandtable`, branch `codex/ci-runtime-optimization`.

## Base, head and scope

Base c24266a5d7dd12c05568c1788e472ad85a20c73e. Implementation head f586b82eed41ae7946783da5faf27223b31fa1cf. Later commit containing only these reviewer packets may be included in delivery; verify actual branch and dirty state before review.

Implementation paths: `.github/scripts/test_partition.py`, `.github/scripts/test_partition_test.py`, `docs/work/ci-runtime-optimization-2026-10-15.md`. No workflow change. Review production helper and regression tests independently before reading author report/explanation.

## Canonical requirements and plan

Session source `/Users/dsteele/repos/sandtable/.planning/combat-repeat-ci-session/session-plan.md`, CI investigation/decision and delivery-control sections; `ci-scale-research.md` in same directory. Applicable AGENTS.md. Retain all required tests, exhaustive/disjoint actual rows, SHA/attempt/SDK/Release, process exits, exact XML membership and strict dependency outcomes. No automatic retry/quarantine/skip, selective required gate, timeout/parallelism increase or paid services. Heavy local tests require Brain lease; hosted runs require Brain budget. Shared README/design/roadmap and domain files excluded from this lane.

## Available verification

`PYTHONDONTWRITEBYTECODE=1 python3 .github/scripts/test_partition_test.py`

`git diff --check c24266a5d7dd12c05568c1788e472ad85a20c73e f586b82eed41ae7946783da5faf27223b31fa1cf`

No heavy local .NET or hosted performance run authorized yet. Read-only GitHub: remove GH_TOKEN/GITHUB_TOKEN for gh commands; use configured Keychain credential for dills122/sandtable; never inspect or extract tokens. Do not export raw diagnostic artifacts.

## Author explanation

After preliminary findings ledger, read `docs/work/reviews/ci01-runtime-author-2026-10-15.md` and main measurement report. Author claims require verification. Reviewer read-only; return preliminary/final findings to Brain. No further reviewers, fixes, merges or hosted experiments from this bootstrap.

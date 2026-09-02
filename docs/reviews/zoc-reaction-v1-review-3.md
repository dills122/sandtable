# ZOC and Reaction v1 Independent Review 3

**Review instance:** 3 of 3

**Date:** 2026-08-30

**Reviewed target:** remediated dirty working-tree documentation delta on
`codex/zoc-reaction-specification` from
`37e2f61c657d43b90dce28aa8d4396504a8963eb`

**Frozen-target verdict:** Not ready

**Post-reconciliation disposition:** All findings accepted and remediated by the implementation
task. The three-instance limit is exhausted, so no further automatic independent review is permitted;
the revised package returns to the owner approval gate with final repository evidence.

**Compatibility-policy update (2026-09-01):** The resolution recorded below required all dormant
checkpoints to land together. Current repository policy instead permits independently merged,
dependency-ordered dormant checkpoints while the complete legacy identity set remains active. The
coordinated 006C activation and prohibition on partial-current successor identities are unchanged.
The current policy is canonical in the specification and design compatibility matrix.

## Findings and reconciliation

| Finding | Severity | Disposition | Resolution |
| --- | --- | --- | --- |
| Clean-cut compatibility conflicted with independently merged version-bump slices | P1 | **Accept** | At this review, lettered tasks were constrained to land together while Tasks 002A-006B kept the complete legacy identity set active and prepared dormant successor artifacts; 006C switched the complete coupled set in one activation commit. The compatibility matrix forbade partial-current states. This delivery constraint was later superseded by the compatibility-policy update above. |
| Task 006 public gate and `ZOR-REQ-012` trace omitted material activation evidence | P2 | **Accept** | Task 006 now gates `ZOR-AC-001`-`009`, `011`-`013`, and `015`-`017`, with the static and creation/World exceptions called out. `ZOR-REQ-012` now spans Tasks 002A-006C and 007A-007B across every new or activated contract. |
| Documentation index and retained ruling-lock status were stale | P2 | **Accept** | `docs/README.md` now records Movement Tasks 001-010/PR #79 complete and links the proposed ZOC/Reaction package; the ruling lock now records accepted/synchronized rulings and the owner approval gate. |
| Ignored planning recovery records lagged the package | P3 | **Accept** | Findings, decisions, review reconciliation, phase state, and final evidence were refreshed in the ignored planning workspace. |

No finding was disputed or deferred.

## Confirmed prior remediations

Instance 3 confirmed topology-local ZOC and remote noninterference, Rule 8.53(b) deferral,
first-step participant selection, scenario-owned current-TOE initialization, combat-only triggers,
Core-only fallback ownership, aggregate controlled-location observation, participant-specific trigger
adjacency, and active/system-close rules. The status correction was partial at the frozen target and
is completed by this reconciliation.

## Independent verification

- Confirmed exact branch/base and the frozen eight-modified/four-untracked target.
- Inspected the complete diff, prior reports, implementation plan, relevant Core version/contracts,
  and official Land Rules/errata.
- `git diff --check`, twelve-file local Markdown-target checks, and whitespace audit passed.
- The reviewer's `just check` and follow-up build entered an environmental restore/build hang and
  were canceled; this was inconclusive, not a product failure.
- After reconciliation, the implementation task reran `just check` outside the sandbox: restore and
  format passed, the build completed with zero warnings/errors, and all 995 tests passed with zero
  failed or skipped. Local targets and whitespace remained clean across all fifteen delivery files.

## Review-loop closure

This is the final permitted independent-review instance. The reconciled package may proceed only
with the implementation task's final verification and explicit owner approval; remaining risk must
be surfaced to the owner rather than hidden behind a fourth review.

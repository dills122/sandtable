# ZOC and Reaction v1 Independent Review 1

**Review instance:** 1 of 3

**Date:** 2026-08-30

**Reviewed target:** dirty working-tree documentation delta on
`codex/zoc-reaction-specification` from
`37e2f61c657d43b90dce28aa8d4396504a8963eb`

**Frozen-target verdict:** Not ready

**Post-reconciliation disposition:** Remediated; review instance 2 required before approval

## Findings and reconciliation

| Finding | Severity | Disposition | Resolution |
| --- | --- | --- | --- |
| Positive ZOC would retain Movement's global fail-closed suppression | P1 | **Accept** | `ZOR-REQ-009`, `ZOR-AC-013`, and Tasks 004B/005/006C now own dormant topology-local entry/exit derivation, remote-ZOC noninterference, exact adjudication, and atomic publication before any active positive-ZOC path. |
| Rule 8.53(b)'s CPA restriction is conditional on a phasing Close Assault declaration absent from v1 | P1 | **Accept** | Proposed `ZOR-DEC-011` explicitly defers the conditional restriction to a declaration-aware Sprint 5 contract, removes unconditional threshold behavior from v1, and preserves the accepted non-disclosure policy for later activation. This is an explicit owner-approval point. |
| Zero-step participant episodes contradicted accepted `ZOR-DEC-002` | P1 | **Accept** | The first accepted Reaction movement step now selects the participant atomically; completion is unavailable until one accepted step, and participant selection/movement/completion share Task 006B. |
| Initial current-TOE source/admission/replay truth was unspecified | P1 | **Accept** | Content scenario placements now declare provenance-bearing component TOE seeds in canonical identity; creation copies them into event/World; maximum TOE is never a default; missing/duplicate/unknown/negative/over-maximum seeds reject. |
| Trigger scope included noncombat representations contrary to Rule 8.51 | P2 | **Accept** | Trigger text now requires an adjacent authoritative non-phasing represented combat element, with an explicit noncombat-only non-trigger case. |
| Asynchronous fallback promised orchestration without a hosting/worker lane | P2 | **Accept** | V1 now owns only a deterministic Core system-close action and reason. Clocks, scheduling, activation, deadlines, and automatic submission remain a separately approved OrleansHost/DecisionWorker task. |
| Movement Task 010 remained active in one dependency paragraph | P3 | **Accept** | The paragraph now records Tasks 001-010 complete and their historical serial delivery. |

No finding was disputed. The Rule 8.53(b) remediation deliberately narrows v1 instead of importing a
Combat declaration into Sprint 4; owner approval of the revised package remains mandatory.

## Confirmed strengths and boundaries

The reviewer otherwise found the stable parent IDs, lettered serial slices, Breakdown research
boundary, compatibility version inventory, replay model, principal fog projections, and most of the
acceptance table coherent. It confirmed that no production behavior changed and that current
contract-version numbers match the repository.

## Independent verification

- Verified the exact branch, base, eight modified files, two untracked proposal files, and no new
  commits on the frozen target.
- Inspected the complete diff, canonical research, Movement contracts, and relevant Core seams.
- Visually checked official Land Rules pages 13-18 and 32-34 plus September 1979 errata.
- Local Markdown targets across all ten frozen review files existed.
- `git diff --check` passed.
- `just check` passed independently: restore current, format clean, build with zero warnings/errors,
  and 995 passed / 0 failed / 0 skipped tests.

## Next review

The P1 revisions materially change source scope, eligibility, Content initialization, action flow,
and task ownership. Review instance 2 must inspect the complete remediated target before this package
can be presented as approval-ready.

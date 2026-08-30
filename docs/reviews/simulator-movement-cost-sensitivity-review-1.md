# Simulator Movement-Cost Sensitivity Independent Review 1

**Review instance:** 1 of 3

**Date:** 2026-08-30

**Reviewed target:** dirty working-tree delta on
`codex/simulator-movement-cost-sensitivity` from
`47a3baeb61ed511adeaa38cc49052f367493a7aa`

**Frozen-target verdict:** Not ready

**Post-reconciliation disposition:** Ready after the final repository gate

## Findings and reconciliation

| Finding | Review severity | Implementation disposition | Resolution |
| --- | --- | --- | --- |
| The roadmap Sprint 3 and Sprint 4 summary rows still described Movement as the current authority stop and Task 009 as upcoming | P2 | **Accept** | Updated the summary to the current Breakdown Determination checkpoint, merged Movement Tasks 001-009, locally complete Task 010 evidence, and the later gameplay-tuning gate. |
| The research note said only controller configuration differs even though the isolated arms require distinct exercise IDs | P3 | **Accept** | Qualified the statement: controller configuration is the only behavior-bearing input difference; exercise IDs differ only as isolated-run labels and are not campaign creation inputs. |

No finding was disputed or deferred. Neither correction changes implementation behavior,
architecture, contracts, canonical identities, experiment inputs, or retained output evidence.

## Confirmed implementation and boundaries

The reviewer found no actionable implementation, architecture, compatibility, determinism, or test
defect. It independently confirmed that:

- Runner candidate v3 carries the already-public exact positive Movement cost and rejects that
  field on non-Movement candidates;
- the lowest-cost policy preserves element priority and exact rational tie-breaking;
- existing policy tokens, identities, and behavior remain stable;
- ordinary Core legal-action membership, submission, adjudication, replay, and terminal authority
  remain unchanged;
- the paired arms have equal campaign creation inputs, initial snapshots, and seed ledgers;
- the first divergence is accepted-action ordinal 10, with the control route costing 8 and the
  lowest-cost route costing 1/2 while the second cost-1 route remains unchanged; and
- both arms reach Breakdown Determination after 13 accepted actions and 94 passed checks with
  successful reconstruction and re-adjudication.

The reviewer also confirmed that the 22-path implementation scope was coherent and that no new
Core Movement, Breakdown, ZOC/Reaction, Contact, combat, victory, tuning, model, export, or
scheduler work entered the change.

## Independent verification

- `just check` passed: format clean, build succeeded with zero warnings/errors, and 995/995 tests
  passed with zero skipped.
- Runner tests passed 378/378 with zero skipped.
- Focused Movement-cost tests passed 3/3.
- The checked paired-command test passed 1/1.
- Two fresh paired CLI runs both emitted
  `sha256:5f997e4d74d0f9b83e43d6d4c2c33ebedae03b188a7e30d64f57092a51edd1bc`.
- Direct artifact inspection confirmed the retained input/snapshot/seed hashes, per-arm action and
  check counts, exact trajectories, divergence ordinal, and zero accepted-step delta.
- `git diff --check` passed on the frozen target.

The initial sandboxed formatter attempt could not create its named pipe; the same official gate
completed successfully outside that restriction.

## Closeout

A second review instance is unnecessary for these documentation-only corrections. A new review is
required if later reconciliation materially changes behavior, architecture, compatibility,
canonical identities, plan scope, or retained experiment evidence.

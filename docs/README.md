# Sandtable Documentation Index

This index separates current governing documents from active decision gates and retained historical
evidence. `README.md` remains the user-facing project map; `tech-design.md`, `naming-overview.md`, and
the pre-alpha roadmap carry repository-wide rationale and delivery state.

## Start here

- [Project overview and setup](../README.md)
- [Contributor workflow](../CONTRIBUTING.md)
- [Security policy](../SECURITY.md)
- [Technical design](../tech-design.md)
- [Checked ZOC/Reaction Maneuver evidence](research/simulator-reaction-trajectories.md)
- [Naming and domain vocabulary](../naming-overview.md)
- [Pre-alpha roadmap](roadmap/pre-alpha-roadmap.md)

## Implemented capability packages

| Capability | Specification | Technical design | Supporting research |
| --- | --- | --- | --- |
| Initiative Determination | [Spec](specs/initiative-determination.md) | [Design](design/initiative-determination.md) | [Spike](research/initiative-determination-spike.md) |
| Content Pack | [Spec](specs/content-pack-v1.md) | [Design](design/content-pack-v1.md) | [Spike](research/content-pack-v1-spike.md) |
| Campaign World | [Spec](specs/campaign-world-v1.md) | [Design](design/campaign-world-v1.md) | Content Pack package above |
| Campaign Observation | [Spec](specs/campaign-observation-v1.md) | [Design](design/campaign-observation-v1.md) | [Fog-boundary spike](research/observation-and-fog-boundary-spike.md) |
| Legal Actions | [Spec](specs/legal-actions-v1.md) | [Design](design/legal-actions-v1.md) | [Action-boundary spike](research/turn-preamble-action-boundary-spike.md) |
| Weather Determination | [Spec](specs/weather-determination-v1.md) | [Design](design/weather-determination-v1.md) | [Preamble spike](research/operation-stage-preamble-spike.md) |
| Operation-Stage Entry | [Spec](specs/operation-stage-entry-v1.md) | [Design](design/operation-stage-entry-v1.md) | [Spike](research/operation-stage-entry-spike.md) |
| Reserve Designation | [Spec](specs/reserve-designation-v1.md) | [Design](design/reserve-designation-v1.md) | [Spike](research/reserve-designation-spike.md) |
| Exercise Harness | [Spec](specs/exercise-harness-v1.md) | [Design](design/exercise-harness-v1.md) | [Capability](research/exercise-capability-and-replay-spike.md), [artifacts](research/exercise-evidence-artifact-spike.md), [reproducibility](research/exercise-reproducibility-and-pairing-spike.md) |

## Active engine package and decision gates

- Movement Foundation: [specification](specs/movement-foundation-v1.md),
  [technical design](design/movement-foundation-v1.md), and
  [source/contract research](research/movement-foundation-spike.md). Tasks 001-010 are implemented;
  PR #79 completed the checked Maneuver exercise evidence and the complete authoritative Movement
  vertical is available.
- Breakdown continuity: [decision packet](research/breakdown-continuity-spike.md). The approved
  continuity seam is implemented through Task 004B and projected side-safely by Task 005.
- Breakdown adjudication: [source/decision packet](research/breakdown-adjudication-spike.md) and
  [design/task plan](design/breakdown-adjudication-v1.md), [governing specification](specs/breakdown-adjudication-v1.md),
  [wire freeze](specs/breakdown-wire-contract-v1.md) and [fixture migration](specs/breakdown-fixture-migration.v1.json).
  Decisions `004`–`007` accepted; Task 001 complete; [Task 002 outcomes](research/breakdown-outcome-rules.md)
  implemented with full gate passing and independent review Ready. [Task 003 campaign contracts](research/breakdown-campaign-contracts.md)
  and certified Truck fixture implemented. [Task 004 move accounting](research/breakdown-move-accounting.md)
  implemented. [Task 005 stop/check authority](research/breakdown-stop-adjudication.md) implemented;
  Task 006 coherent public activation and privacy next. Public adjudication remains unimplemented.
- ZOC and Reaction: approved [specification](specs/zoc-reaction-v1.md),
  [technical design](design/zoc-reaction-v1.md), and
  [research packet](research/contact-reaction-zoc-spike.md), with the accepted
  [user-space boundary decision](research/user-space-declassification-boundary-enforcement.md) and
  [disclosure manifest](specs/user-space-disclosure-manifest.v1.json). Movement and Breakdown continuity
  prerequisites are complete; `ZOR-TASK-002A`-`006C` implement and activate Rules/Content/fixture,
  Campaign World/creation/Snapshot/event-replay, side-safe Observation 6/policy/history,
  topology-local Movement/Reaction, participant episodes, and exact closure/resumption.
  `007A`-`007B` complete bounded Runner adoption, strict checked evidence, matching clean runs,
  and [Ready independent review](reviews/zor-task-007-review-1.md).
- Combat: [source inventory](research/combat-cycle-source-inventory.md) and completed
  [rules/result-surface spike](research/combat-rules-result-surface-spike.md). Research is active;
  implementation contracts are not frozen.
- Sprint 4-5 dependencies: [research-gate audit](research/sprint-4-5-research-gates.md).

## Reviewed future product work

- Player Intent Composer: [specification](specs/player-intent-composer-v1.md),
  [technical design](design/player-intent-composer-v1.md), and
  [input/parser research](research/player-intent-input-and-needle-feasibility.md). This package is
  reviewed but not authorized for implementation.
- Web play and persona research is retained under [`docs/research`](research/); Maproom, hosted
  lifecycle, model-backed commanders, and parser adoption remain later roadmap work.

## Historical evidence

- [`docs/research`](research/) contains source investigations, bounded spikes, simulator studies,
  and decision packets. A research document records evidence at its stated date; it is not current
  implementation truth unless its status says so.
- [`docs/reviews`](reviews/) contains independent review and reconciliation records. These are audit
  history, not active task lists.

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
- [Post-merge simulator check-in (Rules 9)](research/simulator-post-merge-checkin.md)
- [Naming and domain vocabulary](../naming-overview.md)
- [Pre-alpha roadmap](roadmap/pre-alpha-roadmap.md)
- [Current checkpoint, simulation and Orleans planning gates](roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates)
- [Combat delivery review and owner disposition](reviews/combat-delivery-plan-author-review.md#owner-disposition)

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
  [Task 006 public activation and privacy](research/breakdown-public-activation.md) activates current
  authority through first-side Combat entry. [Task 007 Runner closeout](research/breakdown-runner-closeout.md)
  records checked successors and verification; [review 5](reviews/brk-followup-review-5.md) accepts the AC-009 transcript follow-up and completes bounded Tasks 006–007.
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
  [rules/result-surface spike](research/combat-rules-result-surface-spike.md), with decision-ready
  [static Content](research/combat-content-static-schema-spike.md) and
  [mutable-state research](research/combat-mutable-state-spike.md), and
  [RNG/golden research](research/combat-rng-golden-spike.md), plus
  [Reserve release/history](research/reserve-release-history-spike.md).
  [Independent review 2](reviews/combat-reserve-research-review-2.md) is Ready for owner decisions;
  [CMB-DES-001 identity design](design/combat-opportunity-identity-v1.md) is complete for review,
  covering the bounded infantry opportunity, target, participant and Contact/Engaged identity
  handoffs. [CMB-DES-002 sealed protocol](design/combat-sealed-decision-protocol-v1.md) is independently
  reviewed, defining frozen choices, persisted lifecycle, cancellation fallback and strict
  readback. [CMB-DES-003 Combat-step transitions](design/combat-step-transitions-v1.md) is complete
  for review: explicit selection/decline, six closure proofs and prepared/cancelled/settled paths.
  [CMB-DES-004 cost/resolution ordering](design/combat-cost-resolution-order-v1.md) defines atomic
  costs, role-ordered result publication and simultaneous-stage extension boundaries.
  [CMB-DES-005 settlement/disclosure](design/combat-settlement-disclosure-v1.md) defines mandatory
  choice fallback, loss/retreat/custody conservation and side projections. `CYCLE-DES-001`
  [cycle/Reserve composition](design/continual-cycle-reserve-composition-v1.md) now defines
  release windows, repeat/finish and occurrence-aware evidence. The
  [policy register](design/combat-cycle-policy-reconciliation.md) consolidates eight owner choices;
  the [combined contract/implementation plan](design/combat-cycle-implementation-plan.md) defines
  25 staged tasks and their evidence. Owner accepted all eight policies and the review4 correction
  on2026-09-06. TASK-001 source research, TASK-002 Content and TASK-003A Setup/initial ledger
  packets are complete. [TASK-003B World/settlement](specs/combat-world-settlement-v1.md) is complete
  as a contract slice;003C/D/004 and production gates stay open.
  [Independent design review 2](reviews/combat-design-review-2.md) returned Ready for
  DES-001/DES-002/DES-003. [Independent design review 3](reviews/combat-settlement-review-3.md)
  returned Ready for DES-004/DES-005, with no actionable findings; subsequent cycle design is outside
  that review. [Review4](reviews/combat-cycle-plan-review-4.md) assessed cycle/combined planning at
  9f683d1: Not ready with one ordinary break-off handoff gap. Author correction assigns the missing
  work. That checkpoint exhausted its then-authorized4of4 budget. Owner subsequently authorized
  up to three more passes. [Progress review5](reviews/combat-progress-review-5.md) covers the
  unmerged branch through003B: Ready with non-blocking follow-ups; its status correction is applied.
  [003C1 rules inputs/timing](specs/combat-rules-inputs-v1.md) is complete as a contract slice;
  [review6](reviews/combat-inputs-review-6.md) returned Ready, no actionable findings (6of7 used).
  [003D1 sequence/cycle contracts](specs/combat-cycle-sequence-v1.md) are complete;
  [review7](reviews/combat-sequence-review-7.md) returned Ready, no actionable findings (7of7 used).
  [003C2 Rules10/creation envelopes](specs/combat-authority-envelope-v1.md) are complete for the creation
  cut. [003C3a selection/step control](specs/combat-selection-steps-v1.md) is complete with author checks;
  [003C3b sealed round/commitment](specs/combat-sealed-round-v1.md) is also complete as a bounded
  authority fragment. [HOST-RSH-001](research/orleans-publication-feasibility.md) research is complete;
  003C3c/D2a/D2b and [D2c.1 successor/opening contracts](specs/combat-inherited-successors-v1.md)
  are complete within their private boundaries. [D2c.2a opening provenance](specs/combat-opening-preamble-v1.md)
  reaches Weather entry from Created11. [D2c.2b Weather](specs/combat-weather-v1.md) reaches Organization
  entry with34 traces/68 cuts and strict RNG/history checks. [D2c.2c stage entry](specs/combat-stage-entry-v1.md)
  reaches Reserve entry with12 traces/60 cuts. [D2c.2d Reserve designation/completion](specs/combat-reserve-designation-v1.md)
  closes bounded creation-to-first-opening contracts. [Inherited Movement preparation](design/combat-inherited-movement-preparation.md)
  maps D2c.3 fields and replay obligations; D2c.3 implementation and D2c.4 full composition remain open.
  See [result/settlement](specs/combat-result-settlement-v1.md) and
  [snapshot composition/audit](specs/combat-snapshot-composition-v1.md). See
  [ordinary movement](specs/combat-ordinary-movement-v1.md) for D2a evidence and source correction.
  [Reserve Release](specs/combat-reserve-release-v1.md) freezes the private control/history arm.
  [Cycle control](specs/combat-cycle-control-v1.md) freezes guarded repeat/finish and Movement expiry
  with19 literal cases/64 traces. [Review9](reviews/combat-progress-review-9.md) returned Ready with
  non-blocking follow-ups at `a96d2a1`; its status corrections are applied;9of9 budget exhausted.
  Synthetic inherited lineage remains explicit. Production hosting remains gated; parent003C/D stays open.
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

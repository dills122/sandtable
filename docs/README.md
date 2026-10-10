# Sandtable Documentation Index

[README](../README.md) owns the project overview and setup, [technical design](../tech-design.md)
owns architecture, and [naming](../naming-overview.md) owns vocabulary. The
[pre-alpha roadmap](roadmap/pre-alpha-roadmap.md) is the canonical current delivery ledger.
Research and reviews record their dated checkpoints; they are not competing status ledgers.

## Start here

- [First-release audit](research/2026-10-05-first-release-audit.md): current gaps and release exit gates
- [Roadmap checkpoint and next gates](roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates)
- [Contributor workflow](../CONTRIBUTING.md) and [security policy](../SECURITY.md)
- [Exercise Runner runbook](runbooks/exercise-runner.md)
- [Checked Reaction Maneuver evidence](research/simulator-reaction-trajectories.md)
- [Rules9 simulator check-in](research/simulator-post-merge-checkin.md)
- [Combat verifier pin maintenance inventory](research/combat-verification-pin-maintenance.md)

Public Rules9 authority and Runner evidence stop at first-side Combat entry. Private Combat Core
adapters and contracts remain separate. Task019F1's native actual-selection adapter merged in
PR #165, reaching Force Assignment without completion or no-attack Reserve Release for both
supported owners. Trusted production input authentication, actual round/result completion,
public activation and authentic continual-cycle proof remain pending. The first six-turn Land-only
scenario, Maproom hot-seat play and durable save/resume are not started. Host/worker/provider
integration remains scaffolded; AI is optional future work.

## Governing capability packages

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

## Combat and continual-cycle work

- [Combined implementation plan](design/combat-cycle-implementation-plan.md): private tasks, evidence and activation gates
- [Source inventory](research/combat-cycle-source-inventory.md) and [rules/result-surface spike](research/combat-rules-result-surface-spike.md)
- [Policy reconciliation](design/combat-cycle-policy-reconciliation.md): accepted owner decisions
- [Actual-selection contract](specs/combat-actual-selection-v1.md): merged private executable boundary
- [Author delivery review and owner disposition](reviews/combat-delivery-plan-author-review.md#owner-disposition)
- [Orleans publication feasibility](research/orleans-publication-feasibility.md): publication obligations distinct from Core

Movement and Breakdown governing packages: [Movement spec](specs/movement-foundation-v1.md),
[Movement design](design/movement-foundation-v1.md), [Breakdown spec](specs/breakdown-adjudication-v1.md),
[Breakdown design](design/breakdown-adjudication-v1.md), [ZOC/Reaction spec](specs/zoc-reaction-v1.md),
and [ZOC/Reaction design](design/zoc-reaction-v1.md).

## Future product work and retained evidence

[Player Intent Composer](specs/player-intent-composer-v1.md) and its
[design](design/player-intent-composer-v1.md) are reviewed future work. Parser adoption, hosted
lifecycle and model-backed play remain roadmap gates.

- [Research directory](research/): source investigations, spikes, simulator studies and decision packets
- [Review directory](reviews/): independent assessments and reconciliation history
- [Historical project ledger](research/2026-10-05-project-documentation-snapshot.md): former detailed README checkpoint record
- [Contract directory](specs/): exact versions, frozen schemas and executable contract evidence
- [Design directory](design/): domain rationale and delivery plans

Retained Python Combat oracles are outside `just check`. The pin inventory records known failures
and unverified timeouts; historical passing results do not certify a new checkout. Report original
oracle outcomes separately from supplemental semantic probes.

Versioned specifications describe their frozen boundary. In particular, the Content Pack v1
current-evolution note predates implemented Movement/contact work; use the roadmap for current
capability status. Historical spec bytes remain unchanged so provenance pins stay valid.

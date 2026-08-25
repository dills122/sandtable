# Operation-Stage Entry v1 Source and Contract Spike

**Status:** Accepted and implemented; STG-TASK-001 through STG-TASK-022 and STG-TASK-014A complete

**Date:** 2026-08-24

**Decision owner:** Project owner

**Rules target:** `cna-1979.1`

**Roadmap capability:** `STAGE-ENTRY-001`

## Executive conclusion

The smallest honest capability after Weather is an **explicit-empty Operation Stage 1 entry slice**
for the two setup-hashed synthetic rules-laboratory campaigns. It should resolve Organization,
Naval Convoy Arrival, Commonwealth Fleet Assignment, and Commonwealth Fleet Repair through four
mechanic-specific trusted system actions/events, then stop at the first-acting side's Reserve
Designation decision.

The capability must not infer emptiness from absent Organization, arrival, ship, or repair data. A
new versioned setup policy must explicitly assert that the admitted `(GameTurn, OperationStage)` has
none of those obligations. Missing policy, a different stage, or the policy's recognized
`has-obligations` value rejects before advancement with zero events. V1 cannot detect positive facts
that have no domain contract.

This slice preserves the source order and reaches a real downstream side decision without pretending
that positive Reorganization, Construction, Training, scheduled arrivals, Fleet Assignment, Fleet
Repair, or Reserve rules are implemented. Those positive mechanics require contracts the current
Content Pack and Campaign World do not contain.

## Decision question

> What is the smallest source-faithful capability that can advance the current synthetic campaigns
> from the implemented Organization barrier to Reserve Designation without deleting player choices,
> treating missing data as zero, or coupling stage entry to Reserve or Movement?

The answer matters now because Weather v1 stops correctly at Organization, while Task 3.4 is only a
roadmap capability and is not implementation-ready.

## Scope, authority, and stop condition

This spike covers:

- source order from Organization through Commonwealth Fleet Repair;
- the current setup, sequence, legal-action, command/event, replay, observation, and ruleset seams;
- explicit-empty admission for the two synthetic setups at Operation Stage 1;
- the exact cutoff at first-player Reserve Designation; and
- options, owner decisions, acceptance boundaries, and implementation consequences.

It does not normalize or implement positive Organization, arrival, ship, repair, Reserve, Movement,
later-stage Initiative Declaration, general convoy, logistics, construction, training, or naval
rules. It does not change Core behavior.

The stop condition was a decision-ready research packet plus a proposed specification/design with
dependency-sized tasks and requirement-to-evidence traceability. Owner approval and the merged
serial-Maneuver checkpoint now satisfy that research gate.

## Source hierarchy and method

Sources were evaluated in this order:

1. approved Sandtable rulings and implemented contracts;
2. original 1979 Land rules and sequence of play;
3. official errata where relevant; and
4. repository observations and synthetic fixture policy.

The primary Land Rules scan was inspected through its indexed text for the sequence-of-play entries.
This packet retains stable locators and short conclusions, not copied source scans.

## Evidence

### Documented source facts

| Fact | Stable source reference |
| --- | --- |
| Organization follows Weather within each Operation Stage. | `spi-1979-land-rules:5.2` |
| Reorganization, Construction, and Training may occur in player-selected order. | `spi-1979-land-rules:5.2.organization` |
| Reorganization includes attachment, assignment, and detachment of reinforcements, replacements, non-assigned units, and trucks. | `spi-1979-land-rules:5.2.organization` |
| Construction and Training each contain completion followed by initiation/continuation work; participating units can lose voluntary movement for the remainder of the stage. | `spi-1979-land-rules:5.2.organization` |
| Naval Convoy Arrival follows Organization and places actually arriving reinforcement, replacement, and ammunition quantities at designated ports or entrance hexes. | `spi-1979-land-rules:5.2.naval-convoy-arrival`; `spi-1979-land-rules:20.21` |
| Commonwealth Fleet Assignment follows arrival and lets the Commonwealth assign ships to sea/coastal hexes. | `spi-1979-land-rules:5.2.commonwealth-fleet` |
| Commonwealth Fleet Repair follows assignment and undertakes ship repair work. | `spi-1979-land-rules:5.2.commonwealth-fleet` |
| Player A designates reserves after Fleet Repair and before Movement/Combat. | `spi-1979-land-rules:5.2.reserve-designation`; `spi-1979-land-rules:18.0` |

These facts rule out a fixed positive Organization sub-sequence, a generic “finish preamble” action,
and automatic Reserve completion.

### Repository observations

- **Observation:** Weather v1 reaches one Organization phase barrier in the same
  `(GameTurn, OperationStage)` and consumes no downstream obligation.
- **Observation:** `Cna1979LandSequence` already orders Organization, Naval Convoy Arrival, Fleet
  Assignment, Fleet Repair, and first-player Reserve Designation. Organization is one barrier;
  Fleet remains two ordered segment positions.
- **Observation:** the two synthetic setup/content contracts have formations and elements but no
  unattached/replacement assignment state, projects, training state, scheduled arrivals, ports,
  ammunition arrivals, ships, sea/coastal placement, or repair work.
- **Observation:** existing opening-preamble and Weather policies prove that repository-synthetic
  emptiness must be explicit, source-referenced, setup-hashed, serialized, replay-validated, and
  narrow to the admitted fixture.
- **Observation:** Legal Actions currently exposes trusted system candidates at implemented
  mandatory checkpoints and side candidates only through the observation-derived exact-audience
  path.
- **Observation:** Operation Stage 1 actor order is already retained separately from initiative
  holder, so the Reserve successor can resolve the first-acting side without exposing hidden truth.

### Inferences

- **Inference:** positive Organization cannot be represented as one system completion because its
  segment order and choices belong to players and its effects require absent domain state.
- **Inference:** an explicitly empty Organization barrier can be resolved by a trusted system action
  without deleting a choice because the setup contract proves there is no available choice.
- **Inference:** Fleet Assignment and Fleet Repair should remain separate empty resolutions because
  the sequence already models their source order and positive future behavior has different data and
  choice semantics.
- **Inference:** one combined “no stage-entry obligations” event would reduce event evidence and
  allow later replay to skip the exact phase that was admitted; mechanic-specific events are the
  safer reversible boundary.
- **Inference:** the current Stage 1 slice need not implement general opening Naval Convoy or later
  stage declarations because its admitted checkpoint already contains valid Stage 1 order and no
  positive stage-entry subjects. Any future checkpoint lacking those facts remains unsupported.

### Unknowns deliberately left outside v1

- exact positive Reorganization eligibility and attachment/detachment command shapes;
- project/training schedules, markers, costs, and morale effects;
- positive arrival scheduling, eligible ports/entrance hexes, and interaction with convoy outcomes;
- ship identity, placement, coastal/sea topology, assignment legality, and repair schedules;
- whether positive Fleet Repair is automatic, optional, or a compound Commonwealth decision after
  the full naval contract is normalized; and
- later-stage and later-turn policy reuse after real positive subjects exist.

## Options considered

| Option | Result | Decision |
| --- | --- | --- |
| Jump directly from Organization to Reserve or Movement | Deletes mandatory evidence and can erase Fleet/Reserve choices. | Reject |
| Infer all phases are empty because current schemas lack subjects | Converts missing authority data into a rules conclusion. | Reject |
| Implement all positive Organization/arrival/Fleet mechanics now | Requires several absent domains and creates an oversized, untestable slice. | Defer |
| Use one combined generic stage-entry completion event | Hides which mandatory phase was admitted and creates a reusable sequence bypass. | Reject |
| Add an exact setup-hashed empty policy and one system action/event per mandatory position | Preserves order, replay evidence, and fail-closed support while reaching Reserve honestly. | **Recommend** |

## Proposed owner decisions

| ID | Proposed decision | Status |
| --- | --- | --- |
| `STG-DEC-001` | Stage-entry v1 supports only the two current synthetic setups at their admitted Operation Stage 1 checkpoint and only through an exact setup-hashed policy. | Accepted |
| `STG-DEC-002` | The policy separately asserts no Organization, Naval Convoy Arrival, Fleet Assignment, and Fleet Repair obligations; missing schema data never implies any assertion. | Accepted |
| `STG-DEC-003` | Resolve each mandatory position through its own trusted system action, command, event, and replay validation; add no generic sequence-completion primitive and preserve the existing action-set/candidate/submission binding contract. | Accepted |
| `STG-DEC-004` | Empty Organization resolution consumes the single barrier without defining a positive segment order; positive Organization remains a future player-choice capability. | Accepted |
| `STG-DEC-005` | Fleet Assignment and Fleet Repair remain distinct ordered positions/events even when both are explicitly empty. | Accepted |
| `STG-DEC-006` | Accepted Fleet Repair advances to the unchanged catalog Reserve Designation position (`ActorRole=FirstActingSide`, `ActiveSide=null`) and stops. Legal-action and observation projection derive the active side from the retained pair-keyed stage order without mutating the position; `RESERVE-001` remains separate. | Accepted |
| `STG-DEC-007` | Current Stage 1 admission does not broaden general opening convoy or later-stage Initiative Declaration support; any checkpoint that depends on them rejects until Task 3.2 is completed. | Accepted |
| `STG-DEC-008` | The empty policy is repository-synthetic evidence, not a canonical claim that published scenarios have no obligations; the recognized `has-obligations` policy value is unsupported and fails closed, while unmodelled positive subjects are outside v1 detection claims. | Accepted |
| `STG-DEC-009` | Adopt a ruleset ruling that an exact setup assertion may resolve an otherwise mandatory but empty stage-entry position through a mechanic-specific event; without that assertion the position is unsupported. The ruling protects `STG-AC-001`, `STG-AC-002`, `STG-AC-004`, `STG-AC-005`, `STG-AC-006`, `STG-AC-009`, and `STG-AC-010`. | Accepted |
| `STG-DEC-010` | Preserve Legal Actions v1 binding: the action-set envelope carries campaign/state/ruleset/position/audience, candidates carry contract version/action ID/kind, and submissions carry contract version/campaign/expected state/expected position/audience/action ID. Stage Entry adds no duplicate ruleset or kind fields to submissions. | Accepted |
| `STG-DEC-011` | Because v1 has no positive-subject domain contract, its negative acceptance evidence covers missing policy and recognized-but-unsupported obligation policy kinds, not detection of unmodelled arrivals, ships, projects, or units. | Accepted |

### STG-TASK-001 disposition request

The post-`EXR-TASK-014` audit found no evidence requiring a change to `STG-DEC-001` through
`STG-DEC-011`. The Task 001 recommendation is therefore **Accept all eleven decisions as written**.
The project owner accepted all eleven decisions as written on 2026-08-24. Any later revision returns
the affected requirement, task, and contract rows to planning before dependent production work
continues.

The proposed ruling identities are:

- ruling: `cna-1979.1.ruling.explicit-empty-stage-entry-resolution`;
- conflict: `cna-1979.1.conflict.empty-stage-entry-phase`;
- alternative: `reject-empty-stage-entry-as-unsupported`;
- alternative: `resolve-explicitly-admitted-empty-stage-entry`; and
- selected behavior: `resolve-explicitly-admitted-empty-stage-entry`.

Its canonical source set is `spi-1979-land-rules:5.2` plus
`sandtable-rules-lab:stage-entry.no-obligations.v1`. Mechanic-specific event sources are frozen as:

- Organization: `spi-1979-land-rules:5.2.organization` plus the synthetic source;
- arrival: `spi-1979-land-rules:5.2.naval-convoy-arrival` plus the synthetic source; and
- Fleet Assignment and Fleet Repair: `spi-1979-land-rules:5.2.commonwealth-fleet` plus the synthetic
  source.

The ruling does not authorize a missing-policy default, positive-subject resolution, a combined
phase event, or a reusable generic advancement command.

## Recommended contract boundary

Add one versioned `CampaignStageEntryPolicy` to setup identity. Its first contract version binds one
exact `(GameTurn, OperationStage)` and four closed obligation assertions:

```text
organization = explicit-none
naval-convoy-arrival = explicit-none
fleet-assignment = explicit-none
fleet-repair = explicit-none
```

The policy's one shared canonical source list backs every assertion and contains the
repository-synthetic reference `sandtable-rules-lab:stage-entry.no-obligations.v1`. The setup's
canonical bytes/hash, creation history, snapshot, projection validation, and replay preparation all
retain and revalidate the exact policy. A policy for a different pair or any non-`explicit-none`
subject is unsupported in v1.

The authoritative path is:

```text
Organization
  -> ResolveNoObligationOrganization
  -> Naval Convoy Arrival
  -> ResolveNoObligationNavalConvoyArrival
  -> Commonwealth Fleet Assignment
  -> ResolveNoObligationFleetAssignment
  -> Commonwealth Fleet Repair
  -> ResolveNoObligationFleetRepair
  -> catalog FirstActingSide Reserve Designation (stop)
```

All four candidates belong to the trusted `system` audience. Side action sets remain empty at these
positions. Each accepted action rechecks current policy, phase, pair, ruleset/setup identity, and
expected successor; emits exactly one event; increments state version once; and preserves world,
initiative, stage order, Weather, and random state. Rejection emits zero events. The final successor
is the unchanged catalog Reserve position with null stored `ActiveSide`; Legal Actions and
observation projection resolve `FirstActingSide` from `CampaignOperationStageOrder`, preserving
snapshot validation and future `GetNext` equality.

## Implementation consequences

- clean-cut the setup/create/snapshot contract family to retain the new stage-entry policy;
- add the policy source/ruling to setup/ruleset canonical identities as appropriate;
- add four closed system action candidates, mechanic-specific commands/events, strict serializers,
  event factory checks, projection/replay validation, and current-state legal membership;
- generate system actions from current phase/policy rather than treating raw state-version numbers
  as the durable semantic contract;
- preserve the current single Organization barrier and two Fleet segment positions;
- expose no new side-safe data except the already public current position/audience state;
- add forged-policy/history, wrong-pair, stale, wrong-audience, successor, replay, and zero-event
  negatives; and
- stop before Reserve behavior, Movement, positive stage-entry subjects, or adapters.

## Confidence, limitations, and evidence that would change the decision

Confidence is high in the sequence order, the player-controlled nature of positive Organization and
Fleet Assignment, the Reserve cutoff, and the repository's inability to represent positive subjects.
Confidence is high that explicit fixture admission is consistent with the already adopted opening
and Weather policies.

The recommendation would change if a future modeled setup contract admits obligations for either
synthetic setup, if a primary source makes an apparently empty step mandatory in a way that changes
state, or if the first Reserve boundary cannot be derived unambiguously from retained stage order.

## Decision and delivered gate

The project owner approved `STG-DEC-001` through `STG-DEC-011` on 2026-08-24, activating the paired
[specification](../specs/operation-stage-entry-v1.md) and
[technical design](../design/operation-stage-entry-v1.md). The exact explicit-empty capability is
implemented and its final 619-test repository gate is green. The decision still does not authorize
positive Organization/Fleet mechanics, Reserve, Movement, or a generic preamble advance.

## Sources

- [Original 1979 Land Rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Official SPI rules-download index](https://www.spigames.net/rules_downloads.htm)
- [Operation-Stage Preamble source spike](operation-stage-preamble-spike.md)
- [Turn-preamble action boundary spike](turn-preamble-action-boundary-spike.md)
- [Weather Determination v1 specification](../specs/weather-determination-v1.md)

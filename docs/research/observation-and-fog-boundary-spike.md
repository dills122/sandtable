# Observation and Fog Boundary Spike

**Status:** Recommendation approved for `OBS-001`

**Date:** 2026-08-16

**Decision owner:** Project owner

**Capability affected:** `OBS-001`

## Executive conclusion

`OBS-001` should implement a deliberately conservative, immutable side-safe observation kernel:
public campaign timing and board topology, the acting side's own independently placed combat
elements, and no opposing-force facts. It should not pretend that this is the final faithful fog
model.

The original game does not use a simple "all enemy units are hidden" rule. Physical counters,
formation representation, Patrol and Reconnaissance, player-selected disclosures, and Dummy Tank
Formations combine to create a side-specific knowledge state. Sandtable does not yet model map
counters, attached-unit representation, patrol results, dummy identities, or remembered disclosed
facts. Projecting the live opposing `CampaignWorldSnapshot` would leak truth, while inventing
contacts directly from combat elements would encode a false counter model.

The recommended delivery sequence is therefore:

```text
OBS-001 conservative observation kernel
        |
        v
ACTION-001 own-side legal actions and stale enforcement
        |
        v
RECON-001 source-driven counter/contact/knowledge state
        |
        v
MOVE-001 movement and contact using faithful observed presence
```

This keeps the first outward boundary safe and useful for contract testing while preserving a clear
route to the real disclosure mechanics before opposing contacts affect playable movement.

## Question and decision boundary

The spike asks:

> What can `OBS-001` safely and truthfully expose from the current WORLD-001 state without either
> leaking authoritative opponent truth or inventing Patrol, Reconnaissance, and Dummy Tank behavior?

The answer matters now because every Maproom, Intelligence, notification, and future web API must
depend on a side-safe projection rather than authoritative snapshots or complete Content Packs.
Once transport contracts consume a mistaken visibility model, correcting it would require contract
migration and could expose hidden state in logs or clients.

In scope:

- primary-source disclosure and counter-representation evidence;
- current repository state and missing authority;
- an `OBS-001` boundary recommendation;
- explicit dependencies for faithful opposing contacts.

Out of scope:

- implementing observation code;
- transcribing rules prose or component art;
- defining Patrol, Reconnaissance, Dummy Tank, movement, or combat adjudication;
- Maproom, protobuf, HTTP, persistence, or notification DTOs;
- deciding final spectator or replay-redaction policy.

## Source hierarchy and method

Sources were applied in the adopted repository order:

1. [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
   visually inspected because the preserved PDF is image-only.
2. [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf).
3. Existing Sandtable specifications, designs, code, and tests at merged commit `743a604`.

The review focused on Land Rules Sections 4.2, 4.25, 16.0 through 16.5, and the errata addition to
Case 16.11. Only normalized behavior and source locators are retained here.

## Evidence

### Documented facts from the original rules

- **Counter identity is not the same as underlying force truth.** Land Rules 4.21 shows that unit
  counters carry visible counter-facing facts such as designation, unit type, parent formation,
  organization size, and stacking information. It does not place the complete TOE record on the
  counter.
- **Attached units are represented through a parent counter.** Land Rules 4.25 says a unit attached
  to another unit is not separately represented on the map; the parent unit's counter represents
  the attached unit. Therefore one authoritative combat element does not necessarily equal one
  visible opposing map piece.
- **Reconnaissance produces bounded disclosure.** Land Rules 16.1 through 16.3 make Patrol and
  Reconnaissance a procedure with costs, risk, results, and a defender-controlled disclosure step.
- **Dummy Tank Formations intentionally create false apparent identity.** Land Rules 16.4 permits
  dummy formations using plausible designations and says the opponent cannot know whether the
  apparent battalion is real merely from its map presence.
- **Disclosed facts are selected and constrained.** Land Rules 16.5 defines what unit facts are
  revealed, lets the non-phasing player choose qualifying units subject to restrictions, and
  excludes or constrains headquarters and anti-aircraft disclosure in several cases.
- **The errata changes eligible patrol units, not the knowledge model.** September 1979 errata
  Case 16.11 adds eligible vehicle and mechanized-infantry types. No reviewed erratum replaces the
  disclosure or dummy-formation procedures.

### Repository observations

- `CampaignWorldSnapshot` records exact mutable element ID and current location for every
  independently placed combat element. It is authoritative truth, not observed counter state.
- `ContentPackDefinition` contains exact opposing formation, element, organization, capability,
  topology, source, and scenario facts. It is intentionally forbidden as an outward DTO.
- The repository has no authoritative value for map-piece identity, attachments at runtime,
  Patrol or Reconnaissance outcome, dummy formation, disclosed fact, last-known contact, or
  observation memory.
- `CampaignSnapshot` contains the random seed and cursor. Initiative Determination already requires
  those values to remain absent from a player observation.
- Current rules-laboratory elements are all independently placed synthetic combat elements. That
  fixture can prove own-side filtering and deterministic projection, but it cannot prove the
  published game's opponent-counter behavior.
- No Maproom/public API exists yet. The Core observation boundary can be introduced without a
  transport compatibility commitment.

### Inferences

- Filtering `CampaignWorldSnapshot.Elements` by side is safe for own-force projection because a
  player is entitled to their own authoritative force state.
- Exposing opposing combat-element IDs or static definitions would conflate hidden TOE truth with
  visible counter information and would make Dummy Tank behavior impossible to represent honestly.
- Deriving opaque enemy contacts directly from live elements would still leak the number and
  location of real elements, bypass attachments and dummies, and provide no place for remembered
  stale information.
- An empty-opponent conservative observation is less informative than physical play, but it is
  explicitly incomplete rather than falsely authoritative. It can be extended only after the
  missing knowledge state is event-sourced.
- Public topology can be copied into a dedicated observation contract because terrain and map
  structure are not opponent force truth. Source/provenance metadata and the complete Content Pack
  must remain excluded.

### Unknowns requiring later source work

- Which counter-facing facts remain inspectable in every stacking and formation circumstance.
- Whether every map-piece location is public or some physical concealment convention applies
  outside Sections 16.0 through 16.5.
- The exact lifetime of disclosed reconnaissance facts and whether any become stale or are
  forgotten.
- How Patrol information combines with later movement, contact, dummy removal, combat, and
  Chronicle replay.
- Whether side-specific charts or off-map records create additional player-private information.

These unknowns block faithful enemy-contact projection, not the conservative `OBS-001` kernel.

## Options considered

| Option | Privacy safety | Rules fidelity | Delivery cost | Main failure mode | Decision |
| --- | --- | --- | --- | --- | --- |
| Expose live opponent combat elements and locations | Poor | Poor | Low | Leaks real elements and defeats attachments/dummies | Reject |
| Invent opaque contacts from live element locations | Medium | Poor | Medium | Still leaks real count/location and has no knowledge history | Reject |
| Hide every opponent fact permanently | High | Poor | Low | Would erase Patrol, Reconnaissance, and visible counter play | Reject as final model |
| Conservative OBS-001 now; event-sourced knowledge before movement/contact | High | Honest incremental fidelity | Moderate | Requires a named later capability | **Recommend** |
| Implement full Patrol/Reconnaissance/Dummy model before any observation | High | Highest | High | Delays legal-action and outward-contract scaffolding substantially | Defer |

## Recommended OBS-001 contract boundary

The first observation should contain only:

- observation contract version;
- campaign ID and state version;
- canonical public ruleset identity plus scenario ID; exact Content Pack identity remains
  server-side admission data because its hash fingerprints opponent-only content;
- observing side;
- public Game Turn, Operation Stage, phase/segment/step identifiers, active side, and determined
  Initiative holder when present;
- public topology copied into observation-specific location and edge values;
- the observing side's own independently placed elements, with stable IDs, current locations, and
  the minimal own-force static facts needed for the next legal-action slice;
- an explicit observation-policy identifier showing that opponent contacts are not yet modeled.

It must exclude:

- every opposing formation, element, organization, capability, and element-to-location occupancy
  association; public topology location IDs remain visible independently;
- placeholders, counts, or inferred contacts derived from authoritative opponent elements;
- complete Content Pack, campaign snapshot, world snapshot, random state, setup sources, rule
  references, source expressions, and presentation labels;
- legal actions, recent events, narrative, notifications, and transport-specific fields.

The projection remains a pure Core query:

```text
CampaignSnapshot + exact CampaignContentContext + observing LandSide
    -> immutable CampaignObservation
```

It validates the same exact executable/content/world checkpoint used by authority, consumes no
randomness, performs no I/O, and never mutates state.

## Required follow-on capability

Before `MOVE-001` exposes opposing presence to players, add a source spike and specification for
`RECON-001` (working name). The Umpire may and must continue to adjudicate against authoritative
truth; only player-visible observations, legal-action presentation, diagnostics, logs, and adapters
are limited to approved apparent presence. The follow-on capability should define:

- authoritative map-piece/counter representation separate from combat elements;
- attachment and parent-counter visibility;
- dummy formation identity and lifecycle;
- Patrol/Reconnaissance commands, events, costs, losses, and disclosed facts;
- side-specific remembered knowledge and staleness;
- projection of current and last-known opposing contacts;
- replay and negative fog tests for both sides.

`RECON-001` may be folded into a broader movement/contact source package if research shows that the
mechanics cannot be separated cleanly. The stable requirement is the knowledge boundary, not the
working capability name.

## Decision criteria and acceptance implications

Adopting the recommendation means `OBS-001` is complete only when tests prove:

1. Both sides receive byte-identical results for equal state, content, and side.
2. Reversing content/world input order does not change semantic or canonical observation bytes.
3. Own-side elements and locations are complete and correct.
4. Paired positions that vary only opponent identities, static facts, counts, or placements produce
   byte-identical observations with no exemption; no opponent occupancy association, contact, or
   placeholder appears in values or canonical JSON.
5. Random seed/cursor, rules sources, setup sources, full content/world values, and presentation
   text are absent by type and by serialized negative tests.
6. Invalid side, invalid checkpoint, or mismatched exact content rejects without partial output.
7. Projection performs no I/O, mutation, randomness consumption, or Intelligence call.
8. Documentation states that faithful opposing contacts remain a prerequisite for player-visible
   movement/contact choices, while the Umpire continues to adjudicate from authoritative truth.

## Confidence and limitations

**Confidence: high** that the current authoritative element state cannot safely stand in for
opponent-visible counter/knowledge state. The primary rules explicitly separate map
representation, reconnaissance disclosure, and dummy identity.

**Confidence: high** that an own-side-only observation kernel is privacy-safe and useful for
contract, serialization, deterministic-query, and legal-action scaffolding.

**Confidence: moderate** that public topology should be copied in OBS-001 rather than joined later
at Maproom. This is reversible because the observation contract is not yet transported or persisted.

**Limitation:** the complete rules set was not exhaustively re-audited for every player-private
record. The spike inspected the sections directly relevant to counter representation and
reconnaissance and records the remaining source questions above.

## Owner decision

The project owner approved these two linked decisions on 2026-08-16:

1. `OBS-001` is a conservative own-side observation kernel and deliberately emits no opponent
   contacts.
2. Faithful opponent presence requires an explicit, source-driven knowledge/contact capability
   before movement/contact becomes playable.

The governing `OBS-001` specification now carries the implementation requirements. Do not
implement from this research packet alone.

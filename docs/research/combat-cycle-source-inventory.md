# Combat and Continual-Cycle Source Inventory

**Status:** Source inventory, `CMB-RSH-001` result-surface research, and `CYCLE-RSH-001` cycle
identity/history decision complete; production remains gated by later combat and Reserve Release
design

**Date:** 2026-08-25

**Decision owner:** Project owner

**Research work item:** `COMBAT-CYCLE-001`

## Executive conclusion

Source sequence, one deterministic synthetic combat vector, required fact categories, RNG/table
evidence, Reserve Release rules, and the cycle-control architecture can be researched now. Final
combat contracts and implementation tasks must wait for approved ZOC/Reaction, Breakdown, and
Contact/Engaged identity because combat opportunity, retreat/resumption, disclosure, and repeat
eligibility depend on them.

The current Sprint 5 bullets are capability headings, not implementation-sized tasks. They must not
be treated as executable work until the research/design gates below produce a reviewed package.

## Source index

| Source | Locator | Use |
| --- | --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | 5.2; 8.21-8.25; 10.31-10.36; 11-15; 17.2; 18.13-18.26 | Cycle sequence, repeat eligibility, combat procedure, morale, and Reserve Release |
| Common charts | 6.3; 11.4; 12.6; 14.6; 15.79; 15.89; 17.4 | CP, strength, resolution, loss/prisoner, and morale rows |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 8.23; 12.44; 14.47-14.48; 15.27; 15.4; 15.53; 15.56; 15.79; 15.88 | Corrected eligibility, targeting, armor, Close Assault, and result behavior |

## Source and repository findings

**Documented fact:** One cycle contains Movement, Breakdown, Combat, and Reserve Release; the
phasing side may repeat the cycle subject to movement/combat eligibility. Combat includes private
pre-resolution choices, simultaneous Anti-Armor effects, sequential Close Assault instances, and
loss/retreat/contact/engagement outcomes. Reserve I/II release changes later movement/combat
eligibility.

**Repository observation:** The Land catalog has one linear pass then advances. Content lacks TOE
strength/composition, combat ratings, Basic Morale, ammunition/fuel, and gun-position capability.
World state lacks losses, pinning, Contact/Engaged, pending private choices, attacked-target history,
cycle ordinal, retreat/release history, and prisoners/captured equipment. Observation/action/event
and RNG contracts have no combat vocabulary.

**Inference:** Keep structural Land positions stable and add authoritative cycle ordinal/history
rather than duplicating an arbitrary number of positions. Reserve Release should expose deliberate
`repeat-cycle` or `finish-movement-combat` actions once mandatory obligations clear. Current
Movement/Combat continuation determines whether repeat is legal; per-stage attacked-target history
retains committed evidence but does not prohibit a source-legal repeated pair.

## Private and simultaneous choice boundary

Recommended protocol: trusted-Umpire sealed typed submissions. Open one combat-opportunity identity
bound to a frozen base state/hash; accept each side's private envelope without mutating combat
facts; resolve only when all required choices exist or a separately approved deterministic fallback
applies. Public/side-visible revision semantics follow the existing audience-redaction pattern.

- Each envelope binds campaign/rules/config, audience, opportunity, participants/target, cycle
  ordinal, and the same pre-state hash.
- The first envelope cannot change eligibility used by the second.
- Simultaneous effects calculate from the same pre-loss state and apply atomically in canonical
  order; separate Close Assault instances remain sequential.
- Stale, duplicate, cross-audience, changed-contact, or mismatched-base submissions reject without
  combat mutation.
- Cryptographic commit/reveal is unnecessary while the Umpire is trusted; a synchronous batch is
  acceptable only as an internal resolver input after the sealed envelopes exist.

## Minimal synthetic evidence vector

Use one adjacent non-Reserve infantry battalion per side in Clear terrain, no fortification,
artillery, armor, trucks, air, attachments, or pre-existing special state. Give both sufficient CPA,
Cohesion 0, synthetic Basic Morale 0, 10 TOE strength, and symmetric Close Assault rating 1. The
defender declines Retreat Before Assault and both commit all strength to Close Assault. Retain
fixed deterministic draws that exercise morale, a zero-loss attacker result, a defender loss, and a
safe one-hex retreat.

This is evidence, not a production legality shortcut. Every random result reachable from any
admitted table coordinate must be implemented, or the campaign must reject before mutation under a
separately approved bounded rule. Legality cannot depend on a future favorable seed.

## Required fact inventory

Static rules/content includes current/max TOE/component strength; relevant Barrage, Vulnerability,
Anti-Armor, Armor Protection, offensive/defensive Close Assault, AA, Basic Morale, unit/component
class, ammunition/fuel requirements, organization/attachment participation, normalized table rows,
and per-row provenance/errata.

Mutable authority includes current TOE/component strength; CP/Cohesion/Disorganization; pin;
Contact/Engaged; ammunition/fuel; gun position; combat opportunity/target; sealed choices and
pre-state hash; retreat path; per-stage attacker-target history; cycle ordinal; Reserve I/II release
history; losses/prisoners/captured equipment; and RNG cursor.

Authoritative Chronicle may retain all bindings, choices, rolls, table coordinates/modifiers, and
before/after state. Side-visible Chronicle/observation is a separate projection and must not expose
opposing exact TOE, ratings, morale/Cohesion, withheld force, ammunition, bindings, or sealed choice.

## Research/design dependency graph

Research that can proceed now:

1. `CMB-RSH-001` — **complete:** the
   [Combat rules and result surface spike](combat-rules-result-surface-spike.md) normalizes the
   selected infantry Close Assault calculation, Morale closure, five reachable differential
   columns, semantic outcomes including the reachable two-hex Retreat, and errata boundary without
   freezing contracts or reproducing the source chart.
2. `CMB-RSH-002` — choose combat Content/static schema and synthetic values.
3. `CMB-RSH-003` — choose mutable loss/prisoner/ammunition/Disorganization boundary.
4. `CMB-RSH-004` — freeze RNG draw order and golden vectors.
5. `CYCLE-RSH-001` — **complete:** the
   [Continual-cycle identity and attacked-target history decision](continual-cycle-identity-and-history-decision.md)
   freezes the phase-local ordinal, repeat/finish closure, ordered repeat-permitting attacked-target
   history, replay/snapshot boundary, and fog-safe identity/projection requirements without
   authorizing contracts.
6. `RESREL-RSH-001` — freeze Reserve I/II release eligibility/history and movement handoff.

Design that waits for approved Contact and Breakdown boundaries:

7. `CMB-DES-001` — combat opportunity/target/participant identity.
8. `CMB-DES-002` — sealed decision/event/readback protocol and fallback.
9. `CMB-DES-003` — position/barrage/Retreat Before Assault/force-assignment transitions.
10. `CMB-DES-004` — simultaneous Barrage/Anti-Armor and sequential Close Assault boundaries.
11. `CMB-DES-005` — loss/retreat/capture/Contact/Engaged projection and Chronicle redaction.
12. `CYCLE-DES-001` — Reserve Release to repeat/finish plus Exercise/Maneuver evidence.

Only after those gates should Sprint 5 receive implementation-sized tasks, acceptance criteria, and
an independent plan review.

## Retained unknowns

- exact Contact-derived combat-group identity and outward disclosure;
- whether the first combat vertical normalizes the entire reachable selected table surface or a
  different safely closed subset;
- minimum ammunition/supply state;
- whether prisoner/captured-equipment outcomes enter the first skeleton;
- deterministic fallback timing for private decisions; and
- Breakdown/Retreat BP continuity and broken-vehicle consequences.

# Combat and Continual-Cycle Source Inventory

**Status:** Source inventory, `CMB-RSH-001` result-surface research, `CMB-RSH-002` static-content
research, `CMB-RSH-003` mutable-state research, `CMB-RSH-004` RNG/golden research, and the
`CYCLE-RSH-001` cycle identity/history decision and `RESREL-RSH-001` release research are complete
and decision-ready; `CMB-DES-001` identity, `002` protocol, `003` steps, `004` cost/resolution and
`005` settlement/disclosure plus `CYCLE-DES-001` composition are complete for review. Production
remains gated by pending Contact/Combat/Reserve policy, combined contracts and implementation-plan
gates.

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

**Historical repository observation (2026-08-25):** The Land catalog has one linear pass then
advances. Content lacks TOE strength/composition, combat ratings, Basic Morale, ammunition/fuel,
and gun-position capability.
World state lacks losses, pinning, Contact/Engaged, pending private choices, attacked-target history,
cycle ordinal, retreat/release history, and prisoners/captured equipment. Observation/action/event
and RNG contracts have no combat vocabulary.

**2026-09-06 reconciliation:** ZOC delivery now supplies component classification, maximum/current
TOE and defensive Close Assault ratings. The [mutable-state spike](combat-mutable-state-spike.md)
maps that current authority and the remaining Combat obligations at merged PR #88. Its proposals
remain owner-review inputs, not production schema approval.

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

Static Content includes maximum TOE/component strength, immutable unit/component classification,
combat ratings, Basic Morale, source-parent/organization facts, and per-datum provenance. Static
Rules includes eligibility semantics, calculations, normalized table rows, and per-row
provenance/errata. Current TOE/component strength is not static.

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
   columns, semantic outcomes including one-hex Retreat, and errata boundary without
   freezing contracts or reproducing the source chart.
2. `CMB-RSH-002` — **decision-ready:** the
   [Combat Content and static schema spike](combat-content-static-schema-spike.md) selects
   component-granular immutable facts, bounded synthetic values, validation boundaries, and
   explicit mutable/ZOC deferrals without freezing a production schema.
3. `CMB-RSH-003` — **decision-ready:** the
   [Combat mutable-state spike](combat-mutable-state-spike.md) reconciles current TOE/ledger
   authority, full-game ammunition, loss/capture conservation, immediate custody, retreat, and
   Cohesion consequences. Includes synthetic arithmetic evidence; owner approval remains pending.
4. `CMB-RSH-004` — **decision-ready:** the
   [Combat RNG/golden spike](combat-rng-golden-spike.md) proposes role-ordered dice on the existing
   campaign stream, with conditional capture, twelve seeded vectors, and exhaustive draw-domain
   counts. Full loss-table normalization and production contracts remain design-gated.
5. `CYCLE-RSH-001` — **complete:** the
   [Continual-cycle identity and attacked-target history decision](continual-cycle-identity-and-history-decision.md)
   freezes the phase-local ordinal, repeat/finish closure, ordered repeat-permitting attacked-target
   history, replay/snapshot boundary, and fog-safe identity/projection requirements without
   authorizing contracts.
6. `RESREL-RSH-001` — **decision-ready:** the
   [Reserve release/history spike](reserve-release-history-spike.md) proposes first/later release
   obligations, persistent stage ceilings and offensive-use limits, II combat-DP handoff, and the
   immediate next-Movement exception. Full lifecycle activation and owner rulings remain gated.

Design following implemented ZOC/Reaction and Breakdown boundaries (Contact/Engaged lifecycle
remains part of these design gates):

7. `CMB-DES-001` — [combat opportunity/target/participant identity](../design/combat-opportunity-identity-v1.md)
   **complete for review:** bounded singleton Close Assault identity, separate target-hex use and
   unit history, Contact/Engaged membership, fog-safe reference requirements and extension gates.
8. `CMB-DES-002` — [sealed decision/event/readback protocol and fallback](../design/combat-sealed-decision-protocol-v1.md)
   **complete for review:** typed singleton assignments, private/audience revision separation,
   persisted deadline/cancellation, pre-RNG commitment and strict restart/readback requirements.
9. `CMB-DES-003` — [position/barrage/Retreat Before Assault/force-assignment transitions](../design/combat-step-transitions-v1.md)
   **complete for review:** bounded decline-only infantry path, provisional selection, six ordered
   closure proofs, exact prepared-round suffix and no-attack versus settled completion. Actual
   RBA movement, gun/Barrage and broader assignment paths remain explicit extension gates.
10. `CMB-DES-004` — [cost/resolution ordering](../design/combat-cost-resolution-order-v1.md)
    is complete for review: atomic committed use, deterministic result publication, settlement
    handoff and explicit simultaneous-stage/sequential-assault extension requirements.
11. `CMB-DES-005` — [settlement/disclosure](../design/combat-settlement-disclosure-v1.md)
    is complete for review: causal loss/retreat/custody closure, scripted mandatory fallback,
    original-participant relations and side-specific Chronicle/decision projections.
12. `CYCLE-DES-001` — [cycle/Reserve composition](../design/continual-cycle-reserve-composition-v1.md)
    is complete for review: identity/prefix and audience bindings, persistent release restrictions,
    repeat/finish control, carried obligations and occurrence-aware Exercise/Maneuver evidence.

The [policy register](../design/combat-cycle-policy-reconciliation.md) reconciles those design inputs
into eight explicit owner choices. The [combined contract/implementation plan](../design/combat-cycle-implementation-plan.md)
now defines 25 staged tasks, contract ownership and acceptance evidence. Owner accepted the eight
policies and corrected plan on2026-09-06; TASK-001 source/calendar/break-off verification is in
progress, with later tasks and exact contract freeze still ahead.
[Review4](../reviews/combat-cycle-plan-review-4.md) records the combined assessment and author correction.

[Independent design review 2](../reviews/combat-design-review-2.md) is Ready for the
bounded `CMB-DES-001`/`002`/`003` inputs, with no actionable findings. This checkpoint does not replace
the combined implementation-plan review or approve pending policy/production contracts; it does
not cover subsequent DES-004/005. [Independent design review 3](../reviews/combat-settlement-review-3.md)
returned Ready for those bounded inputs, with no actionable findings; subsequent CYCLE-DES-001
is outside that checkpoint. User authorized review4, which found one missing ordinary break-off
handoff in cycle/combined planning. Author correction assigns it without claiming an independent
Ready reassessment; remaining policy/production gates stay open.

## Retained gates and deferrals

- `CMB-POL-001`–`008` and the corrected plan were accepted by owner on2026-09-06. Custody,
  fallback and disclosure policy is settled for the bounded profile; exact contract/source
  verification and broader capability gates remain.
- `CMB-TASK-001`–`004` own complete selected-table source verification, calendar mapping, exact
  Content/world/command/event/snapshot/side/Exercise contracts and compatibility vectors.
- The proposed table choice covers the entire reachable selected surface; another profile would
  require a new closed design. No favorable seed or unhandled capture may narrow admission.
- General resupply, prisoner upkeep/maturity execution, vehicle retreat, real RBA, released-Reserve
  offensive Combat and hosted decisions remain extensions. Bounded Truck Convoy entry retains
  future obligations; it is not a complete playable campaign.
- Review4 returned Not ready at9f683d1. Its ordinary break-off finding is addressed by the author
  in cycle design and TASK-001/003/018/019; owner accepted that correction on2026-09-06. User-approved review
  budget is exhausted at4of4; no fifth review or independent reassessment is implied.

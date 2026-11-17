# Graziani abstract supply: capture, depletion and discovery

**Status:** bounded source analysis for LA-12, not an accepted capture state
machine or proof of six-turn contact. The source explicitly gives an entry
trigger and immediate/free use, but the inspected text does not define a
complete durable ownership, allocation, depletion or dummy-discovery protocol.
Preserve unknowns rather than silently transferring an inventory, deleting it
or waiving the printed use language.

## Original source boundary

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14, 18 and 43 §§8.13, 10.29 and 32.12–.18, and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 4–5 / printed pp. 28–29 §60.92(B–C). SHA-256 values match
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
The [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
SHA-256 matches `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
a bounded extracted-text search for §32.1, supply units, capture, dummy and
§10.29 found no explicit correction to these cases; §32.0's erratum warns the
abstract rules were untested. Extraction gaps, other provisions and later
errata remain possible. Same source edition as previous reviews, not a blind
independent edition.

## What is printed

| Boundary | Source observation | Unsettled implementation consequence |
| --- | --- | --- |
| Real supply representation | §32.12 represents a Supply Unit with a Supply Dump counter and describes it as an abstract substitute for supply points. §32.13 says it has no combat capabilities. | Identify a real unit, its owner, actual location, cargo and counter independently; a map marker alone is not a public truth about its contents. |
| Enemy entry | §32.13 says if any **enemy combat unit enters** a Supply Unit's hex, the Supply Unit is captured and its supplies used **immediately and freely**. | Preserve both capture and the explicit immediate/free-use phrase. It does not specify a detailed resource allocation, unused remainder, long-lived ownership, whether all points are exhausted, or what to do with a carrier. Don't automatically make the captured counter a normal friendly depot, award arbitrary future stock, erase everything, or charge an ordinary supply cost without an accepted interpretation. |
| Capacity and empty unit | §32.15 says a unit initially contains and may never contain more than 40 ammo and 60 fuel; it tracks expenditures and is removed when **both** ammo and fuel reach zero. | `0 ammo, positive fuel` and `positive ammo, 0 fuel` are not both empty. Whether free captured-use reduces both balances, permits partial use or creates a specific removal point is not specified by §32.13 alone. Reject negative/over-cap state under an accepted schema. |
| Rearrangement | §32.15 permits no-cost rearrangement among Supply Units in the **same hex** during Organization. | This is a separate source-authorized procedure, not proof that capture automatically redistributes stock across hexes or during another phase. Define conservation, owner and target capacity. |
| Destruction | §32.17 gives a separate attempt involving CP, one die per supply unit, target-specific success and a timing/once-per-subsegment restriction. | An attempt to destroy, its failed/successful roll, ordinary depletion and enemy capture are distinct transitions; don't substitute an automatic capture roll or apply destruction losses twice. |
| Dummy | §32.18 allows one dummy per three received and/or scenario-start supply units, moves it like the real thing with less motorization, and says it is removed **upon discovery**. | It does not in this passage enumerate every discovery trigger or say a dummy carries real ammo/fuel. Reconcile when entry, ZOC, patrol or other procedures discover it, and order discovery/removal relative to capture. General entitlement versus scenario dummy grants remains separately disputed. |

Scenario §60.92(B) lists for example three real Axis Tobruk units, each with
15 ammo and 50 fuel, and two Bardia units, each 10 ammo and 40 fuel. It also
lists two groups of three dummy counters under the specified Dump 1/2 placement
rules. §60.92(C) lists five Commonwealth Matruh real units, each 40 ammo and
60 fuel, and a group of three dummy counters under its Dump 1 rule. These are
initial printed quantities, not proof of legal co-location, current contents,
additional optional loads, capture reachability or dummy identity visibility.
The separate Derna dash and source-specific stacking ambiguity must not be
silently turned into an empty counter or numeric zero.

## Rule interactions requiring a decision

1. **Occupied-hex entry versus specific capture.** §8.13 generally says a
   unit may not enter an enemy-occupied hex, while §32.13 describes enemy
   combat entry into a Supply Unit's hex. Specify a reviewed hierarchy or
   exception and its timing: removing a defender, entry, capture, immediate
   use and any remaining movement/combat cannot be silently reordered. Test
   an actual combat-unit entry with a real supply unit as well as a hex where
   a combat defender remains; source-specific conditions and complete state
   decide legality. A lone supply counter is still a unit for this conflict;
   calling the hex “empty” merely because it lacks a combat defender begs the
   question.
2. **Enemy ZOC capture may be a different trigger.** §10.29 says that if a
   non-combat unit is **alone** in a hex in enemy ZOC at any time during the
   enemy Movement/Combat Phase and has no strength of any type, it is captured.
   §32.13 calls a Supply Unit non-combat with no combat capabilities, but
   whether its cargo counts as “strength of any type,” what “alone” means for
   multiple real/dummy or accompanying counters, and hierarchy with §32.13
   require a ruling. Thus the earlier [SV-01 vector](2026-11-12-graziani-supply-authority-vectors.md)
   must not be read as a global rule that an adjacent unit never captures:
   adjacency **alone** does not establish qualifying ZOC, phase or conditions,
   yet a qualifying ZOC may trigger §10.29 without entry. Test both candidate
   interpretations and unknown classification; don't silently normalize a
   missing ZOC to no capture.
3. **Discovery and privacy.** A dummy discovered under an accepted procedure
   is removed under §32.18. Neither “enemy adjacency” nor an unreviewed patrol
   outcome automatically proves discovery, and no free-use stock should be
   invented from a dummy. A real/dummy mixed stack must not leak identity or
   hidden amounts through action availability, capture offers, error reason,
   event count/order, stable IDs, timing or intelligence-plane projection.
   Permit only source-authorized disclosure at a defined point; actual
   authoritative state and audience-specific views remain distinct. At a
   potential collision of discovery, entry, capture and removal, version event
   ordering and avoid both a real-capture event and a duplicate dummy removal.
4. **Carrier and stock conservation.** §32.32 allows motorization to carry
   supply units, and §32.56 says motorization assigned to a captured unit is
   also captured and may be used by the enemy. As the [independent check](2026-11-17-graziani-supply-capture-independent.md)
   records, this supports capture of validly assigned points, not unrelated
   co-located points. Which points were assigned, where they stand,
   any immediately used resources, and surviving contents require an explicit
   shared-snapshot transition. Journal original stock = remaining + actually
   used/destroyed/transferred under the chosen interpretation; free use means
   no invented cost, not permission to create quantities. Preserve identity,
   source, actor and privacy without duplicating a carried or captured unit.

## Proposed distinguishing tests

These are unexecuted conditional vectors, not claims of reachable historical
positions:

- Enemy combat entry of a real unit with `(15 ammo, 50 fuel)` under each
  candidate immediate-use policy; no duplicate capture on replay. Compare
  noncombat entrant, combat defender still present, and unrelated adjacency
  with or without a **qualifying** enemy ZOC and correct phase. Verify
  §8.13/§32.13 and §10.29 precedence before declaring any legal result.
- `0/positive`, `positive/0`, and `0/0`: only the last meets §32.15's
  both-zero removal condition; don't seed an already invalid empty state as a
  valid persistent counter. Test exact consumption, unknown remainder,
  out-of-range values, same-hex Organization redistribution and atomic
  conservation, including a crash or retry.
- Undiscovered dummy, known discovery, real-plus-dummy and multiple same-hex
  counters, real carried by motorization and unknown identity. Separate
  authorized discovery and empty-combat reports from omniscient inspection;
  compare equal-authorized histories through all player/intelligence views,
  including errors and event timing.
- Phase/ZOC at start and after enemy movement, excluded noncombat or inadequate
  projection, simultaneous changes, missing occupancy and state coverage;
  preserve unsupported where the rule hierarchy or relevant data is missing.

Before admission, source-review the exact capture/use meaning, general versus
specific entry and ZOC interactions, dummy discovery, carrier treatment,
resource identity, legal placement and authoritative/visible event ordering.
No code, runtime state or readiness flag changed; no gameplay tests ran.

# Loaded motorization, abstract draw and air prepayment

**Disposition:** A coherent Land-only candidate preserves §59.66(B)'s initial
carried stock and its priority for a combat unit's own qualifying expenditures,
with §32.16 governing Supply Unit access when draw is permitted. This is a
proposed interpretation, not established precedence. Section 32.37's 12-Fuel
air-transport prepayment is a separate player obligation naming a Supply Unit
in the origin hex as its source; loaded combat-unit cargo is not established
as a substitute. Splitting that payment and using the transported Supply Unit
itself as payer remain unresolved.

## Evidence and mode boundaries

- Visual inspection of [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  printed p. 26 / PDF p. 2, §§59.62–.66: §59.62 enables the following initial
  setups whenever Logistics is abstracted, whether or not Air is abstracted.
  Section 59.66(B) lets each eligible attached point start with one Ammo or
  three Fuel, then requires the unit to consume these supplies first and says
  it may not draw on a Supply Unit to meet any supply expenditure requirement.
  It prohibits transferring these supplies to a Supply Unit. Its explicit
  “only while” duration qualifies the loaded-point detachment restriction;
  it does not independently settle duration of the draw restriction.
- Visual inspection of [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  printed/PDF p. 43: §32.0 applies when Land is played without Air and Logistics,
  replacing Land and Air supply expenditures with §32 costs. Section 32.11
  says to ignore references to usage and expenditure of Ammo, Fuel, Stores and
  Water and that only its following supply rules apply. Sections 32.15–.16
  describe Supply Unit stocks and draw by a friendly combat unit within the
  specified half-CPA route; §32.23 expressly describes movement fuel as
  expended from a Supply Unit. Section 32.37 requires the owning player to
  expend 12 Fuel before air transport from **any supply unit in the hex of
  origin**. It later permits draw from the transported unit under §32.16 while
  prohibiting movement of it or its supplies for that OpStage.
- Section 59.62 establishes applicability of the scenario setup cases in both
  abstract-Logistics modes; it is an **enabling provision**, not an express
  statement that every later sentence overrides §32.11 or §32.23. Conversely,
  §32.11's broad language is evidence for a competing exhaustive reading, not
  an express repeal of the scenario's special loaded-cargo rule. The coexistence
  of the two provisions does not itself decide precedence. A game with Air
  played and Logistics abstracted needs its own applicable Air rules checked;
  §32.0's Land-only analysis and §32.37 cannot simply be imported into it.

## Candidate and limits

The candidate gives effect to the specifically authorized optional scenario
inventory and its explicit first-use restriction, treating general §32 source
draw as subject to that special case. It also leaves §32's expense amounts,
timing and route checks in force where applicable. An alternative treats
§32.11 and §32.23 as exhaustive in Land-only play, with their Supply Unit
source displacing the carried-stock expenditure and draw restriction. The
printed texts do not explicitly rank these readings. Record a versioned ruling
before making either authoritative; do not present this candidate as a printed
exception or as a decision already adopted.

Even under the candidate, fuel-only cargo with an ammo need, insufficient
matching cargo, partial use and the point at which ordinary draw resumes are
unresolved. The special restriction applies to the **unit** and its supply
expenditure; §32.37 describes a **player** paying for transport from a Supply
Unit. It does not say the transported Supply Unit is a combat unit drawing
under §32.16, or that nearby loaded points pay the 12 Fuel. Nor does §59.66(B)
authorize transferring their cargo into the payer. Apply the independent
origin, destination, stage, enemy-distance and other conditions in
[air-supply witness](2026-11-17-graziani-air-supply-witness.md); sufficient
stock alone proves no shipment.

“Any supply unit” does not expressly answer whether multiple origin units may
split 12 Fuel, nor whether the unit being transported may itself pay. A
separate route to a single payer may be possible first: §32.15 permits
rearrangement among Supply Units in the same hex during Organization Phase.
Two units holding less than 12 each could therefore consolidate enough fuel
into one payer, subject to applicable timing and other conditions. This is
distinct from direct split payment. See the [independent review](2026-11-17-graziani-loaded-fuel-mode-precedence-review.md).
A separate origin unit with at least 12 Fuel provides a conditional, unambiguous
arithmetic witness, subject to all other legality and stock checks. If a
self-paying interpretation is considered, also decide what happens when the
prepayment reduces both its Ammo and Fuel to zero: §32.15 removes a Supply Unit
then. Neither apparent self-payment nor that removal rule proves a transport
can proceed without a surviving cargo counter. Do not infer split payment,
self-payment, or a successful shipment from the presence of aggregate fuel.

## Decision vectors

1. With eligible loaded fuel and a matching combat-unit movement expense,
   compare candidate first-use behavior against the exhaustive §32 source
   reading; do not make either a golden outcome before a ruling. Test ordinary
   §32.16 range and route independently when draw is permitted.
2. Preserve unknown results for irrelevant-class stock, partial matching stock,
   exhaustion and later draw. Never silently synthesize a transfer to a Supply
   Unit or determine an allocation across cargo-bearing points.
3. For §32.37, test a separate origin Supply Unit with 12 Fuel and a distinct
   surviving cargo unit; debit exactly 12 only if all shipment conditions and
   the selected source policy are met. Loaded cargo cannot be assumed to pay.
4. Compare direct split payment from two sub-12 origin units, which remains
   unresolved, against a separately legal §32.15 same-hex consolidation
   followed by one payer. Keep cargo as sole qualifying payer unresolved;
   include its zero-balance removal case separately.

**Provenance:** Visual inspection of local scan copies matching scenario
SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and Land SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
This is a bounded comparison, not an exhaustive errata or edition survey.
See [loaded-point review](2026-11-17-graziani-loaded-motorization-review.md)
and [air witness review](2026-11-17-graziani-air-supply-witness-review.md).
No runtime or data changes; no gameplay tests. Review date: November 17, 2026.

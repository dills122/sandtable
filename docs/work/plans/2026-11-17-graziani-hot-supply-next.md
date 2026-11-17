# Graziani hot weather and abstract supply fuel

**Status:** bounded source analysis and candidate `gra-hot-abstract-fuel-v1`;
not an official clarification, complete weather effect contract or historical
weather outcome. The numerical loss is specified only for exact whole-point
cases below; fraction rounding, unclassified stores and some off-map scope
remain gates. No runtime, content or readiness state changed.

The [independent source review](2026-11-17-graziani-hot-supply-review.md)
confirms the arithmetic while flagging independently loaded fuel and other
holdings, premature preflight blocking, RNG persistence and hidden-state timing.
The proposal remains unadmitted and is not a complete subject inventory.

## Printed rules and scenario subjects

Land §29.34 reduces fuel and water levels by **5% during Weather Determination**
when weather is hot. It includes stock in dumps and trucks, excludes water in
wells and oases, and excludes water/fuel actually being consumed by a unit.
Section 29.31 says hot occurs across all map sections; §29.1 expressly excludes
Malta, Italy, Sicily, Crete and Tunisia–Tripoli from weather, while map E
weather affects Allied off-map airfields. That special E statement is not an
unlimited rule for every other off-map location. The rule does not specify
rounding, aggregation of separate holdings, or a Land-only supply-unit mapping.

Land §32.0 applies when both Air and Logistics are absent, replaces ordinary
supply expenditures with its own ammo/fuel costs and introduces Supply Units.
Section 32.11 says to ignore references to usage and expenditure of ammo, fuel,
stores and water and use §32's supply rules. Section 32.12 calls a Supply Unit
an abstract substitute for those points and represents it with a Supply Dump
counter; §32.15 gives each real Supply Unit fuel and ammunition, with explicit
maximums, tracks depletion and removes it when **both** reach zero. Section
32.18 describes dummies, and §32.32 permits supply units to be carried with
motorization. A real supply counter's fuel is therefore an identified potential
stored-resource subject, not automatically a nonexistent subject because the
Logistics game is omitted. The presence of motorization transporting a Supply
Unit is not by itself evidence that all the unit's stock is currently being
consumed in vehicle tanks.

Scenario §60.92(C) specifies, among other supplies, five real Commonwealth
Supply Units at Matruh with 60 fuel each, plus three each at Alexandria and
Cairo, also with 60 each. Scenario §60.92(B) specifies, for example, three
Italian Tobruk units with 50 fuel each and two Bardia units with 40 each. These
are setup facts, not proof that an actual hot result occurs, the counters still
contain those balances at a later snapshot, or all locations have certified
coverage. In particular 5% of the printed 50 is 2.5; this is a fractional loss
under either arithmetic, **not** a printed integer answer.

## Competing Land-only readings

1. **Stored loss survives abstraction (candidate).** Section 32.11 suppresses
   usage/expenditure accounting, while §29.34 independently reduces a stored
   balance; §32.15 expressly preserves that balance and §32.12 uses dump
   counters. Applying hot loss to eligible real Supply Unit fuel preserves the
   general weather effect. This is an interpretation: the general text may be
   read broadly enough to replace the loss as well.
2. **Abstract supply replaces it.** Read §32.11 and §32.0 as an exhaustive
   supply-accounting substitute: the §29.34 fuel reduction no longer applies to
   abstract units because §32 does not repeat it. This produces zero weather
   debit even on positive stores. It is plausible but is not expressly called
   out as a §29.34 exception.
3. **Defer.** Keep hot effect adjudication unsupported until a reviewed
   precedence and complete subjects/rounding contract exist. This preserves
   uncertainty without either changing balances or treating no-op as a source
   fact. All three choices have distinct replay and balance consequences.

## Proposed narrow, exact-only policy

On an actual, source-valid hot result, apply §29.34 to the stored fuel balance
of **each positively identified real Commonwealth or Axis Supply Unit in a
certified on-map A–E hex** at the accepted Weather Determination snapshot.
Take 5% of each unit's snapshot balance independently; compute as exact rational
arithmetic. Treat the remainder as the same unit's new fuel balance. The
per-unit aggregation boundary is a **project interpretation**, not a printed
rounding/grouping direction. Preserve ammunition unchanged and apply the
§32.15 removal condition only if the accepted post-effect state has both
ammo and fuel zero. No fuel reduction is applied to a dummy marker under this
candidate: it represents a false supply unit, not a positive real fuel
balance. Dummy type and actual stock are authoritative private facts, not
public labels.

Do not commit this candidate as a complete effect unless **all** potentially
applicable holdings, including non-Supply-Unit fuel, in-transit or loaded
stock, off-map locations and their exemptions, are classified under an accepted
mode contract. An explicitly excluded Tunisia–Tripoli box is outside the
weather effect; unclassified other off-map regions are unknown, not treated
as exempt or on-map. Water and wells are not automatically invented for
Land-only play; determine their applicable existence separately. The printed
actual-consumption exception needs a versioned definition if a stored balance
can overlap it. Do not silently exempt every transported Supply Unit. Unknown
counter identity, balance, location, effect subject, classification or source
coverage blocks **the whole** hot effect before any partial balance mutation.

This candidate fixes no fractional rounding rule. For an integer-only state,
an effect can be committed only when the calculated 5% loss for **every**
eligible unit is an exact integer; otherwise record an unresolved rounding
condition and do not mutate any affected balance. A rational-storage alternative
requires its own versioned unit representation and later spending/comparison
rules; floor, ceiling and nearest are also possible policy decisions, not
printed facts. Do not discard per-unit fractions or aggregate first as an
unstated rounding policy. This exact-only subset may block the initial 50-fuel
Tobruk case if hot actually occurs and that unit is eligible. It is a deliberate
fail-closed research boundary, not an assertion of a playable historical rule.

Weather and immediate effects belong to one authoritative stage transition.
Choose and version either complete preflight before drawing, or a durable
pending-result protocol that records the already-drawn weather and RNG cursor
before blocking; never redraw until a more favorable result appears, expose a
partly applied reduction, or advance the stage while a required effect is
unresolved. Any intervention or source correction must retain original stage
identity, snapshot, weather draw and policy version for deterministic replay.
Private balances, dummy identities, holdings and unknown-reason details require
separate owner/opponent/intelligence-plane projection; avoid a failure or
timing oracle for hidden subjects.

## Conditional checks and admission gates

- Synthetic hot snapshot with one real 60-fuel unit and one real 40-fuel unit:
  if all subjects and locations are certified and no separate effect blocks,
  exact losses are 3 and 2 respectively; ammo is unchanged. Same input under
  the no-evaporation alternative gives no loss. These are *conditional tests*,
  not observations of rolled historical weather.
- A real 50-fuel unit gives exact loss 2.5; integer-only exact policy returns
  unresolved and leaves **all** balances unchanged. Compare per-unit and
  aggregate approaches using two units of 10 fuel each: each loss is 0.5,
  whereas aggregate loss is 1 before any allocation. Do not make either an
  integer result without an accepted grouping/rounding choice.
- Separate real versus dummy, a unit carried by motorization, actual-consumption
  fuel, an expressly excluded Tripoli/Tunisia box, an unknown off-map subject,
  zero fuel with positive ammo, and unknown counter identity. Validate
  ownership, exact balance conservation and no unintended counter removal;
  test water, wells or other stores only after their mode applicability is
  decided.
- Normal, rain and sand outcomes must not execute a hot debit. Preserve the
  real weather result and RNG order across retry, crash and replay; a failed
  effect must neither advance the stage nor duplicate a debit. Compare
  equal-authorized observations with different hidden supply identities and
  amounts, including failure, event metadata and timing.

Before admission, independently review §32.11 precedence and dummy semantics;
define every source-supported subject and explicit exclusion, original map
identity, fractional representation/rounding and aggregation, effect order,
observability and replay. Initial setup and an exact synthetic arithmetic test
do not establish that the six-turn scenario can execute this effect end to end.

## Sources and limits

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 41 and 43, §§29.1, 29.31, 29.34 and 32.0–.18 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`), and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 4–5 / printed pp. 28–29, §60.92(B)–(C)
(`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`).
Searched extracted text of [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
(`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`)
for §29.34, §32.11/.12/.15 and hot-fuel loss; no explicit correction appeared
in that bounded search. §32.0's erratum says abstract rules were untested.
OCR, unrelated cases, later errata and different editions remain unexhausted.
The existing synthetic no-subject weather fixture, described in
[weather scope](2026-11-12-graziani-weather-scope.md), is not proof that this
historical setup has no fuel subject. No gameplay tests were run.

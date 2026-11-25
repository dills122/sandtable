# Graziani Land-only logistics: reachability classification

**Source-derived document.** Every source citation below (§ numbers, row IDs,
chart values) comes from the repository's own one-reader transcriptions of the
original 1979 *Campaign for North Africa* scenario, Land rules, charts and
September errata. None of it has had independent or blind review, and the
original scans are not in Git. Per the README, source-derived material is **not**
covered by the Apache-2.0 code license. This is a planning document; it adopts no
rule and changes no code.

**Date:** 2026-11-25. **Task:** T5 of the RC critical-path analysis (study only).
**Baseline:** `origin/main` at `022806979abc`. **Companion:** the one-page
[D4 ruling request](2026-11-25-d4-logistics-fidelity-ruling-request.md).

## Evidence labels and method

- **VERIFIED**: I read the code, data or document text at the baseline commit.
- **DOC**: a repository document states it; I did not re-check it against a scan.
- **ASSUMED**: my inference; basis stated.
- I did **not** open the original PDFs. "Source" below means the cited
  repository transcription. The §60.92 text was read through
  [the supply accounting pilot](2026-11-12-graziani-supply-accounting-pilot.md)
  and [source register](2026-11-12-graziani-source-register.md) (LA-01..06,
  LA-11..12), not from the scan.
- I read the five matrices named in the task, the
  [surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md),
  the [victory matrix](2026-11-12-graziani-victory-matrix.md), the
  [victory-supply review](2026-11-16-graziani-victory-supply-source-review.md),
  the [weather scope](2026-11-12-graziani-weather-scope.md) and the
  [abstract convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md).
  I skimmed (grep only) the roughly 1,190 other Graziani research and review
  documents. A row I mark "empty" or "inert" could be contradicted by one of them.

### Class definitions

| Class | Meaning |
| --- | --- |
| **NEEDS** (reachable-and-needs-rules) | A six-turn game can reach it and the outcome changes game state, a legal-action set or the victory result. Needs a rule module, or an explicit owner exclusion. |
| **INERT** (reachable-but-inert under Land-only) | The position or phase is reachable, but under Land-only abstraction the effect either has no state to act on or is voluntary and can be legally declined. Inert is conditional; the condition is stated. |
| **EMPTY** (source-proven-empty) | The source text states that the effect does not exist in this mode. Every EMPTY row here is one-reader DOC, never independently confirmed. |

Two facts about the engine shape the whole table (both VERIFIED):

1. The Land sequence catalogue already carries a position for every one of these
   mechanics in every side block: `truck-convoy-movement`,
   `commonwealth-rail-movement`, `repair.towing`, `repair.maintenance`, `patrol`
   (`Cna1979LandSequence.cs:109-113`), plus `naval-convoy.schedule`,
   `naval-convoy.tactical-shipping`, `naval-convoy-arrival` and
   `commonwealth-fleet.repair` (lines 34, 35, 76, 78).
2. In the native chain, `TruckConvoyPassPending` and `PostCombatPassPending` are
   explicit owner passes (`PendingDecision.cs:78-79`). No code in
   `src/Cna.Core/Exercises` implements rail, repair, patrol, towing, maintenance,
   consumption or a victory evaluator (DOC from the RC analysis, grep-based; I
   re-confirmed only the two `PendingDecision` records).

Most of these actions are **voluntary** in the source. A legal player may decline
to rail-move, patrol, tow or convoy. So an explicit pass is a legal line of play,
not by itself a rules violation. The risk is different: the engine must **reject**
an attempt at an unimplemented action (`rule-not-implemented`) and never silently
accept it or zero-fill its cost. That is the fail-closed requirement that every
INERT row below depends on.

## A. Supply and consumption

| # | Obligation | Source rows | Class | Why, and what depends on it |
| --- | --- | --- | --- | --- |
| A1 | Initial supply units, contents, placement, dummy groups | §60.92(B-C); §§59.62-59.66; §32.12-32.15; register LA-03/04/11/12; pilot (Axis 22 units / 1,036 fuel / 375 known ammo; Commonwealth 15 / 810 / 600) | **NEEDS** (setup data) | Reachable at turn 1. Content, not a stage rule. Source track already holds Axis and Commonwealth supply worlds (VERIFIED: `CampaignSourceAxisSupplyWorld.cs`, `CampaignSourceCommonwealthSupplyWorld.cs`, `CampaignSourceSupplyUnit`). Not wired to the native chain (DOC). Pilot rows still need independent double entry. One printed dash (Axis Derna ammo) is unresolved; do not read it as 0. |
| A2 | Dummy supply units (§32.18) | §32.18; §60.92 dummy groups; register LA-11 | **INERT** for victory; NEEDS for hidden-information fidelity | Dummies change what the opponent can see, not any supply total. Victory depends on none of it. Setup placement is needed if A1 is admitted. Discovery and removal rules are not enumerated in the reviewed text. |
| A3 | Matruh five-unit stacking vs the three-unit §32.14 limit | §60.92(C); §59.65; §32.14 ([crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md#matruh-supply-stacking-interaction)) | **NEEDS** (setup validation) | Reachable at setup and again whenever a unit re-enters. A setup validator must either admit the §59.65 exception or fail closed. Small, but it blocks A1 admission. |
| A4 | Local supply access (§32.16 distance/ZOC test) | §32.16; crosswalk "Access and debits" | **NEEDS iff** consumption is enforced (see A5); otherwise **INERT** | §32.16 answers "may this unit draw from a nearby supply unit". It is a prerequisite only for A5. It is **not** the victory test (victory-supply review, "What the source establishes"). |
| A5 | Ammunition and fuel costs for actions (§§32.21-32.25) | §§32.21-32.25; crosswalk "Access and debits" | **NEEDS** for fidelity; **no victory dependency** | Real Land-only costs. Reachable in the first Combat or Movement. Cost tables, timing and multi-unit competition are not yet transcribed (DOC: "Source exact distances, route type, blocked hexes, action costs"). Largest single fidelity item. Owner may accept an "unlimited supply for RC" exclusion; if so A4 and A6 become INERT and the hot-weather fuel loss (B2) becomes INERT. |
| A6 | Supply-unit capture (§32.13) | §32.13, §10.29; [capture continuation](2026-11-17-graziani-supply-capture-continuation.md) | **NEEDS iff** A5 enforced | The capture doc keeps capture "unsupported for executable play until an explicit ruling". If supply is not consumed, a captured supply unit has no use to its captor. |
| A7 | Ordinary ammo / fuel / stores / water usage rules | §§32.0, 32.11; errata §32.0; pilot | **EMPTY** (DOC, one reader) | §32.0 and §32.11 say references to ordinary usage and expenditure are ignored and replaced by §32. This empties the *ordinary* accounting, not the §32 replacement (A5). Errata §32.0 warns the abstract procedures were untested, so "abstracted" is not "no work". |
| A8 | Supply-unit transport: motorization (§§32.32-32.33, 32.58), Commonwealth rail (§32.34), Axis coastal shipping (§32.35), Commonwealth sea (§32.36), abstract air (§32.37) | [transport matrix](2026-11-13-graziani-supply-transport-matrix.md) | **NEEDS iff** victory option 2 ("actual shipment") is chosen or A5 is enforced; otherwise **INERT** (voluntary Truck Convoy / Movement choices) | The matrix says "Do not treat the catalogue as proof each mode is admitted." §32.37 air transport (12 fuel) is explicitly inside §32 and survives. No victory dependency under the hypothetical-route reading. |
| A9 | Motorization allocation, attachment and per-turn conversion | §§32.51-32.58; §60.92(A); register LA-02 | **NEEDS** for fidelity; **INERT** for victory | Reachable. §60.92(A) governs incoming conversion one-for-one. Motorization points are exempt from breakdown (§32.55). Without A5/A8 the points have no consumer. |
| A10 | Monthly motorization loss (§32.57 / §58.5 chart) | crosswalk "Monthly motorization loss"; register LA-17 | **NEEDS iff** A9 enforced. Chart identity and month are unresolved | First October stage is turn 3. September cell is a dash, October is `6/2`. Do not zero-fill the dash. |
| A11 | Supply availability charts (§32.44-32.47; Axis month letter B, §32.46: 0,1,1,2,2,3; Commonwealth period I, §32.47: 1,1,2,2,3,3) | crosswalk "Supply availability" | **NEEDS iff** A5 or victory option 2; else **INERT** | Pre-start planning (§60.37) and §32.43 lead times mean arrivals 1-4 depend on pre-scenario plans (crosswalk planning-horizon table). Zero-fill is forbidden; a missing plan is unsupported, not empty. |

## B. Weather effects

Weather determination itself is built (VERIFIED: `StageEntryWeatherPending`,
`ThirdStageWeatherPending` in `PendingDecision.cs`; DOC: Core V49-V54). Only
Initiative Declaration is Host-wired.

Under the repository's weather-season ruling all six turns use the fall row:
normal 17, hot 11, sandstorm 3, rainstorm 5 of 36 ordered pairs (weather scope,
"Calendar and outcomes"). So foul weather (sandstorm or rainstorm) is 8/36 per
stage. **ASSUMED** stage independence, the chance that none of the 18 stages is
foul is (28/36)^18, about 1.1 percent. Foul weather is therefore effectively
certain to occur in a full game; it is not a corner case.

| # | Obligation | Source rows | Class | Why |
| --- | --- | --- | --- | --- |
| B1 | Foul-weather movement cost, construction and terrain effects | §§29.42, 29.44, 29.54-29.58 | **NEEDS** (movement lane, not logistics) | Reachable with near-certainty. Needs accepted map-area identity. Belongs to the Movement/terrain backlog; listed here so D4 does not hide it. |
| B2 | Hot-weather 5% loss of stored fuel/water (§29.34) | §29.34; §32.11; weather scope | **INERT iff** fuel is not consumed (A5 excluded); **NEEDS** if A5 enforced | Weather scope notes two readings (replaced expenditure vs distinct loss); neither is selected. Rounding rule needed either way. |
| B3 | Foul-weather breakdown modifiers (§§29.33, 29.45) | §§29.33, 29.45; §32.55 | **NEEDS iff** vehicle Breakdown is in scope (see C1) | Native Breakdown covers only the no-vehicle case (DOC, RC analysis 1.2). |
| B4 | Repair barred in certain weather (§22.13) | §22.13 | **NEEDS iff** repair is in scope (C2) | Folded into repair eligibility. |
| B5 | Patrol blocked by rain or sandstorm at origin or target (§16.17) | §16.17 | **INERT iff** patrol is a pass (D1) | |
| B6 | Grounded aircraft, depleted wells (§§29.47, 29.53) | §§29.47, 29.53; weather scope | **INERT** (ASSUMED empty) | The Land-only profile has no aircraft or well state. Weather scope says "Establish Land-only applicability"; I did not. Reject positive subjects rather than ignore them. |
| B7 | Nile Delta sandstorm exclusion (§29.41) | §29.41 | **NEEDS** with B1 | Exclusion matters only where B1 effects are applied. |

## C. Repair, towing and maintenance

| # | Obligation | Source rows | Class | Why |
| --- | --- | --- | --- | --- |
| C1 | Vehicle Breakdown (Movement) | §§29.33, 32.55; native chain | **NEEDS** (precondition for C2-C4) | Not a logistics rule but it decides whether repair is reachable at all. DOC: vehicle-cohort Breakdown is unreachable in the native chain today. If an accepted exclusion removes Breakdown, then C2-C4 are INERT. They stand or fall together. |
| C2 | Field repair (trucks §22.23, recce/armored cars §22.24, tank TOE §22.26) | [repair matrix](2026-11-13-graziani-repair-resource-matrix.md) | **NEEDS iff** C1 in scope | §§22.23-22.24 cost no supply (EMPTY by their own text). §22.26 fuel debit: candidate is to omit under §32.11; not accepted. |
| C3 | Facility repair and destroyed-tank repair (§§22.35, 22.38, 22.42; §22.31 major facilities; Mersa Matruh temporary facility, RUL-GRA-01) | repair matrix; register "Commonwealth repair" | **NEEDS iff** C1 in scope | §22.42's stores and fuel are labeled Logistics Game only (EMPTY for that debit). Facility locations still need independent review; Tobruk efficiency conflict is open. |
| C4 | Repair eligibility restrictions (§22.13: supply availability, towing in phase, enemy control, weather) | repair matrix | **NEEDS iff** C1 in scope | Open question: does §32.11 displace the "no supplies available" condition? Repair matrix lists three alternatives. |
| C5 | Towing and maintenance segments | §22.13 reference; sequence lines 111-112 | **INERT iff** C1 excluded or both are passes. **Source gap** | No matrix in the repository covers towing or maintenance rules beyond the §22.13 mention. I could not classify their reachability from the documents; ASSUMED voluntary. A one-hour source pass is needed before the owner rules. |
| C6 | Rail-line damage and repair by engineers (§24.6x) | rail scope | **INERT** (ASSUMED) | Needs a destroyed-track source; none identified in a six-turn window. Not verified. |

## D. Rail, patrol and Truck Convoy phases

| # | Obligation | Source rows | Class | Why |
| --- | --- | --- | --- | --- |
| D1 | Patrol (§§16.0-16.5) | [patrol matrix](2026-11-13-graziani-patrol-fog-matrix.md) | **INERT** if declined; **NEEDS** if enabled | Voluntary and information-only: patrols do not move units (matrix, "Target and route") and §32.25 says Land-only patrol needs no supply expenditure (EMPTY for the debit). No victory dependency. Fog-of-war fidelity is lost, not correctness. Eligible types need the errata additions. |
| D2 | Commonwealth rail movement of units (§§8.71-8.73, errata §8.71, §8.73) | [rail scope](2026-11-13-graziani-rail-scope.md) | **NEEDS** if the Commonwealth player is meant to use it; else **INERT** (voluntary) | Reachable: the railroad runs Alexandria to Mersa Matruh at start (§60.7). Not victory-dependent unless a trace uses the rail edge (see E). Once per stage, one stack each direction, ZOC and prior-CP conditions. |
| D3 | Axis use of captured Commonwealth rail (§54.4, errata §8.71) | rail scope | **INERT** (ASSUMED, unproved) | Needs controlled contiguous rail plus imported rolling stock under the detailed rule. Rail scope calls it "unproved, not impossible". Do not hard-code Commonwealth-only. |
| D4 | New railroad construction (§§24.61-24.67) | rail scope | **INERT** (ASSUMED) | Only the 10th and 13th NZ railroad construction companies can build, west of the railhead. I did not verify they are in either order of battle during turns 1-6. |
| D5 | Benghazi-Barce line | §8.71 | **EMPTY** (DOC) | Source says it is historical and unusable. |
| D6 | Truck Convoy phase movement of supply units / motorization | §§32.33, 32.51, 32.58; §5.2; victory-supply review | **INERT** as an owner pass; **NEEDS** if victory option 2 | Native chain passes it today (VERIFIED). §10.29 also bars convoys from entering enemy ZOC unless a friendly combat unit is already there. |

## E. Convoys and fleet

| # | Obligation | Source rows | Class | Why |
| --- | --- | --- | --- | --- |
| E1 | Axis naval convoy planning and arrival (§§32.61-32.62; §60.37 pre-start plan, lanes 2, 3, 6) | [convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md) | **NEEDS iff** A5 or victory option 2; else **INERT** | Source track already has partial turn-1 handling (VERIFIED: `CampaignSourceSetupBridge.cs:18,103,117`, `CampaignSourceConvoyPlan`). Not in the native chain. |
| E2 | Commonwealth attack on arriving convoys; bombardment lookup and loss rounding (§§32.63-32.65; 10%/20%/5% results) | convoy matrix | **NEEDS iff** E1 | Matrix: the detailed Air Game is omitted but the abstract lookup is retained. The 5% case has no explicit Land-only rule. |
| E3 | Port capacity, Tobruk efficiency conflict (7 vs 5 vs San Giorgio -3) | convoy matrix, ruling conflict | **NEEDS iff** E1 | Source conflict, owner-level ruling required first. |
| E4 | Commonwealth fleet repair and tactical shipping positions | sequence lines 35, 78; §60.45 first paragraph | **INERT** (ASSUMED) | Errata §60.45 invalidates the later fleet paragraphs. I found no reachable effect in the reviewed documents. Not verified. |
| E5 | Detailed Air Game, detailed Logistics fuel/stores bookkeeping, air-facility supplies (§60.93) | surface inventory, source register ("do not automatically belong") | **EMPTY** (DOC) | Scenario supplies the Land-only abstraction. Excluded modules, but retained abstract effects (E1-E2, A8) are not "no work". |
| E6 | Pre-start convoy rolls for "turns 0, -1..." | crosswalk planning table | **EMPTY** as game events | Relative slots, not game turns. Must be explicit setup inputs, never invented history. |

## F. The one item victory depends on: supply trace

Scenario §60.81 names six outcome rows (Italian: map D from any hex, Tobruk;
Commonwealth: Alexandria or Cairo by truck convoy). Whether and how supply
connects to each is **not specified** by the reviewed passages (victory-supply
review; victory matrix "Condition decomposition").

| # | Obligation | Class | Inputs available now |
| --- | --- | --- | --- |
| F1 | Supply trace for the six §60.81 rows | **NEEDS** (the only victory-dependent logistics item) | Four competing readings are on file: hypothetical route, actual/feasible shipment, local §32.16 access, defer. A pure route-trace module is implementable once the policy is a parameter. |
| F2 | Trace map inputs | available | VERIFIED: `hexsides.v1.json` carries per-edge `road` (355 values), `railroad` (183), `track` (1,808) over 7,073 hexes; `Cna1979HistoricalMap.EdgeBetween`, `NeighborsOf`, `HexAt` expose them. I counted feature strings; an edge may be listed from both sides (ASSUMED listed once). `CampaignSourceFourthIndianRoad` already tests "plain road" edges (VERIFIED). |
| F3 | Trace obstruction inputs: enemy ZOC, occupancy, hex control | partially available | `CampaignSourceGroupedZoc` derives ZOC over the real battlefield (VERIFIED, file header comment). Hex control and holding rules are not in the logistics docs; they come from the victory/End-of-Turn lane (T4). |
| F4 | Unit-versus-hex quantifiers (map D "any hex", Tobruk, two-place decisive row) | **unresolved** | Needs owner ruling; module must return a typed "unsupported policy" result rather than guess. |

## Totals

Not tallied. Many rows carry a condition ("NEEDS iff ...") or two classes (for
example A5 needs rules for fidelity but nothing for victory), so a class count
would suggest precision the one-reader sources do not support. Unconditionally
NEEDS: A1, A3, B1, B7, C1 and F1. Everything else flips on one of the rulings
below. This is not a GATE-01 unsupported-item count; that gate needs the surface
measurement from T7.

## Coupling structure (why the owner only needs four decisions)

```
Victory supply semantic (F1) --option 2--> A8, D6, E1 (shipment must exist)
Consumption enforced (A5)   --yes--------> A4, A6, A9, A10, A11, B2, E1-E3
Breakdown in scope (C1)     --yes--------> C2-C5, B3, B4
Everything else (D1-D4, E4, B6) -> voluntary passes or EMPTY
```

All dependencies reduce to three yes/no questions plus the victory semantic. The
[ruling request](2026-11-25-d4-logistics-fidelity-ruling-request.md) asks exactly
those, with a recommended answer for each.

## What I could not verify

- The original scans: all "source rows" are repository transcriptions, one reader.
- Whether any of the roughly 1,190 other Graziani documents already classifies
  towing, maintenance, §24.6x company availability or aircraft effects.
- Which Commonwealth railroad construction units, if any, appear in turns 1-6.
- Whether road/rail edge data are complete along Tobruk-Cairo and Tripoli-Tobruk
  routes (seam between maps C and D has its own policy, `graziani.cd-seam.v1`,
  VERIFIED constant). A connectivity probe is the first test any trace module
  needs and is not done here.
- That native-chain code has no hidden supply or repair hook beyond the grep
  results inherited from the RC analysis.

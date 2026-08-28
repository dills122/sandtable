# Combat Rules and Result Surface Spike

**Status:** Research complete for the selected infantry Close Assault surface; contract freeze
remains gated by `BREAKDOWN-001`, `CONTACT-001`, and the later combat design tasks

**Date:** 2026-08-28

**Decision owner:** Project owner

**Research work item:** `CMB-RSH-001`

## Executive conclusion

The first combat rules surface can be bounded to the existing synthetic evidence vector: one
adjacent, non-Reserve infantry battalion per side in Clear terrain, with no guns, armor,
fortification, minefield, trucks, air, attachment, pin, or prior combat state. Barrage and
Anti-Armor then remain explicit empty combat steps. The selected resolution surface is one full
Close Assault after the defender has declined Retreat Before Assault and both sides have committed
all eligible strength.

For the retained values of 10 TOE Strength Points, Close Assault rating `1`, Basic Morale `0`, and
Cohesion `0`, each side begins with 10 Raw and 1 Actual Close Assault Point. The Basic Assault
Differential is `0`. The Cohesion-0 Morale row can adjust each side by `-1`, `0`, or `+1`, so the
complete reachable Final Close Assault Differential is the closed set `-2` through `+2`. Every
ordered two-die coordinate in every one of those five columns must be normalized before this
vector can become authoritative. A favorable fixed seed cannot narrow implementation scope.

Within that closed surface, the chart can produce zero or percentage losses, an attacker Engaged
result, a one- or two-hex defender Retreat, and a Captured trigger for either side. Captured invokes
one additional die and the Prisoners Captured table. The semantic outcomes are source-locked
below; the source chart matrix is not reproduced in this repository.

This packet does not freeze combat commands, events, content, mutable state, random draw order,
Contact/Engaged identity, retreat choices, ammunition representation, loss allocation, prisoner or
captured-equipment persistence, or cycle control. Those boundaries remain with `CMB-RSH-002`
through `004`, `CMB-DES-001` through `005`, and `CYCLE-DES-001`.

## Decision question and scope

This spike asks:

> What is the smallest source-locked combat input, modifier, random, and result surface that can
> support the retained synthetic infantry vector without deciding later combat contracts?

In scope:

- the existing six structural combat steps;
- the source calculation from committed TOE and Close Assault ratings to Raw and Actual Points;
- the Basic and Final Close Assault Differential for the selected symmetric vector;
- the complete Morale outcomes reachable from Basic Morale `0` and Cohesion `0`;
- the complete semantic result set reachable from the `-2` through `+2` Close Assault columns;
- the September 1979 errata that affects or bounds the selected surface; and
- explicit handoffs to later content, state, RNG, identity, disclosure, and cycle research.

Out of scope:

- transcribing or reproducing the combat-result matrices;
- authorizing a rules artifact, Content schema, command, event, snapshot, observation, or action;
- choosing Contact-derived opportunity or participant identity;
- choosing ammunition, prisoner, captured-equipment, or Disorganization persistence;
- defining Retreat Before Assault or post-result Retreat legal actions and paths;
- freezing seeded-random draw order or golden seeds;
- resolving Barrage, Anti-Armor, armor, guns, trucks, fortifications, minefields, air, Probes,
  Overrun, pinned defenders, zero-rated defenders, unsupported tanks, or special units; and
- changing Breakdown, ZOC/Reaction, Reserve Release, or repeat-cycle authority.

The stop condition is a reviewed research packet. Later design must reject any unsupported input
before combat mutation.

## Method and source index

The research visually inspected temporary copies of the three primary sources already locked by
the repository's Combat-cycle inventory. The scans remained outside Git. This packet retains
normalized facts and precise locators, with no copied source prose, chart layout, scan, or artwork.

### Primary sources inspected

| Source | Exact locators | Use |
| --- | --- | --- |
| [Original 1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | 5.2; 10.31-10.36; 11.0-11.38; 12.0-12.6; 13.0-13.28; 14.0-14.6; 15.0-15.89; 17.0-17.28 | Combat sequence, strength calculation, empty-step eligibility, Close Assault modifiers, dice interpretations, losses, retreat, capture, Contact/Engaged, and Morale |
| [Charts common to both players](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | 6.3; 11.4; 12.6; 14.6; 15.53; 15.79; 15.89; 17.4 | Capability expenditure, strength summary, combat tables, organization-size shifts, capture percentage, and Morale outcomes |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 12.44; 14.47-14.48; 15.27; 15.4; 15.53; 15.56; 15.79; 15.88 | Corrected target scope, armor cases, dice reuse, Combined Arms, size-chart heading, pinned defense, result range, and surrender behavior |

### Repository evidence inspected

- [`Cna1979LandSequence`](../../src/Cna.Core/Rules/Cna1979LandSequence.cs), which already retains
  Position Determination, Barrage, Retreat Before Assault, Force Assignment, Anti-Armor, and Close
  Assault as distinct structural positions;
- the [Combat-cycle source inventory](combat-cycle-source-inventory.md), which selects the
  synthetic vector and separates research from later contract design;
- the [Sprint 4-5 research-gate audit](sprint-4-5-research-gates.md), which keeps Sprint 5 behind
  Breakdown and ZOC/Reaction approval;
- the [ZOC/Reaction packet](contact-reaction-zoc-spike.md), which keeps combat-created Contact and
  Engaged state out of the Movement interruption package;
- the [Movement Foundation](movement-foundation-spike.md), which stops before Breakdown and Combat;
  and
- the [reconnaissance/contact research](recon-contact-knowledge-spike.md), which separates real
  force truth, map representation, and side knowledge.

## Documented facts

### Structural combat procedure

The Land sequence puts Combat after Movement and Breakdown and before Reserve Release. The source
combat procedure contains six ordered steps already represented by the repository catalog:
Position Determination, Barrage, Retreat Before Assault, Force Assignment, Anti-Armor, and Close
Assault. Separate Close Assault instances resolve sequentially. Barrage and Anti-Armor effects are
simultaneous within their respective source boundaries.

For the selected vector:

| Step | Required source input | Selected-vector result | Locator |
| --- | --- | --- | --- |
| Position Determination | Eligible gun positions | No gun exists; no position choice or mutation | 11.0; 12.1 |
| Barrage | Barrage-capable committed unit and target | No Barrage-capable unit exists; no table coordinate or result | 12.0-12.6 |
| Retreat Before Assault | Eligible, unpinned defender choice and movement authority | The retained evidence choice is decline; the general choice remains a later design concern | 13.0-13.28 |
| Force Assignment | Eligible TOE allocation to Anti-Armor, Close Assault, or withholding | Both sides' retained evidence input commits all eligible TOE to full Close Assault | 14.0; 15.1-15.29 |
| Anti-Armor | Anti-Armor Points directed at Armor Protection Points | No armor or Anti-Armor participant exists; no table coordinate or result | 14.0-14.6 |
| Close Assault | Committed strength, modifiers, Morale, and both sides' dice | Resolve the complete selected surface below | 15.0-15.89; 17.0-17.28 |

An empty step is an explicit supported result of the selected input. It is not permission to skip a
structural position or to treat a future non-empty case as empty.

### Close Assault input and calculation surface

The selected fixture needs these source facts. Their eventual ownership is deferred to
`CMB-RSH-002` and `CMB-RSH-003`.

| Fact | Attacker | Defender | Effect in this vector |
| --- | ---: | ---: | --- |
| Independently participating unit | one infantry battalion | one infantry battalion | Same organization size; no size shift |
| Current committed TOE Strength Points | 10 | 10 | All eligible strength is committed |
| Applicable Close Assault rating | offensive `1` | defensive `1` | Produces 10 Raw Points per side |
| Actual Close Assault Points | 1 | 1 | Rounded source calculation after all Raw Points are totalled |
| Available Operation-Stage capability | sufficient | sufficient | The full assault spends 5 CP for the attacker and 3 CP for the defender; the selected differential never reaches the defender's reduced-cost case |
| Basic Morale | 0 | 0 | Starting Morale value |
| Cohesion at assault | 0 | 0 | Selects the Cohesion-0 Morale row |
| Terrain | Clear | Clear | No terrain column shift |
| Reserve, pin, ammunition, special state | source-eligible, unpinned, no special state | source-eligible, unpinned, no special state | No selected special-case adjustment or surrender |

The source calculation totals `rating x committed TOE` as Raw Points, divides the total by 10, and
rounds the result to the nearest whole Actual Point with halves rounded upward. Ten Raw Points per
side therefore give one Actual Point per side and a Basic Assault Differential of `0`.

The selected vector has no Combined Arms reduction, terrain shift, organization-size shift,
two-to-one Raw Point shift, minefield or Engineer shift, pinned-defender shift, zero-rated-defender
shift, Probe adjustment, or Overrun column. Those modifier categories remain unsupported inputs,
not implicit zeroes for other content.

### Morale and Final Close Assault Differential

Each side makes its own Morale table check before the Close Assault result rolls. At Cohesion `0`,
the complete table outcome set is:

| Ordered two-die coordinate | Morale modifier |
| --- | ---: |
| `11` | `+1` |
| all valid ordered d6 coordinates from `12` through `65` | `0` |
| `66` | `-1` |

With Basic Morale `0`, each side's Adjusted Morale is therefore one of `-1`, `0`, or `+1`. The
attacker's Adjusted Morale minus the defender's Adjusted Morale shifts the Basic Differential.
The complete reachable Final Close Assault Differential set is `-2`, `-1`, `0`, `+1`, and `+2`.

This is a closed result surface. A later rules artifact must retain all 36 ordered Morale
coordinates for both sides and all 36 ordered Close Assault coordinates in each of these five
columns.

### Close Assault dice interpretations

Each side rolls two distinguishable d6 once for its Close Assault result. The same pair has three
source meanings:

1. the ordered two-die coordinate selects the percentage-loss row;
2. the arithmetic sum tests attacker Engaged or defender Retreat; and
3. the arithmetic sum tests whether part of that side's loss is Captured.

A Captured trigger consumes one additional d6 and selects the captured percentage. The exact
authoritative draw order, stream labels, and golden seeds belong to `CMB-RSH-004`; this packet
records only the source-required draw dependencies.

### Complete semantic result surface for the selected columns

The union below covers every random result reachable from Final Close Assault Differentials `-2`
through `+2`. It records normalized output values without reproducing the source matrix or its
coordinate ranges.

| Final Close Assault Differential | Attacker loss percentages | Defender loss percentages | Attacker Captured possible | Defender Captured possible | Engaged possible | Defender Retreat distances |
| ---: | --- | --- | --- | --- | --- | --- |
| `-2` | `0`, `5`, `10`, `15`, `20`, `25` | `0`, `5`, `10`, `15`, `20` | yes | no | yes | zero or one hex |
| `-1` | `0`, `5`, `10`, `15`, `20`, `25` | `0`, `5`, `10`, `15`, `20` | no | no | yes | zero or one hex |
| `0` | `0`, `5`, `10`, `15`, `20`, `25` | `0`, `5`, `10`, `15`, `20` | no | yes | yes | zero or one hex |
| `+1` | `0`, `5`, `10`, `15`, `20`, `25` | `0`, `5`, `10`, `15`, `20` | no | yes | yes | zero or one hex |
| `+2` | `0`, `5`, `10`, `15`, `20`, `25` | `0`, `5`, `10`, `15`, `20`, `25` | no | yes | yes | zero, one, or two hexes |

`Engaged possible` refers to the attacker's arithmetic-sum test. A defender Retreat takes priority
if both occur.

| Result component | Complete reachable value set | Immediate calculation consequence |
| --- | --- | --- |
| Attacker percentage loss | `0`, `5`, `10`, `15`, `20`, or `25` percent | Applied to 10 Raw Points and rounded upward, producing `0`, `1`, `2`, or `3` Raw Points lost |
| Defender percentage loss | `0`, `5`, `10`, `15`, `20`, or `25` percent | Applied to 10 Raw Points and rounded downward, producing `0`, `1`, or `2` Raw Points lost |
| Attacker Engaged test | false or true | May create Engaged if no defender Retreat takes priority |
| Defender mandated Retreat | zero, one, or two hexes | Retreat takes priority over Engaged; two hexes is reachable only at differential `+2` in this surface |
| Attacker Captured trigger | false or true | When true, one additional d6 selects the captured share of attacker losses |
| Defender Captured trigger | false or true | When true, one additional d6 selects the captured share of defender losses |
| Captured share | `10`, `25`, `33`, `50`, or `75` percent | Applied to the affected side's losses and rounded upward |
| Post-assault relationship | Contact, Engaged, or separated by Retreat | Exact identity, participants, lifecycle, and outward disclosure remain design-gated |

Because every participating TOE point has rating `1` in this vector, each Raw Point allocated as a
loss corresponds to one TOE point. That equivalence belongs only to this synthetic vector. Later
loss allocation must use the actual participating ratings and unit types.

The retained golden evidence path executes a required one-hex Retreat when produced. The complete
selected surface also reaches a two-hex Retreat at differential `+2`. A later state contract must
retain the exact mandated distance until the defender supplies any required retreat choice, and a
later action contract must validate a route or the source-defined additional-loss alternative.
This packet does not select the event boundary for those steps. The alternative loss, its possible
30-percent Disorganization consequence, surrender, and retreat-path failure remain with
`CMB-RSH-003` and `CMB-DES-005`.

### Errata reconciliation

| Errata locator | Consequence | Selected-surface treatment |
| --- | --- | --- |
| 15.27 | The same Close Assault dice also supply the arithmetic sum used by Retreat and related tests | Included in the three-use dice dependency above |
| 15.79 | Corrects one defender-loss coordinate in the `+4` differential column | Source-locked for later full table normalization; outside the selected `-2` through `+2` columns |
| 12.44 | Narrows a Barrage pin to the selected battalion-equivalent target | No Barrage exists in the selected vector; mandatory for any future non-empty Barrage surface |
| 14.47-14.48 | Corrects self-propelled-gun and halftrack Anti-Armor loss behavior | No gun, armor, or halftrack exists in the selected vector |
| 15.4 | Corrects a Combined Arms example/result | No tank TOE exists in the selected vector |
| 15.53 | Corrects the organization-size chart heading | Both selected units are equal battalion organizations, so no shift applies |
| 15.56 | Defines the Close Assault effect when every defender is pinned | The selected defender is unpinned |
| 15.88 | Clarifies assault-triggered automatic surrender versus mere enemy adjacency | The selected units have Cohesion `0` and must satisfy source eligibility before commitment |

## Repository observations

- The structural Land catalog already contains all six combat positions, but its successor is one
  linear Reserve Release pass. It has no cycle ordinal or repeat/finish authority.
- Core has no combat rules artifact, combat result vocabulary, combat RNG labels, combat command,
  event, snapshot, observation, legal action, or replay path.
- Content has no TOE strength, Close Assault ratings, Basic Morale, combat/unit class, ammunition,
  fuel, armor, gun, or attachment participation facts.
- Campaign World has no current TOE strength, pin, Contact/Engaged, ammunition, loss, retreat,
  combat opportunity, sealed choice, prisoner, captured-equipment, or per-stage attacked-target
  history.
- The current synthetic content has equal battalion organizations and Clear terrain, but it does
  not yet carry the combat values used by this research vector.
- The existing apparent-presence and representation boundary does not define Contact-derived
  combat opportunity or disclose opposing combat values.

## Inferences

- The selected infantry vector is the smallest closed result surface because it preserves a real
  Close Assault and Morale check while keeping Barrage and Anti-Armor structurally present but
  empty.
- Admitting one table column would be unsafe. Cohesion `0` still gives each side three Morale
  outcomes, which makes five Final Close Assault Differential columns reachable before the result
  rolls.
- A rules artifact may store the complete coordinate matrix later, but this research packet should
  retain only the normalized semantic set, locators, and independent acceptance vectors. This
  avoids reproducing source chart layout while keeping implementation scope auditable.
- Position, Retreat Before Assault, force assignment, loss allocation, and post-result Retreat may
  contain player decisions. The research vector supplies fixed evidence inputs; it does not remove
  those decisions from future legal-action design.
- Authoritative events may need every roll, coordinate, modifier, allocation, and before/after
  value for replay. Player observations and Chronicle-derived output need a separate redacted
  projection because exact opposing TOE, Morale, choices, and losses can be hidden.
- The first future rules artifact should fail admission unless its five selected columns cover all
  36 ordered coordinates for both attacker and defender and its Morale row covers all 36 ordered
  coordinates. Golden examples alone are insufficient.

## Owner decisions and later gates

This packet requests no new rules interpretation. It recommends the selected infantry surface as
the first normalization boundary, subject to project-owner review. The following choices remain
open and must not be inferred from this packet:

1. whether the first combat implementation adopts this exact safely closed subset or a broader
   independently normalized table surface;
2. the minimum source-faithful ammunition and fuel state required to establish Close Assault
   eligibility;
3. whether prisoner and captured-equipment outcomes enter the first skeleton or cause admission to
   reject before any combat mutation;
4. Contact-derived opportunity, participant, Contact, and Engaged identity and disclosure;
5. sealed private submission, fallback, loss allocation, and retreat-choice protocols;
6. seeded RNG stream labels and exact draw order; and
7. Breakdown/Retreat BP continuity, ZOC/Reaction handoff, Reserve Release, and repeat-cycle
   eligibility.

`CMB-RSH-002` through `004` may now use this result surface as input. Combat design and production
implementation remain blocked until the roadmap's Breakdown and ZOC/Reaction gates are approved.

## Acceptance consequences for later work

1. Independent table entry must prove all 36 Morale coordinates for Cohesion `0` map to exactly
   `+1`, `0`, or `-1` as source-locked above.
2. Every combination of the two sides' Morale outcomes must select one of the five exact Final
   Close Assault Differential columns.
3. Each selected column must cover all 36 attacker and all 36 defender ordered Close Assault
   coordinates with no overlap or gap.
4. Exhaustive selected-column tests must produce only the percentage, Engaged, Retreat, Captured,
   and captured-share values listed above.
5. A fixed golden vector must include zero attacker loss, non-zero defender loss, and safe one-hex
   Retreat, as required by the retained Combat-cycle inventory.
6. A differential-`+2` vector must exercise the source-reachable two-hex Retreat and preserve its
   pending distance through the later state/action handoff without silently reducing it to one hex.
7. Reordered, missing, duplicated, out-of-range, or uncited table rows must fail rules-artifact
   admission before campaign mutation.
8. Unsupported Barrage, Anti-Armor, modifier, result, loss-allocation, capture, retreat, Contact,
   or disclosure inputs must reject before combat mutation rather than use a default.
9. Equal source inputs and seeded draws must reproduce identical events and state after the later
   RNG, command, event, snapshot, and replay contracts exist.

## Confidence and limitations

Confidence is high in the selected calculation, Morale outcome set, five reachable differential
columns, and semantic result union because each was checked against the primary rules, common
charts, and applicable September 1979 errata.

The source matrices have not been double-entered into repository-owned typed data. This packet is
therefore research input, not an executable combat table. Confidence is intentionally limited for
all deferred content, state, choice, loss-allocation, disclosure, and random-order boundaries.

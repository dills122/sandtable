# Graziani Land-only logistics: source crosswalk and proof obligations

**November 16 follow-up:** [victory-supply source review](2026-11-16-graziani-victory-supply-source-review.md)
records the continuing Truck Convoy phase and distinguishes it from both local
supply draw and a proposed end-to-end victory trace. The interpretation remains open.

**Status:** research crosswalk; no runtime contract, adopted victory-supply ruling,
or full six-turn reachability claim. Original [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–45 and [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
pp. 26–29 visually read by one researcher; [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
§32.0 warns that abstract rules were not tested. Scenario- and row-specific status
is tracked in [source register](2026-11-12-graziani-source-register.md) LA-01–20.

## Composition map

| Entry point | Source effect | Consumer / decision boundary | Required investigation |
| --- | --- | --- | --- |
| Scenario creation | §§59.62–59.66 derive initial motorization from trucks and define additional supplies; §60.92 replaces both side-specific initial supply cases. | Setup selection, owner allocations, map placement, first authoritative snapshot. | Independent double entry; distinguish initial vs arriving conversion, real vs dummy, optional extras, and schema/ruleset identity. See [accounting pilot](2026-11-12-graziani-supply-accounting-pilot.md). |
| Supply-unit state | §§32.12–32.15 define noncombat units, capture, stacking and capacity; §32.18 provides dummies. | World objects, ownership, hidden attributes, capture and legal placement. | Reconcile scenario's stated dummy groups with general allowance and setup stacking exceptions; specify declassification and provenance. |
| Access and debits | §§32.16, 32.21–32.25 connect eligible units to nearby supply and impose ammo/fuel costs. | Action legality, resource reservation/consumption, results and canonical event. | Source exact distances, route type, blocked hexes, action costs and timing; model simultaneous or multi-unit costs without order-dependent overspend. |
| Unit movement | §§32.31–32.37 and 32.58 specify transport and movement of supply units, including alternative modes. | Organization attachment, Movement/Combat, convoy, rail and other source-defined arrivals. | Separate real/dummy transport, one-action limits, allowed stacking and source/destination constraints; do not treat the catalogue as proof each mode is admitted. |
| Availability | §§32.41–32.47 plan and receive units with side-specific timing; scenario supplies initial exceptions and §60.37 adds Axis planning. | Turn opening, private side plans, delayed arrivals, seeded table results and ownership. | Build a turn 1–6 schedule with source tables, identify pre-scenario planned state and conflicts, define no-op only where source-proven. |
| Motorization | §§32.51–32.58 govern allocation, use, loss and transport; §60.92(A) controls incoming conversion. | Attachment, movement/Breakdown, losses, supply transport and ledger. | Initial §59.63 uses medium plus heavy; incoming §60.92(A) names medium. Verify timing and source exceptions; do not reuse physical-truck Breakdown blindly. |
| Convoys/fleet | §§32.61–32.75 retain simplified Axis convoys and Commonwealth fleet attack effects. | Turn planning, lane/cargo choices, source-selected rolls, delayed arrival, side-safe observations. | Reconcile original tables, cargo loss/rounding and setup; no detailed Air Game dependency is implied by an abstract effect. |
| Other modifications | §§32.8–32.9 modify anti-air and road/track stacking in this mode. | Scenario creation and action legality through both side blocks. | Enumerate affected types and replacement-point categories; ensure unrelated stacking limits still apply. |

## Supply versus victory trace

The §32.16 access test answers whether a unit can draw from an eligible nearby
supply unit. Scenario §60.81 describes conditions involving a convoy or supply
route to a named source for victory. These are **different questions**: satisfying
one is not proof of the other. §32.6 retains abstract convoys, but the original
scenario does not explicitly state in the inspected passages how each §60.81
victory condition maps onto that abstraction. A community interpretation proposes
a route test; it is comparison evidence, not an adopted Sandtable rule.

Before writing a victory contract, reconcile at least: target/source ownership;
unit-versus-hex conditions; route direction and length; terrain and ZOC treatment;
enemy/friendly occupancy; what qualifies as a convoy or origin in Land-only mode;
timing of the test; and what happens if no level or both sides' levels match.
Capture each alternative and a versioned decision; do not substitute §32.16's
nearby-supply test without a source-backed ruling.

## Verification design

For each future authority slice, freeze typed command/event/snapshot identities
and exact source/ruling references first. At minimum test:

1. Fixed source setup, allowed choice and explicit unsupported or malformed choice;
   exact per-side accounting without duplicate replacement supplies.
2. Resource sufficiency at the relevant boundary and one unit below it; multi-unit
   competition, costs paid once, capture/depletion, and rollback on rejection.
3. Terrain/ZOC/visibility forks where equal authorized observations must not reveal
   hidden opposing supply, dummies, plans or RNG. Legal-action queries do not mutate.
4. Same seed and accepted commands reproduce events, state and resource totals;
   each nonterminal checkpoint can be restored, stale retries rejected and accepted
   retries deduplicated. No external/model I/O inside authoritative turns.
5. Turn 1 through turn 6 planning and arrival boundaries, including any prior-plan
   exception. Unknown table rows remain unsupported until checked, never zero-filled.

These are planned acceptance vectors, not executed tests. The current Core's
synthetic Movement and Combat fixtures cannot establish complete abstract logistics
or a source-verified scenario. User-facing lifecycle and two independent complete
games remain separate MVP gates.

## Supply availability: initial chart inspection

The original Axis charts PDF p. 34 contains §32.46 and §32.66; its p. 36
contains the Axis monthly convoy-level chart. The Commonwealth charts PDF p. 31
contains §32.47. The corresponding source identities are recorded in the
[arrival pilot](2026-11-12-graziani-arrival-scope.md) and
[formation investigation](2026-11-12-graziani-source-register.md#recovered-formation-addenda).
These are first-reader observations; no chart has been admitted as typed rules.

- §32.44 uses the Axis **planning month's** convoy letter for a supply arrival
  two turns later; §32.45 selects the Commonwealth supply period for the
  **arrival**, four turns later. Scenario §60.37 requires Axis convoy planning
  before play for the remainder of September. These are different temporal inputs;
  initial-turn and pre-scenario plan representation remain open.
- The monthly Axis chart lists B for September and October 1940. Conditional on
  a qualifying B planning month, §32.46 yields 0, 1, 1, 2, 2, 3 units for die
  results 1–6. This is a chart reading, not proof that every such roll is reachable.
- §32.47 period I includes 1940; its six die rows yield 1, 1, 2, 2, 3, 3 units.
  Verify scenario calendar/arrival applicability and the exceptional opening before
  claiming how many rolls or arrivals occur in six turns.
- §32.66 maps Axis convoy routes and Commonwealth positions into bombing inputs;
  §32.64 then uses the separate bombardment results table. Scenario §60.37's
  initial lane restriction and source revisions must be joined before deriving
  candidate outcomes. This is not evidence that a detailed Air Game is required.

Minimum test vectors after independent review: all six rows of each applicable
availability column; planning/arrival month boundaries; pre-scenario plan present,
missing and inconsistent; a roll yielding zero versus an unsupported/missing
input; immutable delayed receipt; separate player visibility and stale retry.
Additional table coordinates and route conditions remain unenumerated.

### Monthly motorization loss: chart identity and date ambiguity

Original Land §32.57 calls for a monthly motorization loss chart and §32.59
names it. The inspected original common charts PDF p. 13 instead labels an
**Abstract Truck Loss Chart** §58.5; its legend reports Commonwealth and Axis
motorization percentages. This is a strong candidate for the missing cross-reference,
not a confirmed official correction. The chart's 1940 September cell is a dash;
October is `6/2` (Commonwealth/Axis). Do not convert the dash to zero or apply
October's row to a September loss without a separate interpretation.

Land §32.57 says losses for the **previous month** are determined during the
first naval-convoy stage of each month, then instructs players to use the
appropriate month. On the six-turn calendar, the first October stage occurs in
turn 3; whether its table lookup uses September's dash or October's `6/2` is not
settled by this first reading. Also verify starting-turn treatment, integer
rounding, loss selection location, unit ownership at calculation time, and
whether any applicable erratum changes the procedure. Do not derive actual lost
points from initial setup totals before those questions are resolved.

The initially accessed Air/Logistics package transitions from printed p. 22 to
inserted charts before the scenario; printed pp. 23–25 were not present at that
point. **Later discovery:** the [complete archival package](https://spigames.net/PDFv2/CampaignNorthAfrica.pdf)
contains those pages at PDF pp. 74–76. Printed p. 25 was rendered and visually
checked. Its §58.42 expressly points physical-truck losses to §32.57/§32.59,
and §58.44 names an abstract truck loss chart. This strengthens the candidate
cross-reference to the common chart labelled §58.5, but does not establish an
official renumbering or the applicable month. The complete PDF's SHA-256 is
`836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`;
the retrieved scan stays outside Git. This is the same reader inspecting another
source package, not independent review.

Section 58 is explicitly for playing Land **with** detailed Logistics while
abstracting Air. For example, §58.43 distinguishes truck replacement arrivals
from reinforcement-track trucks when accounting for air-facility support.
The playable RC uses Land **without** both detailed games and is governed by
§32 and scenario §60.92. Do not silently import §58's physical-truck or fuel
procedures, including its support reduction, into that profile merely because
the chart references overlap. Check source mode, conversion and any exception
before specifying behavior. LA-17 remains unresolved.

Candidate tests after a ruling: first-month and month-rollover applicability;
both sides' chart ordering; missing/dash versus numeric cells; source-defined
rounding at small totals; legal loss location/selection; repeat submission,
checkpoint and replay. Keep physical truck losses and abstract motorization
losses distinct even if they consult the same printed chart.

### Planning horizon: arithmetic, not an arrival schedule

Under the §32.43 lead times alone, arrival turn `T` would ordinarily be planned
at `T-2` for Axis and `T-4` for Commonwealth. `0` and negative values below are
relative pre-scenario slots, **not** valid game-turn identifiers or invented
accepted events. §32.43 also states a starting-turn exception, and §60.37 adds
pre-start Axis planning for the rest of September; their exact interaction with
both sides' initial pending supplies needs a sourced ruling.

| Arrival turn | Axis ordinary planning slot | Commonwealth ordinary planning slot | Status |
| ---: | ---: | ---: | --- |
| 1 | -1 | -3 | Starting-turn exception; do not infer an arrival or a pre-start roll. |
| 2 | 0 | -2 | Both slots precede start; §60.37 Axis September plan also applies. |
| 3 | 1 | -1 | Axis plan occurs during scenario; Commonwealth pre-start state unresolved. |
| 4 | 2 | 0 | Axis plan occurs during scenario; Commonwealth pre-start state unresolved. |
| 5 | 3 | 1 | Both sides' ordinary planning slots occur during scenario. |
| 6 | 4 | 2 | Both sides' ordinary planning slots occur during scenario. |

These slots expose why a creation contract must specify whether carried-in plans
exist and how they are authenticated. They are not proof that every slot produces
a roll or that a convoy arrives. Test missing and malformed pre-start evidence,
authorized no-obligation cases, source-permitted zero result, delayed execution and
replay; never fabricate past history to reach a convenient starting state.

**Candidate contract direction, not an adopted rule:** model source-required
pre-start commitments as explicit, typed setup decisions and recorded seeded
outcomes at campaign creation, rather than claiming the player executed negative
turns. Preserve each side's private plan, source/ruling identity, target turn and
any genuinely required random draws. Distinguish a specified zero arrival from an
unmade plan. Compare this with a source-supported predetermined initial schedule
or another documented mechanism before choosing. The source investigation must
resolve which commitments actually apply, including the starting-turn exception;
this candidate does not itself authorize rolling, cargo selection or arrivals.

For a future contract review, check setup with no required pre-start obligations,
one side pending, both sides submitted in either allowed order, stale submissions,
wrong-side access and a missing plan at the execution boundary. Reconstruct the
same seed, plan commitments and eventual receipts after process restart; show that
opponent observations do not reveal private cargo or unused randomness. Boundaries
and default decisions must follow accepted source rulings, not fixture convenience.

## Bounded initiative cross-check

Original [common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
p. 2 (retrieved SHA-256
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`)
places turns 1–6 in the Commonwealth rating-3 band. Axis rating is based on
qualifying Rommel/German land-combat presence, with holding boxes excluded. The
original Axis schedule begins after this scenario; initial placement and all
scenario-specific exceptions still need independent verification before treating
absence of qualifying German forces as an admitted invariant.

**Conditional proof vector:** if Axis rating is 1 and Commonwealth rating 3,
the 36 possible one-round die pairs partition into 6 Axis wins, 26 Commonwealth
wins and 4 ties. Example: Axis 3 versus Commonwealth 1 ties; the next complete
pair must determine or reroll again. This is arithmetic over a stated hypothesis,
not a claim about randomness distribution, scenario admission or future support.
The source fixes Italian initiative on turn 1, so the contested vector applies only
to turns 2–6 if that hypothesis remains true. Existing
[initiative acceptance](../../specs/initiative-determination.md#acceptance-scenarios)
and the 21 focused rating tests passed locally in the preceding work session are
bounded foundation evidence, not a six-turn replay proof.

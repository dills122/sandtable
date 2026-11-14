# Six-turn abstract supply planning: candidate timing and tests

**Status:** source analysis and test design, not a ruling, source-complete table,
or implemented scenario. This pass visually rechecked Land §§32.31, 32.41–32.47
on printed pp. 43–44 of the local compiled scan SHA-256
`836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`.
Original charts, errata, scenario start procedure, timing and rights still need
independent review. This is a same-stream source check, not double entry.

A [blind source-review packet](graziani-rules-review/README.md) now targets
the relevant printed cases; its independent entry and ruling review remain open.

## Planning horizon

§32.43 says Axis plans two turns ahead and Commonwealth four, while permitting
as much advance planning as needed so supply can arrive in non-starting turns
when dice permit. It excepts the scenario's starting turn from that ordinary
arrival expectation; it does **not** say all later results must be positive.
The following grid is arithmetic over those offsets, not an assertion that a
negative-numbered campaign turn or a specific pre-scenario planning event exists.
`Before start` means a normal offset would precede turn 1; how the permitted
advance planning is actually initialized, dated and randomized remains open.

| Potential arrival turn | Map D turn-track date | Axis: normal two-turn offset | Commonwealth: normal four-turn offset |
| --- | --- | --- | --- |
| 1 | September III | Starting-turn exception; initial setup separately sourced | Starting-turn exception; initial setup separately sourced |
| 2 | September IV | Before start | Before start |
| 3 | October I | Turn 1: September III | Before start |
| 4 | October II | Turn 2: September IV | Before start |
| 5 | October III | Turn 3: October I | Turn 1: September III |
| 6 | October IV | Turn 4: October II | Turn 2: September IV |

The date column was rechecked against the original Map D turn track in the
[map pilot](2026-11-12-graziani-map-identity-pilot.md); it is not an independently
reviewed calendar or a pre-start plan initialization.

The source does not establish that only these arrivals are planned: choices or
plans beyond turn 6 might still have effects before the scenario ends. Determine
that applicability rather than silently truncating the real plan state. For
Axis, §32.44 specifies the availability letter for the **planning month**;
Commonwealth §32.45 selects its table period by **arrival**, not planning, date.
Initialization of pre-start rolls and any scenario or errata overrides can affect
which Axis month applies; do not manufacture a calendar date or reinterpret a
recorded result on replay. Scenario §60.37 separately requires the Axis to
plan convoy runs for the remainder of September before the game; reconcile
that instruction with §32.43 rather than assuming it alone initializes every
future supply plan.

The earlier [chart inspection](2026-11-12-graziani-abstract-logistics-crosswalk.md#supply-availability-initial-chart-inspection)
records conditional Axis letter-B outcomes `0, 1, 1, 2, 2, 3` and Commonwealth
period-I outcomes `1, 1, 2, 2, 3, 3` for die results 1–6. Those are single-reader
observations and must not be frozen as scenario outcomes until the calendar,
original chart and errata are independently checked.

## Replacement points are a separate schedule

For the Axis, the original Axis charts §4.43b (PDF p. 2) list the first
regular land reinforcement for Game Turn 9, Operation Stage 2. Axis charts
§20.66 (PDF p. 33) begin Italian replacement production planning on Game Turn 2
for Light AA, with arrival two turns later; the next listed categories start
later. Thus these regular schedules specify no Axis land-reinforcement or
replacement-production receipts in Game Turns 1–2. Land-only §32.83 modifies
AA treatment and must be applied when implementing the later entries. Scenario
§60.92A and Land §32.52 connect motorization production to medium-truck points;
the medium-truck production schedule begins on Game Turn 6, so it likewise
specifies no resulting production receipts in Game Turns 1–2.

These are schedule-specific observations, not proof of an empty convoy or
initial pool. Scenario §60.31 optionally places two Autoblinda 40 TOE as trained,
fueled replacement points in Tobruk at setup. Supply allocations, other cargo,
the starting-turn exception and timing of pregame decisions remain separate
questions. Locators refer to the original scenario scan (PDF p. 3), Land rules
(printed cases as cited), and Axis charts; independently reconcile any other
applicable exceptions before declaring an entire plan complete.

Scenario §60.47's Commonwealth subsection says no Replacement Points arrive
before November 1940; the map track places this six-turn scenario entirely in
September–October. This is a candidate exclusion for Commonwealth replacement
**arrivals**, pending independent scope/errata review. It does not establish an
empty land reinforcement, supply, Axis replacement or convoy phase, and it does
not settle whether plans for later turns are required during play.

Land §5.2's outline mentions Commonwealth replacement production two turns
hence; §20.21 says four turns, and the detailed §20.72 is corrected by errata
to one month ahead and selects the production table using the planning month. This needs reconciliation;
it must not be conflated with §32.45's four-turn supply-unit planning and
arrival-period table.

## Placement and timing boundaries

§32.42 sends Commonwealth supply to Cairo, with an Alexandria option when more
than three supply units arrive in a single Game Turn: one per three received may
be placed there. Candidate boundary vectors, subject to detailed placement and
stacking rules, are 0–3 received: no Alexandria option; 4–5: at most one; 6–8:
at most two. `May` preserves owner choice; it does not require the maximum. The
rule's aggregation window is the **Game Turn**, not a separate allowance per
operation stage. A source-confirmed table result of zero is distinct from an
absent plan, unrolled result, unknown table value or no applicable obligation.

§32.31 permits a supply unit to move by land or rail in the operation stage it
arrives. Actual action eligibility remains subject to the detailed transport,
capacity, stacking, owner and phase rules. This is separate from the erratum
about military reinforcements and cannot by itself establish every arrival's
movement legality. §32.42 Axis supplies follow the convoy rules; §32.61 includes
supply units, motorization and replacements in abstract Axis convoys. A land-only
unit schedule cannot establish that any of those categories are empty.

## Contract and failure-sensitive vectors

- Record planning owner, actual planning date/turn, target turn, table identity,
  exact source/ruling version, die/result and chosen allocation; distinguish
  pre-scenario initialization from ordinary in-campaign planning once resolved.
- Reject missing or reinterpreted plans, wrong lead time/table period, duplicate
  draws or deliveries, and a fake zero substituted for unknown. Replay preserves
  original draws and RNG cursor; retry after a lost response cannot roll twice.
- Cover both sides and every turn in the grid; test the turn-1 exception, an
  actual zero, minimum/maximum chart entries, calendar boundaries and plan state
  at scenario termination. Do not claim chart coverage before checking the
  original table and errata.
- Test Alexandria eligibility at 3/4 and group thresholds, owner choice below
  maximum, aggregate multiple stages within one turn, invalid locations,
  capacity and duplicate IDs. Preserve private future plans in outward views.
- In the arrival phase, prove simultaneous land, supply, replacement and convoy
  obligations cannot erase one another. Prove a permitted arrival-stage supply
  move with a legal path and an explicit rejection for each unsupported path.

These vectors are not an implementation or a conclusion that a particular supply
arrival is reachable. They refine LA-16 and the arrival handoff without closing
`MVP-GATE-01`.

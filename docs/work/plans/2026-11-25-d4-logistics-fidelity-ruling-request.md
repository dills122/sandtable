# D4 ruling request: logistics fidelity under Land-only §60.92

**Source-derived document.** Facts below come from one-reader repository
transcriptions of the original 1979 rules and scenario; none is independently
reviewed, and source-derived material is outside the Apache-2.0 code license.
**Date:** 2026-11-25. **Needed from:** project owner. **Evidence:** [full
classification](2026-11-25-graziani-logistics-reachability-classification.md)
(labels VERIFIED / DOC / ASSUMED there; row IDs below refer to it).

## The question

For supply, consumption, repair, towing, rail, patrol, convoy and weather
effects: where may the first release candidate use a **legal, explicit pass**
(the engine rejects any attempt at the action as `rule-not-implemented`, and the
exclusion is recorded in the scenario/ruleset identity), and where does it need
**real rules**? Default already on file (analysis D4): real where victory or
per-turn arrivals depend on it, otherwise an explicit pass.

## What the study found

- Only **one** logistics item is victory-dependent: the §60.81 supply trace
  (F1). The source does not say how it works under Land-only (four readings on
  file).
- Nearly everything else hangs on three yes/no switches (below). Most actions
  are voluntary, so declining them is legal play; the danger is silent
  acceptance, not absence.
- Foul weather occurs in about 99 percent of full games (ASSUMED independent
  stages, 8/36 per stage). Its movement effects (B1) are not optional.
- Towing and maintenance (C5) have no source matrix in the repository. A
  one-hour source pass is needed before they can be ruled.

## Rulings requested

| # | Question | Recommended answer | If you answer otherwise |
| --- | --- | --- | --- |
| **D4-a** | Victory supply semantic for §60.81 | **Hypothetical route**: a road/rail/track path for a medium truck from the holding unit to the named source, blocked by enemy units and unneutralised enemy ZOC. It matches the community reading, needs no shipment history, and keeps the module pure. Policy stays a parameter so other readings fail as "unsupported", never guessed. | "Actual shipment" pulls in A8, D6, E1-E3 (about 60-120 wh, ASSUMED). "Local §32.16 draw" is cheap but changes the scenario's printed condition. "Defer" keeps victory unadjudicated. |
| **D4-b** | Enforce ammo/fuel costs and supply access (§§32.16, 32.21-32.25)? | **No for RC1**: accepted, recorded exclusion ("supply unlimited"). Supply units still exist as setup content (A1, A3) so the trace and later releases have them. | **Yes** adds A4-A6, A9-A11, B2, E1-E3: roughly 60-120 wh plus the unfinished cost-table transcription (ASSUMED). |
| **D4-c** | Vehicle Breakdown and repair in scope? | **Out for RC1 together**: Breakdown, field and facility repair, towing, maintenance all excluded as one unit; they are inert without each other (C1-C5). | **In** adds C2-C5, B3, B4 (about 60-120 wh, ASSUMED) and the vehicle-cohort Breakdown that is unreachable today. |
| **D4-d** | Rail movement, patrol, Truck Convoy phase | **Explicit pass** for patrol (D1) and Truck Convoy (D6). Rail (D2): pass in RC1; revisit if playtests show the Commonwealth cannot hold Matruh without it. | Each is separately sizeable: patrol is fog-sensitive and needs privacy tests; rail needs ZOC and CP history. |

## What stays real regardless of your answers

1. Weather determination and **foul-weather movement effects** (B1, B7), in the
   Movement lane.
2. Initial supply setup validation (A1, A3) and its source reconciliation,
   because the trace's endpoints and the §60.92 replacement depend on it.
3. The **supply trace** and its map inputs (F1-F3): the data (`road`, `railroad`,
   `track` edges on 7,073 hexes) and ZOC derivation already exist.
4. Fail-closed rejection and an exclusion list in ruleset identity, so a saved
   game never silently changes meaning when an excluded rule is later built.

## Cost of the recommended set (ASSUMED, wh = agent-worker hours)

| Tier | Contents | Estimate |
| --- | --- | --- |
| 1 (recommended) | Trace module, ZOC/occupancy adapter, exclusion registry and rejection tests | 10-18 wh |
| 2 | Tier 1 + supply ledger, costs, arrivals, convoys | adds 60-120 wh |
| 3 | Tier 2 + repair, towing, rail, patrol | adds a further 60-120 wh |

The analysis put the swing at "0 vs 200+ wh"; these ranges are consistent with
that but are my estimates, not measurements.

## Known limits of this request

- Not independently reviewed against scans. D4-a in particular rests on a
  community interpretation (comparison evidence only) and on the two blank
  spots the victory-supply review lists: map D "any hex" quantifier and whether
  a friendly unit lifts ZOC obstruction.
- Whether road/rail edges actually connect Tobruk, Alexandria/Cairo and map D
  across the C/D seam is **unmeasured**; the first test any trace module needs.
- Stage 2/3 Host wiring and End of Turn / victory timing are separate lanes (T3,
  T4) and not decided here.

## Next step after your ruling

If D4-a = hypothetical route, build in new files, unwired, with targeted tests
(no edits to `ExerciseSession.cs`, `CampaignExercises.cs`, `PendingDecision.cs`):
(1) `CampaignSupplyTrace`, a pure BFS/Dijkstra over `Cna1979HistoricalMap`
edges, parameterised by policy and obstruction predicate, returning a typed
result with reasons or "unsupported policy"; (2) a connectivity probe test over
the real map; (3) an exclusion registry plus rejection tests. Hand-off to the
victory evaluator (T4) is by interface only.

# Graziani: rail eligibility and construction research

**Status:** one-reader source analysis, not an accepted railway model or proof
of six-turn reachability. Visually checked original Land pp. 15 and 37
(§§8.71–8.73 and 24.6), scenario p. 28 §60.7, Air/Logistics p. 23 §54.4,
Land p. 43 §32.34, and September errata p. 1. The original scans and rights
remain separately tracked in the [source register](2026-11-12-graziani-source-register.md).

| Source | First-reader observation | Open consequence |
| --- | --- | --- |
| Scenario §60.7 | At start the railroad runs to Mersa Matruh and ends there. | Verify exact constructed hexes and initial control on the admitted map; a named endpoint is not complete topology. |
| Land §8.71; errata §8.71 | The Land text describes the Alexandria–Mersa Matruh railroad as Commonwealth-only; the later correction explicitly permits Axis use under §54.4. The separate Benghazi–Barce line is historical and unusable. | Preserve the correction rather than hard-coding all rail as Commonwealth-only. Determine the Axis phase and Land-only applicability, including the distinction between the two physical lines. |
| Land §§8.72–8.73; errata §8.73 | Commonwealth rail movement is once per Operations Stage with one stack in each direction. A travelling unit must start on the railway, spend no CP for another reason during that stage and not be in or traverse enemy ZOC or an enemy-occupied hex. The correction also denies reorganization points in the stage of rail travel. | Resolve exact load/stack calculation, phase, movement completion, ownership, history, and whether detailed cases create exceptions. Test both directions, two stages, ZOC and a move followed by attempted reorganization. |
| Land §§24.61–24.67 | Only the 10th and 13th New Zealand railroad construction companies can build new railroad; one requires two Operations Stages per new hex, while both in the same hex can build one in one stage. Construction proceeds from the railhead west toward Tobruk; enemy-controlled or occupied hexes cannot be built or rebuilt. Engineers may repair destroyed track under separate conditions. | Independently verify arrival dates, location, control, stage timing, costs and map distance. The printed ordinary cost includes stores; reconcile §32.11's abstract expenditure rule. Do not treat repairing damaged existing rail as building new track. |
| Air/Logistics §§54.41–54.46 | Axis use of controlled, contiguous Commonwealth rail requires imported rolling stock in the detailed rule; Axis cannot build new rail but can repair it. The specified rolling-stock conversion consumes stores and fuel, and the detailed text describes movement during the convoy stage. | Decide which conditions survive Land-only abstraction and whether fuel/stores have a replacement cost. Verify phase relative to the Land outline, map control and actual imported resources before admitting any Axis action. |
| Land §32.34 | Commonwealth may move up to two real and/or dummy supply units in one direction per Operations Stage under normal railroad rules. | This is a supply-specific allowance; do not infer it authorizes Axis supply movement, overrides all unit rail capacity, or creates an additional general movement in the other direction. Reconcile its timing and cargo with §8.72. |

The scenario's existing railhead, the limited construction rate and the Axis
control/stock conditions make an initially available Axis railroad **unproved**,
not impossible. A convincing exclusion requires the real map, complete source
and errata, initial control, construction company schedule, legal movement and
all relevant choices for the full horizon. Equally, a synthetic test of Axis
rail shows only conditional behavior; it does not demonstrate historical
reachability.

Before implementation, bind source, profile and any ruling to content identity;
model track/railhead separately from printed unbuilt terrain; enforce actor,
one- and two-direction allowances, rail occupancy/ZOC, prior CP and no same-stage
reorganization. Test incomplete construction, one/two company timing, damaged
track repair, capture/control, private observations, replay and stale or duplicate
construction/rail commands. Resource and phase decisions remain explicit blockers.

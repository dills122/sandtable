# Graziani weather scope and existing capability boundary

**Status:** single-reader source crosswalk and test design, not historical content
admission or an implementation certificate. Original Land p. 41 §§29.0–29.58
and common chart p. 6 §29.61 were visually inspected. See the existing
[weather source analysis](../../research/operation-stage-preamble-spike.md),
[weather design](../../design/weather-determination-v1.md), and
[weather specification](../../specs/weather-determination-v1.md) for the
season-boundary conflict and bounded `WEATHER-001` authority.
A [blind source packet](graziani-weather-review/README.md) records selected
calendar, correction and effect cases; its second entry remains open.

## Calendar and outcomes

Scenario turn 1 begins September III 1940, followed by September IV and October
I–IV for turns 2–6. Original Land §29.1 places those dates in fall. The printed
chart mislabels the turn bands; existing versioned ruling
`cna-1979.1.ruling.weather-season-boundary` remaps the turn bands and preserves
fall's printed probability row. All six scenario turns therefore use that row
under the existing ruling; this does not independently reapprove the ruling.

| Ordered d66 results | Outcome | Number of pairs |
| --- | --- | ---: |
| 11–35 | Normal | 17 |
| 36–54 | Hot | 11 |
| 55–61 | Sandstorm | 3 |
| 62–66 | Rainstorm | 5 |
| Total | | 36 |

The two dice are read in order, not added. Numeric spans above count only values
with digits 1–6. Weather is determined per operation stage: the six-turn scope
has 18 such positions if the full scenario reaches its ordinary end. Foul results
require the additional location die; normal/hot do not. Do not treat this count as
proof that all stages or all effects are currently executable.

## Source obligations and limits

| Boundary | Source-backed observation | Scenario proof still needed |
| --- | --- | --- |
| Outcome and scope | §§29.1, 29.31: hot is global; foul weather affects map sections selected by the location chart. | Bind each admitted scenario location to correct map area; preserve deterministic RNG, conditional draw, event and replay. |
| Sandstorm exclusion | §29.41 excludes Nile Delta hexes even if section E is selected. | Published-map delta subarea identity and effect enforcement are deferred in Weather v1; validate exact locations and negative cases. |
| Hot stored fuel | §29.34 reduces stored fuel and water by 5% during weather determination, with stated exclusions. §32.15 supply units hold fuel in Land-only mode, while §32.11 replaces ordinary resource usage/expenditure rules with §32's own rules. | Decide whether hot-weather loss is covered by that replacement, and resolve rounding, off-map areas and reachable stores; test per-unit versus aggregate rounding and conservation. Do not presume abstracted water exists or vanishes as a separate pool. |
| Foul movement/construction | §§29.42, 29.44, 29.54–29.58 affect construction, movement costs and terrain crossing in affected areas. | Connect accepted map/terrain identities and each relevant phase to actual effects; test unaffected locations and repeated movement without compounding incorrectly. |
| Breakdown | §§29.33, 29.45 modify certain breakdown calculations; §32.55 exempts motorization points but not all breakdown. | Determine affected vehicle and mode cases, thresholds and timing; do not apply physical truck breakdown to abstract motorization by analogy. |
| Well and aircraft effects | §§29.47, 29.53 mention grounded aircraft and depleted wells. | Establish Land-only applicability and reachable subjects; do not invent omitted detailed Air/Logistics state. Explicitly reject or implement positive subjects under an admitted policy. |

`WEATHER-001` currently accepts only setup-hashed synthetic policy declaring no
immediate-effect subjects, resolves and records weather, and stops at the
Organization barrier. Its synthetic location assignments are not published-map
identities. Scenario §60.92 includes fuel-bearing supply units; therefore the
absence-of-subjects fixture is not evidence that Graziani's weather effects are
complete. The exact §29.34/§32.11 interaction still needs source/ruling review:
one reading treats evaporation as a replaced fuel expenditure; another treats
loss of stored fuel as distinct from usage or expenditure. Neither is selected
here. Existing bounded rules and replay are useful
foundations, not an end-to-end scenario proof.

## Targeted verification after source and contract review

1. Six turns × three stage identities, with fall-table boundary pairs `35/36`,
   `54/55`, `61/62` expressed as d66 values and valid ordered dice; test all 36
   outcomes and conditional foul-location draw. Cross-stage checkpoints must not
   reuse a previous result.
2. For hot fuel, exercise zero, below-one-loss, exact-one-loss and multiple-unit
   distributions using the accepted rounding/aggregation rule. Assert no negative
   balance, no duplicate debit, correct exclusion and restored/replayed totals.
3. For each foul area outcome, distinguish inside, outside, shared-edge and
   Nile Delta exception locations. Cover movement, construction and breakdown only
   where their own contracts are admitted.
4. Reject incompatible or unmodeled positive subjects before drawing; verify
   observation privacy and exact deterministic replay after source/ruleset change.

These are candidate vectors; none was run by this research packet. First resolve
source hierarchy, historical map identity and immediate effects; then design typed
command/event/snapshot changes and prove integration with supply and movement.

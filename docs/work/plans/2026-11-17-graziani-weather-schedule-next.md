# Graziani weather: stage schedule and effect gates

**Status:** bounded primary-source matrix, not an admitted historical effect
contract, set of rolled outcomes, or proof every stage is reached. The named
season-boundary ruling is an existing project selection, not a publisher
clarification. No runtime or readiness changes.

## Schedule and startup

Land §29.0 requires weather in **each individual Operations Stage**. §5.2
places its Weather Determination Phase after that stage's initiative
declaration and before Organization. The player holding initiative rolls
two dice sequentially, not as a sum; §60.6 gives the Italians initiative for
the whole first Game Turn. The initiative holder's choice of player A/B is a
separate stage decision. Scenario §60.22 starts GT1 OpStage 1 and ends at the
conclusion of GT6 OpStage 3; Map D's track shows the dates below. The normal
stage outline contains weather even in GT1 OpStage 1. The scenario does not
expressly waive that phase or supply a starting weather outcome. A startup
implementation must bind its actual stage-entry boundary; do not infer a
preexisting roll or skip weather merely because the earlier **Naval Convoy
Stage** has a separate scenario-start ambiguity.

| Game Turn | Printed date | Stages 1, 2, 3 if reached | Season under existing ruling |
| --- | --- | --- | --- |
| 1 | September III 1940 | Determine each stage, including opening stage | Fall |
| 2 | September IV 1940 | Determine each stage | Fall |
| 3 | October I 1940 | Determine each stage | Fall |
| 4 | October II 1940 | Determine each stage | Fall |
| 5 | October III 1940 | Determine each stage | Fall |
| 6 | October IV 1940 | Determine each stage, including final stage | Fall |

These are 18 distinct possible stage determinations if play reaches its
normal end, not 18 already-executed rolls. Stage identity includes both turn
and OpStage. Every determination needs the two base dice; one **additional**
location die is required only after a sandstorm or rainstorm result, so 36 base
dice plus zero to 18 location dice over the full six turns. Missing stage,
unknown eligibility, or an unexecuted draw is not normal weather or an empty
event.

## Season and outcome conflict

Land §29.1 states fall starts September III and ends December II inclusive.
Common chart §29.61 instead labels GT1–12 spring, even though its fall row is
separately identified. September errata §29.61 calls the chart backwards and
refers to §29.1, but its parenthetical gives spring March IV–June III, one week
later than the §29.1/errata §29.1 March III–June II statement. Existing
`cna-1979.1.ruling.weather-season-boundary` selects §29.1's boundaries and
remaps turn bands while keeping the labeled seasonal probabilities. It makes
GT1–6 fall; it does **not** make the errata internally consistent.

A possible alternative extrapolates the erratum's later spring interval to
shift all seasons one week, under which September III could be summer; the
erratum does not explicitly list that full alternative calendar. Another
literal chart-band reading would use spring for GT1–6 despite the correction.
Either can change GT1 outcome at the same d66; do not call them official
corrections. Under the selected fall row, ordered d66 `11–35` is normal,
`36–54` hot, `55–61` sandstorm, `62–66` rainstorm; valid dice have digits
1–6. These are respectively 17, 11, 3 and 5 of 36 ordered pairs. For example,
`35` is normal and `36` hot; `54` hot and `55` sandstorm; `61` sandstorm and
`62` rainstorm. Do not invent actual values, add dice, or consume the extra die
for normal/hot.

## Location and effects: known versus unresolved

| Case | Source-proven scheduling/scope | Unresolved execution boundaries |
| --- | --- | --- |
| Normal | §29.2 says no weather effect on play. No foul-location die. | Other stages and independent obligations still occur; normal is not evidence that the whole turn has no action. |
| Hot | §29.31 applies hot across all game-map sections. §29.34 reduces eligible fuel and water levels 5% **during weather determination**, excluding wells/oases water and fuel/water actually being consumed by a unit. No location die. §29.1 excludes specified off-map places, including Tunisia–Tripoli. | §32.11 replaces ordinary supply usage/expenditure while §32.15 creates fuel-bearing supply units. Whether stored-fuel evaporation survives Land-only abstraction is unresolved, as are grouping and fractional rounding, off-map membership and coverage. Hot is not a proven no-effect case. |
| Sandstorm | §29.1 requires one extra d6 for location; unaffected map sections are normal. §29.41 excludes Nile Delta hexes even when section E is selected. §29.42 blocks construction; §29.44 doubles movement costs in affected hexes. | Map-area, delta and crossing identities, construction and movement implementations, and the detailed §29.45 breakdown threshold/subject are not certified by the weather roll alone. |
| Rainstorm | §29.1 requires one extra d6; unaffected sections are normal. §29.53 fills depleted wells; §§29.54–.58 constrain construction, wadis, roads, rivers and delta movement. | Applicable well state in Land-only mode, map/edge identities, vehicle type, phase and affected hexes remain independently gated. |

The weather text names the foul location table §29.7, while chart §29.61's
asterisk refers to §29.62. The [independent check](2026-11-17-graziani-weather-schedule-review.md)
located the original §29.7 table on common-chart p. 8 and confirmed prior
mappings `1→A,B`, `2→C,D`, `3→D,E`, `4→B,C`, `5→B,D`, `6→B,C,D`.
Its asterisk excludes Nile Delta hexes from sandstorm effects; the table also
says sandstorms do not extend north of coastal hexes. The §29.62 reference is
not proof of a second location mapping. Delta
subarea identity remains required even when E is selected. No rule says an
unknown area or missing effect implementation is normal or has no subjects.
Grounded aircraft and water/wells require mode applicability; do not create
Air/Logistics subjects merely because the full rules name them. Conversely,
scenario initial supply is not evidence of an empty fuel subject set.

## Focused acceptance checks

- At `(1,1)` establish the actual stage entry, Italian initiative, two ordered
  base dice and phase ordering before Organization; compare startup alternatives
  without silently dropping weather. Repeat distinct stage identities through
  `(6,3)` and test checkpoint/resume for exactly-once draw and effects.
- Exhaust 36 ordered fall pairs and all six location values for each foul
  outcome after independently verifying the location table. Test transitions
  `35/36`, `54/55`, `61/62`, no location draw for normal/hot, and no reuse
  across stages. Compare GT1 September III under the selected season with an
  explicitly specified shifted-calendar alternative.
- Test hot fuel: zero and positive stored quantities, exclusions, sub-one and
  exact-one losses after a rounding/aggregation rule is selected, section and
  off-map scope, resource conservation and atomic replay. Missing or unknown
  required subjects block effect adjudication; they are not zero stores.
- Test sandstorm E with verified delta and non-delta cells; rainstorm wadi,
  road/rail, river and delta cases; unaffected sections; construction and
  movement only through accepted spatial and action rules. Test normal weather
  as a weather-effect no-op without suppressing other turn obligations.

These are proposed source-sensitive tests, not executed results. Historical
effect admission also requires a reviewed scenario startup, season policy,
location table, map coverage, Land-only resource interaction and phase-specific
contracts.

## Evidence and limits

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 41 and 43, §§5.2, 29.0–.58 and 32.11/.15 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`);
[common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
printed/PDF p. 6 §29.61 (`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`);
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 2 §§29.1 and 29.61 (`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`);
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 3–4 / printed pp. 27–28 §§60.22 and 60.6
(`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`);
and [map](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 4 turn track
(`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`).
All are same-edition scans. This is not an exhaustive errata, location-table,
map or effect audit; no gameplay tests were run.

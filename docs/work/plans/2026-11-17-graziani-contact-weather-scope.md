# Weather scope at the Map C contact hexes

**Finding:** Fort Maddalena C3019, the neighboring 11th Hussars C3020, Sheferzen C3419, 2nd Rifle C3320 and 1st RTR C3520 are inland hexes on **Map C** in the original scan. For a sandstorm or rainstorm result, the original foul-weather location table selects Map C on location die **2, 4 or 6**. The Nile Delta sandstorm exception is described for Map E, and the coastal cutoff concerns the Mediterranean north of coastal hexes; neither establishes an exemption for these inland C locations. This is a conditional source reading, not an actual rolled weather result or an admitted implementation.

## Source and local map

Reverified [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Visually inspected map PDF p. 3; common chart p. 8 §29.7; Land p. 41 §§29.0–.1, .31, .41, .44–.46 and .51; scenario PDF p. 3 §§60.22 and 60.31/.41 and p. 4 §60.6; errata p. 2 §§29.1 and 29.61.

```sh
pdftoppm -f 3 -l 3 -r 150 -x 1150 -y 1550 -W 1600 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/contact-weather/map
pdftoppm -f 8 -l 8 -r 200 -png -singlefile /private/tmp/sandtable-common-charts.pdf /private/tmp/contact-weather/chart
```

Map crop SHA-256 `d7f22af0e10960814853d1e77276c99637b354af9bc79cf37428d1261993c0b3`; chart PNG `b6c78bee35b98bc2e182b4ede743ea5c5753fdacae7d547b368a24c68f64f5a7`. Renderer bytes can vary; source hashes and parameters are primary. Crops remain outside Git. Prior [weather schedule](2026-11-17-graziani-weather-schedule-next.md), its [independent review](2026-11-17-graziani-weather-schedule-review.md) and contact-map notes were consulted; this is an informed same-scan check, not a new edition or rights grant.

The wide crop shows the Mediterranean coast near Bardia, Sollum and Halfaya, while the five listed coordinates lie inland farther south around Fort Maddalena and Bir el Sheferzen. `3019`, `3020`, `3419`, `3320` and `3520` are printed in or near their individual cells; the local 500/600-dpi identity and shared-side checks remain in [Fort contact](2026-11-17-graziani-map-fort-maddalena-contact.md), [Sheferzen](2026-11-17-graziani-map-sheferzen-initial-contact-review.md) and [1st RTR side](2026-11-17-graziani-map-first-rtr-side.md). No sea or coast symbol is visible in these five interiors; wider edge/terrain completeness is not certified. These coordinates do not require a sheet seam to identify Map C.

## Weather matrix for these hexes

Common chart §29.7 supplies the following **conditional location die**, used only after a sandstorm or rainstorm result under Land §29.1:

| Location die | Printed selected sections | Map C contact-hex reading |
| --- | --- | --- |
| 1 | A, B | C unaffected: normal for that foul-weather determination. |
| 2 | C, D | C selected. |
| 3 | D, E | C unaffected: normal for that foul-weather determination. |
| 4 | B, C | C selected. |
| 5 | B, D | C unaffected: normal for that foul-weather determination. |
| 6 | B, C, D | C selected. |

The chart's star says a sandstorm on a Nile Delta hex is treated as normal, and its note says sandstorms do not extend north of coastal hexes. Land §29.41 identifies delta hexes as Map E; §29.46 says sandstorms stop in coastal hexes. Neither is an inland Map C blanket exception. This conclusion does not infer that all Map C cells are land, that coastal hexes are excluded, or that a rainstorm has the delta sandstorm exception. Ordinary weather-area identity, per-hex terrain and any later correction remain separate evidence gates.

Land §29.0/.1 determines weather **each Operations Stage**; on a foul result the extra die identifies affected map sections and unaffected sections have normal weather. Section 29.31 makes hot weather apply to all sections. Scenario §60.22 gives the six-turn endpoint and §60.6 gives Italians initiative for the first game turn; stage-specific weather is not fixed by those placements. Under the existing weather-season planning ruling, the six turns use the fall row, subject to the conflicting chart/errata season wording documented in the prior schedule. No dice, actual storm, route, movement cost, breakdown or terminal weather is inferred.

If C is selected, determine effects against the particular action and state: §29.44 doubles applicable movement costs in sandstorm hexes, while §§29.45 and 21.37 and rainstorm terrain rules require separately resolved timing, type and edge semantics. ZOC, combat obligations, supply and unit presence are not certified merely because a weather section is selected. Missing stage roll, section mapping or cell identity remains unknown, not silently normal.

Independent review should verify the five labels and inland status, all six chart cells and scope of the delta/coast qualifications. Proposed tests cover location dice 1–6 for both foul types, hot/normal without a location die, stage rollover and unknown rolls, Map C inland versus a separately certified Map E delta/coastal case, and replay. These are unexecuted vectors. No runtime data, readiness or gameplay tests changed.

# Air-distance examples: Land-only evidentiary scope

**Date:** November 18, 2026. Follow-up to the [Tobruk–Ben Gania example](2026-11-18-graziani-map-distance-example-next.md), [registration source check](2026-11-18-graziani-map-registration-source-next.md) and [map-distance decision](2026-11-17-graziani-map-distance-decision.md). The Air/Logistics worked examples and §37.42 are relevant **printed evidence about intended map-distance conventions** even when detailed Air is omitted. The checked rules do not expressly make every value or construction in that chart a binding Land-only seam, route or adjacency rule. No precedence between conflicting sources is selected here.

## Printed placement and mode

The physical **Map C sheet itself** prints both “Determining Shortest Distance,” including the two worked examples, and the §37.42 Land Distance Chart and footnote. The same text also appears in the Air/Logistics booklet, PDF pp. 24–25. This is a useful distinction from describing the examples solely by booklet location: a player using the shared original map can see them without treating two impressions of the same material as independent editions. The map copy visibly retains Example 1's 31-hex Tobruk–Ben Gania construction, Example 2's eastward 33-hex legs and its `zero ... E3425` wording; §37.42's printed values and 33-hex note also appear there.

Land §4.1 describes the shared A–E maps and their physical arrangement, including its disputed `01xx`/`39xx` overlay wording. Land §32.0 applies its abstract logistics and air rules when playing Land without Air and Logistics. Scenario §60.92 labels its corresponding profile “Air & Logistics Abstracted (Land Game Only).” Neither §32.0 nor §60.92, in the checked passages, expressly says to discard the printed shared map's geographical evidence or expressly incorporates §37.42 as the authoritative Land-only geometry contract. The map and Air booklet copies are materially related source impressions, not a new edition or independent resolution of §4.1.

## Scope of the distance evidence

§37.42 describes shortest hex distances, excluding origin and including destination, with an assumed all-sea grid where needed. Its example explicitly says shortest distance is **not the actual flight path**. The map copy's eastward 33-hex examples are evidence favoring the candidate regular-grid translation under the assumptions recorded in the [example check](2026-11-18-graziani-map-distance-example-next.md); they do not identify which printed partial cells overlap, certify a particular side, or settle feature and terrain continuity. The documented chart triangle violations and the worked example's unexplained zero-hex leg prevent treating every printed value as one consistent distance oracle.

Land-only §32.37 separately uses an enemy-combat-unit **five-hex origin** condition and specifies its own abstract transport requirements. The checked sentence does not expressly name §37.42, define a cross-sheet alias or impose the chart's assumed sea-grid method for that condition. Land §8.33's road/track movement requires consecutive connecting road/track hexes through the intervening feature; a geometric or air-range distance alone does not prove any such route, or terrain/ZOC/occupancy legality. Whether a Land-only rule's geometric count adopts this chart convention, and what to do with conflicting map labels, are project interpretation and evidence-admission questions.

A defensible evidence hierarchy must preserve all of: explicit rule wording, same-map physical vertices and marks, chart values and their limits, separately identified errata, and any versioned project choice. The present [provisional decision](2026-11-17-graziani-map-distance-decision.md) retains unresolved cross-sheet identity and edges as unknown/unavailable while preserving its three already bounded C/D contract crossings. The source observations here do not broaden that admission or prove a source-mandated priority between Land §4.1 and the map-printed chart.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3200 -y 250 -W 2400 -H 2600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapc-air-examples
shasum -a 256 /private/tmp/mapc-air-examples.png
```

Map C crop SHA-256 `c2b06aa1f2ce702630997303866282610b2565f9a4d4bc3c5384257b24df55dd`; PNG bytes vary with renderer. Visually checked [map package](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [Air/Logistics](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf) pp. 24–25, `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) printed/PDF pp. 7, 14 and 43, `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; and [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF pp. 4–5, `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. Same scan family, no independent edition or exhaustive errata review. Documentation only; no data/runtime edits or gameplay tests.

# Graziani map: independent source transcription

## Scope and provenance

Blind reading of the three source PDFs only, before consulting existing Graziani map research, queue, comparison results, or interpretations. Rendered map sheets and relevant scenario and chart pages; page numbers below identify one-based PDF pages and printed page numbers when present. Observations are restricted to visible source markings. No seam adjacency or traversability is asserted.

SHA-256 calculated locally:

| Source | SHA-256 | Expected-hash check |
| --- | --- | --- |
| `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` | No map hash in supplied manifest; baseline not independently established here. |
| `/private/tmp/sandtable-scenarios.pdf` | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` | Matches `scenario` in `docs/work/plans/graziani-source-review/manifest.json`. |
| `/private/tmp/sandtable-air-rules.pdf` | `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` | No air-rules hash in supplied manifest; baseline not independently established here. |

## Benghazi and setup

- Maps PDF p. 1, Map A, coastal city and port labelled Benghazi; its printed Summary of Important Locations lists **Benghazi A4827**, with marks for major city, port, and victory condition. Chart is clearer than the small number obscured by city artwork on the map itself. Nearby labels include El Berca and Benina; scenario p. 4 [60.5] lists their airfields as A4728 and A4829 respectively.
- Scenario PDF p. 3, printed p. 27, [60.31] Italian Initial Deployment: **Benghazi (B4827): Benghazi Garrison**. Same section lists Benina A4829, Soluch A4130, Barce B5504 and Tobruk C4807. The printed B prefix is clear; no correction is supplied by this observation.
- Scenario PDF p. 3, [60.34] Axis Initial Supply Status names Benghazi without a hex and lists ammo 100, fuel 250, stores 100, water dash. Scenario PDF p. 4, printed p. 28, [60.92]B, land-game-only replacement supply table prints **Benghazi (B 4827)**, one regular supply unit, with 10 ammo and 10 fuel. The normal supply listing and the abstraction table are distinct provisions.
- Maps PDF p. 2, Map B: the printed 4827 appears inland among yellow stippled terrain corresponding visually to rock/gravel in Map A's terrain key; no Benghazi label or major-city/port symbol is visible in that hex. Fine markings and exact feature boundaries at scan resolution remain uncertain. This is an observation of printed B4827, not a ruling on the scenario's intended correction.
- Air-rules PDF p. 25, [37.42] Land Distance Chart prints **Benghazi (A4827)**. Map C's copy of the chart (maps PDF p. 3) likewise prints A4827.

## Sheet edges and visible features

Maps PDF pp. 1-3 are separately labelled Map A, Map B, Map C. In upright sheet orientation, Map A ends along its 33/34 edge, Map B begins along its 01 edge and ends along its 33/34 edge, and Map C begins along its 01 edge. Coordinate numerals at individual edge hexes alternate with the staggered grid. Same-numbered hexes on different sheets are not automatically identical: [37.42] footnote explicitly says a hex and the same numbered hex on an adjacent map section are 33 hexes apart.

- A/B edge: Map A edge shows coast, roads, railway, dark escarpment-like bands, brown terrain, stippling, clear and desert coloring; Map B's opposite edge shows coast, Barce-area road and railway, escarpment-like bands, brown terrain, clear and desert coloring. Symbols and colors are visibly present near the edges, but this inspection does not establish a particular cross-sheet hex pair, whether a feature crosses the join, or movement admission.
- B/C edge: Map B's 33/34 edge includes coastal Gazala area, roads/tracks, escarpment-like marking, stippling, clear and desert coloring. Map C's 01 edge includes coast, Tobruk-side road, dark escarpment-like marking, stippling, clear and desert coloring. Exact alignment of artwork, hexside terrain, and any traversable connection remains unverified. Labels near sheet margins can be visually crowded or clipped; colors and line styles alone do not establish connectivity.

## Distance chart cells

Air-rules PDF p. 25, [37.42], triangular chart; row/column intersection, distances in hexes:

| Locations | Printed cell |
| --- | ---: |
| Benghazi A4827 / Barce B5504 | 13 |
| Benghazi A4827 / Tobruk C4807 | 46 |
| Benghazi A4827 / Bir-el-Qatrani D2903 | 84 |
| Barce B5504 / Tobruk C4807 | 40 |
| Barce B5504 / Bir-el-Qatrani D2903 | 80 |
| Tobruk C4807 / Bir-el-Qatrani D2903 | 38 |

The footnote describes shortest distance from the first location exclusive to the other inclusive, including across the Mediterranean using an assumed all-sea grid where necessary. These values do not themselves prove a land route, a sheet-edge crossing, or legal movement.

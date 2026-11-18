# Map E: Kafr el Douwar junction sides

**Date:** November 18, 2026. **Disposition:** Three E3516 sides visibly carry railway; two also carry road. Pinned route labels agree on those sides. A fourth side appears river-marked but has no explicit pinned pair; retain as a source/pin discrepancy for review, not a data correction. This survey does not establish a continuous Alexandria route through the junction.

Inspected PDF page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), following the [east-branch review](2026-11-18-graziani-alex-rail-fork-followup-review.md). Printed 3516, neighboring numbers, and Kafr el Douwar identify the hex. Directions and approximate coordinates refer to the crop below. Cross-tied blue ink indicates railway; dark parallel lines indicate road. Pinned data was visible during inspection; this is not a blind comparison or independent edition.

| E3516 side and neighbor | Scan reading | Pinned pair |
| --- | --- | --- |
| Left, E3515 | No apparent road or railway crossing the vertical side. | No explicit pair. |
| Upper-left, E3615 | Railway and road cross the diagonal river-bearing side near `(580, 410)`. Artwork and lines converge toward Alexandria. | `minor-river`, `railroad`, `road`. |
| Upper-right, E3616 | Blue river ink follows the diagonal side from approximately `(660, 365)` to `(880, 490)`; no apparent road or railway crossing. | No explicit pair; river appearance merits independent review. |
| Right, E3517 | Railway and road cross the vertical side near `(880, 620)` and `(880, 570)` respectively. Railway overlaps other ink near the junction inside E3516. | `railroad`, `road`. |
| Lower-right, E3416 | No apparent road or railway crossing; location lettering overlays part of the side. | No explicit pair. |
| Lower-left, E3415 | Cross-tied railway crosses the diagonal river-bearing side near `(550, 800)`, consistent with the earlier incoming trace. | `minor-river`, `railroad`. |

Sparse absent pairs do not establish clear terrain or rule out other features. The three railway-marked sides are local observations, not proof of which branches connect through overlapping ink at the junction near `(790, 610)`. Stop there; do not infer a complete route, service, movement legality, or gameplay reachability. Nearby labels and lines outside these six sides were not systematically surveyed.

**Reproduction:**

```sh
pdftoppm -f 5 -l 5 -r 700 -x 6300 -y 10200 -W 1900 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-junction
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `852e8c72f591c3025e4eec13a7c51fe04e53d0207bf845144862f974b9550443`; [pinned sides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Renderer versions can change PNG bytes; crop stays outside Git. No runtime or map-data changes; no gameplay tests.

# Map D pinned rail: batch B source reading

**Date:** November 18, 2026. Four exact sides from the [Map D worklist](2026-11-18-graziani-map-d-rail-worklist.md), inspected against the original scan. This is a source reading, not an independent edition or a change to map data.

## Source and method

[Original five-sheet map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; PDF p. 4 is Map D, p. 1 the terrain key. Pin file `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Both hashes checked. The key distinguishes cross-tied blue continuous railroad from cross-tied blue broken-centerline unfinished railroad, and thick dark dashes for track. At default rotation, north is up. Reproduce the view from the verified PDF:

```sh
pdftoppm -f 4 -l 4 -r 300 -x 200 -y 4050 -W 1300 -H 850 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-b
pdftoppm -f 4 -l 4 -r 600 -x 480 -y 8800 -W 1800 -H 250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-b-detail
pdftoppm -f 1 -l 1 -r 300 -x 2300 -y 300 -W 2000 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-key
```

Coordinates below are approximate in the 300-dpi crop, top-left origin. The blue cross-tied symbol shows short interruptions in its centerline, consistent with the key's unfinished class; scan softness, ties and grid overprint qualify fine class discrimination. Crossings are well inside the vertical sides, not at their vertices.

| Exact pinned side | Rail evidence | Separate track evidence |
| --- | --- | --- |
| `D3602|D3603` | Blue cross-tied line crosses vertical side near `(284, 434)`; broken centerline supports unfinished rail. Grid and nearby printed number overlap the mark. | Thick dark dash crosses same side near `(284, 403)`, separate from blue line; source supports pinned `track`. |
| `D3603|D3604` | Blue cross-tied line crosses near `(472, 427)`; broken centerline supports unfinished rail. | Dark dashed track bends downward within D3603 and approaches the lower vertex near `(472, 493)`; this does not establish a crossing of the vertical side. No track is pinned here. |
| `D3604|D3605` | Blue cross-tied line crosses near `(660, 419)`; broken centerline supports unfinished rail. Grid and a blue tie coincide near the crossing. | No track feature pinned; this observation does not establish all other features. |
| `D3605|D3606` | Blue cross-tied line crosses near `(849, 425)`; broken centerline supports unfinished rail. Grid and tie overprint qualify the exact point. | No track feature pinned; this observation does not establish all other features. |

The four pinned rail features are source-positive with the stated scan and overprint qualification. Only the first side's separately pinned track is assessed as a crossing. Neighboring marks or vertex proximity do not assign a feature to another side. No data, runtime, or gameplay conclusions follow.

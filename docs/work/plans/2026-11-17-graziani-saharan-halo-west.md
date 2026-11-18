# Graziani: C0716 western halo interiors

Reviewed 17 November 2026. The [batch envelope](2026-11-17-graziani-playable-map-work-batch.md) less its 37-cell disk gives 24 geometric distance-four candidates; the western C03–C06 subset comprises **11**: C0314–C0318, C0413, C0418, C0513, C0519, C0612 and C0619. Inclusive radius and origin remain proposed interpretations, not legal placement or map-boundary rulings.

Compared [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and p. 1 terrain key with pinned [hex records](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json), source `vassal:CNAv2.1.0`. Key identifies yellow fill as desert. Source scan SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hex data SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. This is an informed same-scan visual reading, not an independent edition. Printed numbers in these cells match the canonical IDs after prefixing `C`.

| Canonical IDs (count) | Visible interior and occlusion | Pinned terrain; settlement; coast | Assessment |
| --- | --- | --- | --- |
| C0314–C0318 (5) | Yellow fill; no settlement symbol apparent. Grid and printed numbers cover small portions. | `desert`; null; false | Desert artwork supports terrain; no definite difference seen. |
| C0413, C0418 (2) | Yellow fill; no settlement symbol apparent. Grid and numbers cover small portions. | `desert`; null; false | Desert artwork supports terrain; no definite difference seen. |
| C0513 (1) | Yellow fill; no settlement symbol apparent. Grid and number cover small portions. | `desert`; null; false | Desert artwork supports terrain; no definite difference seen. |
| C0519 (1) | Yellow fill; no settlement symbol apparent. Number and grid cover small portions; blue checkered ink overlays portions of its upper-right and right boundary. | `desert`; null; false | Desert artwork supports terrain; boundary ink limits observation there. No definite interior difference seen. |
| C0612 (1) | Yellow fill and pale uncolored area both visible, separated by irregular artwork boundary; no settlement symbol apparent. Number and grid cover small portions. | `desert`; null; false | Desert artwork present; whole-cell assignment depends on unresolved artwork convention. |
| C0619 (1) | Yellow fill and pale uncolored area both visible; no settlement symbol apparent. Number and blue checkered ink cover portions near left and lower-left boundaries. | `desert`; null; false | Desert artwork present; whole-cell assignment depends on unresolved convention. Boundary ink limits observation. |

No distinct obstacle symbol or coast apparent in exposed interiors at this resolution. Numbers, grid, and checkered overprint prevent a categorical absence claim. The checkered mark is recorded as visible ink, with meaning and effects unresolved here; this interior review does not certify any side as feature-free or adjudicate obstacles, crossings, routes or legal movement. No settlement symbol apparent is a limited visual observation, not proof beneath overprint. No definite pinned difference identified; mixed-fill cells require assignment rules and clearer source review before correction or admission. This subset does not establish incoming force locations, ZOC, closure, or the rest of the halo.

Reproduce the broad and detail crops (coordinates in rendered page pixels):

```sh
pdftoppm -f 3 -l 3 -scale-to 5000 -x 350 -y 3200 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-halo-west
pdftoppm -f 3 -l 3 -scale-to 10000 -x 2000 -y 8600 -W 1300 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-halo-west-detail
pdftoppm -f 3 -l 3 -scale-to 10000 -x 3200 -y 8500 -W 1000 -H 750 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-halo-west-east
```

Reviewer: Codex. Disposition: retain observations as research; review whole-cell assignment and checkered boundary separately. No runtime or data changes; no gameplay tests.

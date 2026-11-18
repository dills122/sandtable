# Siwa C0127: six-side source and pin reading

**Date:** November 18, 2026. Original [Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, show two marked leads across C0127's upper sides. This local reading does not establish route connectivity, movement legality or a negative route result. It combines the earlier [source review](2026-11-17-graziani-siwa-route-source-review.md) with a fresh visual inspection of the same scan, not an independent edition.

The partly overprinted `0127`, adjoining labels and oasis artwork identify the bottom-row Siwa hex. Directions refer to the rendered sheet. The key depicts track as single dashes and unfinished road as paired dashes. Compare sparse VASSAL-derived [hexside pins](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json); missing pair entries do not mean clear sides.

| C0127 side | Visible source | Pinned pair |
| --- | --- | --- |
| Upper-left, C0226 | Single dashed track crosses and continues into oasis artwork, with a lead west through C0226. | `track` |
| Upper-right, C0227 | Paired dashes and separate single dashes cross close together; artwork obscures portions of their continuations. Paired lead goes upward, single lead curves right. Exact junction under ink remains uncertain. | `unfinished-road`, `track` |
| Left, C0126 | Shared side visible; no route crossing apparent. Desert-colored fill lies inside C0126. | No explicit pair |
| Right, C0128 | Shared side visible; no route crossing apparent. Salt-marsh pattern lies inside C0128. | No explicit pair |
| Lower-left, sheet margin | Boundary meets blank margin; no printed neighboring cell or apparent route crossing. | No pair to a printed neighbor |
| Lower-right, sheet margin | Boundary meets blank margin; no printed neighboring cell or apparent route crossing. | No pair to a printed neighbor |

[Hex pins](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) list C0127 as `clear` with `oasis`, C0126 as `desert`, and C0128 as `salt-marsh`. Visible artwork and local fill are consistent with those labels, but do not independently classify every side. No printed neighbor at a sheet margin does not prove physical impassability, absence of another sheet or off-map coverage, or a legal board limit. Check applicable rules and coverage separately. Neither lack of an apparent crossing nor a missing sparse pin proves absence of other terrain or off-road movement.

**Reproduction:** viewed the 600-dpi crop generated from the map PDF with:

```sh
pdftoppm -f 3 -l 3 -r 600 -x 9200 -y 19300 -W 2500 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-detail
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Source crop is renderer-dependent. No runtime or map-data changes; no gameplay tests.

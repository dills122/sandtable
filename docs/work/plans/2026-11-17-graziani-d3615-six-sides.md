# D3615: six-side original-map reading

**Finding:** Map D shows road and blue cross-tied railway crossing D3615–D3715, a road crossing D3615–D3616, and a blue cross-tied line apparently crossing D3615–D3516 near their crowded common vertex. The last observation supports an existing **candidate** mismatch with pinned railway placement, not an accepted correction. The D3515 side shows slope and a blue broken railway branch. Remaining sides contain exposed grid or rough artwork, with overprints limiting negative claims. This is not a legal-edge or network certification.

## Cell and six sides

The printed `3615` lies in the central brown-patterned cell in the 500-dpi crop. Neighbor labels `3715`, `3716`, `3616`, `3516`, `3515`, and `3614` establish the local geometry; some labels are crossed by terrain, route, or place-name ink. North is up in this rendering. D3615's approximate vertices clockwise from top are `(835,550)`, `(995,640)`, `(995,820)`, `(835,910)`, `(680,820)`, `(680,640)`. Coordinates refer to pixels in that crop. Brown rough artwork fills much of the cell; shoreline and route artwork crowd its eastern vicinity. Pinned `hexes.v1.json` gives `rough`, no settlement, `coastal: true`. This local reading does not establish water-side passability or all terrain effects.

| Neighbor and side | Visible marks and pinned comparison |
| --- | --- |
| D3715, upper-left diagonal | Dark parallel road and blue cross-tied line cross the side; pinned `road`, `railroad`. Brown fill, grid, and route marks overlap near the crossing. Prior [Fourth Indian check](2026-11-17-graziani-fourth-indian-edges-next.md) and its linked review support this bounded reading. |
| D3716, upper-right diagonal | Thin grid and pale/brown artwork visible along much of the side; yellow coastal artwork approaches its eastern endpoint. No identified road or rail crossing in exposed portions. No explicit pinned pair; endpoint overprint limits absence claims. |
| D3616, right vertical | Parallel road crosses above the lower vertex; coastline, label, and dark ink overlap portions. Pinned `road`, `railroad`: road supported; visible blue line appears instead to cross the D3516 diagonal. Railroad placement remains questionable, not categorically disproved. |
| D3516, lower-right diagonal | Blue cross-tied line appears to intersect near the upper-right vertex, with dark track/road and grid overprinted. Pinned `track` only. Candidate missing railway; crossing of track itself is not cleanly established by this local image. The adjacent D3516–D3616 track mark near the common vertex also remains unresolved. |
| D3515, lower-left diagonal | Brown slope band with splash toward D3615 and broken blue cross-tied branch intersect side. Pinned `slope`, `unfinished-railroad`, `up: D3515`; prior source check reads splash as down side under Land §8.35. Overprint limits detailed branch interpretation. |
| D3614, left vertical | Grid runs through brown rough artwork; irregular pale/brown markings and nearby blue branch obscure portions, especially toward the lower endpoint. No identified route crossing on exposed grid. No explicit pinned pair; lack of record does not prove a clear side. |

The [eastern junction candidate](2026-11-17-graziani-d3615-east-junction-next.md) and [same-scan recheck](2026-11-17-graziani-d3615-east-junction-review.md) support the apparent rail diagonal but retain uncertainty. This informed inspection used their conclusions and pinned data; it is neither blind nor another edition. Neighbor-side crossings, coastal classifications, unfinished-rail status and route continuity require separate evidence. Fixed D3615 appears in the [Commonwealth setup inventory](2026-11-17-graziani-fixed-contact-inventory.md); this map reading does not establish actual stack, occupancy, ZOC, movement, or setup legality.

## Source and reproducibility

Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 Map D and p. 1 key; `/private/tmp/sandtable-maps.pdf` SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Key distinguishes dark parallel road, blue cross-tied railroad, broken blue cross-tied unfinished railroad, and brown splash-edged slope. Reviewed existing source crops:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 3900 -y 6750 -W 1500 -H 1350 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/indian500
pdftoppm -f 4 -l 4 -r 800 -x 7500 -y 11800 -W 1300 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/d3615east800
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

PNG SHA-256 in command order: `fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191`, `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`, `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`. PNG bytes may vary by renderer; PDF hash and crop coordinates identify source. Crops remain outside Git pending rights. Pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Pinned data attributes source to `vassal:CNAv2.1.0`.

No runtime or map-data changes; no gameplay tests. Any later data correction needs independent admission, versioning, and replay-impact checks. Visible markings do not establish legal movement or operational rail.

# C3419: six-side source check

**Finding:** Original Map C supports five dashed track approaches/crossings at the C3419 junction. The C3418/C3419 side also visibly bears blue checkered blocks; the map key's gray `Border` symbol does not itself explain their appearance. The C3320 side is overprinted by the place name. None of these observations establishes a legal edge, a complete absence of hidden features or the effect of the checkered mark.

## Six sides and pinned comparison

The scan's printed `3419` is partly crossed by dark route ink, but the grid, nearby numbered cells and Bir el Sheferzen dot support the C3419 cell. Directions below are **page-image directions**, not compass bearings. The keyed track symbol is a single dark dashed line, distinct from paired unfinished-road marks. The source-side observations use a 600-dpi crop with top-left `(6600,8700)`, dimensions `1700×1450` (pixels in full rendered Map C); positions are approximate within that crop.

| Neighbor | Side in crop | Original visible evidence | Pinned `hexsides.v1.json` |
| --- | --- | --- | --- |
| C3418 | Left vertical, about `(395,795)`–`(395,1020)` | Blue checkered blocks follow the side. A dark dashed route approaches from both interiors near `(395,860)`; its exact meeting is obscured by the blocks and overprinted `3419`, but alignment supports a track crossing. | `track`; no separate checkered/border feature recorded. |
| C3519 | Upper-left diagonal, about `(395,795)`–`(580,690)` | Dark track dash straddles the diagonal around `(520,720)`, aligned with dashes in both hexes. | `track`. |
| C3520 | Upper-right diagonal, about `(580,690)`–`(770,795)` | Dark dash crosses near `(690,755)`, with aligned dashes in the adjoining interiors. | `track`. |
| C3420 | Right vertical, about `(770,795)`–`(770,1020)` | Dark horizontal dash crosses near `(770,870)` and continues into both interiors, with `3420` text partly overprinted. | `track`. |
| C3320 | Lower-right diagonal, about `(580,1125)`–`(770,1020)` | `Bir el Sheferzen` text covers part of the side and southwest vertex; exposed side looks like a thin grid stroke. No track dash is visible crossing the exposed portion. | No literal pair record found; absence is not proof of an empty side. |
| C3319 | Lower-left diagonal, about `(395,1020)`–`(580,1125)` | Dark track dash straddles the diagonal around `(500,1080)` and aligns with the junction and continuation into C3319. | `track`. |

Blue blocks also continue beyond C3419's left side to adjoining edges; do not attribute that continuation to the C3319/C3419 diagonal solely from a shared vertex. The key separately depicts escarpment as a thick dark band with an irregular down-side edge. No such band is apparent in the exposed side portions, but track dashes, blue blocks and lettering hide parts of several edges and endpoints. No complete absence claim follows. For C3320 and C3520, see the independent [Sheferzen-side](2026-11-17-graziani-map-sheferzen-initial-contact-review.md) and [RTR-side](2026-11-17-graziani-map-first-rtr-side-review.md) checks; the [border analysis](2026-11-17-graziani-map-sheferzen-border.md) treats the country interpretation separately.

## Provenance

Visually inspected [original Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Reproduce crop: `pdftoppm -f 3 -l 3 -r 600 -x 6600 -y 8700 -W 1700 -H 1450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/south-terrain-sheferzen`. The rendered PNG SHA-256 is `9bf51282e635228ae349dcccda84906cacf781bb9e3d44177a265af1ea8855e5`; renderer bytes can vary. Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; compared literal pair entries, not runtime behavior or an independent map edition. Source obscuration, other rules and any later corrections remain open. No runtime changes or tests.

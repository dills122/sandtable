# C3020: six-side source check

**Finding:** Original Map C shows three track crossings around C3020, matching the three literal pinned `track` entries. Its C3019 side has blue checkered blocks without a literal pinned pair. The other two sides have exposed thin grid strokes, but nearby name and route ink prevent treating the complete sides as independently certified empty. None of this establishes legal movement, a border rule or a ZOC.

## Six sides and pinned comparison

C3020's printed number is legible; its blue `Bir el esc Scegga` dot is east of the distinct Fort Maddelenna dot in C3019. The table uses **page-image** directions. Original Map C PDF p. 3 was rendered at 600 dpi with crop `(6400,10200,2200,1800)` in full-page pixels. Coordinate positions below are approximate within that crop. Map PDF p. 1 key depicts a single dark dashed **track** and a gray `Border` graphic; the visible blue checks do not directly reproduce the latter.

| Neighbor | Side in crop | Source observation | Pinned `hexsides.v1.json` |
| --- | --- | --- | --- |
| C3019 | Left vertical, about `(975,585)`–`(975,800)` | Blue checkered blocks follow the side; no dark track crossing visible there. Blocks hide underlying grid. | No literal pair entry found. |
| C3120 | Upper-left diagonal, about `(975,585)`–`(1165,480)` | Dark dashed route approaches from both hex interiors, crossing near the upper-right part of the diagonal; the dash overlaps the grid. | `track`. |
| C3121 | Upper-right diagonal, about `(1165,480)`–`(1350,585)` | Exposed part is a thin grid line; nearby `Bir el esc Scegga` text and the vertex/route marks obscure details. No track crossing clearly seen. | No literal pair entry found. |
| C3021 | Right vertical, about `(1350,585)`–`(1350,800)` | A horizontal dark dash crosses near `(1350,640)`, aligned with the route from the bir dot into C3021. | `track`. |
| C2921 | Lower-right diagonal, about `(1165,900)`–`(1350,800)` | A dark dashed route from the bir dot straddles the diagonal near `(1300,825)` and continues into C2921. | `track`. |
| C2920 | Lower-left diagonal, about `(975,800)`–`(1165,900)` | Exposed side appears a thin grid stroke, without a visible dark dash. The checkered line on C3019/C2920 meets its western vertex; colored ink there obscures the junction. | No literal pair entry found. |

The regional country-label evidence and the key-symbol mismatch are discussed in the [Fort boundary check](2026-11-17-graziani-map-fort-maddalena-contact-review.md) and [boundary review](2026-11-17-graziani-border-mark-review.md). A checkered mark on one side cannot be extended to a neighboring side solely because both meet at a vertex. Absence of a literal pinned entry does not prove absence on the source; no broad feature-free edge or track-network continuity follows from these local observations.

## Provenance

Visually checked [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Reproduce: `pdftoppm -f 3 -l 3 -r 600 -x 6400 -y 10200 -W 2200 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3020-sides`; crop SHA-256 `d57a3c2457fd725bf1ef7c6ec615fb2b332ca07063e069dbd2e6e10d8b827664`. Renderer bytes can vary. Compared literal pairs in `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same source scan, not a new edition, complete edge audit or runtime test. No runtime changes.

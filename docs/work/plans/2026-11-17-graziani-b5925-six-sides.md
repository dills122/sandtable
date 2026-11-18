# B5925: six-side visual source audit

**Finding:** Original Map B shows Derna at `B5925`, with a road crossing the `B5924` and `B5825` sides, and a brown slope band along the `B5824` side. These match three explicit pinned pair records. The coast, wadi, road and name overlap near the western side and vertices; this is not a complete edge or legality certificate.

## Original map observations

Visually inspected [original map and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 2 and 1. On the 600-dpi crop below, approximate `B5925` vertices clockwise from top are `(1252,729)`, `(1442,835)`, `(1442,1055)`, `(1253,1161)`, `(1067,1055)`, `(1067,835)`. Directions refer to displayed crop. Map A's key distinguishes road by parallel dark lines, wadi by a green band, slope by a brown band with light splash, sea by blue fill, port by an anchor, and village by a dot. The Derna hex contains a dot, port symbol, coastal blue, and brown land; its pinned hex record says `rough`, `village`, `coastal: true`.

| Neighbor | Shared side | Visible source and pinned comparison |
| --- | --- | --- |
| `B6024` | upper-left diagonal | Thin grid through blue sea; no other feature identified on exposed side. No explicit pinned pair. |
| `B6025` | upper-right diagonal | Thin grid through blue sea; anchor lies inside Derna hex, near but not on this side. No explicit pinned pair. |
| `B5926` | right vertical | Blue shoreline crosses near lower end; grid visible elsewhere. No explicit pinned pair. |
| `B5825` | lower-right diagonal | Parallel road lines cross; land and sea meet near eastern vertex. Pinned pair lists `road`. |
| `B5824` | lower-left diagonal | Brown band and pale splash lie along the shared side, primarily on the `B5925` side of the grid. Pinned pair lists `slope`, `up: B5824`; the artwork alone does not validate the meaning or direction of `up`. Road, wadi and label crowd the western vertex, so their exact relationships there are not settled by this reading. |
| `B5924` | left vertical | Road crosses lower portion; green wadi approaches/crosses the same side near its lower end, and shoreline crosses higher on it. Pinned pair lists `wadi` and `road`. Name ink and converging lines obscure part of the crossing. |

Absence of a pinned entry does not establish feature-free coverage. Shoreline, name, symbol, road, wadi, terrain and grid overprint obscure pixels, especially at the western and southern vertices. Do not infer movement, supply, ZOC, setup or route legality from this local visual comparison; slope direction is stated as pinned data rather than a fresh rules determination.

## Reproduction and provenance

```sh
pdftoppm -f 2 -l 2 -r 600 -x 8100 -y 600 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-b5925/derna600
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-b5925/fullkey
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Derna crop SHA-256 `28e200e8e822b4b998f1b29032c876ab4d29cd498f3f0e4c02493f0e5005e1a5` (renderer-dependent); pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Same scan as existing work, not an independent edition or rights grant. An [independent same-scan review](2026-11-17-graziani-b5925-six-sides-review.md) prompted the correction to slope artwork placement. No runtime edits or tests.

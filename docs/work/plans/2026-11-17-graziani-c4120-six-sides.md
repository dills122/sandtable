# C4120: six-side visual source audit

**Finding:** Map C prints `4120` inside a clear-looking hex. Three of its six neighboring pairs have feature records in pinned side data; the other three have no records. The scan supports paired-road and escarpment marks on the recorded sides, subject to overprint. This visual comparison does not decide movement or slope direction.

## Original map and pinned data

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1. In the 300 dpi crop reproduced by the command below, C4120's approximate vertices are `(594,416)`, `(688,365)`, `(783,416)`, `(783,526)`, `(688,576)`, `(594,526)`. Directions refer to the displayed crop, not compass directions. Neighbor labels are read from visible numbering and grid sequence where ink obscures them. Pinned entries are literal records in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), sourced from `vassal:CNAv2.1.0`.

| Neighbor | Shared side | Visible marks and pinned comparison |
| --- | --- | --- |
| `C4219` | upper-left diagonal | Blue Sidi Azeiz lettering overlaps part of side; no route or terrain side mark discernible. No pinned pair record. |
| `C4220` | upper-right diagonal | Paired dark road lines cross side. Pinned: `road`. Neighbor number partly overlapped by road. |
| `C4121` | right vertical | Dark escarpment ink adjoins and obscures side. Pinned: `escarpment`, `up: C4120`; up direction is not independently established. Neighbor label is obscured. |
| `C4020` | lower-right diagonal | Paired road lines cross near upper/right vertex. Pinned: `road`. Road and escarpment ink meet near vertex. |
| `C4019` | lower-left diagonal | No crossing discernible. Dashed track and blue crossed unfinished-railroad line run nearby outside C4120, close to left vertex; overlap and proximity limit certainty. No pinned pair record. |
| `C4119` | left vertical | No crossing discernible. Track and blue crossed line approach lower vertex from neighboring hexes but appear outside C4120; vertex proximity limits certainty. No pinned pair record. |

Key depicts paired-line road, dashed track, blue crossed unfinished railroad, dark escarpment, and a gray `Border` sample. Blue checks along the regional Libya–Egypt boundary appear farther right and south, not on any of these six sides; their geographic interpretation is distinct from the key's gray sample. Labels, route strokes, and dark terrain overprint can conceal coincident features; no visible mark does not prove absence beneath ink. No-record means no literal pair entry, not an independently certified open side. Pinned [hexes.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) gives C4120 `clear`, no settlement, and `coastal: false`; this audit does not certify full terrain classification.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3000 -y 3200 -W 1100 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4120/c4120-300
pdftoppm -f 3 -l 3 -r 600 -x 7000 -y 7000 -W 1000 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4120/detail-600
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 180 -W 1400 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4120/key-200
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; 300 dpi crop `51464a9b19a75df93ec54dc67cf8208d7dc64b9161369eee5c09913b79c0a06d`; 600 dpi detail `cc5c78cf03ca0189ac91ac88aa3b6ec2ba031598eecc006a384dd9acdab55009`; key crop `a6f4d7ff641f39742b5795ebcf0348377785092a28034fd721063e41e07abf0e` (crop hashes renderer-dependent). Pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Same scanned source edition; no independent sheet, runtime changes, or tests.

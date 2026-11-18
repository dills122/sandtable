# Map D D3609: six-side rail source survey

**Date:** November 18, 2026. Inspected all six printed sides of D3609 for blue cross-tied rail and compared literal pins. Both observed crossings are pinned; **no additional rail crossing identified in exposed portions of the other four sides**. This is a bounded local search, not a clear-edge certification, whole-sheet inventory, route or operational finding. Coordinates below are approximate in the unrotated 600-dpi crop; vertices and linework are broad, not survey points.

| Literal sorted pair | Exposed-side observation | Literal pinned features |
| --- | --- | --- |
| `D3608|D3609` | Blue cross-tied line crosses the left vertical side around `(330, 755)`, well between vertices `(330, 665)` and `(330, 880)`. Blue ties, numeral and grid overlap. Nearby centerline segments show repeated gaps consistent with unfinished symbol. | `unfinished-railroad` |
| `D3609|D3709` | Brown band follows upper-left diagonal `(330, 665)`–`(515, 560)`. No blue rail crossing identified in exposed portions; brown ink and grid qualify endpoints. | `slope`, `up: D3609` |
| `D3609|D3710` | Blue cross-tied line bends across upper-right diagonal `(515, 560)`–`(705, 665)` around `(640, 630)`, closer to the D3610 vertex but between endpoints. Repeated gaps along the bend are consistent with unfinished symbol; grid and a tie crowd the crossing. | `unfinished-railroad` |
| `D3609|D3610` | No blue centerline crossing identified on right vertical side `(705, 665)`–`(705, 880)`. Rail passes above its upper vertex on the D3710 diagonal; a nearby blue tie and grid qualify the vertex. | No literal pair found. |
| `D3509|D3609` | No blue rail crossing identified along lower-left diagonal `(330, 880)`–`(515, 988)`. | No literal pair found. |
| `D3510|D3609` | No blue rail crossing identified along lower-right diagonal `(515, 988)`–`(705, 880)`. | No literal pair found. |

The [batch C class check](2026-11-18-graziani-map-d-rail-batch-c-class-check.md) separately finds both pinned unfinished symbols source-positive with scan and overprint qualification. This survey agrees at generic rail and local interrupted-line level, but same scan is not independent edition evidence. Brown band is a separate observation; the pinned slope direction is reported, not reassessed. Dark paired road above D3609 is distinct from blue rail; neighboring marks and tie ends do not automatically cross another side. No record does not prove absent ink or other featureless terrain. An informed second review remains needed.

## Reproduction

```sh
pdftoppm -f 4 -l 4 -r 600 -x 2900 -y 8200 -W 1500 -H 1450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-3609-source
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/mapd-3609-source.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

[Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1: PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `b8318d071dc1ca62b0af2386da11a94c46bc4eef64d886e360b53fb791edcc25`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Embedded map image is 300 ppi; 600-dpi output enlarges existing scan pixels. PNG bytes vary with renderer. Documentation only; no data/runtime edits or gameplay tests.

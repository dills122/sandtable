# Graziani C4507: six-side source comparison

**Finding:** Original Map C shows El Adem at the right edge of C4507, paired dashed route ink and broad brown bands on its four diagonal sides. Pinned records cover all six sides. The map's pale interior does not visibly resemble the key's rock/gravel pattern, despite the pinned `gravel` value; dot, route and text overprint make exact side attribution uncertain. This is a local source comparison, not a movement, supply, ZOC or contact finding.

## Identity and six sides

Inspected [original map and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. Scenario §60.31 lists 4 CCNN at C4507; see [fixed setup inventory](2026-11-17-graziani-fixed-contact-inventory.md). In the 500-dpi crop below, the `4507` label lies in the central pale hex. Its approximate vertices clockwise from top are `(570,850)`, `(727,940)`, `(727,1125)`, `(570,1205)`, `(414,1125)`, `(414,940)`. Neighbor labels identify the six cells. Directions refer to this unrotated crop, not compass bearings.

The key shows village/bir as a blue dot, rock/gravel as circles and stipple, unfinished road as paired broken dark lines, track as a single broken dark line, and ridge as a brown band with splash on both sides. A blue dot at El Adem lies very close to and partly overlaps the C4507/C4508 side; lettering and route marks overlap it. Pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) says `terrain: gravel`, `settlement: village`, `coastal: false`. The dot supports a village/bir mark near this boundary, but its exact cell assignment is less clear from its position alone. No obvious rock/gravel circle pattern is visible in the exposed pale interior; scan quality and overprint limit that negative observation. No sea appears in this hex.

| Neighbor | Visible shared side | Pinned pair |
| --- | --- | --- |
| `C4606` | Upper-left diagonal: broad brown band with splash on both sides, consistent with ridge. | `ridge` |
| `C4607` | Upper-right diagonal: broad brown ridge-like band; paired broken dark route crosses it. | `ridge`, `unfinished-road` |
| `C4508` | Right vertical: paired broken dark route crosses near El Adem dot. Dot and name overlap linework. | `unfinished-road` |
| `C4407` | Lower-right diagonal: broad brown ridge-like band. A thick broken track approaches and crosses or nearly crosses the side amid a gap between dashes; exact crossing attribution is uncertain. | `ridge` |
| `C4406` | Lower-left diagonal: broad brown ridge-like band; track and adjacent ink converge near its right endpoint. | `ridge` |
| `C4506` | Left vertical: paired broken dark route crosses; a thicker single dashed line approaches the upper part of the side amid the band and junction. Exact assignment of the track crossing is uncertain. | `track`, `unfinished-road` |

Each listed pinned side has `side: null` and `up: null`. An [independent same-scan review](2026-11-17-graziani-c4507-six-sides-review.md) corrected the crop coordinates and rechecked the side readings; the original note had mistakenly given vertices in the C4606/C4607 area. The bands and paired route marks support the broad comparison; the crowded junction, boundary dot, scan and similar ink do not certify every crossing or feature. No route continuity or rules effect is established.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1500 -y 4000 -W 1700 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4507-six
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 160 -W 1500 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4507-key
pdftoppm -f 1 -l 1 -r 70 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4507-full-key
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `6353b79e770cd823808cd5243ff0f4502128cf78c0a91ff29294a7631a5303de`; key crop `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`; full key page `337402ef1f89db6a76a5ffdd79f04240c560e4f2c5e2cf2e81727f948b57d508`. PNG bytes may vary by renderer. Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; both declare `vassal:CNAv2.1.0` as source. Crops remain outside Git. This informed same-scan reading is not independent edition verification. No runtime changes or gameplay tests.

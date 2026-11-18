# Graziani D3612: six-side source comparison

Original Map D shows Charing Cross in D3612, with a blue village dot and overlapping road, railroad and lettering. This visual transcription does not certify any legal movement, supply, ZOC or contact edge. Scenario §60.41 places 7th Armored Division at D3612 with its listed division units above detached; this does not settle all constituents or co-locations. See [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), printed p. 27 / PDF p. 3.

## Source and six sides

Inspected [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 / printed Map D, and terrain key on PDF p. 1. Directions below describe the clockwise-rotated crop, not compass bearings. D3612 is pale with blue dot at left, blue Charing Cross lettering on right, and heavily overprinted black and blue routes. Approximate crop vertices clockwise from upper left: `(230,355)`, `(301,355)`, `(337,417)`, `(301,479)`, `(230,479)`, `(194,417)`; these are approximate pixels in the 560 × 700 crop.

| Neighbor | Shared side and visible marks | Pinned pair |
| --- | --- | --- |
| D3611 | Top horizontal: paired black road and blue crossbar railroad strokes cross; label and strokes overlap. | `road`, `unfinished-railroad`; `up: null` |
| D3712 | Upper-right diagonal: blue text overlaps part of side; no route crossing apparent. | No pair found. |
| D3713 | Lower-right diagonal: brown feathered slope-like band and blue text overlap side. | `slope`; `up: D3612` |
| D3613 | Bottom horizontal: paired black road and blue crossbar railroad strokes cross. | `road`, `unfinished-railroad`; `up: null` |
| D3513 | Lower-left diagonal: village dot and brown markings crowd its upper vertex; no route crossing apparent along rest of side. | No pair found. |
| D3512 | Upper-left diagonal: paired black dashed strokes cross brown feathered slope-like band toward village dot. | `slope`, `unfinished-road`; `up: D3512` |

Map A's key distinguishes clear, Village/Bir., Road (paired solid black lines), Unfinished Road (paired dashed black lines), Railroad (blue line and crossbars), Unfinished Railroad (interrupted blue line and crossbars), and Slope (one-sided feathered brown band). D3612's pale interior and dot agree with pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json): `terrain: clear`, `settlement: village`, `coastal: false`. Paired dashed strokes match unfinished road; paired solid strokes match road. Blue crossbars are visible at both horizontal sides, but overlap and scan resolution prevent an independent confident finished-versus-unfinished reading; pinned v1 calls them unfinished railroad. [Land §8.35](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), p. 14, puts slope splash on the down side. The upper-left splash extends into D3612, supporting D3512 up as pinned; the lower-right side is too overprinted to certify its direction. This applies the printed convention, not independent elevation evidence. Absence of a pinned pair or a visible mark does not establish a clear side. All four pinned records have `side: null`. Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) and hexes both declare `vassal:CNAv2.1.0` as source; same-edition visual inspection is not independent edition verification. An [informed second reading](2026-11-17-graziani-d3612-six-sides-review.md) used the same scan, not a separate edition.

## Reproduction

```sh
pdftoppm -f 4 -l 4 -r 200 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/d3612/map-d-200
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/d3612/map-d-200.png"); im.rotate(-90,expand=True).crop((3800,1100,4360,1800)).save("/private/tmp/d3612/d3612-200.png")'
```

Crop coordinates are clockwise-rotated page pixels, left/top inclusive and right/bottom exclusive. Source PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `9adbb89777773a8e9bf377014b394bddd4002502295d1443c5bca32d7cd71a3e`. PNG bytes depend on renderer and Pillow versions. Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crops remain outside Git. No runtime changes or gameplay tests.

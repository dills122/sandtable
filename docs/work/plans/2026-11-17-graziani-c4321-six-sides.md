# Bardia C4321: six-side source check

**Finding:** Original Map C depicts Bardia as a coastal major-city/port hex with a road crossing toward C4320 and a brown slope-pattern side toward C4220. Two neighboring cells are sea; two others contain both land and sea, with coastline crossing their shared sides. Axis figures, city blocks, lettering and coastal symbols obscure parts of the hex and its sides. These observations do not certify every feature or establish land entry, ZOC or setup legality.

**Direction follow-up:** A later [slope inspection](2026-11-17-graziani-bardia-slope-direction.md)
and [independent review](2026-11-17-graziani-bardia-slope-direction-review.md)
support C4220 up/C4321 down on the visible part of this side. C4321's obscured
label is inferred from the surrounding grid and part of the slope is overprinted.

## Reproduction and source identity

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`: PDF p. 3 Map C, p. 1 terrain key. The [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, §60.31 lists C4321 as a fixed Axis setup location; the map's AXIS artwork is not a substitute for counter and setup transcription.

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5700 -y 4700 -W 1800 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4321-detail
```

PNG SHA-256 `c6a59d5713be4147eeeada0b3d5751c7fe8b406040011752fd5646fd155074c6`; renderer versions may change bytes. Scan and crop remain outside Git. Coordinates below refer to this crop, in its displayed orientation; they are not compass directions. Labels on adjacent cells and the printed Bardia location establish the grid, though the central number is covered. This is a targeted same-scan reading, not an independent edition or blind transcription.

## Six shared sides

C4321's approximate vertices clockwise are `(610,785)`, `(770,700)`, `(925,785)`, `(925,970)`, `(770,1055)`, `(610,970)`. Neighbors follow the printed grid. Coastline means the blue land/sea boundary crosses a side, not that the entire hexside is a coast barrier.

| Neighbor | Crop side | Visible evidence and limits |
|---|---|---|
| C4420 | upper-left diagonal | Coastline crosses near `(700,740)`: land on the left, sea on the right. No continuous escarpment band is discernible; city/shore detail crowds the endpoint. |
| C4421 | upper-right diagonal | Blue sea along the side; port symbol lies close inside C4321. No road or terrain band discernible. |
| C4322 | right vertical | Blue sea; large Bardia lettering crosses and obscures part of the side. No complete feature-free conclusion. |
| C4221 | lower-right diagonal | Coastline crosses near `(880,995)`; sea toward C4322 and land toward C4221. Axis figures and lettering approach this side; inspect overprint before treating it as complete. |
| C4220 | lower-left diagonal | Broad brown splash-edged band follows the side, visually matching the key's slope pattern; city and AXIS overprint cover portions. The key distinguishes brown slope/ridge from dark gray escarpment; direction of the up side needs independent confirmation. |
| C4320 | left vertical | Two parallel dark lines cross near `(610,840–885)`, matching the key's road. City blocks and Axis figure obscure the lower part and nearby junction. |

The p. 1 key depicts coast as a blue shoreline, port as an anchor in a circle, major city as blue building blocks, road as paired continuous lines, slope as a brown splash-edged band, and escarpment as a darker gray band. Bardia shows city blocks, a port anchor, shoreline and AXIS artwork; the latter is not itself a terrain-key classification. The crop also shows nearby terrain and tracks, but this check makes no claim about their full routes.

## Comparison and boundary

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`, records `C4220|C4321` as `slope`, `up: C4220`, and `C4320|C4321` as `road`, `up: null`. No other record contains C4321. This is an absence of records, not evidence of feature-free sides or a decision that coastline is represented there. Pinned `hexes.v1.json`, SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`, describes C4321 as `major-city`, settlement `major-city`, `coastal: true`. Both files cite `vassal:CNAv2.1.0`; they are comparisons, not the original map.

[Independent same-scan review](2026-11-17-graziani-c4321-six-sides-review.md) confirmed the side observations and corrected the neighboring-cell description. Further review should trace both obscured sides and the slope's up direction against original key and rules. Partial sea, port status and road marks alone do not decide whether a unit may enter, project ZOC, move, or attack across a side. No runtime or content changes were made.

# Graziani map evidence: Cairo E1830

Source: `/private/tmp/sandtable-maps.pdf`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF page 5 is Map E; page 1 has the terrain key and location summary. Directions follow the displayed page, with Cairo upright. They denote geometric sides, not legal moves. Grid, artwork, and black figures overlap; obscured details remain uncertain.

## Six-side comparison

E1830 bears blue city blocks and black soldier figures. Its label is obscured; neighboring labels and the grid identify its position. Pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records `major-city` terrain and settlement, `coastal: false`. The following comparison uses literal pairs in [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json).

| Side | Neighbor | Visible evidence | Pinned features |
| --- | --- | --- | --- |
| NW | E1930 | Blue river follows much of the diagonal boundary; city blocks and figures crowd it. | `minor-river` |
| NE | E1931 | City artwork at diagonal boundary; figures and lines crowd its northern corner. No distinct route crossing confidently identified. | No pair record |
| E | E1831 | Two dark parallel road lines cross the vertical side, through figures and city artwork. | `road` |
| SE | E1731 | City blocks approach diagonal side; no distinct route crossing confidently identified. | No pair record |
| SW | E1730 | City and blue river artwork approach the diagonal, especially its western corner; exact river relation to the side is crowded. | No pair record |
| W | E1829 | Two parallel dark road lines cross the vertical side amid blue river and black figures. River artwork also follows the boundary. | `minor-river` |

**Candidate discrepancy:** E1829–E1830's pinned record lacks `road`, while the original scan shows a pair of dark parallel lines continuing across this shared side from E1829 into E1830, matching the key's road symbol. Figures cover parts of both lines, but the continuation is visible on either side of the boundary. This is a visual source comparison, not a patch or determination of movement legality. Missing pair records likewise do not prove absence beneath artwork.

## Printed key and summary

The Terrain Key names blue blocks `Major City`, two parallel dark lines `Road`, blue cross-tied line `Railroad`, blue side symbols `Major River` and `Minor River`, and soldier silhouettes `Training Area`. Black soldier artwork is a printed training-area overlay, not proof of unit occupancy. Nearby railroad and river artwork is dense; this check does not classify every local crossing beyond the table.

The page 1 `SUMMARY OF IMPORTANT LOCATIONS` lists Cairo `E1730/1829/1830/1930/1931`, with X under Major City and Victory Condition and dashes under Port and Assigned Garrison. This location-level entry does not establish occupancy or movement legality.

## Reproducible crops

Poppler coordinates start at the upper-left of the rotated rendered page, in pixels at the stated DPI. Hashes identify PNG bytes from this environment; renderer versions can differ.

```sh
mkdir -p /private/tmp/e1830
pdftoppm -f 5 -l 5 -r 150 -x 2440 -y 3330 -W 820 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1830/cairo
pdftoppm -f 5 -l 5 -r 300 -x 5320 -y 7110 -W 930 -H 740 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1830/detail
pdftoppm -f 1 -l 1 -r 150 -x 1160 -y 150 -W 1020 -H 850 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1830/key
pdftoppm -f 1 -l 1 -r 200 -x 900 -y 2550 -W 1400 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1830/locations
```

| Crop | SHA-256 |
| --- | --- |
| `cairo.png` | `bf5cac3b95af18ee39f20bbb6b45cb8dba5f2642e0858bf40d617f05598f2ca5` |
| `detail.png` | `7f2cf5ef1d2790ab81673b39979c15494a770cb85d2454d92fe37a884fc33467` |
| `key.png` | `db053e3addbb612b9c9a531c108b188529586f4ff9f336ffa2a844fafaa012e1` |
| `locations.png` | `658341e7bda567cbf6bc3c89e1bad0bfbf6b2f2f9fce9426ffb560bd921f5287` |

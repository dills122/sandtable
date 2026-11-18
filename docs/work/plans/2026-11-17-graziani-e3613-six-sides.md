# Graziani map evidence: Alexandria E3613

Source: `/private/tmp/sandtable-maps.pdf`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF page 5 is Map E; PDF page 1 contains the terrain key and location summary. Directions below refer to the displayed PDF page 5, with Alexandria text upright. They identify geometric sides, not legal moves. The printed grid and symbols overlap in places; absence of a visible symbol is not proof of a rule or movement permission.

## E3613 and its six sides

E3613 is the coastal Alexandria hex bearing a blue major-city pattern, blue port/anchor symbol, road lines, and black soldier figures with `CMNWLTH` overprint. The hex label itself is partly overprinted but its grid position is identifiable. City/port symbols and lines overlap coast, hex boundaries, and overprint; the scan does not warrant treating every obscured line as independently resolved.

| Side on displayed page | Printed neighbor | Visible evidence at shared side |
| --- | --- | --- |
| NW | E3713 | Blue sea on neighbor side; coast/city artwork approaches the boundary. No distinct route crossing identified. |
| NE | E3714 | Coastal city strip and black road lines cross the shared side; figures obscure parts. |
| E | E3614 | Shared vertical edge passes near road junction and coastal water; dense city and label overprint obscure exact local detail. |
| SE | E3514 | Blue coastal/port water at shared diagonal edge. No distinct route crossing identified. |
| SW | E3513 | Black road lines cross shared diagonal near its western end; coastal artwork and overprint crowd the crossing. |
| W | E3612 | Blue sea meets the shared vertical edge; coast/road artwork converges near its southern corner. No distinct mid-edge crossing identified. |

A cyan railroad symbol is visible through the Alexandria region; its precise relation to crowded E3613 side junctions is not independently resolved here. Lack of a distinct crossing above is a visual observation, not a claim that none exists under the overprint.

## Comparison with pinned data

The pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) sets E3613 to `terrain: major-city`, `settlement: major-city`, and `coastal: true`, consistent with visible city and coast artwork. The pinned [side records](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) contain:

| Side | Pinned pair | Pinned features | Source comparison |
| --- | --- | --- | --- |
| NW | E3613 / E3713 | No literal pair record | Sea visible; obscured features cannot be excluded. |
| NE | E3613 / E3714 | `railroad`, `road` | Road visible; railroad crossing unresolved. |
| E | E3613 / E3614 | No literal pair record | Crowded junction prevents a complete reading. |
| SE | E3514 / E3613 | `coast` | Blue coastal water visible. |
| SW | E3513 / E3613 | `railroad`, `road` | Road visible; railroad crossing unresolved. |
| W | E3612 / E3613 | No literal pair record | Sea visible; obscured features cannot be excluded. |

Missing pair records are file observations, not proof that the source lacks features or that movement is legal. See the [independent source review](2026-11-17-graziani-e3613-six-sides-review.md), which checks the same scan and records the same railroad uncertainty.

## Printed key, location summary, and overlays

The original Terrain Key names blue hex fill `Sea`, mixed blue/land `Coast`, blue city blocks `Major City`, anchor `Port`, double parallel dark lines `Road`, cyan line with cross-ties `Railroad`, and soldier silhouettes `Training Area`. The `CMNWLTH` soldier artwork is a printed training-area overlay, not proof of a unit occupying the hex. Map E also prints an `Alexandria Holding Box` with separate entries `Hex 3613` and `Hex 3714`; the box is an off-map printed aid, not an occupancy statement.

Page 1's `SUMMARY OF IMPORTANT LOCATIONS` lists Alexandria as `E3613/3714`, with X under Major City, Port, and Victory Condition, and a dash under Assigned Garrison. This is a location-level summary: it does not by itself assign each listed feature separately to both individual hexes, establish simultaneous occupation, or establish movement legality.

## Reproducible visual crops

Commands use Poppler's page rotation as rendered, pixel coordinates from the upper-left at the stated DPI, and one-based PDF page numbers. SHA-256 values below identify the generated PNG bytes in this environment; renderer versions may produce different bytes.

```sh
pdftoppm -f 5 -l 5 -r 150 -png -x 980 -y 1960 -W 850 -H 740 /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e3613/alex
pdftoppm -f 5 -l 5 -r 300 -png -x 2250 -y 4290 -W 650 -H 650 /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e3613/detail
pdftoppm -f 1 -l 1 -r 150 -png -x 1160 -y 150 -W 1020 -H 850 /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e3613/key
pdftoppm -f 1 -l 1 -r 200 -png -x 900 -y 2550 -W 1400 -H 1100 /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e3613/locations
```

| Crop | SHA-256 |
| --- | --- |
| `alex-5.png` | `f015cd5713811216de3d83ed1e0f0a554c7d735e711f11ea48fe91dcf0d2eaf6` |
| `detail-5.png` | `5e33fb3e0e822f80ac13e4ad2c7b50f9dcf0f23f340b97062eb20a9820d888ce` |
| `key-1.png` | `db053e3addbb612b9c9a531c108b188529586f4ff9f336ffa2a844fafaa012e1` |
| `locations-1.png` | `658341e7bda567cbf6bc3c89e1bad0bfbf6b2f2f9fce9426ffb560bd921f5287` |

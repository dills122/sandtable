# Graziani E1730: six-side source check

**Disposition:** Map E depicts Cairo as a multi-hex city. Its E1730 cell carries a training-area overprint and blue river imagery, with visible city blocks. Six adjoining cells can be identified geometrically, but print and overprint obscure some labels and edges. This is a visual source record, not a certified edge dataset, setup placement, occupancy finding, or movement ruling.

## Source and reproduction

Inspected [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 5 (Map E) and 1 (terrain key and Summary of Important Locations). Local PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Directions below refer to the displayed crop, not compass bearings. Reproduce with Poppler:

```sh
pdftoppm -f 5 -l 5 -r 300 -x 5200 -y 7270 -W 900 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e-cairo
pdftoppm -f 1 -l 1 -r 150 -x 700 -y 150 -W 1500 -H 2450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e-cairo.png /private/tmp/graziani-e-key.png
```

For this render, Cairo crop SHA-256 `4f5bac96f379bac2d44d9b423b96f7c9448a75680517574e6b86bbe53a0acafe`; unrotated key crop `f8296508e1c107557a46cdc9421def87e5669bef284a7433893b7a87d33276ec`. Key and summary appear sideways in this crop; rotate 90 degrees clockwise to read. Render hashes may vary with Poppler version.

## Source reading

Summary row prints Cairo at `E1730/1829/1830/1930/1931`, with X under Major City and Victory Condition, and dashes under Port and Assigned Garrison. It identifies five city hexes; it does not identify unit locations. Terrain key depicts blue building blocks as Major City, blue water as Major River, double black lines as Road, blue transverse-ticked line as Railroad, and soldier silhouettes as Training Area. The E1730 picture includes blue blocks, blue water, and soldier silhouettes above `CMNWLTH`; the silhouettes are the printed training-area symbol, not evidence of an individual counter or occupied stack. The key's delta color also appears immediately west of the river; do not extend that observation into a whole-hex classification without a separate terrain reading.

In the 300-dpi crop, E1730's approximate vertices clockwise from top are `(420,166)`, `(516,220)`, `(516,335)`, `(420,390)`, `(326,335)`, `(326,220)`. Printed `1730` is largely covered; the summary and surrounding grid locate the cell. Neighbor references below follow the visible grid and printed coordinate sequence; nearby numbers hidden by symbols are inferred from that sequence, not independently legible.

| Crop side | Adjoining cell | Visible evidence at or near boundary |
|---|---|---|
| Upper left | E1829 | Blue river imagery meets/crosses the diagonal; city and training-area printing crowd the upper part. |
| Upper right | E1830 | Blue city blocks extend around the diagonal; river and dark overprint crowd its upper endpoint. |
| Right | E1731 | Vertical grid line visible; city blocks lie near its upper portion, with Cairo name and brown terrain in the adjoining cell. |
| Lower right | E1630 | Diagonal grid line visible; city printing lies near it and brown terrain lies on the adjoining side. |
| Lower left | E1629 | Blue river imagery overlaps the diagonal toward its lower endpoint; railroad runs nearby to the left. |
| Left | E1729 | River imagery overlaps the vertical line; blue railroad runs nearby west of it. |

The broad river, printed grid, city blocks and black silhouettes overlap at this scale. Descriptions of imagery meeting a line do not decide whether a river follows, crosses, or legally constitutes a particular hexside. Road and railroad lines elsewhere in the surrounding Cairo complex should not be assigned to an E1730 edge solely because they appear in the same crop. No source-backed legal crossing or route follows from this visual check.

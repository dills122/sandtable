# Graziani Map E: Cairo E1930 visual evidence

Source: `/private/tmp/sandtable-maps.pdf`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF page 5 is Map E; page 1 contains the terrain key and Summary of Important Locations. This is a visual transcription of the supplied scan, not a rules or movement determination. Directions below refer to the rendered page orientation, with its text sideways; they are not compass directions.

The locations summary explicitly lists Cairo as **E1730/1829/1830/1930/1931**, with X for Major City and Victory Condition, and dashes for Port and Assigned Garrison. The terrain key identifies blue building clusters as Major City, blue lines with crossbars as Railroad, paired dark lines as Road, blue water along a hexside as Major River, and soldier silhouettes as Training Area. Cairo's city art spans multiple hexes; the printed black soldier silhouettes are a training-area overprint, not evidence of a unit's current position. The `CMNWLTH` label is printed below a separate cluster in E1730. No inference about occupancy or legal movement is made here.

E1930 is the upper city hex centered in `detail.png`, between the numbered E1929 river hex and E1931 city hex. The city art, soldier silhouettes, river and transport lines overlap the grid, so exact line junctions at corners should be checked against the crop rather than inferred from symbol centers.

| Side in rendered image | Adjacent hex | Visible evidence at/near shared side |
| --- | --- | --- |
| Upper left | E2029 | Dark paired road and blue crossbar railroad converge toward the city at this side; printing and silhouettes obscure their exact intersection with the hex line. |
| Upper right | E2030 | Blue water and dark road lines appear nearby; overlap at the upper vertex does not establish a water feature crossing this side. |
| Right | E1931 | Blue river band runs along the shared vertical side; major-city artwork and soldier overprint continue across it. |
| Lower right | E1830 | Blue river follows or overlaps the slanted boundary toward the lower vertex; city art continues on both sides. |
| Lower left | E1829 | Dark paired road and blue crossbar railroad visibly cross the diagonal in its interior near blue water; figures obscure parts of their continuation. |
| Left | E1929 | The blue river branch meets the city at or near this vertical boundary; dark roads and railroad converge nearby. |

Hex-neighbor labels follow the printed numbering grid, not an assertion that every feature is a passable connection. Overprinting and proximity to vertices prevent a confident assignment of every line to a single side; retain the source crop for review.

An [independent review](2026-11-17-graziani-e1930-six-sides-review.md) prompted these transport and water qualifications. Its inspection of pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records E1930 as `major-city` terrain and settlement, `coastal: false`. The review reports [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) for E1829 (`road`, `railroad`), E1830 (`minor-river`), E1929 (`unfinished-road`), E1931 (`minor-river`, `track`), and E2029 (`railroad`); it found no E2030 pair record. These records are comparison evidence, not proof of absent features or legal connections.

## Reproduce source crops

Poppler `pdftoppm`, 1-based page numbers, default PDF page rotation. Coordinates are pixels at the specified DPI, measured from the rendered page's upper left. PNG hashes below identify the outputs produced for this review; different Poppler versions may render differently.

```sh
mkdir -p /private/tmp/graziani-e1930
pdftoppm -f 5 -l 5 -r 100 -x 1600 -y 2200 -W 600 -H 650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1930/cairo
pdftoppm -f 5 -l 5 -r 200 -x 3350 -y 4550 -W 700 -H 850 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1930/detail
pdftoppm -f 5 -l 5 -r 300 -x 5250 -y 7100 -W 650 -H 650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1930/close
pdftoppm -f 1 -l 1 -r 150 -x 1100 -y 120 -W 1100 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1930/key
pdftoppm -f 1 -l 1 -r 150 -x 680 -y 1800 -W 1100 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1930/locations
shasum -a 256 /private/tmp/graziani-e1930/{cairo,detail,close,key,locations}.png
```

- `cairo.png`: `805bbb4a8589a4eab2992d58bd662268be62f7685822e63b7a90abf50c82c102`
- `detail.png`: `b98ac4b801ffb0131cdc9c53b49906d1721f9ea7b2914a816ad5553a46d26822`
- `close.png`: `0c81f30eb88284f7df636ae4657e471a076455bc9814ef55c2c7a1d059ba6474`
- `key.png`: `cc0aa5f1cf9d045231f94fb75bfd4027ea24eb5d2a77ac86ba7de8a0cbd99d61`
- `locations.png`: `eaccfff953687281bab02b2c2fe439816ef38465d354fc8ccfe26ac86f07fc58`

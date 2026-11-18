# Graziani map evidence: Cairo E1931

Source: `/private/tmp/sandtable-maps.pdf`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF page 5 is Map E; page 1 has the terrain key and location summary. Directions follow the rendered page with Cairo upright. E1931's number is obscured by artwork; identification follows neighboring grid labels and the location summary. This records visible map evidence, not occupancy or legal movement.

## Six sides

Blue city blocks and black soldier silhouettes overlap E1931 and adjoining cells. Pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records `major-city` terrain and settlement, `coastal: false`. Pinned features below are literal pairs from [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json); no pair record is not proof that artwork contains no feature.

| Side | Neighbor | Original map observation | Pinned features |
| --- | --- | --- | --- |
| NW | E2030 | Blue city blocks approach the diagonal. Water and black lines crowd its western vertex; no distinct crossing is confidently identified. | No pair record |
| NE | E2031 | Blue transverse-ticked railroad crosses the diagonal toward Heliopolis. | `railroad` |
| E | E1932 | Vertical grid side remains visible; no river, road, or railroad crossing identified. | No pair record |
| SE | E1831 | Diagonal appears clear of blue city blocks; no distinct route crossing confidently identified. | No pair record |
| SW | E1830 | Blue city artwork covers parts of the diagonal; figures crowd its western vertex. No distinct route crossing confidently identified. | No pair record |
| W | E1930 | Blue river artwork follows the vertical boundary; city blocks and black figures obscure parts. A separate track crossing cannot be confidently isolated from the overprint. | `minor-river`, `track` |

[Independent review](2026-11-17-graziani-e1931-six-sides-review.md) checked the crops, side ordering, key, summary, and pinned data. Black figures and other artwork obscure parts of the city and crowded vertices.

The west-side water is visually substantial, but crowded printing prevents a confident major-versus-minor river classification from this crop alone. The pinned track is not independently confirmed here. These observations do not establish a correction to either feature.

## Printed key and location summary

Page 1's Terrain Key identifies blue blocks as `Major City`, transverse-ticked blue line as `Railroad`, blue water side symbols as `Major River` and `Minor River`, double dark line as `Road`, dashed dark line as `Track`, and soldier silhouettes as `Training Area`. The silhouettes are printed training-area artwork, not unit counters. The nearby `CMNWLTH` marking belongs to the printed overlay; it does not specify a stack in E1931.

The `SUMMARY OF IMPORTANT LOCATIONS` lists Cairo `E1730/1829/1830/1930/1931`, with X under Major City and Victory Condition and dashes under Port and Assigned Garrison. This location-level entry supports the obscured city-cell identification, without resolving each edge.

## Reproducible crops

Poppler crop coordinates use pixels at the stated resolution from the upper-left of the rotated rendered page. Hashes identify PNG bytes in this environment; renderer versions may differ.

```sh
mkdir -p /private/tmp/graziani-e1931
pdftoppm -f 5 -l 5 -r 200 -x 3500 -y 4600 -W 850 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1931/e1931-detail
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 200 -W 1450 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1931/key
pdftoppm -f 1 -l 1 -r 200 -x 900 -y 2550 -W 1400 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e1931/locations
```

| Crop | SHA-256 |
| --- | --- |
| `e1931-detail.png` | `19300d5bbc8653bf2b568e5e7e3b4ee9e3891f5dd00aa3206b602435ce121986` |
| `key.png` | `fbf8a760ffbb7216574e47cd15462ad986b7587890cd646abc6ca82b8c7b4cad` |
| `locations.png` | `658341e7bda567cbf6bc3c89e1bad0bfbf6b2f2f9fce9426ffb560bd921f5287` |

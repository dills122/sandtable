# Map D Fuka–El Daba road and rail sides

**Finding:** Original Map D shows the blue cross-tied railway and parallel
road crossing seven consecutive local sides D3323–D3330. All seven pinned
records list both features. This bounded source comparison does not certify
construction, legal rail movement, or a complete route beyond these sides.

## Source and reproduction

[Original map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF p. 4,
terrain key PDF p. 1, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
The key identifies solid blue cross-tied railway, parallel dark road, and
separately dashed track. In this north-up Poppler crop, printed coastal cell
labels 3323–3330, Fuka, Galal, Ghaza and El Daba identify the strip. Road and
rail follow roughly horizontal paths; each pair below shares a vertical side.

```sh
pdftoppm -f 4 -l 4 -r 500 -x 6800 -y 7700 -W 2400 -H 1200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fukaeast500
```

Local PNG SHA-256
`ae637121c64f1c45847c89ce78e4f2988c7c42d048537c3d7c8193bf31065a48`.
Renderer versions may alter PNG bytes; coordinates and source hash identify
the inspected scan. Crop stays outside Git pending rights. Coordinates below
are approximate pixels **in that crop**, not surveyed geometry.

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
Data was known before this visual inspection; it is informed source checking,
not blind transcription. All rows below are represented as `road`, `railroad`.

## Seven source observations

| Shared side | Road and railway crossings | Qualification |
| --- | --- | --- |
| D3323–D3324 | Vertical grid near `x=445`; road crosses near `y=530`, blue rail near `y=580`. | Fuka dot, text and nearby dashed track crowd left cell; blue centerline visibly passes both sides. |
| D3324–D3325 | Side near `x=762`; road near `y=560`, rail near `y=610`. | Road, grid and number overprint; crossings appear below the upper vertex. |
| D3325–D3326 | Side near `x=1076`; road near `y=580`, rail near `y=625`. | Galal label and cell numeral cross linework; rail/road crossings remain distinguishable. |
| D3326–D3327 | Side near `x=1392`; road near `y=585`, rail near `y=635`. | Blue tie partly overlays the grid, but the blue centerline continues across the side. |
| D3327–D3328 | Side near `x=1707`; road near `y=585`, rail near `y=635`. | Ghaza lettering and numeral overlap the side; distinguish railway ink from text. |
| D3328–D3329 | Side near `x=2023`; road near `y=580`, rail near `y=615`. | El Daba label, road and blue line are close; grid and both line continuations are visible. |
| D3329–D3330 | Side near `x=2336`; road near `y=580`, rail near `y=625`. | Close to right crop boundary; confirm complete 3330 label and line continuation in a wider independent render. |

No claim is made that these sides lack any other feature. Visible dashed
tracks in the area and coastline/sea or slope classification are separate
questions. The blue railroad's geometry does not establish all railway hexes
are constructed under Land §8.47, nor ownership, movement phase, ZOC,
occupancy, terrain legality or other network connectivity.

## Review checks

1. Reproduce the crop and independently associate each vertical edge with its
   printed labels, especially D3329–D3330 at the crop's right edge.
2. Trace the blue centerline on both sides of each grid crossing, distinguishing
   cross-tie overprint from the grid. Confirm both dark road lines separately.
3. Use the seven positive records only as source features; test actual route
   admission and construction under independently reviewed rules and state.
   A missing additional feature is unknown, not automatically clear.

No JSON/runtime edits, source admission, or gameplay tests.

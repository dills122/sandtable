# Map E western coastal strip: bounded unpinned-rail search

**Date:** November 18, 2026. **Disposition:** No clear unpinned railway crossing found in the seven-cell strip `E3006`, `E3007`, `E3108`, `E3109`, `E3209`, `E3210`, `E3211`. One vertex leaves two adjacent unpinned sides as uncertain candidates, not positive omissions. This source-led local search is bounded to these cells and their visible sides; it is not a whole-sheet omission survey.

Inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, using blue cross-tied line as railway, distinguished from dark road and track. The source scan and pinned records were available before inspection; this is an informed reading of one scan, not blind or independent-edition corroboration. Crop coordinates below are approximate pixels, upper-left origin. Trace covers visible rail in the seven named cells and crossings at their boundary, including incoming and outgoing sides; no separate branch is visible inside the strip. Other map features are not assessed.

| Side or candidate | Scan observation | Literal pin |
| --- | --- | --- |
| `E3005–E3006` | Incoming line crosses vertical near `(23, 565)`, close to crop edge. | `railroad` |
| `E3006–E3007` | Line crosses vertical near `(243, 527)`. | `railroad` |
| `E3007–E3108` | Line crosses diagonal near `(350, 480)`, near El Hamman ink. | `railroad` |
| `E3108–E3109` | Line crosses vertical near `(574, 340)`, under lettering. | `railroad` |
| `E3109–E3209` | Line crosses diagonal near `(680, 290)`, amid settlement and road ink. | `railroad` |
| `E3209–E3210` | Line crosses vertical near `(904, 180)`. | `railroad` |
| `E3210–E3211` | Line reaches three-cell vertex near `(1125, 105)`; exact side attribution is uncertain. | `railroad` |
| `E3210–E3311`, `E3211–E3311` | Sides meeting the same vertex have no distinct crossing away from the vertex. Thick ink and unknown vertex convention leave either as a possible attribution; neither is a positive omission. | No exact pair record for either. |
| `E3211–E3312` | Outgoing line crosses diagonal near `(1250, 48)`, close to vertex and crop edge. | `railroad` |

Every named `railroad` pair above has one exact literal record with `side: null` and `up: null`; the two unpinned vertex candidates have no exact pair records. No other visibly cross-tied line crosses the seven cells' sides in this crop. Grid and ink overlap, lettering, crop boundaries, and the vertex limit the negative observation. See [coast review](2026-11-17-graziani-east-rail-coast-batch-review.md) and [Alexandria extension review](2026-11-17-graziani-alex-coastal-rail-extension-review.md) for prior readings of the same scan, including the vertex qualification. These observations do not establish a network, service, construction or damage state, control, occupancy, or a legal gameplay result.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 350 -x 1250 -y 5800 -W 1400 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/map-e-unpinned-west
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/map-e-unpinned-west.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
python3 - <<'PY'
import json
rows = json.load(open('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json'))['hexsides']
pairs = ['E3005 E3006', 'E3006 E3007', 'E3007 E3108', 'E3108 E3109',
         'E3109 E3209', 'E3209 E3210', 'E3210 E3211', 'E3211 E3312',
         'E3210 E3311', 'E3211 E3311']
for pair in pairs:
    a, b = pair.split()
    print(pair, [r for r in rows if {r['a'], r['b']} == {a, b}])
PY
```

SHA-256 in command order: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, `3da1d27976ff721435baf495fd68602183d8171ffe74a08eda1e57981be3f283`, `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer; crop remains outside Git. Documentary work only; no runtime or map-data changes or gameplay tests.

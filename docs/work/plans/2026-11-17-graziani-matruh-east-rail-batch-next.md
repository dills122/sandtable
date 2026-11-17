# Map D railway sides east of Matruh: source batch

**Finding:** Original Map D shows blue cross-tied railway crossing several
locally adjacent sides east/southeast of D3518. The inspected pinned records
list `railroad` on each of the six pairs below. Some crossings lie near a
vertex or are overprinted; a positive side observation is not a complete
route, legal railway service, or certification of other features.

## Source and reproduction

[Original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4
Map D and p. 1 terrain key, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Key distinguishes blue cross-tied railroad, dark parallel road, dark broken
track and splash-edged slope. North up in Poppler render. Wider 500-dpi crop
shows printed 3518–3522, 3419–3422 and 3322–3323 cell labels, coast and local
landmarks; 800 dpi resolves the crowded Matten Baggush road/rail area.

```sh
pdftoppm -f 4 -l 4 -r 500 -x 5200 -y 7300 -W 2200 -H 1150 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/matruheastrail500
pdftoppm -f 4 -l 4 -r 800 -x 9500 -y 12200 -W 1500 -H 900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/matruheastrail800
```

PNG SHA-256, in order:
`7e99f0dacb88c45e63adc75eec87427e89eb879dd32b8ead826254ff4fb0d0f4`,
`f000351177e0e482eb2799fad6e6e2581b400c9d80d4fe365436168c5eeed78b`.
Renderer versions may change PNG bytes. Pixel references below are approximate
coordinates in the **500-dpi crop**, not georeferenced measurements. Crops
stay outside Git pending rights.

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
Existing data was available before scan review; this is an informed visual
comparison, not blind transcription.

## Per-side observations

| Pair | Visible original ink and bounds | Pinned features and qualification |
| --- | --- | --- |
| D3518–D3519 | Shared vertical grid around `x=473`, `y=355–535`. Blue rail centerline crosses around `(473,525)` near its lower vertex; dark road crosses higher around `y=480–500`. Rail/grid/road ink crowd the junction, so independently verify which side owns the blue intersection. | `railroad`, `road`; apparent rail and road support both, but rail near vertex deserves close review. |
| D3419–D3519 | Shared diagonal runs approximately `(630,625)` to `(785,535)`. Blue line appears to cross near `(680–700,590–600)`; road, junction and grid overlap there. | `railroad`, `road`; candidate rail crossing supports the record, but exact road and rail intersections are overprinted. |
| D3419–D3420 | Shared vertical line `x≈942`, `y≈625–805`; blue centerline visibly crosses near `(942,710)`, well below upper vertex. Parallel road lines meet the top of this side around its upper vertex and are difficult to attribute conclusively. | `railroad` only; railway supported. **Road presence unresolved**; no claim that the omitted road is clear or an admitted mismatch. |
| D3420–D3421 | Shared vertical `x≈1258`, `y≈625–805`; blue line crosses near `(1258,760)` and dark road crosses higher around `(1258,690)`. Compare the separate 800-dpi crop, where the line/side intersections are more legible. | `railroad`, `road`; both visible crossings are consistent, subject to ordinary source review. |
| D3322–D3421 | Shared diagonal approximately `(1415,895)` to `(1575,805)`; blue rail crosses around `(1500,845)` within a brown slope band. Coast, road and printed label crowd the nearby top end. | `railroad`, `slope`, up `D3421`; rail and brown band visible. **Direction and road membership not independently checked** here. |
| D3322–D3323 | Shared vertical `x≈1733`, from `y≈895` downward; blue rail crosses near `(1733,950)`, through the local Fuka name/track artwork. | `railroad`; visible centerline supports feature. Exact other marks and nearby labels remain separate checks. |

The six sides are: D3518–D3519, D3419–D3519,
D3419–D3420, D3420–D3421, D3322–D3421 and D3322–D3323. They are local
observations; vertex-adjacent candidates do not supply a validated path between
them. No absent mark is treated as verified clear. For railway construction
and possible operation, use Land §8.47, scenario state and all other
movement/rail rules separately. Coast, actual unit positions, ownership, ZOC,
map extent and complete connectivity are outside this inspection.

## Checks for independent review

1. Retrace each side from the printed cell labels and nearby vertex, especially
   D3518–D3519 and D3419–D3519 where blue rail meets other ink.
2. At D3419–D3420, compare the 800-dpi upper vertex and both road lines before
   classifying a road crossing or proposing a data change; railroad itself is
   observed lower on the side.
3. Compare D3322–D3421 slope direction independently using the original key
   and §8.35 before relying on pinned `up`; this note does not certify it.
4. A source-confirmed side does not authorize movement without a separately
   reviewed legal graph, phase, construction status and actual game state.

No JSON/runtime edits, source admission or gameplay tests.

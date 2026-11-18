# Fuka approach: bounded road and railway observations

**Date:** November 17, 2026. **Status:** informed visual comparison, not
independent-edition corroboration, map-data admission, or a complete legal
route. This checks the Fuka junction between the earlier
[Matruh east ledger](2026-11-17-graziani-matruh-east-rail-batch-next.md) and
[eastward continuation](2026-11-17-graziani-continuation-map.md).

## Source and method

[Original five-sheet map scan](https://spigames.net/PDFv10/CNA_Maps.pdf),
Map D, PDF page 4; terrain key on page 1. PDF SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
North is up. The key distinguishes blue cross-tied railroad from paired dark
road lines. The following crop retains printed cell numbers and junction:

```sh
pdftoppm -f 4 -l 4 -r 800 -x 10400 -y 12900 -W 1900 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/fuka-prefix
```

Crop SHA-256 `e7342e8a58df5a2dcd0d0d437374032c267ecdb1c48d23e164f98fefc61b1570`;
renderer differences can change PNG bytes. Pixel positions below are
approximate crop coordinates. Source and pinned records were both available
before inspection, so this is not blind transcription. Pinned
[hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

## Local comparison

| Shared side | Source observation | Pinned record / qualification |
| --- | --- | --- |
| D3322–D3323 | Blue railway crosses vertical side around `(690, 290)`; cross-tie and grid overlap. Paired road lines lie above its upper vertex, around `(690, 130–170)`, in the neighboring cell. | `railroad` only; railway consistent. Do not infer road on this side from the road's proximity. |
| D3323–D3422 | Paired road lines cross diagonal from approximately `(690, 220)` to `(940, 80)`, near `(800–850, 130–160)`; Fuka lettering overlaps ink. | `road`; consistent with apparent crossing. |
| D3421–D3422 | Paired road lines meet vertical side at approximately `x=440`, close to its lower vertex `(440, 80)`. Road, slope and grid crowd the junction. Blue railway appears below that vertex, on the D3421–D3322 diagonal instead. | `road`, `railroad`, `slope`, up `D3421`. Road supports a candidate crossing, but exact vertex attribution and the pinned railway on this side require independent close review; this crop alone is not a certified absence or correction. Slope and direction are not checked. |
| D3322–D3421 | Blue railway appears across the diagonal leading southwest from `(440, 80)`, within slope artwork. | `railroad`, `slope`, up `D3421`; railway consistent. Slope direction and road membership remain outside this check. |
| D3323–D3324 | Road and railway continue east; upper road stroke approaches upper vertex at approximately `(1195, 220)`. | `road`, `railroad`; see [independent same-scan geometry qualification](2026-11-17-graziani-continuation-map-review.md). |

These are local observations only. Road and railway follow different sides at
the Fuka vertex; their visible continuation does not settle preceding Matruh
junctions, further eastward sides, the D/E seam, terminal connections, or
service and movement legality. Overprint, vertex ownership, slope direction,
construction, damage, control, occupancy and ZOC require separate evidence.
No absent feature is certified. No JSON or runtime change and no gameplay test.

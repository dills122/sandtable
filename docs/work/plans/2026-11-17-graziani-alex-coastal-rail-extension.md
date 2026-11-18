# Map E railway east of E3108: bounded extension

**Date:** November 17, 2026. **Disposition:** Six consecutive visual crossing candidates extend the reviewed E3108 endpoint to E3412. Stop at the first apparent railway fork in E3412, before choosing a branch; six is below the requested eight-to-twelve target for that reason. Each of the six pinned sides contains `railroad`. This is an informed reading of one scan, not a complete route or proof of operational service.

## Source and method

Inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map E PDF p. 5, and the prior [coastal batch and review](2026-11-17-graziani-east-rail-coast-batch-review.md). PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Pinned data was available before inspection; comparison is not blind or an independent edition. Blue cross-tied ink indicates railway; nearby dark parallel lines indicate road. North is up.

```sh
pdftoppm -f 5 -l 5 -r 350 -x 1600 -y 5000 -W 3000 -H 1200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-alex-north
```

Crop SHA-256 `8c0ce81f5bda8499a8de89a144a6ca44189611e6d32e9d3f4774746a219e277b`; renderer versions can change PNG bytes. Crop remains outside Git. Coordinates below are approximate pixels in this crop.

| Shared side | Visual observation and pinned comparison |
| --- | --- |
| E3108–E3109 | Blue line crosses vertical side near `(225,1130)`, beneath Burg el Arab lettering. Pinned `railroad`. |
| E3109–E3209 | Blue line crosses rising diagonal near `(330,1090)`, amid road and settlement ink. Pinned `railroad`. |
| E3209–E3210 | Blue line crosses vertical side near `(553,980)`. Pinned `railroad`. |
| E3210–E3211 | Blue line approaches near `(773,910)`; the [independent review](2026-11-17-graziani-alex-coastal-rail-extension-review.md) places it at a crowded three-hex vertex, so side attribution remains uncertain. Pinned `railroad`. |
| E3211–E3312 | Blue line crosses diagonal near `(900,875)`, close to its western vertex; attribution merits independent higher-resolution review. Pinned `railroad`. |
| E3312–E3412 | Blue line crosses rising diagonal near `(1095,715)`, close to its eastern vertex. Pinned `railroad`; proximity to vertex and overlapping road ink limit precision. |

At approximately `(1175,650)` blue cross-tied ink appears to split north toward Alexandria and east toward Kafr el Douwar. Pinned E3412–E3513 and E3412–E3413 both carry `railroad`; these are not counted as surveyed continuation crossings. Fork attribution and subsequent sides need separate review. Other features on the six sides were not systematically checked.

## Limits

This finite local sequence does not verify the clipped D/E seam, a continuous Matruh–Alexandria route, railway operation, or absence of other witnesses. Geographic comparison remains conditional on the proposed sheet ordering; a qualifying witness also depends on construction/destruction, occupancy, and snapshot rules. No map or runtime changes; no gameplay tests.

# Map E railway east branch from E3412

**Date:** November 18, 2026. **Disposition:** Four consecutive east-branch crossings are visually attributable and each has `railroad` in pinned data. Stop at the junction within E3516, where railway and road ink converge; no continuation side is counted. This is a bounded reading of one scan, not an independent edition or a data correction.

## Evidence and method

Inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5 (Map E), starting at the fork identified in the [prior review](2026-11-17-graziani-alex-coastal-rail-extension-review.md). PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Pinned data was available during inspection; comparison is not blind. Blue cross-tied ink denotes railway; dark parallel lines denote road.

```sh
pdftoppm -f 5 -l 5 -r 700 -x 5450 -y 10400 -W 2200 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-detail
```

Detail SHA-256 `ae1c57c72c45956ec7959fc3d3633ae9fbd4494a7ce528e914aebda804e37b58`; renderer versions can change PNG bytes. Crop stays outside Git. Approximate pixel coordinates below refer to this detail.

| Consecutive side | Visual reading | Pinned feature |
| --- | --- | --- |
| E3412–E3413 | Eastbound cross-tied line crosses vertical grid near `(180, 895)`, above lower vertex. | `railroad` |
| E3413–E3414 | Line crosses river and vertical grid near `(620, 800)`. | `railroad` |
| E3414–E3415 | Line crosses vertical grid near `(1065, 715)`. | `railroad` |
| E3415–E3516 | Line crosses diagonal grid and river near `(1380, 590)`. | `railroad` |

At the Kafr el Douwar junction near `(1640, 420)`, railway meets another cross-tied line and overlaps road ink. E3516–E3517 is pinned `railroad` and `road`, but the particular onward branch cannot be followed unambiguously through this junction in this bounded trace; it is not counted. Northbound branch from E3412 was not surveyed.

## Limits

Other features and sides were not systematically checked. These observations do not establish a D/E seam, operational service, or a gameplay witness; no occupancy assumptions were made. No map or runtime changes; no gameplay tests.

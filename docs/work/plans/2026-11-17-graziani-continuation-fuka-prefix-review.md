# Fuka approach: independent visual review

**Date:** November 17, 2026. Same scan, separate visual inspection; not an
independent map edition or blind transcription. This checks the local claims in
[Fuka approach](2026-11-17-graziani-continuation-fuka-prefix.md), not route
legality or complete feature inventories.

Source: [five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D,
PDF page 4; PDF SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
North up. Reviewed a wider crop to retain the full vertical side above the
junction:

```sh
pdftoppm -f 4 -l 4 -r 800 -x 10000 -y 12300 -W 2600 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/fuka-prefix-review
```

Crop SHA-256 `246b3d7e20e319e4af70e99eece3c90adbf32dc488a4a38d95249cd2be9f2a07`;
renderer differences may change bytes. Coordinates below are approximate crop
pixels.

| Side | Visual review and pinned record |
| --- | --- |
| D3322–D3323 | Blue cross-tied line crosses vertical grid at `(1090, 900)`, well below upper vertex `(1090, 820)`. Pinned `railroad` supported. Road strokes pass above that vertex; proximity alone does not support road on this vertical side. |
| D3323–D3422 | Paired dark road strokes cross diagonal northeast of `(1090, 820)` amid Fuka lettering; pinned `road` consistent. Overprint limits exact crossing position. |
| D3421–D3422 | Vertical grid runs down to junction near `(840, 680)`. Upper road stroke crosses close above junction; lower stroke crowds it. Pinned `road` is a reasonable candidate, with vertex attribution uncertain. Blue railway runs southwest of junction and crosses the adjacent diagonal near `(720, 745)`, not visibly across the vertical side. Pinned `railroad` on this vertical side remains unsupported by this view; overprint and vertex convention prevent a certified absence or correction. Pinned `slope`, up `D3421`, not assessed. |
| D3322–D3421 | Blue cross-tied railway visibly crosses diagonal southwest of `(840, 680)`, consistent with pinned `railroad`. Pinned `slope` and direction not assessed. |
| D3323–D3324 | Railway crosses vertical side; road approaches its upper vertex amid grid and lettering. Pinned `road`, `railroad` consistent with qualified geometry in [continuation review](2026-11-17-graziani-continuation-map-review.md). |

The distinct railway crossing on the southwestern diagonal is positive visual
evidence; it does not by itself establish absence of railway on the adjacent
vertical side or decide vertex ownership. No map-data or runtime change follows
from this review. No gameplay test run.

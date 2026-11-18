# Map D pinned rail batch A: independent reading

**Date:** November 18, 2026. Rechecked [batch A](2026-11-18-graziani-map-d-rail-batch-a.md) against the same original [map scan and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. This is an independent reading of the same scan, not an independent edition. Source PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` and pinned sides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` checked.

| Exact pin | Review of source evidence |
| --- | --- |
| `D3514|D3515` | Agree blue cross-tied ink crosses the side away from vertices. Fine centerline interruptions versus continuous line remain uncertain, so the pinned unfinished class remains unknown. |
| `D3514|D3613` | Agree blue cross-tied ink crosses the diagonal away from vertices. Centerline class remains unknown amid grid and fine ink. |
| `D3611|D3711` | Agree blue cross-tied ink crosses the diagonal; dark road linework and grid overlap it. This does not settle the pinned finished/unfinished distinction. |
| `D3710|D3711` | Agree with the qualified reading: blue rail appears to reach the shared side just inside its vertex, but road, grid and nearby vertex limit attribution. Treat the exact-side crossing as a candidate, not an unqualified positive. Its class remains unknown. |

No substantive disagreement with batch A. Key shows continuous centerline for railroad and interrupted centerline for unfinished railroad; cross-ties alone do not distinguish the classes. The two `road` pins remain outside this rail-focused review; apparent dark strokes should not substitute for separate feature assessment. No inference about construction or use follows.

Reproduction of this recheck uses default rotation, with coordinates distinct from batch A's clockwise-rotated crop:

```sh
pdftoppm -f 4 -l 4 -r 200 -x 750 -y 2407 -W 1250 -H 1150 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/mapd-a-review-crop
pdftoppm -f 1 -l 1 -r 300 -x 2300 -y 300 -W 2000 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-key
```

Documentation only; no data or runtime changes, and no gameplay tests.

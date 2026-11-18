# Map D rail batch A higher-resolution recheck: review

**Date:** November 18, 2026. Reviewed [recheck A](2026-11-18-graziani-map-d-rail-unknown-recheck-a.md) against the original [map and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. This is a prompted second reading of the **same scan**, not an independent printing. PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` and pinned-side file SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` checked. The key shows a continuous blue centerline with ties for railroad and an interrupted one with ties for unfinished railroad.

| Exact pin | Independent reading |
| --- | --- |
| `D3514|D3515` | Agree the blue cross-tied line crosses the side interior. Nearby repeated separations of the blue centerline are visible; qualified source-positive for the pinned unfinished class. Grid and label overlap the immediate crossing. |
| `D3514|D3613` | Agree the cross-tied line crosses the diagonal away from its vertices. The same local blue run shows repeated interruptions on either side, consistent with the unfinished key; qualified source-positive. A tie and grid overlap at the side. |
| `D3611|D3711` | Agree generic blue rail crosses the diagonal. Visible separated blue segments near the crossing support the proposed unfinished reading, with greater qualification: dark road strokes, grid and brown ink obscure parts of the symbol. This is a local pattern interpretation, not a clear isolated centerline at the intersection. |
| `D3710|D3711` | Agree overall **unknown**: road/grid crowd the line near the shared vertex, so stronger evidence of an interrupted nearby symbol does not settle exact-side attribution. Nearby blue segments appear separated but road overprint limits class reading there as well. |

No substantive disagreement with the recheck's dispositions. A 600-dpi render enlarges the embedded 300-ppi map image rather than adding source information. Pixel softness and overprint remain qualifications, especially for the two road-adjacent sides; no absence or gameplay inference follows. Separate road features were not assessed.

Reproduce the rechecked map view and key:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 3700 -y 8550 -W 2000 -H 2100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-a-review-600
pdftoppm -f 1 -l 1 -r 300 -x 2300 -y 300 -W 2000 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-key
```

Documentation only; no data or runtime changes, and no gameplay tests.

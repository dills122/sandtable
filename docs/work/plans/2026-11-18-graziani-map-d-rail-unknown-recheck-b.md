# Map D rail class recheck: D3611–D3613

**Date:** November 18, 2026. Reinspection of two exact pins from the [open-evidence worklist](2026-11-18-graziani-map-d-rail-open-evidence.md). Generic rail crosses both sides. Neither pinned `unfinished-railroad` class is independently established by this scan; both remain **unknown**. This same-scan recheck is not an independent edition, proof of absence, or a construction or usability conclusion.

## Source and view

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF p. 4 and terrain key p. 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Both checked. The printed key distinguishes continuous blue centerline with cross-ties for railroad and interrupted blue centerline with cross-ties for unfinished railroad. Cross-ties alone do not settle class.

The Map D embedded image is 300 ppi; the first crop samples at that resolution. The enlarged 600-dpi view interpolates the same scan, not additional source detail. At default rotation, north is up; approximate coordinates below refer to the first crop's top-left origin.

```sh
pdftoppm -f 4 -l 4 -r 300 -x 1900 -y 4250 -W 850 -H 500 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/rail-unknown-b
pdftoppm -f 4 -l 4 -r 600 -x 4170 -y 8810 -W 1000 -H 240 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/rail-unknown-b-detail
pdftoppm -f 1 -l 1 -r 300 -x 2300 -y 300 -W 2000 -H 1300 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/mapd-key
```

| Literal pin | Exact-side reading and class limit |
| --- | --- |
| `D3611|D3612` | Blue cross-tied line crosses the vertical side near `(280, 225)`, between its vertices near `(280, 184)` and `(280, 294)`. Side/grid, dark parallel road ink and the D3612 label crowd the line. Blue portions continue on either side, but this view does not reliably establish the interrupted centerline pattern required to distinguish the pinned class. |
| `D3612|D3613` | Blue cross-tied line crosses the vertical side near `(469, 250)`, between vertices near `(469, 184)` and `(469, 294)`. Dark road, grid, and the D3613 label overlap the centerline near the crossing. Blue segments continue on both sides and curve away from the road to the right, but the fine centerline and nearby ties do not give a sufficiently reliable repeated interruption pattern for a class determination. |

The geometry supports generic crossings away from vertices, not classification by proximity to neighboring pins. A cleaner, independently identified print or scan preserving the centerline and grid could improve the class reading; interpolation cannot create missing information. No other feature or gameplay finding is made. Documentation only; no data or runtime changes, no gameplay tests.

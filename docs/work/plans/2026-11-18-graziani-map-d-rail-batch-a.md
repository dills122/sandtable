# Map D pinned rail: batch A source reading

**Date:** November 18, 2026. Inspected four exact sides from the [worklist](2026-11-18-graziani-map-d-rail-worklist.md) against [Map D and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. All four pins name `unfinished-railroad`; generic blue cross-tied rail ink crosses each side, but this reading does **not** independently establish the pinned finished/unfinished class on any of them. The key distinguishes uninterrupted versus interrupted blue centerline. Thin ink, grid crossings, and overprint make that distinction uncertain at these crossings. Positive generic rail is not positive unfinished-rail classification.

| Literal pin | Exact-side observation in clockwise-rotated crop | Pinned rail class disposition |
| --- | --- | --- |
| `D3514|D3515` | Blue cross-tied line crosses horizontal side around `(463, 1018)`, between its vertices around `(422, 1018)` and `(495, 1018)`. It continues through the numbered area below; brown ink and label crowd that continuation. | **Unknown**: rail crossing visible; interrupted centerline not confidently established. |
| `D3514|D3613` | Blue cross-tied line crosses shared diagonal around `(518, 932)`, between vertices around `(495, 892)` and `(530, 955)`; crossing is away from both vertices. | **Unknown**: rail crossing visible; completion class not confidently distinguished. |
| `D3611|D3711` | Blue cross-tied line crosses diagonal around `(622, 612)`, between vertices around `(603, 578)` and `(638, 641)`. Black road strokes and brown ink overlap the rail and grid at this crossing. | **Unknown**: generic rail visible despite overprint; completion class unsettled. |
| `D3710|D3711` | Blue cross-tied line crosses horizontal side around `(650, 515)`, close to its left vertex around `(638, 515)`; black road and grid crowd it. The blue stroke appears on the horizontal side just to the right of that vertex, rather than conclusively on the adjoining diagonal. | **Unknown**: candidate exact-side rail crossing is vertex-qualified; completion class unsettled. |

Coordinates approximate pixels in the 1,150 × 1,250 crop below, not survey measurements. `D3611|D3711` and `D3710|D3711` pins also list `road`; this rail-focused reading does not independently settle road attribution. All four pins have `side: null`, `up: null`. Pin labels are data claims, not evidence of construction status or operational consequences. Same-scan reading is not independent edition verification.

## Reproduction and provenance

```sh
pdftoppm -f 4 -l 4 -r 200 -singlefile -png /private/tmp/sandtable-maps.pdf /private/tmp/map-d-200
python3 - <<'PY'
from PIL import Image
im = Image.open('/private/tmp/map-d-200.png').rotate(-90, expand=True)
im.crop((3500, 750, 4650, 2000)).save('/private/tmp/railbatcha.png')
PY
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/railbatcha.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `3a1e994a93dddc00f2f5ea3b9cad06f7bcbc446ba2821d996c204f14e7e58b2a`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer and Pillow versions; crop remains outside Git. Documentation only; no data or runtime changes, and no gameplay tests.

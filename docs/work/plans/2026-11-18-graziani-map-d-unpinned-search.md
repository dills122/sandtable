# Map D bounded unpinned rail search: D3613

**Date:** November 18, 2026. Searched all six printed sides of D3613 for blue cross-tied rail, including sides without pinned rail. This bounded six-side search checks an area beyond the two already discussed rail crossings; it is not a full-sheet inventory. **No additional unpinned rail crossing identified in exposed side portions.** Overprint limits negative findings, especially at vertices; no missing entry is certified clear. This search does not revisit finished versus unfinished class.

| Literal sorted pair | Source observation | Pinned features |
| --- | --- | --- |
| `D3612|D3613` | Blue cross-tied rail crosses the shared vertical side with numeral, grid and paired road ink nearby. Previously reviewed; class has been uncertain. | `road`, `unfinished-railroad` |
| `D3514|D3613` | Blue cross-tied rail crosses the lower-right diagonal away from endpoints; separately rechecked in [batch A](2026-11-18-graziani-map-d-rail-unknown-recheck-a.md). | `unfinished-railroad` |
| `D3613|D3714` | Paired black road strokes cross the upper-right diagonal and brown band. No separate blue cross-tied rail crossing identified there; road/grid/brown overprint limit an absence finding. | `slope`, `road` |
| `D3613|D3713` | No rail crossing identified in exposed upper-left diagonal; brown band and blue place lettering overlap portions. | `slope` |
| `D3613|D3614` | No rail crossing identified on the exposed right vertical side; brown band and endpoints limit certainty. | `slope` |
| `D3513|D3613` | No rail crossing identified on exposed lower-left diagonal; grid and neighboring markings limit endpoints. | No literal pair found. |

All six sides and vertices fit inside the inspected crop. The blue cross-tied line entering D3613 from D3612 exits toward D3514; its tie strokes are not independently assigned as centerline crossings on adjacent sides. Dark paired road strokes and blue place lettering are distinct marks, not railroad evidence. Pin absence does not establish absent ink, clear terrain, or a legal edge. No conclusion about other features, a whole route, construction or use follows.

Source: [Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Exact literal pairs and feature lists checked in JSON. Reproduce wider unrotated scan view:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 4100 -y 8000 -W 1800 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-3613-full
shasum -a 256 /private/tmp/mapd-3613-full.png
```

Crop SHA-256 `de5ae0f627d54e052daac0446f100226bbe75c33c189aad3d8f2219f117fcd18`; renderer affects PNG bytes. The embedded map image is 300 ppi; 600-dpi output interpolates existing scan pixels. Same scan as earlier reviews, not an independent edition. Documentation only; no data or runtime changes and no gameplay tests.

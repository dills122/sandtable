# Map D bounded unpinned rail search: D3608

**Date:** November 18, 2026. Inspected all six printed sides of D3608, distinct from the [D3613 window](2026-11-18-graziani-map-d-unpinned-search.md). The two visible rail crossings are already pinned. **No additional unpinned rail crossing identified in exposed side portions.** This is a finite search, not an absence certification or a full-sheet inventory.

| Literal sorted pair | Source observation | Pinned features |
| --- | --- | --- |
| `D3607|D3608` | Blue cross-tied line crosses vertical shared side near its middle; grid and tie crowd the intersection. Well away from side vertices. | `unfinished-railroad` |
| `D3608|D3609` | Same blue line crosses vertical shared side near its middle; grid and tie crowd the intersection. Well away from vertices. | `unfinished-railroad` |
| `D3608|D3708` | Upper-left diagonal bears a brown band. No blue rail crossing identified on its exposed length. | `slope` |
| `D3608|D3709` | Upper-right diagonal bears a brown band. No blue rail crossing identified on its exposed length. | `slope` |
| `D3508|D3608` | No blue rail crossing identified on lower-left diagonal. | No literal pair found. |
| `D3509|D3608` | No blue rail crossing identified on lower-right diagonal. | No literal pair found. |

All six sides and vertices fit inside the crop. Blue rail centerline crosses the two vertical sides and does not approach the four diagonal vertices in this local cell. Brown artwork and grid qualify inspection near upper endpoints. Paired black road strokes lie in row 37, distinct from D3608's rail, and thick black dashes farther south are separate track ink. No missing pair proves clear terrain or absence of other features. Pinned class was separately assessed in [batch C class check](2026-11-18-graziani-map-d-rail-batch-c-class-check.md); this search does not infer construction, usability, or a route.

Source: [Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1. Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Exact literal pairs and feature lists checked in JSON. Reproduce unrotated view:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 2300 -y 8300 -W 1500 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-d3608-search
shasum -a 256 /private/tmp/mapd-d3608-search.png
```

Crop SHA-256 `21a7a1a11a2d7837a0e6ec2ef6d4a9c4e698ed7df36d733340e638dad2763a9b`; renderer affects PNG bytes. Embedded image is 300 ppi; 600-dpi output interpolates existing scan pixels. Same scan as earlier reviews, not an independent edition. Documentation only; no data or runtime changes and no gameplay tests.

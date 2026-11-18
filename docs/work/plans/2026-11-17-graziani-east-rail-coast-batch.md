# Map E coastal railway: bounded eastern witness survey

**Date:** November 17, 2026. **Disposition:** Eight local printed railway-side candidates run from clipped E3101 to E3108 near El Hamman. All eight have `railroad` in pinned data. This informed reading of one scan does not establish a complete route, operational railway, or absence of qualifying witnesses.

## Source and bounds

Inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map E PDF p. 5 and terrain key p. 1. PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Pinned data was available before inspection; comparison is not blind or an independent edition. Blue cross-tied ink is railway; nearby dark parallel lines are road. North is up.

```sh
pdftoppm -f 5 -l 5 -r 350 -x 0 -y 5000 -W 4500 -H 2100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-coast-survey
```

Crop SHA-256 `cfb737c781cbe087c7e09e2e65ea7cde29b9b8a4f524780d59d0130f22b897b3`; renderer versions can change PNG bytes. Crop remains outside Git. Coordinates below are approximate pixels in that crop.

| Shared side | Visible railway and pinned comparison |
| --- | --- |
| E3101–E3001 | Blue line crosses lower-right sloping side near `(175,1250)`; E3101 is clipped at the sheet edge. Pinned `railroad`. |
| E3001–E3002 | Railway crosses vertical side near `(395,1360)` amid El Alamein lettering and a dark track. Pinned `railroad`. |
| E3002–E3003 | Blue centerline crosses vertical side near `(615,1390)`; pinned `railroad` and `road`. |
| E3003–E3004 | Blue line crosses near `(835,1400)`, close to the lower grid vertex around `(835,1420)`; exact side ownership deserves independent high-resolution review. Pinned `railroad` and `road`; do not treat vertex-adjacent attribution as settled. |
| E3004–E3005 | Railway crosses vertical side near `(1054,1385)`, with dark track nearby. Pinned `railroad` and `road`. |
| E3005–E3006 | Blue centerline crosses vertical side near `(1273,1370)`. Pinned `railroad`. |
| E3006–E3007 | Blue centerline crosses vertical side near `(1493,1340)`. Pinned `railroad`. |
| E3007–E3108 | Blue line crosses the upper-right diagonal near `(1600,1300)`, east of El Hamman and its road spur. Pinned `railroad`. |

Other features on these sides were not systematically checked. At El Alamein, especially the near-vertex E3003–E3004 crossing and pinned E2905–E3004 railway, verify full local junction attribution before treating the sequence as an unqualified connected graph. The next visible eastward blue line continues toward Burg el Arab; this batch stops at E3108 and does not classify its next crossing, branches, or further Alexandria approaches.

## Ordering and witness limits

Only under the **conditional 33-column D/E ordering** does this E-side area lie east of used Matruh rail hex D3714. E3101 is partly missing from the scan; the proposed D3134/E3101 overlap and any D-to-E side remain unverified because grid fit and approximate feature placement do not settle the seam. See [D/E anchor review](2026-11-17-graziani-de-seam-anchor-batch.md). No seam or continuous Matruh–Alexandria route follows from these observations.

These endpoints are finite candidates for a possible eastern witness, not an exhaustive census. Actual qualifying rail construction, destruction policy, Axis combat occupancy, snapshot and geographic comparison must be evaluated separately. No candidate or omission proves absence of a witness; off-route rail can matter independently of a connected coastal path. No runtime or map data edits; no gameplay tests.

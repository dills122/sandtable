# Cairo northern river junction: independent same-scan review

**Date:** November 18, 2026. **Disposition:** Agree with the bounded [survey](2026-11-18-graziani-cairo-river-junction.md). The approach reaches the river vertex, but overlapping grid, river, road and rail ink do not securely assign its crossing to `E2329–E2428` or `E2329–E2429`. The pin for `E2329–E2428` contains `major-river` and `road`, not `railroad`; this is a data comparison, not proof of printed absence.

Independently rendered page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) at 300 dpi and inspected the junction with its labeled grid. The six comparisons below are local printed-ink observations; the pin was checked by literal unordered endpoint pairs in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json).

| Side | Independent scan check | Pin check |
| --- | --- | --- |
| `E2329–E2428` | River follows diagonal to the crowded vertex; road and ticked rail converge there. Exact approach-side attribution remains unresolved. | `major-river`, `road` |
| `E2329–E2429` | Converging lines also meet this side at the vertex; no secure rail crossing attribution. | No literal record |
| `E2428–E2429` | River and eastbound ticked branch meet the vertical boundary near its upper end; overprint limits exact attribution. | `minor-river`, `railroad` |
| `E2428–E2529` | Northbound ticked rail and paired road cross the diagonal above the junction. | `railroad`, `road` |
| `E2429–E2529` | River follows the boundary toward the branch vertex; rail crossing cannot be securely assigned there. | `minor-river` |
| `E2429–E2530` | Eastbound ticked rail crosses the diagonal away from the crowded vertex. | `railroad` |

All five present records have `side: null` and `up: null`. Missing literal record is not map absence. Pin classifications are not independent source adjudications. Printed crossings do not establish bridge condition, service, occupancy or a usable route; this review uses the same scan, not an independent edition.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 3900 -y 6075 -W 2625 -H 2100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-junction-independent
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-junction-independent.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop `96d3c278e1cf92b4691eff96f30491c6bf4e66ded39a6216631a3faeadd42b32` (renderer-dependent); pinned data `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No runtime or map-data changes; no gameplay tests.

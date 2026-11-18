# Graziani Cairo northern river junction: bounded witness survey

**Date:** November 18, 2026. **Disposition:** The original Map E scan shows ticked rail approaching a crowded river vertex and continuing on two visible branches. It does not settle which side at the vertex carries the approach. This is a local ink survey, not a route, service, or absence finding.

## Evidence

Compared page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) and its page 1 symbol key with literal unordered pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). The [previous same-scan review](2026-11-17-graziani-cairo-rail-extension-review.md) leaves `E2329–E2428` unresolved. Blue transverse ticks identify printed rail; paired dark lines identify road. Broad river ink and the grid vertex obscure precise attribution at their meeting. Readings below concern printed ink, while pins report data only.

| Side | Scan observation | Pinned record |
| --- | --- | --- |
| `E2329–E2428` | River follows the diagonal toward the common vertex. Road and ticked rail approach the vertex closely; their exact crossing of this side remains uncertain. | `major-river`, `road` |
| `E2329–E2429` | The other side below the same vertex meets the converging road and rail ink; exact crossing attribution remains uncertain. | No literal record found. |
| `E2428–E2429` | River and eastward ticked branch meet the vertical side near its upper vertex; overlapping ink limits exact attribution there. | `minor-river`, `railroad` |
| `E2428–E2529` | Northbound ticked rail and paired road cross the diagonal above the river junction. | `railroad`, `road` |
| `E2429–E2529` | River and grid converge near the branch vertex; the scan does not securely assign a rail crossing to this side. | `minor-river` |
| `E2429–E2530` | Eastward ticked rail crosses the diagonal, away from the crowded junction. | `railroad` |

All five present records have `side: null` and `up: null`. A missing literal record is a data lookup result, not proof of absence on the map. Pinned river classifications are not independent source adjudications. A printed crossing does not establish bridge condition, service, occupancy, or a continuous usable route. Same-scan review is not independent-edition verification.

## Reproduction

The prior extension's [reproduction command and hashes](2026-11-17-graziani-cairo-rail-extension.md#reproduction) identify the PDF, crop, and pinned data used here. No runtime or map-data changes; no gameplay tests.

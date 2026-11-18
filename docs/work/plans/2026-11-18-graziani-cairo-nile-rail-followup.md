# Cairo northbound rail: bounded continuation

**Date:** November 18, 2026. **Disposition:** The northbound ticked branch crosses `E2428–E2529`. At the next grid vertex, rail ink and paired road lines overlap the grid; the scan does not securely assign the rail crossing. Stop there. The earlier Cairo river-vertex approach remains unresolved as described in the [junction review](2026-11-18-graziani-cairo-river-junction-review.md).

## Evidence

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), following the northbound branch from the labeled `2428` and `2529` hexes. Compared literal unordered endpoint pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). Blue transverse ticks mark printed rail; paired dark lines mark road. This is a scan reading and data comparison, not an adjudication of service or control.

| Side | Printed scan observation | Pinned record |
| --- | --- | --- |
| `E2428–E2529` | Ticked rail and paired road cross the diagonal above the river junction, as previously observed. | `railroad`, `road` |
| `E2529–E2628` | Ticked rail approaches and appears to pass the next three-hex vertex on this side, but grid, road and rail overlap close to the vertex. Exact attribution remains uncertain. | `railroad`, `road` |
| `E2528–E2529` | Meets that same vertex; no secure separate rail crossing can be assigned. | No literal record |
| `E2528–E2628` | Meets that same vertex; no secure separate rail crossing can be assigned. | No literal record |

Both present records have `side: null` and `up: null`. Missing literal records do not establish printed absence; pin entries do not independently establish a scan crossing. Printed ink does not establish bridge condition, service, occupancy, or a continuous usable route. No inference extends west of the original junction. Same-scan evidence is not independent-edition verification.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2500 -y 2400 -W 2200 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/cairo-nile-north.png"); im.crop((850,1000,1550,2200)).rotate(270,expand=True).save("/private/tmp/cairo-rail-wide.png")'
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-north.png /private/tmp/cairo-rail-wide.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`; rotated detail `59fa300231a8377f699c043c81ea4bd75f50da0fdb2d62285c311a115911e574` (rendering and image-library dependent); pinned data `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No runtime or map-data changes; no gameplay tests.

# Cairo northbound rail: independent same-scan review

**Date:** November 18, 2026. **Disposition:** Agree with the bounded [follow-up](2026-11-18-graziani-cairo-nile-rail-followup.md). Ticked rail and paired road cross `E2428–E2529`. At the next vertex, overlapping ink prevents a secure rail assignment; stop without extending the route.

Independently rendered page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) at 300 dpi and inspected the labeled grid. Compared literal unordered endpoint pairs in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json).

| Side | Independent scan check | Pinned record |
| --- | --- | --- |
| `E2428–E2529` | Blue ticked rail and paired dark road lines cross the diagonal away from the next vertex. | `railroad`, `road` |
| `E2529–E2628` | Rail approaches the vertex and appears to continue across this side, but grid, road and rail overlap there; exact side attribution remains uncertain. | `railroad`, `road` |
| `E2528–E2529` | Meets the crowded vertex; no secure separate rail crossing. | No literal record |
| `E2528–E2628` | Meets the crowded vertex; no secure separate rail crossing. | No literal record |

Both present records have `side: null` and `up: null`. A missing record does not prove printed absence, and a pinned feature does not settle ambiguous ink. Printed crossings do not establish bridge condition, service, occupancy, or a usable route. This is an independent inspection of the same scan, not independent-edition evidence. Earlier river-junction attribution remains unresolved.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 3750 -y 3600 -W 3300 -H 3300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-independent
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/cairo-nile-independent.png"); im.crop((1275,1500,2325,3300)).rotate(270,expand=True).save("/private/tmp/cairo-nile-independent-detail.png")'
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-nile-independent.png /private/tmp/cairo-nile-independent-detail.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop `29ea23ca1f7b46cb1482bd5f7d1ef6952948c67cf69c30d3e544db4d95f07835`; rotated detail `ffa4ae240cd0be66255586abd0d00fd210ff510c5d3dfd97b0cd96dc1d08f1e6` (renderer and image-library dependent); pinned data `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No runtime or map-data changes; no gameplay tests.

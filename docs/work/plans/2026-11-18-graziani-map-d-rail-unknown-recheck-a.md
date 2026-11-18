# Map D rail batch A: higher-resolution recheck

**Date:** November 18, 2026. A targeted rereading after [batch A](2026-11-18-graziani-map-d-rail-batch-a.md) and its [review](2026-11-18-graziani-map-d-rail-batch-a-review.md). The 600-dpi output displays the PDF's embedded 300-ppi image at double scale; it adds no independent source pixels, but avoids the information loss of the prior 200-dpi rendering and makes small centerline gaps easier to see. This is the same scan, not an independent edition. Findings below are a new informed reading, pending a separate review.

| Literal pin | Recheck and proposed disposition |
| --- | --- |
| `D3514|D3515` | Blue cross-tied line crosses the side interior. Short repeated gaps in the nearby centerline are visible between ties, consistent with the key's interrupted unfinished symbol. **Qualified source-positive** for the pinned class; numeral and grid overlap near crossing. |
| `D3514|D3613` | Blue cross-tied line crosses the diagonal away from vertices. Repeated separations along the nearby blue run support the unfinished class. **Qualified source-positive**; grid and tie crowd the crossing. |
| `D3611|D3711` | Blue line crosses the diagonal where black road, grid and brown ink overlap. The blue run beyond the intersection shows repeated small gaps even though a black road stroke obscures portions. **Qualified source-positive** for unfinished class using adjacent linework, not a gap demonstrated at the obscured intersection. |
| `D3710|D3711` | Local blue line shows interrupted segments consistent with unfinished class. However, road and grid crowd the crossing near the common vertex; this view does not remove the earlier review's exact-side attribution concern. **Unknown overall:** stronger class evidence does not by itself establish that the centerline crosses this exact side. |

The key distinguishes interrupted blue centerline from continuous centerline. Gaps are interpreted from repeated segments of the same local symbol rather than a single soft pixel or a cross-tie. Scan softness and overlaps qualify all readings. No assertion follows about construction, usability, route legality or separately pinned road features.

Reproduce the enlarged same-scan crop (coordinates in unrotated 600-dpi output):

```sh
pdftoppm -f 4 -l 4 -r 600 -x 3700 -y 8550 -W 2000 -H 2100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-a-600
```

[Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1, PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `23bdb7a196594a94eb9f5107cbb91b257221aff4c4e7f5c572a1ad011a4b82aa`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop bytes depend on renderer. Documentation only; no data or runtime edits and no gameplay tests.

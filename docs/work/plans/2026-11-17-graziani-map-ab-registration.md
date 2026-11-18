# El Abiar A/B local grid registration pilot

**Result:** A coarse four-vertex fit is consistent with the proposed local overlap A4934 ↔ B4901. At 300 dpi the estimated corresponding grid vertices align within roughly 3 image pixels after translation. The overprinted town marks do **not** align as closely: estimated centers differ by roughly 36 pixels (about 19% of a hex's approximately 190-pixel width) after that grid translation. This is a local visual registration pilot, not proof of legal cell identity, transport continuity, accuracy of other rows, or a source correction.

## Method and measurements

[Map scan](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, verified before this check. Rendered Map A PDF p. 1 and Map B PDF p. 2 at 300 dpi using the exact crop commands in [independent identity review](2026-11-17-graziani-map-ab-el-abiar-identity-review.md). Coordinates below are relative to each output crop, not full-page coordinates. Numbers are manually estimated grid-line junction centers, about ±3 px, not automated surveyed observations; overlapping stroke widths and scan distortion limit precision. Paired vertices assume the proposed candidate identity rather than establishing it independently.

| Visually paired vertex of candidate cell | A crop `(x,y)` | B crop `(x,y)` |
|---|---:|---:|
| upper | (1078, 234) | (145, 234) |
| upper right | (1172, 287) | (240, 287) |
| lower right | (1172, 393) | (240, 396) |
| lower | (1078, 447) | (145, 447) |

Fit a **translation only**, using the arithmetic mean of B-minus-A coordinates: `(-932.5, +0.75)` pixels. Residual vectors, in table order, are approximately `(-0.5,-0.75)`, `(+0.5,-0.75)`, `(+0.5,+2.25)`, `(-0.5,-0.75)` px; RMS Euclidean residual ≈1.4 px and maximum ≈2.3 px. These small computed values are below the point-picking uncertainty, so report only agreement within roughly 3 px, not subpixel accuracy. Points are four vertices from one cell, with no independent holdout, so fit says nothing quantitative about the whole seam or absolute map coordinates. Regular congruent hex grids can fit unrelated cells under a lattice translation; even held-out grid vertices alone cannot identify the correct discrete lattice shift. Distinctive, reliably matched landmarks are needed to discriminate shifts. Rotation/scale were not estimated.

The dark blue El Abiar town-mark center, partly obscured by crossing lines, was visually estimated at A `(1067,344)` and B `(170,345)` (rough uncertainty ±5 px per image). Applying the grid translation predicts B around `(134.5,344.75)`, leaving an observed B-minus-predicted difference about `(+35.5,+0.25)` px, or roughly 36 px total. Town ink and terrain are redrawn/overprinted, so this is not a precise survey of geographic coordinates and does not prove that the places differ; it shows that perfect alignment of named marks cannot be asserted from the grid fit. Intersecting routes also occupy different visible positions relative to the cell, and require separate side-by-side analysis before any feature continuity claim.

The left side of B's candidate cell is clipped, and both candidate cell numbers are obscured. Adjacent label sequence and the repeated town name support the candidate; they do not establish the printed rule's legal interpretation. In particular, Land §4.1's wording and known chart contradictions remain outstanding. Fitting neighboring grid junctions is geometric evidence conditional on chosen correspondence, not an authoritative decision to collapse the cells or admit a movement edge.

## Reproduction of arithmetic

```text
A = [(1078,234), (1172,287), (1172,393), (1078,447)]
B = [(145,234), (240,287), (240,396), (145,447)]
translation = mean(B[i] - A[i])
residual[i] = B[i] - (A[i] + translation)
rms = sqrt(mean(residual_x² + residual_y²))
```

A stronger follow-up would independently pick vertices across several rows with held-out points to assess scale, rotation, skew and residuals, then use distinctive landmarks to discriminate lattice shifts before studying each side feature. No map data, geometry, route, test or readiness flag changed.

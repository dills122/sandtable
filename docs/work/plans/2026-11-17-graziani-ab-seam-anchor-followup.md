# A/B seam: geographically separated anchor follow-up

**Date:** November 17, 2026. Reinspection finds two additional distinctive
edge motifs, separated from El Abiar, consistent with the existing 33-column
candidate. They constrain a local geographic reading more than a fit of
repeated grid vertices does. They do not establish a legal alias, a connected
track, a corrected rule, or complete seam validity. These are observations of
one scanned edition, not independent editions or an independent review.

## Source and method

Inspected [original map sheets](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF
pp. 1–2, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
This check follows the [El Abiar fit](2026-11-17-graziani-map-ab-registration.md)
and was not blind. Overview at 100 dpi, then 300-dpi crops below. Coordinates
are crop pixels; the crop origins are the same in page y, but not in x. Row
counts below are differences in printed row numbers, **not** surveyed distances,
travel distances, or proof of legal adjacency. Features and text overlap, and
B's western edge clips some cells and leaves an unlabeled sliver west of row 26's complete `2601`.

```sh
pdftoppm -f 1 -l 1 -r 300 -x 6000 -y 5600 -W 650 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/ab-detail-a
pdftoppm -f 2 -l 2 -r 300 -x 0 -y 5600 -W 650 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/ab-detail-b
```

## Observations

| Anchor | Map A | Map B | Constraint and limit |
| --- | --- | --- | --- |
| El Abiar | Town in outer candidate `4934`, east of `4933`. | Same name and town in clipped candidate `4901`, west of `4902`. | Common named locality; labels are sequence-based where obscured. Previous fit found town centers offset about 36 px after chosen grid translation. |
| Agadabia–Giuf el-Matar direction, row 26 | Eastward dashed line from the Agadabia direction traverses distinctive circular-pattern terrain in `2632` and outer `2633`, ending at sheet edge. | Dashed line enters an unlabeled clipped sliver in matching circular-pattern terrain, crosses into complete `2601`, and continues through `2602` toward labeled Giuf el-Matar. | About 23 printed rows south of El Abiar. Track and terrain form a geographically distinctive correspondence, not a separately duplicated place label. Under the candidate, A2633 neighbors B2601; they are not aliases. The two exposed line portions are about y=516 and y=500 px in their respective crops, a rough 16 px difference at 300 dpi, subject to stroke thickness, slant and scan distortion. These are different horizontal positions: this compares crop heights, not a fitted residual, overlay, or evidence of crossing. |
| Escarpment and dashed-line motif, row 21 | At outer `2134`, diagonal dashed line meets broad brown escarpment on its upper-left boundary; the line approaches from the El Hosaiet direction. | Clipped `2101` shows a similar broad brown band and diagonal dashed line at its northwest edge; line continues northeast through `2201` toward `2302`. | About 28 printed rows south of El Abiar, five south of row 26. Consistent with candidate A2134/B2101 overlap; clipped and partly overprinted edges prevent full-cell or side-feature equivalence. |

The row-26 line is associated with different named places on opposite sheets;
this makes the combined landscape more specific than a generic horizontal
stroke, but neither name is repeated at that junction. Row-21 motif combines
line and escarpment geometry rather than a second named common point. Both
readings use artwork that may be independently redrawn and cannot certify a
continuous route. No separate landmark survey, exhaustive alternative search, or held-out transform was fitted. The [independent review](2026-11-17-graziani-ab-seam-anchor-followup-review.md) corrected the initial mistaken description of B2601 as clipped; its western sliver has no readable ID.

## Alternatives and disposition

The 33-column candidate places A odd-row `xx34` over B `xx01` and
A even-row outer `xx33` next to B `xx01`; it gives plausible placements to
these features and El Abiar over a span of 28 printed rows. A neighboring
whole-column shift could still align the hex grid, but would place these
observed motifs against different cells; it needs its own matched landmark
and linework explanation, not a grid-residual comparison. Redrawn or duplicated
art, clipping, and sheet assembly may create apparent alignment without
establishing intended cell identity. No measurement here excludes all
translations, rotation or deformation, or proves registration in intervening
rows.

Land §4.1's `01xx`/`39xx` wording and distance-chart discrepancies remain
unreconciled; see [geometry comparison](2026-11-17-graziani-seam-geometry-comparison.md)
and [distance decision](2026-11-17-graziani-map-distance-decision.md). Keep
cross-sheet identities and crossings unknown under existing bounded policy.
Next: independent high-resolution reading of these motifs, landmarks with
visible labels on both sheets if available, quantitative multi-point fit with
held-out features, and authoritative rule/errata interpretation. No runtime,
map data, route, test, or readiness flag changed.

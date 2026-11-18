# Distance chart: disjoint inconsistency bounds

**Finding:** Four triangle-inequality violations have no chart cell in common
and use no asterisk-marked major-city location. If the chart is a single
shortest-distance metric with these fixed endpoints and the transcription is
correct, at least **four distinct cells** must change to satisfy just these
four constraints. This does not identify the erroneous cells, establish their
correct values, or certify map geometry. The source defines distances excluding
the starting hex and including the destination, and assumes sea hexes where
needed; those conventions do not remove a shortest-path triangle inequality.

| Three locations | Printed pair values | Inconsistency |
| --- | --- | --- |
| El Agheila, C2803, Fort Maddalena | 57, 75, 17 | 75 > 57 + 17 = 74 |
| Agedabia, Ben Gania, Bardia | 24, 67, 42 | 67 > 24 + 42 = 66 |
| Soluch, Ben Gania, Wadi Natrun | 20, 130, 109 | 130 > 20 + 109 = 129 |
| Barce, Ben Gania, Bir-el-Qatrani | 20, 80, 58 | 80 > 20 + 58 = 78 |

Within each row, the values are respectively first–second, first–third and
second–third. The three pair cells for every row are distinct from those of
every other row. A correction to a cell can therefore touch at most one of
these four violations. The bound concerns **cell changes under a fixed
metric interpretation**, not the number of historical mistakes: a single
wrong location, convention or systematic transcription issue could cause
multiple cell inconsistencies. It also does not mean four changes suffice.

For a broader conditional analysis of the 24 recorded violating triangles,
an exhaustive hitting-set search found a seven-cell set intersecting every
violation, and seven cell-disjoint violating triangles prove that fewer than
seven cannot hit all 24. Three of those seven triangles include Alexandria or
Cairo, whose chart headings carry a major-city asterisk. Therefore the
seven-cell lower bound additionally assumes a single consistent endpoint for
each starred location across chart cells. Even under that assumption, hitting
every currently violated triangle is **necessary, not sufficient**, for a
valid corrected metric: different replacement values may cause other
violations. The four non-starred rows above provide a narrower independently
checkable lower bound without that starred-location assumption.

Computed from [independent visual transcription](graziani-distance-independent.json)
of the same [Air rules chart](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
PDF p. 25 §37.42, SHA-256
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`.
The prior [Barce analysis](2026-11-17-graziani-distance-discrepancy-detail.md)
and source review independently checked that row's six shorter two-leg sums.
This computation used an existing transcription and is not a new visual
reading or separate edition. Recheck row/column labels and values before using
any proposed correction. No source value, runtime, map data, route or readiness
flag changed. An [independent source and computation review](2026-11-17-graziani-distance-disjoint-violations-review.md)
checked the four non-starred rows and reproduced the broader bound.

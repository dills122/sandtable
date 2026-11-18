# Air distance example: separate local consistency check

**Date:** November 18, 2026. The [24-triangle audit](2026-11-17-graziani-distance-transcription.md) covers the 20 named locations and 190 cells of Air §37.42's **p. 25 matrix**. It does not include the distinct `E2629`–`E3425` zero transition or Ismailia in the **p. 24 worked example**. A same-sheet Map E grid check supplies an additional, local inconsistency under the ordinary adjacent-hex counting convention; it does not identify a corrected source or certify a seam.

## What the existing audit covers

The [independent transcription](graziani-distance-independent.json) contains 20 chart labels, including Agedabia `A2629`, but **not** `E2629`, `E3425`, `E3433` or Ismailia. Its 24 violations are inequalities between matrix cells, found from the 1,140 triples of those 20 labels. Therefore the zero term in Example 2 was not one of those 24 matrix triples. The [later registration reading](2026-11-18-graziani-map-registration-source-next.md) and [review](2026-11-18-graziani-map-registration-source-next-review.md) already identify the zero phrase and its different-cell problem; this note adds the bounded local count below, not a claim that the issue was previously unknown.

## Local geometric implication

Air PDF p. 24 literally prints four 33-hex terms to `E2629`, followed by **zero** northwest to `E3425`, then eight to `E3433` and nine to Ismailia, giving 149. Map E visibly prints `2629` and `3425` in distinct cells. In the local grid, the following sequence follows eight shared sides northwest:

`E2629 → E2729 → E2828 → E2928 → E3027 → E3127 → E3226 → E3326 → E3425`

Labels and grid in this strip are visible with route/river overprint on some cells. Under the regular same-sheet adjacent-hex convention, each step changes the first two-digit component by at most one; its difference here is `34 − 26 = 8`. Thus the displayed sequence both meets that lower bound and gives **eight local geometric steps**. This is a conditional same-sheet counting result, not a land route: it does not require traversable edges, and rivers or road marks are not classified. A separately hypothesized nonlocal transition or different source meaning of the worked term is not excluded by the local grid alone.

If the printed `E2629` and `E3425` are intended as successive physical positions counted by that local convention, zero is incompatible with that leg. The source does not determine whether the number, either coordinate, the intended operation, or some other part should be corrected. Substituting eight gives arithmetic `132 + 8 + 8 + 9 = 157`, **only as a counterfactual sum**, not a published correction, proven end-to-end shortest distance, or proof that all four 33-hex transitions form a legal registered path. The matrix's 24 triangle violations independently show that some chart values cannot all be one shortest-path metric; this local example cannot identify which matrix values are wrong and does not raise the matrix-triple count to 25.

The p. 24 §36.53 table separately lists Ismailia off map at entry/exit `E3433`, distance nine. It corroborates that one air leg but not a ground connection. Land §4.1's different printed overlay notation remains unresolved by this same-sheet count.

## Reproduction and limits

```sh
pdftoppm -f 5 -l 5 -r 300 -x 4500 -y 4500 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/registration-delta-review
```

Visually inspected [Air/Logistics](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf) PDF pp. 24–25 and [Map E](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 5. Their SHA-256 values are `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` and `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, verified in the [example review](2026-11-18-graziani-map-registration-source-next-review.md). Same source scans, not independent editions. No full chart retranscription, complete seam check, exhaustive errata search, runtime/data changes or gameplay tests.

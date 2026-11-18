# D/E coastal seam: grid-registered anchor batch

**Date:** November 17, 2026. **Disposition:** overlap is a strong visual candidate, not a verified alias or traversable seam. No Matruh–Alexandria road or rail route is established. This informed reinspection uses the same scan as the [seam review](2026-11-17-graziani-continuation-de-seam-review.md), not an independent edition.

## Source and registration test

Inspected [original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), pages 4 and 5, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. North is up in these renders. At 350 dpi, D3134 has vertices approximately `(1525,1118)`, `(1635,1055)`, `(1745,1118)`, `(1745,1240)`, `(1635,1300)` and `(1525,1240)` in the D crop below. E3101 is clipped at the page image's left margin; its visible top and right vertices are approximately `(175,1045)`, `(285,1107)` and `(285,1230)` in the E crop. Translating D by approximately `(-1460,-10)` aligns these vertices with the candidate E3101 grid, without using coast or transport marks to choose the transform. Coordinates are visual estimates, not a fitted survey; no rotation or scaling was applied.

Under that test registration:

- D's road pair approaches through D3133, crosses its side into D3134, bends gently and stops short of D3134's right vertical edge. E's corresponding coastal road pair enters the visible part of E3101 and continues across the right vertical edge into E3102. The two pairs lie close but are visibly offset in a 50% overlay; this is approximate shape and placement agreement, not an exact match or a witnessed D-to-E crossing.
- D's blue cross-tied railway crosses D3133 into D3134 and meets its lower-right sloping side near the sheet's unprinted area. On E, railway enters the visible E3101 portion and crosses the corresponding lower-right side into E3001, then bends toward El Alamein. The feature positions and southeast trend are close under the grid registration, but strokes and cross-ties do not coincide exactly; their independent identity remains unproved. D's clipped continuation cannot itself demonstrate the E-side edge.
- The coast passes through the corresponding upper grid area on both images, but its bends are visibly displaced under the same transform. Yellow coastal markings are also suggestive but do not provide a uniquely identified shared landmark. El Daba and Sidi Abd el Rahman on D and El Alamein and El Hamman on E are different labels; no common named point is visible in these cropped coastal cells.

Grid agreement plus similar feature ordering strengthens the 33-column overlap hypothesis, but imperfect feature registration, E's missing western portion, and the unresolved printed-rules/geometry conflict prevent asserting D3134 = E3101 or adding D3133–E3101 as an authoritative edge. Even a confirmed geometric seam would not alone establish complete road/rail coverage, service or movement legality through Alexandria. Inspect an independent full-edge scan or physical sheets, verify shared distinctive features and partial-cell boundaries, and reconcile Land §4.1 before promotion.

## Reproduction

```sh
pdftoppm -f 4 -l 4 -r 350 -x 5750 -y 5000 -W 2000 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/de-d-anchor
pdftoppm -f 5 -l 5 -r 350 -x 0 -y 5000 -W 2200 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/de-e-anchor
```

D and E crop SHA-256 respectively `4906197b106baf21a7aab558958cdb8a0189fbe4fd9498785ce43ecf0dc7c376` and `3605382d52682057cd28e43caf6758610f677c76abdb4637fda504cd0b7f42c2`. For the visual overlay, crop D `(1525,1000,1745,1300)` and E `(65,990,285,1290)` and blend at equal opacity; these PIL crop rectangles use exclusive right/bottom edges. The composite is a diagnostic, not evidence of independent source or exact coincidence. No runtime or map data changes; no gameplay tests run.

# Maaten Baggush: D3520 dot containment recheck

**Result:** On the retained Map D scan, the blue-green dot associated with **Maaten Baggush** lies **inside D3520**, above its lower-right diagonal and its lower grid vertex. This addresses the locality-dot containment question, not every route junction, the scenario facility symbol or a terrain classification. The dot overlaps a dark dashed lead; those separate marks should not be collapsed into one route or boundary conclusion.

In the 300-dpi crop, printed `3520` identifies the target cell. `3519` lies to its left, `3521` to its right and `3420` below-right in the displayed orientation. The target's lower boundary joins at the junction below and slightly left of the dot; its lower-right diagonal runs from that vertex toward the `3521` side. The round mark is visibly above that diagonal and inside the numbered cell, although the adjoining dashed stroke touches it and the name extends across the grid. The map key calls the blue round symbol **Village/Bir**; it does not choose between those two types, establish a landing strip or decide whether any covered feature exists under the overprint. No full interior or side-feature classification follows.

The [first reading](2026-11-18-graziani-landing-strip-edges-a.md) and [review](2026-11-18-graziani-landing-strip-edges-a-review.md) appropriately noted the dot near the lower junction and left several adjoining route/side assignments provisional. This reread narrows **dot containment only**: it does not resolve which lines cross `D3420`, `D3419` or `D3519`, coastline classification, a route, ownership, facility capacity or scenario placement. The scenario's Matten spelling and coordinate are carried from that source work, not freshly re-read here.

## Reproduction and limits

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) p. 4; p. 1 terrain key; PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. PDF image listing reports the page's underlying image at **300 × 300 ppi**; the crop uses that native stated resolution, not an assertion that enlargement creates new source detail:

```sh
pdftoppm -f 4 -l 4 -r 300 -x 3172 -y 4204 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/baggush-native
```

Crop SHA-256 `55d5bc8b4996a45aa8dae6eddd1e316469c7ee6e7609ae53fd0472747c6f51a8`, matching the first reading's renderer-dependent crop. Informed rereading of the same scan, not a new edition or independently verified physical sheet. No runtime/data edits or gameplay tests.

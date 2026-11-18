# Map D/E coastal seam: continuation candidate

**Date:** November 17, 2026. **Status:** bounded, informed reading of one scan; not an independent edition, verified identity, or admitted route. Prior [Map D east reading](2026-11-17-graziani-continuation-map-east.md) and [D/E edge transcription](2026-11-17-graziani-seam-de-independent.md) were known before inspection. Both sheets are from the [original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 4 and 5; SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. The page 1 key identifies road as paired dark lines and railroad as blue cross-tied line. North is up in the displayed crops.

## Observations

- On D, Sidi Abd el Rahman is printed by the coastal dot in D3232. Coast bends southeast past yellow coastal markings in D3233 toward D3134. Paired road and blue railroad cross D3133–D3134, as reported in the east reading; their strokes continue into D3134 and stop at or near the printed sheet boundary. Their exact termination relative to the boundary is not clear enough to infer another crossing.
- On E, the western margin cuts into the coast and the paired road and blue railway at E3101 vicinity. Eastward, coast bends around a bay; road remains close to shore while railroad bends south toward El Alamein, printed near E3001/E3002. Farther east the label El Hamman appears near E3006/E3007. These are separate visual anchors, not proof of continuous service from D.
- Under the previously discussed **33-column candidate**, D3134 and E3101 occupy the same candidate position, while D3133 would neighbor E3101. The D3133–D3134 visual crossing could therefore correspond to a candidate D3133–E3101 connection, but only if the overlap, registration, and features on both copies are established. D3134 is a printed overlap cell, not evidence that D3134 and E3101 are adjacent distinct cells. Retain both printed labels in any further review.
- Similar coastal strip, road, and railway occur in both candidate areas, but their apparent curves and positions are not an exact visual registration in these crops. Sidi Abd el Rahman and El Alamein are different labels at different positions; do not use them as identical-place anchors. No measured overlay or full partial-cell reconciliation was done. The prior [geometry comparison](2026-11-17-graziani-seam-geometry-comparison.md) records unresolved conflict between printed Land §4.1 wording and candidate geometry; this reading does not resolve it.

## Reproduction and limits

With the PDF at `/private/tmp/sandtable-maps.pdf`, render:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 8750 -y 7850 -W 2300 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-east-next
pdftoppm -f 5 -l 5 -r 500 -x 0 -y 7700 -W 3000 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-e-west
```

Crop SHA-256 values respectively `4d7fd50c901eccdd81acb251491f6d5d12842dc74e3550777dca566a387a1f35` and `8585e70fe37cabefb4e5a9a3a845faf3a49848bf332a35dae464c00b3011f855`; renderer differences may change PNG bytes. The E crop cuts its western cells and omits Alexandria. Compare grid vertices, coastline and each road and railroad stroke on a registered overlay and inspect intervening sides before considering any identity or crossing. Connection to Alexandria, complete route, terrain and feature completeness, and traversal legality remain unproved. Similarity alone does not authorize a data correction, alias, seam edge, or road/rail service. No runtime or map data changed; no gameplay tests run.

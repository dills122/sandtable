# Map D D3516–D3517 railway crossing

**Finding:** Original Map D shows a blue cross-tied railway crossing the shared
D3516–D3517 hexside. The pinned side-feature file has no record for that pair.
This is a candidate missing railway feature, not a verified clear edge or a
claim that the whole railway is usable or connected.

## Original scan and geometry

[Original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D
PDF p. 4 and terrain key PDF p. 1, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
The original key distinguishes blue cross-tied railroad from solid dark
parallel road and separated dark track dashes. North is up in the Poppler
rendering. The D3516 label is in the left/lower coastal cell containing the
Gerawla dot; D3517 is the adjacent cell to its right. Their shared side is the
near-vertical grid line about `x=580`, `y=450–740` in the crop below. The
blue centerline and cross-ties visibly pass through that side near
`(580,580)`, well below the crowded northern vertex. Coordinates are
approximate pixels, not georeferenced measurements.

```sh
pdftoppm -f 4 -l 4 -r 800 -x 7500 -y 11800 -W 1300 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/d3615east800
```

Local PNG SHA-256
`7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`.
Renderers may differ; PDF hash and coordinates identify the source. The crop
stays outside Git pending rights. The broader 500-dpi context and command in
[Fourth Indian map check](2026-11-17-graziani-fourth-indian-edges-next.md)
show Mersa Matruh and adjacent printed labels.

## Dataset comparison and limits

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`
contains no `D3516|D3517` record. The pinned file was known before
inspection; this is an informed source comparison, not blind transcription. An
absent record means unrepresented, not verified featureless. The crop gives
positive evidence for the railway on this one side only.

The road and coastal line run close to the shared side's **northern vertex**,
where overlapping brown grid, black road and coastline make exact road-side
membership a separate question. This review does not assert that the road
crosses D3516–D3517 or that the side has no road. Nor does it certify its
coastal passability, ZOC, actual unit placement, a legal move, a complete
railroad network or a live service; construction status and §8.47 require
separate checks. The adjacent junction is analyzed separately in
[D3615 eastern junction note](2026-11-17-graziani-d3615-east-junction-next.md).

## Conditional checks

1. Independently trace the grid line and blue centerline at the stated
   crossing; compare to the original key and wider crop without relying on the
   dataset. The side has a visible line crossing, not merely a blue tie near a
   vertex.
2. Distinguish absent feature record from verified clear. If an independent
   review accepts the reading, propose a future versioned railway feature with
   provenance, inspect adjacent connectivity and evaluate replay impact before
   admitting it. This note makes no JSON or runtime edit.
3. Keep road membership and other features unknown until separately checked;
   confirm rail movement phase, ownership, construction, occupancy and route
   rules independently before treating this one side as traversable.

No gameplay tests or complete map certification.

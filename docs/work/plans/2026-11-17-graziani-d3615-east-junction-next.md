# Map D D3615 eastern junction: bounded source check

**Finding:** Original Map D visibly shows a road crossing D3615–D3616. Its
blue cross-tied railway appears to cross the adjacent **D3615–D3516 diagonal**
instead of the D3615–D3616 vertical side where the pinned dataset places it.
The three-way junction is heavily overprinted; treat this as a candidate data
mismatch needing independent review, not a certified correction or route.

## Locators and geometry

Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D PDF
p. 4 and terrain key p. 1, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Key identifies the road as dark parallel lines, railroad as blue line with
cross-ties, and track as heavy separated dark dashes. North is up in the
Poppler rendering. The labels D3615, D3616, D3516, neighboring D3715 and
D3716, and surrounding coastline establish the local sides. D3615 lies left
of D3616; D3516 lies below their shared vertex. The three sides meet near
`(330,310)` in the 800-dpi crop below; the vertical D3615–D3616 side extends
upward from that vertex. D3615–D3516 runs down-left from it; D3516–D3616
runs down-right. Coordinates are approximate pixels in the crop, not surveyed
positions.

```sh
pdftoppm -f 4 -l 4 -r 800 -x 7500 -y 11800 -W 1300 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/d3615east800
```

Crop SHA-256 `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`.
For wider context, the earlier 500-dpi crop and reproduction command in
[Fourth Indian source check](2026-11-17-graziani-fourth-indian-edges-next.md)
show all cell labels and the road/rail approach from Matruh. Renderer differences
may alter PNG bytes; the PDF hash and crop coordinates identify the source.
Crops remain outside Git pending rights.

## Observed sides versus pinned data

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
The existing data was known before this scan, so this is an informed visual
check, not a blind transcription.

| Shared side | Source observation | Pinned record and limit |
| --- | --- | --- |
| D3615–D3616 | Dark parallel road lines cross the vertical boundary around `(330,210–240)`, above the junction. At the north end, coastline, a yellow mark and numerals partially overlap ink; the rest of the side is visible. The continuous blue rail centerline travels farther south and is not visibly crossing this vertical side. | `road`, `railroad`. Road crossing is supported; the `railroad` component is **not independently confirmed** and appears inconsistent with the visible rail's intersection with the neighboring diagonal. Road and ink overlap limit an absolute absence claim. |
| D3516–D3615 | Blue cross-tied line from D3615 appears to cross the diagonal near `(255–290,330–355)`, just down-left of the junction; it continues southeast through D3516. The black track/road junction, shoreline and side line are crowded at the intersection. | `track` only. A railway feature is a **candidate omission**, subject to independent visual reading of the overprinted diagonal and blue line. Do not infer every feature or both track endpoints from this observation. |
| D3516–D3616 | The opposite diagonal leaves the same vertex down-right; road and heavy track marks overlap its upper portion near the vertex. | `track`. No clean, source-certified intersection is claimed here; a line touching a vertex does not alone prove which of the three sides it crosses. |

The blue line and separate shorter blue branch in the broader crop overlap at
D3615; do not confuse their junction with proof of connected, finished rail on
every adjacent side. The map key and scan identify visible marks only; railway
operability and unfinished sections need §8.47, scenario construction state,
other sides and movement rules. Coastal terrain, water/land classification,
passability, occupancy, ZOC, legal movement and full network connectivity are
not decided by this local check.

## Review vectors

1. Re-open the same 800-dpi crop and trace the vertical grid side and blue
   centerline independently, without consulting the pinned features. Distinguish
   the road's crossing from the blue rail's apparent diagonal crossing.
2. If the diagonal crossing is confirmed, compare the pair of records:
   D3615–D3616 has pinned road+rail, D3516–D3615 has pinned track only. Record
   any correction in a future versioned data change, with source-review and
   replay impact; this note does not alter JSON.
3. For either side, keep overlapping vertex/track and coastal classifications
   unknown until independently resolved. A visible road crossing alone is not
   a certified complete movement route or railway service.

No runtime edits, source admission or gameplay tests.

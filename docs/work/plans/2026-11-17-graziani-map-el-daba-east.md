# Map D: El Daba east road and rail observations

**Finding:** Original Map D appears to show both road and railway crossing
D3231–D3232, while the pinned record lists railway alone. This is a candidate
missing road feature, not an admitted data correction. The adjacent
D3231–D3331 railway record also needs independent review: its apparent mark
may be a cross-tie at a grid vertex rather than a railway centerline crossing.
Neither finding establishes a legal or complete route.

## Evidence and reproduction

[Original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), Map D
PDF page 4, map key on page 1; SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Hash verified against local source before inspection. Key and preceding
[strip check](2026-11-17-graziani-fuka-east-rail-batch-next.md) identify
parallel dark lines as road and blue line with cross-ties as railway.
El Daba and Sidi Abd el Rahman, printed cells 3330, 3331, 3231, 3232,
and the hex grid locate these observations. Source data was known before
inspection; this is informed comparison, not blind transcription.

```sh
pdftoppm -f 4 -l 4 -r 500 -x 8800 -y 7600 -W 2800 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-map-next/east
```

PNG SHA-256 `51a54dad5911bcf12fb219750265ff90ee0e26d1397e02943bbaf163383c73d7`;
Poppler clips this request at the page edge to 2349 × 1400 pixels. Crop remains
outside Git. Renderer versions can change image bytes. Coordinates below are
approximate pixels within this crop, not surveyed measurements.

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

| Side | Visible source evidence | Pinned record and qualification |
| --- | --- | --- |
| D3231–D3232 | Vertical side at x≈1124; parallel dark road strokes cross around y≈910–940, below upper vertex at y≈865. Blue railway centerline crosses around y≈950. Both continue on either side. | `railroad` only. Road is a candidate omission. Dark strokes partly overlap numeral 3232; independent source check needed. |
| D3230–D3231 | Vertical side at x≈810; blue centerline crosses near y≈900, immediately below upper vertex. | `railroad`, consistent with visible centerline. Vertex and blue tie overlay limit precision. |
| D3231–D3331 | Diagonal side from approximately (650, 780) to (810, 870); dark road crosses it. Blue railway centerline appears below this side and crosses D3230–D3231 just below their shared vertex; a blue tie overlaps the vertex. | `road`, `railroad`. Source does not clearly support a separate centerline crossing of this diagonal; vertex interpretation and scan registration need independent review before changing it. |
| D3232–D3133 | Road and blue rail bend southeast across the diagonal side west of 3133. | `road`, `railroad`, consistent with visible marks; line and grid overprint complicate exact intersection. |

No absence of other terrain or side features is asserted. The map marks alone
do not establish construction, ownership, rail or road movement eligibility,
ZOC, weather, connectivity beyond this crop, or seam geometry. A vertex-touch
policy is a project interpretation and is not supplied by this observation.

## Follow-up

Independently reproduce the crop and verify labels, road strokes, railway
centerline versus cross-tie, and crossing positions. If accepted, assess a
versioned historical-map data correction, provenance, replay effects, and
focused map/route consumer tests. Keep missing-feature and uncertain-vertex
cases fail-closed until their semantics and source coverage are defined. No
runtime data, readiness flag, or gameplay test changed in this research.

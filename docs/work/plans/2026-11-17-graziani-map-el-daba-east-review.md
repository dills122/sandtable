# El Daba east map observations: independent check

**Disposition:** corroborate the [source comparison](2026-11-17-graziani-map-el-daba-east.md): D3231–D3232 visibly carries road and railway, whereas the pinned record contains only railway. The railway centerline appears to cross D3230–D3231 just below its shared vertex with D3231–D3331; a blue cross-tie overlaps the vertex. This does not clearly establish a separate railway centerline crossing of the diagonal D3231–D3331, which the pinned data also marks as railway. Both remain candidate data issues, not admitted corrections or proof of traversable routes.

## Independent visual reading

Inspected the [original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 (Map D), at 600 dpi using a separately rendered crop. Labels 3230, 3231, 3232, 3331 and 3133, together with El Daba and Sidi Abd el Rahman, establish the local cells. Compared the rendered source to the other note and pinned records; this is informed, same-scan review, not a blind transcription or an independent edition.

| Side | Reading | Pinned data |
| --- | --- | --- |
| D3231–D3232 | Two dark parallel road strokes and the blue railway centerline visibly cross the vertical boundary below its upper vertex. Road continues on both sides; the 3232 label overlaps the line just east of the boundary. | Railway only; candidate missing road. |
| D3230–D3231 | Blue railway centerline meets the vertical boundary immediately below its upper vertex; a tie and grid line overlap nearby. | Railway, consistent with this reading. |
| D3231–D3331 | Dark paired road strokes cross the diagonal. The blue centerline runs below the diagonal at its eastern end and reaches the adjoining vertical side just below the vertex; a blue transverse tie extends toward or over the vertex. Exact contact is affected by printing and scan registration. | Road and railway; diagonal railway crossing remains unproved, not certified absent under every vertex policy. |
| D3232–D3133 | Road and blue railway continue across the diagonal west of 3133, with line and grid overlap. | Road and railway, consistent with this reading. |

No other features, continuity outside this bounded view, or legal route are certified. A rule for a centerline touching a vertex and treatment of uncertain features require a separate project decision; map marks alone do not settle them. Before any correction, assess map-data versioning, provenance, replay compatibility, and focused map and route-consumer tests. Unknown terrain, control, ZOC and coverage remain separate gates.

## Reproduction and provenance

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
pdftoppm -f 4 -l 4 -r 600 -x 10400 -y 9000 -W 2800 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-el-daba-review
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. The map hash matches the recorded package; it does not establish rights. Crop remains temporary. No runtime data, readiness flags or gameplay tests changed.

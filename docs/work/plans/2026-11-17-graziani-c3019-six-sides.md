# Fort Maddalena C3019: six printed sides

**Finding:** Original Map C shows three track-marked sides around the Fort Maddalena hex and blue checkered ink on its other three sides. The three track sides agree with the explicit pinned feature entries. Blue checks are a strong national-boundary interpretation from regional context, but do not directly match the key's gray `Border` sample; the pinned side file has no explicit entry for those three pairs. Ink obscures portions, so these are local observations, not six complete clear-side or legal-movement certifications.

## Six sides

Visually inspected [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map C, and p. 1 terrain key. The blue Fort Maddalenna dot sits in the hex identified by printed `3019` and corroborated by scenario `C3019`; name and track overlap some label/line ink. Neighbor identities below follow the visible grid and printed neighboring labels. Directions describe the displayed crop, not a separate rules coordinate system.

| Neighbor | Crop side | Source observation | Pinned side entry |
| --- | --- | --- | --- |
| `C3119` | upper left diagonal | Dark single dashed track crosses the side; dash covers the crossing and part of the grid. | `track` |
| `C3120` | upper right diagonal | Alternating blue checks follow the side; no dark track crossing is visible. Checks obscure underlying grid. | No explicit pair entry found |
| `C3020` | right vertical | Blue checks follow the side; no dark track crossing visible. The mark covers the underlying grid. | No explicit pair entry found |
| `C2920` | lower right diagonal | Blue checks follow the side; no dark track crossing visible. Checks obscure underlying grid. | No explicit pair entry found |
| `C2919` | lower left diagonal | Dark single dashed track crosses the side; dash overlaps the grid at the crossing. | `track` |
| `C3018` | left vertical | Dark single dashed track crosses the side; printed name/number and track ink overprint part of the line. | `track` |

Key's single dark dashes identify track; its `Border` illustration is a gray side and adjoining rays, not blue checks. Country-label continuity supports interpreting the blue checks as Libya–Egypt boundary, as documented in [boundary evidence](2026-11-17-graziani-border-mark-next.md) and [review](2026-11-17-graziani-border-mark-review.md). No explicit pair entry is a statement about this sparse pinned file, not proof that the map side has no feature or that a boundary has no rules effect. Separately inspect any coincident terrain, unit entry restrictions and ZOC qualifications; absence of an obvious escarpment band in visible portions does not certify features beneath overprint. No inference of a complete track route follows from a local crossing.

## Reproduction and provenance

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Rendered crop SHA-256 `d57a3c2457fd725bf1ef7c6ec615fb2b332ca07063e069dbd2e6e10d8b827664`; PNG bytes may vary with renderer.

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6400 -y 10200 -W 2200 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3019-sides
```

Same source edition and informed prior map reading; no independent physical sheet, exhaustive errata check, runtime edit or gameplay test.

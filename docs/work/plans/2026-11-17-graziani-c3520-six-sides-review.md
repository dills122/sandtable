# C3520 six-side source review

**Disposition:** The [six-side note](2026-11-17-graziani-c3520-six-sides.md) correctly locates C3520 and its six immediate neighbors in the inspected Map C crop. Two single-dash track crossings match the pinned entries; the remaining four have thin exposed grid and no literal pair entries. This is a bounded source reading, not a certification of hidden details or legal movement.

## Six sides

Independently re-rendered [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 at 600 dpi, crop `(6500,8400,2100,1550)`. The number `3520` is legible inside the central hex. The [map key](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 1 distinguishes single dark dashed tracks from paired unfinished-road dashes and broad splashed escarpment bands.

| Neighbor | Visible side | Pinned literal entry |
| --- | --- | --- |
| C3619 | Upper-left diagonal: thin grid; no crossing track apparent. Sidi Omar text and its separate route are in adjoining territory. | None found. |
| C3620 | Upper-right diagonal: dark dash straddles the side, aligned with the two interior continuations. | `track`. |
| C3521 | Right vertical: exposed thin grid with no identified crossing. | None found. |
| C3420 | Lower-right diagonal: exposed thin grid with no identified crossing. | None found. |
| C3419 | Lower-left diagonal: a dark dash straddles the grid and continues toward the Sheferzen junction. | `track`. |
| C3519 | Left vertical: exposed thin grid; the nearby separate dashed route lies within C3519 and crosses another side. | None found. |

Blue checkered linework appears to the west, not on the exposed C3520 sides. Its interpretation is separate; nearby linework or a shared junction should not be assigned to a side by proximity alone. Track ink covers its underlying grid at crossings; an unlisted pair in the sparse file does not prove no feature exists. These observations do not establish unit entry, ZOC or a complete route.

Literal pairs checked against `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; independently rendered crop SHA-256 `a35a4376afc3ac2dfaa6047ac020216853f118040d710b9a8f07ea0d74eb8b6b`. Same-scan informed review, not a separate physical map or exhaustive edge audit. No runtime changes or tests.

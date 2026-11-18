# C3721 six-side source review

**Disposition:** The [six-side note](2026-11-17-graziani-c3721-six-sides.md) correctly identifies the six neighboring cells and the two visible track crossings, matching the pinned pair entries. Name ink crosses part of the southern boundary; thin exposed grid and missing pinned pairs do not certify all other sides feature-free or legal.

## Same-scan observations

Independently rendered [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 at 600 dpi, crop `(7000,7900,1800,1600)`. The `3721` number and blue Sidi Suleiman dot are inside the same central hex. PDF p. 1 map key separates single dark dashed track from blue cross-tied unfinished railroad and the broad, splashed escarpment symbol.

| Neighbor | Reviewed visible side | Literal pinned entry |
| --- | --- | --- |
| C3820 | Upper-left diagonal: exposed thin grid, no crossing route apparent. | None found. |
| C3821 | Upper-right diagonal: dark single dash straddles grid, aligned with route in both interiors. Blue cross-tied railway is nearby in C3821/C3722 area; it is not by proximity established on this side. | `track`. |
| C3722 | Right vertical: thin grid exposed; blue `Sidi` lettering approaches/overprints the lower endpoint. | None found. |
| C3621 | Lower-right diagonal: portions covered by blue place-name; visible grid is thin. | None found. |
| C3620 | Lower-left diagonal: dark dash crosses grid and aligns with interior continuation. | `track`. |
| C3720 | Left vertical: thin exposed grid, no track identified. | None found. |

Nearby rail, terrain bands and country-mark linework cannot be assigned to an edge simply by proximity. Dashed crossings obscure underlying short grid sections; missing literal pair entries are a sparse-file observation, not proof of source absence. Unit entry, ZOC and continuity beyond this crop remain separate questions.

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; independently rendered crop `e937ab6fa944699e35d3bc193076c56c8943800cf0946857a2d0dbe7608ad1b9`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same-scan informed review, not a second physical edition or complete map audit. No runtime changes or tests.

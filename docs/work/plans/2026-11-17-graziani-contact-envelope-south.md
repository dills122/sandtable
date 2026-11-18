# Sheferzen contact envelope: southern five-cell side ledger

**Date:** November 17, 2026. **Reviewer:** Codex, informed source check. **Disposition:** local observations only; no side or playable-area certification. This ledger covers six candidate sides each of C3418, C3419, C3420, C3519 and C3520, including sides to cells outside the ten-cell envelope. Printed four-digit labels correspond to canonical `C`-prefixed IDs below. Duplicate pairs are repeated so each cell has six sides. Page-image orientations are not compass bearings.

Original [Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Checked 600-dpi crop top-left `(6100,8200)`, size `2700×2500`, PNG SHA-256 `804c354352b4ef19880dda757193b84ebd7a5ffd21aa6e39024215740f2eb54e` (renderer-dependent). Reproduce with:

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6100 -y 8200 -W 2700 -H 2500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/contact-south-batch
```

`T` below means visible dark dashed track crossing or aligned approaches, **not** a passability ruling; ink can obscure underlying side. `B` means blue checkered blocks on side, whose interpretation and effect require the separate [border rules review](2026-11-17-graziani-contact-border-rules-review.md). `G` means visible thin grid without another identified crossing on exposed portion, **not** certified feature-free. `U` means material obscuration or unresolved crossing. Pinned comparison uses literal entries in `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; `—` means no pair entry, not an assertion of clear terrain. No directional higher-side mark identified on exposed portions; obscured portions remain unknown.

| Cell | Neighbor | Side on page | Source observation and disposition | Pinned |
| --- | --- | --- | --- | --- |
| C3418 | C3417 | left vertical | T; dash crosses side. | track |
| C3418 | C3518 | upper-left diagonal | T; dash crosses near middle. | track |
| C3418 | C3519 | upper-right diagonal | B follows side; G where exposed; vertex obscured by blue blocks. | — |
| C3418 | C3419 | right vertical | B and T; crossing supported by aligned dashes, exact intersection obscured by blocks/number. | track |
| C3418 | C3319 | lower-right diagonal | B follows side; vertex and underlying grid obscured. | — |
| C3418 | C3318 | lower-left diagonal | T; dash crosses side. | track |
| C3419 | C3418 | left vertical | B and T; exact intersection obscured. | track |
| C3419 | C3519 | upper-left diagonal | T; dash crosses diagonal. | track |
| C3419 | C3520 | upper-right diagonal | T; dash crosses diagonal. | track |
| C3419 | C3420 | right vertical | T; dash and number overprint grid. | track |
| C3419 | C3320 | lower-right diagonal | U: Bir el Sheferzen lettering and endpoint hide part; exposed stretches thin. No track observed across exposed part. | — |
| C3419 | C3319 | lower-left diagonal | T; dash crosses diagonal; nearby name ink. | track |
| C3420 | C3419 | left vertical | T; number and track overprint grid. | track |
| C3420 | C3520 | upper-left diagonal | G; no distinct crossing seen. | — |
| C3420 | C3521 | upper-right diagonal | G; no distinct crossing seen. | — |
| C3420 | C3421 | right vertical | G; no distinct crossing seen. | — |
| C3420 | C3321 | lower-right diagonal | T; dash crosses diagonal; name approaches vertex. | track |
| C3420 | C3320 | lower-left diagonal | G on exposed part; place-name ink near endpoint leaves U there. | — |
| C3519 | C3518 | left vertical | B follows side; obscures underlying grid. | — |
| C3519 | C3618 | upper-left diagonal | T; dash crosses diagonal. | track |
| C3519 | C3619 | upper-right diagonal | G on exposed part; Sidi Omar ink near upper vertex. | — |
| C3519 | C3520 | right vertical | G; track within C3519 crosses other sides, not visibly this one. | — |
| C3519 | C3419 | lower-right diagonal | T; dash crosses diagonal. | track |
| C3519 | C3418 | lower-left diagonal | B follows side; underlying grid partly hidden. | — |
| C3520 | C3519 | left vertical | G; nearby track is in C3519. | — |
| C3520 | C3619 | upper-left diagonal | G; no distinct crossing seen. | — |
| C3520 | C3620 | upper-right diagonal | T; dash crosses diagonal. | track |
| C3520 | C3521 | right vertical | G; no distinct crossing seen. | — |
| C3520 | C3420 | lower-right diagonal | G; no distinct crossing seen. | — |
| C3520 | C3419 | lower-left diagonal | T; dash crosses diagonal. | track |

C3419 and C3520 readings agree with prior [C3419](2026-11-17-graziani-c3419-six-sides.md) and [C3520](2026-11-17-graziani-c3520-six-sides.md) checks; [C3320](2026-11-17-graziani-c3320-six-sides.md) retains unknown under lettering on the contact edge. These checks use the same scan, not independent editions. No material disagreement identified; B marks are visible side evidence missing as a separate pinned feature, not a claim that the pinned schema represents them. Trace edges again with clearer source or other evidence before promoting any G or U to feature-free. Check all endpoint overprints and any rules for these marks.

Cell interiors are separate from sides: earlier [terrain audit](2026-11-17-graziani-south-contact-terrain.md) describes exposed C3419 and C3520 as unpatterned clear and locates the Bir dot in C3419; it does not establish side clearance. Neither a track crossing nor visible thin grid proves movement, ZOC projection, compulsory combat or route closure. Incoming ZOC requires checking each exterior neighbor and possible force, direction, eligibility, terrain entry, phase and rules; this local ring does not exclude outside forces or establish a legal boundary. Retain C3419–C3320 obscuration and the checkered-side treatment as blockers where material. No runtime or pinned data changes; no gameplay tests.

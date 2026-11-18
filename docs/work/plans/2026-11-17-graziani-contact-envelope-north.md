# Sheferzen contact envelope: four halo cells

**Date:** November 17, 2026. **Reviewer:** Codex source check. **Disposition:** bounded visual ledger, not side certification or a legal-move list. This covers all six sides of C3219, C3220, C3319 and C3321 from the [contact batch](2026-11-17-graziani-playable-map-work-batch.md); C3320 and C3419 have separate [six-side](2026-11-17-graziani-c3320-six-sides.md) [checks](2026-11-17-graziani-c3419-six-sides.md). Page-image directions are used, not compass directions. Printed four-digit numbers map to canonical C-prefixed IDs; adjacent cell labels and grid establish the listed pairs. Each shared side is the same physical side in both directions; its game effect may differ by direction.

## Evidence and interpretation

Original [Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Inspect at 600 dpi with:

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6200 -y 9050 -W 2900 -H 2900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/contact-north
```

The scan has pale, apparently unpatterned interiors for these four cells; track ink, labels and checkered side ink are separate observations. Do not infer whole-cell terrain assignment from exposed interior alone. Dashed dark marks match the key's track; blue checks follow grid sides but their identification and effect need the separate [border review](2026-11-17-graziani-map-sheferzen-border.md) and [rules review](2026-11-17-graziani-contact-border-rules-review.md). No coast or settlement dot is apparent inside these four cells; nearby Sheferzen dot is in C3419. Name ink crosses C3319/C3320 and C3320/C3321 near their upper ends. No physical edge of the sheet or seam appears here. No higher-side direction is visible in exposed portions; overprint and feature ink prevent a complete absence claim.

`F` below means exposed side appears to be thin grid with no identified crossing or side feature; it is **not** independently certified feature-free. `T` means visible dashed track crossing with aligned continuation. `B` means blue checkered blocks follow the side; do not infer the mark's rules effect. `U` means overprint or crowded vertex prevents a complete reading. All dispositions are limited to this scan, not an independent edition. A track or border observation does not establish enterability or ZOC transmission.

| Cell | Neighbor and image side | Observation / disposition | Pinned feature |
| --- | --- | --- | --- |
| C3219 | C3319, upper-left | T crosses diagonal with continuation in both cells. | track |
| C3219 | C3320, upper-right | F; track crosses neighboring C3219/C3319, not visibly this side. Vertex and dash gap warrant care. | none |
| C3219 | C3220, right | F. | none |
| C3219 | C3120, lower-right | T crosses diagonal close to right vertex; continuation visible. | track |
| C3219 | C3119, lower-left | B along diagonal; blocks obscure underlying side. | none |
| C3219 | C3218, left | B along vertical; blocks obscure underlying side. | none |
| C3220 | C3320, upper-left | F in exposed scan. | none |
| C3220 | C3321, upper-right | F in exposed scan. | none |
| C3220 | C3221, right | F. | none |
| C3220 | C3121, lower-right | F. | none |
| C3220 | C3120, lower-left | F; nearby track crosses adjacent C3219/C3120, not visibly this side. | none |
| C3220 | C3219, left | F. | none |
| C3319 | C3418, upper-left | B along diagonal; blocks obscure underlying side. | none |
| C3319 | C3419, upper-right | T crosses diagonal. Adjacent blue checks meet vertex; do not extend them onto this diagonal. | track |
| C3319 | C3320, right | F on exposed vertical; U at upper endpoint under place-name ink. | none |
| C3319 | C3219, lower-right | T with continuation; dash crosses diagonal. | track |
| C3319 | C3218, lower-left | B along diagonal; blocks obscure underlying side. | none |
| C3319 | C3318, left | B along vertical; blocks obscure underlying side. | none |
| C3321 | C3420, upper-left | T crosses diagonal with continuation through both cells. | track |
| C3321 | C3421, upper-right | F. | none |
| C3321 | C3322, right | T crosses vertical with continuation through both cells. | track |
| C3321 | C3221, lower-right | F. | none |
| C3321 | C3220, lower-left | F. | none |
| C3321 | C3320, left | F on exposed vertical; U under place-name lettering near upper end. | none |

Pinned comparison: `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. The five tracks shown are the only literal records involving these four cells; `none` means no pair record, not a reviewed empty side. Blue checks lack a separate pinned feature here, an observation about representation, not proof of a transcription error. C3320-side observations agree with the prior check; same underlying scan does not provide independent source corroboration. No higher direction is recorded on these five track records. A clearer source, endpoint tracing and assignment rules remain necessary for obscured marks and whole-cell facts.

## Contact and incoming ZOC limits

This ledger inventories crossing marks in both directions, including pairs leading outside the ten-cell envelope. Check each potential incoming force's actual location, unit qualifications, phase, entry and applicable ZOC and terrain rules before determining projection. Check directional blocking and enterability separately for each direction. Unknown overprint or feature meaning stays unknown; neither an absent pair record nor an exposed thin line settles legal crossing. A one-neighbor halo does not exclude forces farther away, close movement or supply routes, or establish compulsory combat. No data, runtime behavior or tests changed.

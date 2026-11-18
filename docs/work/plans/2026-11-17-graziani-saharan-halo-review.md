# Graziani: C0716 halo interior review

**Reviewed:** 17 November 2026. Informed second reading of the [western](2026-11-17-graziani-saharan-halo-west.md) and [eastern](2026-11-17-graziani-saharan-halo-east.md) ledgers against the same [Map C scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and terrain key on p. 1. This is not a blind review or an independent edition.

The 11 western and 13 eastern IDs are disjoint and together match the 24 cells at geometric distance four from C0716. Using the printed grid's stagger, axial coordinates `q = first`, `r = second - (q + (q mod 2))/2` and distance `max(|dq|, |dr|, |dq + dr|)` reproduce the listed ring. This checks the proposed envelope's geometry, not the scenario's placement interpretation or legal movement.

Compared all 24 records with [pinned hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json): the terrain and settlement values in both ledgers match; every record also has `coastal: false`. Source is `vassal:CNAv2.1.0`. Visible artwork supports the ledgers' broad observations: desert fill in the western cells, mixed pale and yellow areas in C0612/C0619, marsh patterns in C0819/C1018, blue village/bir dots in C0812/C1118, brown artwork in C1114, and an oasis symbol predominantly in C0919. The key depicts those symbol types; whole-cell and edge assignments still require applicable rules.

C0919 remains a positive missing-oasis **candidate** against pinned `settlement: null`, subject to [its separate review](2026-11-17-graziani-c0919-oasis-review.md). No further definite interior difference identified in this reading. Pale artwork does not independently establish pinned `clear`; numbers, lettering, tracks and boundary ink obscure portions of several cells. A symbol not apparent in exposed artwork is not proof of absence underneath overprint. Brown and blue edge artwork, including checkered ink near C0519/C0619, remains unresolved. No side is certified feature-free, and no terrain, obstacle, crossing, direction, force reach, ZOC or legal boundary is adjudicated here.

## Evidence

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned JSON SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Reviewed crops and reproduction commands are in both ledgers and the linked C0919 review. The PDF and JSON hashes were checked locally. Retain observations as research; seek clearer or independent source and assignment rules before any correction or admission. Documentation only; no gameplay tests.

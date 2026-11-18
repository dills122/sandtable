# Siwa eastern leads: independent visual review

**Date:** November 17, 2026. **Verdict:** [Observations](2026-11-17-graziani-siwa-east-leads.md) supported within stated limits; C0230 vertex remains unassigned, and C0133–C0134 is a printed mark missing from pinned pair data, not a certified data correction.

Inspected 600 dpi rendering of [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3. Single dashes from C0229 enter C0230 and approach its upper-right vertex with C0330 and C0331. Brown ink and overlapping grid obscure the crossing at that vertex; a dash continues into C0331, but the image does not settle which side owns the crossing. Do not assign C0230–C0331 from this image alone.

Separately, single dashes emerge from the C0130 oasis artwork and cross C0130–C0131, C0131–C0132, C0132–C0133 and C0133–C0134. The last crossing overlaps the C0134 number. Artwork obscures the exact join inside C0130. The line continues to the right boundary of the printed Map C hex grid; no adjoining hex is visible there. This does not identify a neighboring sheet cell or establish a seam crossing.

Checked [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json): C0230–C0331 has `track`; C0230–C0330 and C0330–C0331 have `slope`, up C0330; the first three C0130–C0133 pairs have `track`; C0133–C0134 has no explicit pair. Sparse VASSAL-derived records are comparison data, not independent proof, and absence does not certify a clear side. No wider terrain, route, movement legality or victory survey performed. Two leads do not establish connectedness or a route to Tobruk.

**Provenance and limits:** Same scan and crop as the original, not an independent edition or physical sheet. Verified SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `2e68d17c449c9c6a07d8dd9c16ddbc61919802d6293c66bb293208d92c3db78a`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Key identification follows prior [source review](2026-11-17-graziani-siwa-route-source-review.md). No runtime or data changes; no gameplay tests.

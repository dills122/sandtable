# Siwa route extension: eight adjacent sides

**Date:** November 17, 2026. **Status:** source-printed local leads, not a certified route. Extends the [reviewed local batch](2026-11-17-graziani-siwa-branch-batch-review.md) toward Map C's interior. Directions describe the rendered sheet. The original [Map C scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and its p. 1 key distinguish single-dashed track from paired-dashed unfinished road. Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) are sparse VASSAL-derived comparison data, not independent proof of the sheet.

| Adjacent pair | Printed observation | Pinned features |
| --- | --- | --- |
| C0325–C0424 | Single dashes continue upward-left from the C0225 lead; a dash gap lies at the shared side. | `track` |
| C0424–C0524 | Single dash crosses the side over brown terrain ink. | `slope`, `track` |
| C0426–C0527 | Paired dashes continue upward from C0327 through C0426; they cross the slanted side close to its C0427 vertex. | `unfinished-road` |
| C0527–C0627 | Paired dashes cross the upper side of C0527. | `unfinished-road` |
| C0627–C0728 | Paired dashes cross the upper-right side. | `unfinished-road` |
| C0728–C0828 | Paired dashes continue upward-right across the shared side. | `unfinished-road` |
| C0828–C0929 | Paired dashes continue upward-right across the shared side. | `unfinished-road` |
| C0527–C0626 | A separate single-dashed line crosses the upper-left side of C0527 toward C0626. Its lower end approaches the paired dashes within C0527, but dash gaps do not certify a junction. | `track` |

These eight side observations do not settle exact connections within hexes. In particular, the C0426–C0527 paired crossing is near a three-hex vertex; distinguish ink position from a rule for resolving vertex crossings. The single-dash C0325–C0424 lead has an intervening gap, and brown terrain ink overlaps C0424–C0524. Earlier C0225 and Siwa junctions also have gaps or oasis occlusion. Neither slope geometry nor its movement effect is adjudicated here. The paired and single leads are recorded separately even where they approach; no inference of interchange follows solely from proximity.

This is not an exhaustive side or terrain survey. Leads past C0524, C0626 and C0929, other local branches, off-road options and board-edge coverage remain open. No Siwa–Tobruk connection, convoy eligibility, legal movement, or impossibility is established; those require remaining geometry, applicable rules and current state. No data or runtime change and no gameplay test.

**Reproduction:** rendered PDF p. 3 at 600 dpi using `pdftoppm -f 3 -l 3 -r 600 -x 8000 -y 16500 -W 4500 -H 4300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-extension`. SHA-256: source PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `69261669fdcc4b7f8274ee0b1d538a4d03aa3c217b6b39fc12dee20562456a2d` (renderer-dependent); pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan as prior batch, not a physical sheet or independent edition.

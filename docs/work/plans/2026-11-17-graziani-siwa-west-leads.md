# Siwa west leads: bounded Map C observations

**Date:** November 17, 2026. **Status:** candidate printed marks, not route proof. Continues two distinct endpoints from the [branch batch](2026-11-17-graziani-siwa-branch-batch.md). Inspected [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and its p. 1 key: a single dashed line denotes track. Arrows below identify apparent adjacent-side crossings, not legal movement or certified joins. Image directions refer to the rendered sheet.

| Lead and side | Observation | Pinned pair features |
| --- | --- | --- |
| C0224 → C0324 | Single dashes leave the oasis artwork toward upper left and cross the slanted shared side into C0324. Palm ink and dash gaps obscure parts of the local connection back toward C0225. | `track` |
| C0325 → C0424 | Single dashes head upper left; a gap falls at the shared side. | `track` |
| C0424 → C0524 | Single dash crosses over brown terrain ink. | `slope`, `track` |
| C0524 → C0523 | Single dashes pass left across the shared vertical side, close to its upper vertex. | `track` |

The C0224 lead continues visually toward the upper-left vertex of C0324, where dark route ink meets brown terrain and grid lines. Whether it crosses C0324–C0323 or C0324–C0423, or follows the vertex, is not settled here. Pinned C0324–C0423 has `slope` but no `track`; C0324–C0323 has no record. Stop at that ambiguity rather than assign an onward side. The C0325 lead approaches the upper-left vertex of C0523; a dash and grid lines crowd the crossing. Pinned C0523–C0622 and C0523–C0623 both have `track`, but these records do not resolve side ownership on the scan. Stop there. The first two C0325 sides repeat the earlier [extension](2026-11-17-graziani-siwa-route-extension.md); C0524–C0523 is the additional candidate. Neither lead has been followed past its first unclear mark.

Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) are sparse VASSAL-derived comparison data, not independent confirmation; missing records do not certify absence. Neither slope geometry nor its effect is classified. Exact joins, other sides, onward paths, and any connection between these leads remain open. No Siwa–Tobruk route, convoy eligibility, legal movement, or impossibility follows from these observations. Separate challenge review against the scan remains needed.

**Reproduction:** Source PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned sides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Inspected existing 600 dpi crop `/private/tmp/siwa-extension.png`, SHA-256 `69261669fdcc4b7f8274ee0b1d538a4d03aa3c217b6b39fc12dee20562456a2d` (renderer-dependent), reproducible with:

```sh
pdftoppm -f 3 -l 3 -r 600 -x 8000 -y 16500 -W 4500 -H 4300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-extension
```

Same scan as prior work, not an independent edition or physical sheet. Hash identity does not establish rights or durable custody. No runtime or data changes; no gameplay tests.

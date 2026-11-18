# Map E Bilbeis rail source review

**Date:** November 18, 2026. **Result:** Eight listed rail-side candidates agree with visible ticked line and pinned records, with vertex qualifications below. Review used the cited original PDF and specified crop; the candidate list and pinned data were known during inspection, so this is not a blind reading or an independent edition.

## Source check

Rendered page 5 independently using the batch's `pdftoppm` command and inspected the resulting image, including printed grid labels and neighboring hex boundaries. PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Re-rendered crop SHA-256: `095d93258c06f05bcab7d2ad77451c81d02862c88d93db309ffde6608590229e`, matching the batch crop; output depends on renderer. Pinned `hexsides.v1.json` SHA-256: `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

| Pair | Visual check |
| --- | --- |
| `E2429–E2530` | Ticked line crosses their diagonal boundary near the river and a vertex; attribution qualified. |
| `E2530–E2531` | Ticked line crosses their shared vertical boundary. |
| `E2531–E2631` | Ticked line crosses their diagonal boundary near a vertex; attribution qualified. |
| `E2631–E2632` | Ticked line crosses their shared vertical boundary. |
| `E2632–E2633` | Ticked line crosses their shared vertical boundary under Bilbeis lettering. |
| `E2632–E2732` | Northwest branch crosses their diagonal boundary near Bilbeis. |
| `E2732–E2831` | Northwest branch crosses their diagonal boundary near a vertex; attribution qualified. |
| `E2830–E2831` | Ticked line crosses their shared vertical boundary toward Zagazig. |

Each literal unordered pair has one pinned record with `features: ["railroad"]`, `side: null`, and `up: null`. Grid numbering and boundary continuity support listed pairs; proximity to a vertex does not establish a crossing on either adjoining side. Town lettering and line overprint limit precision. This review did not systematically check other features or negative sides.

Line in `E2633` reaches the printed sheet edge. Heliopolis line separately ends at the edge in `E2134`; the crop shows no connection between these lines. Neither edge mark establishes an off-map hex, facility, connection, service, control, or occupied witness. This check does not settle sheet seams or absence elsewhere. No runtime or map-data changes; no gameplay tests.

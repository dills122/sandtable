# Siwa eastern leads: bounded Map C observations

**Date:** November 17, 2026. **Status:** candidate printed marks only; no route, legal movement, map correction or victory result established. Continues the two eastern endpoints of the [Siwa branch batch](2026-11-17-graziani-siwa-branch-batch.md), separately. Image directions follow the rendered sheet. The [Map C scan and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, are the same source used previously; the key identifies single dashed marks as track. Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) are sparse VASSAL-derived comparison data, not independent sheet evidence. No pair record does not certify a clear side.

| Lead and next side | Visible mark and limit | Pinned pair features |
| --- | --- | --- |
| C0230 toward C0331 | Single dashes run upper right through C0230, then meet brown terrain ink at the C0230/C0330/C0331 vertex. A dash appears to continue into C0331, but crossing side versus vertex and ownership are unclear under the overprint. Stop at this first uncertain mark; do not certify C0230–C0331 as a crossing. | C0230–C0331 `track`; C0230–C0330 `slope`, up C0330; C0330–C0331 `slope`, up C0330. These records do not settle the printed vertex. |
| C0130 → C0131 | Single dashed lead emerges from palm artwork in C0130 and crosses the right side. Oasis artwork obscures its interior approach; its exact join to the incoming C0229 lead remains uncertain. | `track` |
| C0131 → C0132 | Single dashes continue right across the shared side. | `track` |
| C0132 → C0133 | Single dashes continue right across the shared side. | `track` |
| C0133 → C0134 | Single dashes continue right across the shared side, over the printed hex number. | No explicit pair. |

The C0134 line continues to the printed right edge of Map C; no adjoining cell is visible on this sheet. Stop at the edge, after four observed sides. The line at the edge is not evidence of an identified neighboring sheet cell or crossing. Brown terrain beside the C0230 vertex and the oasis/label overprints limit interpretation; no full terrain, other-side, off-road or seam survey was made. The two leads are distinct observations, not proof of a connected route to Tobruk or of movement legality or impossibility. A clearer source and vertex convention are needed for C0230; sheet registration and applicable rules are needed beyond C0134.

## Reproduction and limits

```sh
pdftoppm -f 3 -l 3 -r 600 -x 10500 -y 18500 -W 6000 -H 3000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-east-leads
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop `2e68d17c449c9c6a07d8dd9c16ddbc61919802d6293c66bb293208d92c3db78a` (renderer-dependent); pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan, not an independent edition or physical sheet. Hash identity does not establish rights or durable custody. Separate challenge review remains needed. No runtime, map data or gameplay tests changed.

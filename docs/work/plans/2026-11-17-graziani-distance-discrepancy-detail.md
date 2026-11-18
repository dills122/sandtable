# Barce–Bir-el-Qatrani distance discrepancy

**Finding:** Printed 80 conflicts with six cell-disjoint two-leg bounds in the same chart. If all other 12 cells in those bounds are correct shortest distances, the direct value must be at most 78. These constraints share one scanned source and fixed-endpoint, common shortest-path assumptions; they are not independent source evidence. This is conditional corroboration for the candidate 78, not a source correction or proof that 78 is the correct map distance. Retaining 80 requires changing at least one cell in each of the six distinct pairs, assuming fixed endpoints and ordinary shortest-path semantics.

## Printed evidence

Visually checked [Air rules](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25 §37.42, including row/column headings and chart footnote. The Bir-el-Qatrani (`D2903`) row intersects Barce (`B5504`) at **80**. Same row and Barce column provide these distinct intermediates:

| Intermediate | Barce to intermediate | Intermediate to Bir | Sum |
| --- | ---: | ---: | ---: |
| Ben Gania | 20 | 58 | 78 |
| Mechili | 20 | 58 | 78 |
| C2803 | 46 | 33 | 79 |
| Tobruk | 40 | 38 | 78 |
| Fort Maddalena | 61 | 17 | 78 |
| Bardia | 56 | 22 | 78 |

The chart footnote defines shortest hex distance excluding origin and including destination, and explicitly assumes an all-sea hex grid when needed. Neither endpoint carries the chart's asterisk for one major city hex. Counting convention and sea crossings do not explain a direct shortest path longer than a valid concatenation of its two shortest legs. Each intermediate uses two chart cells unique among these six pairs, so, under the stated fixed-endpoint assumptions, keeping 80 entails corrections to at least six other cells. Lowering this one cell to 78 satisfies these six inequalities, but may not satisfy other chart constraints; minimum edits to this subset are not an authoritative erratum.

## Limits

See [independent review](2026-11-17-graziani-distance-discrepancy-detail-review.md) for verification and assumptions.

This is a mathematical consistency check of one scanned edition, not independent source confirmation of either leg, map seam or 78. The comparison is not blind; see [full transcription and other violations](2026-11-17-graziani-distance-transcription.md) and [source decision](2026-11-17-graziani-map-distance-decision.md). Different corrections to a location, chart convention or map interpretation may change several cells together. A valid map geometry or authoritative correction could discriminate among them; these six inequalities alone cannot establish 78 as exact, identify a source typo, admit a cross-sheet edge, or determine actual traversal. Prior errata search reported no relevant correction but was not exhaustive.

## Provenance

Air PDF SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; chart visualized from `/private/tmp/sandtable-air-rules.pdf` at 160 dpi. Same source edition as previous readers. Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; no fresh map alignment claimed. Errata PDF SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`; prior search only. No runtime changes.

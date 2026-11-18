# Map E rail sweep B: visual challenge

**Date:** November 18, 2026. **Result:** The 12 source-positive and three unknown side assessments in [sweep B](2026-11-18-graziani-map-e-rail-sweep-b.md) survive this same-scan visual challenge. This is not independent-edition corroboration. Unknown means the crossing cannot be attributed securely to that particular side; it does not mean rail is absent.

I inspected a fresh rendering of the [original scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5, using the printed hex numbers and shared grid sides, and compared the page 1 terrain key. The key shows railroad as a blue cross-tied line, road as a pair of dark parallel lines, and unfinished railroad as a broken blue cross-tied line. The assessed marks are the blue cross-tied symbol, not the nearby parallel road. Local centerlines look continuous at this scale; scan quality and overprint limit a finer distinction between railroad and unfinished railroad.

| Exact pair | Independent visual assessment |
| --- | --- |
| `E1927–E2026` | Unknown: blue rail reaches the diagonal close to its vertex; side attribution is insecure. |
| `E2025–E2026` | Positive: cross-tied line crosses the shared vertical side. |
| `E2025–E2125` | Positive: cross-tied line crosses the shared diagonal. |
| `E2124–E2125` | Positive: cross-tied line crosses the shared vertical side. |
| `E2124–E2223` | Unknown: line and grid converge close to the vertex; crossing side is insecure. |
| `E2223–E2323` | Positive: cross-tied line crosses the shared diagonal. |
| `E2323–E2422` | Positive: cross-tied line crosses the shared diagonal. |
| `E2421–E2422` | Positive: cross-tied line crosses the shared vertical side despite number overprint nearby. |
| `E2422–E2523` | Unknown: line meets the diagonal very close to its vertex. |
| `E2523–E2623` | Positive: cross-tied line crosses the shared diagonal. |
| `E2623–E2723` | Positive: cross-tied line crosses the shared diagonal. |
| `E2723–E2823` | Positive: cross-tied line crosses the shared diagonal. |
| `E2226–E2326` | Positive: cross-tied line crosses the shared diagonal. |
| `E2326–E2425` | Positive: cross-tied line crosses the shared diagonal; river artwork is nearby. |
| `E2425–E2526` | Positive: cross-tied line crosses the shared diagonal beside river artwork. |

Checked all 15 exact records in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json): each has literal `features: ["railroad"]`, `side: null`, and `up: null`. This checks the pin, not its source provenance or completeness. Positive describes visible local ink only; it establishes no route, service, construction condition, occupancy, or unblocked result.

Reproduction: render PDF page 5 at 300 dpi with `pdftoppm -f 5 -l 5 -r 300 -x 3800 -y 5700 -W 2400 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-sweep-b-review`. Source PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Pin SHA-256: `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Rendered crop remains outside Git. Documentation-only review; no gameplay tests.

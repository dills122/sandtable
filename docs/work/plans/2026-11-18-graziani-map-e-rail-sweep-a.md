# Map E lower Nile rail: bounded source sweep

**Date:** November 18, 2026. Eleven previously unreviewed pinned pairs selected from the [Map E inventory](2026-11-18-graziani-pinned-map-e-rail-inventory.md), following the lower Nile from `E0127` to `E1229`. The candidate list and pin were visible during inspection; this is an informed comparison, not a blind reading or independent-edition corroboration. The selected pairs do not overlap the [Bilbeis batch](2026-11-18-graziani-suez-rail-source-batch-review.md).

## Evidence

Inspected page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf) and the page 1 terrain key. Printed numbers and intervening hex boundaries identify the cells. The key depicts railroad as a blue solid line with transverse ties, distinct from the dashed unfinished-railroad sample and dark parallel road strokes. The blue ticked line in this crop follows the river alongside dark road ink; proximity and overprint limit side precision. Positive below means visible rail ink crosses the named side, not that every feature or legal effect is established.

| Pinned unordered pair | Scan observation |
| --- | --- |
| `E0127–E0227` | Ticked line crosses their diagonal boundary. |
| `E0227–E0328` | Ticked line crosses their diagonal boundary. |
| `E0328–E0428` | Ticked line crosses their diagonal boundary. |
| `E0428–E0529` | Ticked line crosses their diagonal boundary. |
| `E0529–E0628` | Ticked line crosses their diagonal boundary near adjacent road ink. |
| `E0628–E0729` | Ticked line crosses their diagonal boundary near a vertex; side attribution qualified. |
| `E0729–E0828` | Ticked line crosses their diagonal boundary near a vertex; side attribution qualified. |
| `E0828–E0929` | Ticked line crosses their diagonal boundary near a vertex; side attribution qualified. |
| `E0929–E1029` | Ticked line approaches the shared boundary near a vertex and road ink; exact side attribution unresolved. |
| `E1029–E1130` | Ticked line crosses the diagonal boundary near a vertex and dashed track; side attribution qualified. |
| `E1130–E1229` | Ticked line runs near a vertex and road ink at the shared boundary; exact side attribution unresolved. |

Each pair has exactly one literal unordered record in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), with `railroad` and `road`, `side: null`, and `up: null`. The scan observations above compare railroad only; they do not independently certify each pinned road. Qualified crossings do not establish a crossing of either adjoining side. Unresolved means unknown, not a negative or a proven pin error.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 4700 -y 7800 -W 1600 -H 2600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-lower-rail
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-lower-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/e-lower-rail.png /private/tmp/e-lower-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rail crop `8eb60a0f4581a81263324910ce4a57f99d529e80274e35d810d26e2c9ee52816`; key crop `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop bytes depend on renderer.

This bounded selection does not check omissions, other sides, sheet-edge connections, the whole network, construction or destruction, control, or occupancy. Neither this scan nor the pin proves an occupied witness or an unblocked result. No runtime or map-data changes; no gameplay tests.

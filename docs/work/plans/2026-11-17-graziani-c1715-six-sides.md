# C1715: six-side visual source audit

**Finding:** Map C places El Grein's village/bir dot inside C1715. Three dashed track crossings appear on its sides, consistent with three pinned `track` pairs. The remaining three sides show no obvious track or broad terrain band. Ink overlaps and scan quality limit negative observations; this is not a complete edge certificate or a legal movement finding.

## Source observations

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1, at the crop coordinates below. Scenario fixed Axis C1715 is recorded in the [fixed contact inventory](2026-11-17-graziani-fixed-contact-inventory.md) and its [source review](2026-11-17-graziani-fixed-contact-inventory-review.md). Printed `1715` identifies the central cell; printed labels identify all six neighbors. Directions refer to the displayed crop. Approximate central vertices clockwise from top: `(690,590)`, `(875,690)`, `(875,910)`, `(690,1015)`, `(500,910)`, `(500,690)`.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `C1814` | Upper-left diagonal | Dark dashed track crosses close to upper vertex. Pinned `C1715|C1814` has `track`. |
| `C1815` | Upper-right diagonal | Dark dashed track crosses near right vertex. Pinned `C1715|C1815` has `track`. |
| `C1716` | Right vertical | Thin grid visible; blue El Grein lettering overlaps its lower end. No obvious track crosses this side; no explicit pinned pair. |
| `C1615` | Lower-right diagonal | Dark dashed track crosses near right vertex, near blue lettering. Pinned `C1615|C1715` has `track`. |
| `C1614` | Lower-left diagonal | Thin grid visible, no obvious track or broad terrain band. No explicit pinned pair. |
| `C1714` | Left vertical | Thin grid visible, no obvious track or broad terrain band. No explicit pinned pair. |

The key depicts a track as a single dark dashed line, a village/bir as a blue dot, and wadi as a green band. El Grein's blue dot and converging tracks lie close to C1715's right side, while its name overlaps the lower-right vertex. Dashed lines cross three diagonals; vertex proximity and lettering obscure portions of the grid, so exact ink continuity is less certain than the overall visual reading. Green wadi ink lies north in C1815, not visibly on a C1715 side. Blue checked ink lies farther east; the key's border example does not unambiguously identify those checks. No broad ridge, slope or escarpment band visibly touches C1715. Missing pinned pairs do not prove absence of features.

Pinned [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records C1715 as `clear`, settlement `village`, `coastal: false`; pale interior and blue dot fit the key's clear and village/bir examples. The three pinned [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) records each have `side: null`, `features: ["track"]`, and `up: null`. This is a bounded same-scan reading, not independent-edition verification or a rights grant.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 600 -x 4800 -y 14300 -W 1600 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c1715-crop
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c1715-key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop SHA-256 `c9ed286e4fec9ac52092b94008c57d7776e4bb719838dfa6f55b9b3b920becb5`; key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894` (crop hashes renderer-dependent). Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crops remain outside Git. No runtime or data changes; no gameplay tests run.

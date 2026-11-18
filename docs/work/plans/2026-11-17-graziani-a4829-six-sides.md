# A4829: six-side visual source audit

**Finding:** Map A places Benina's village/bir dot inside A4829. Railroad and track cross its west and east sides, matching two pinned pairs. A gold slope-like band also follows the east side, absent from that pinned pair. Four other sides show no obvious broad terrain band or transport crossing. Artwork and scan quality limit negative observations; this is not a legal movement, supply, ZOC, or contact finding.

## Source observations

Inspected [original Map A and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 1, at crop coordinates below. Scenario fixed Axis A4829 is recorded in the [fixed contact inventory](2026-11-17-graziani-fixed-contact-inventory.md). Printed `4829` identifies the central cell, although blue and black lines overprint its number. Printed labels identify all six neighbors. Directions refer to the displayed crop, not compass bearings. Approximate central vertices clockwise from top: `(880,500)`, `(975,553)`, `(975,662)`, `(880,715)`, `(786,662)`, `(786,553)`.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `A4929` | Upper-left diagonal | Thin grid; no obvious transport crossing or broad terrain band. No explicit pinned pair. |
| `A4930` | Upper-right diagonal | Thin grid; dark escarpment artwork begins near the right vertex, but is not visibly along this diagonal. No explicit pinned pair. |
| `A4830` | Right vertical | Blue railroad and dark dashed track cross; gold stippled band follows the side and resembles key's slope. Printed `4830`, lines, and terrain overlap. Pinned `A4829|A4830` has `railroad`, `track`, no slope. |
| `A4730` | Lower-right diagonal | Thin grid; blue Benina lettering overlaps near its lower end. Dark escarpment approaches its right vertex but is not visibly along this diagonal. No explicit pinned pair. |
| `A4729` | Lower-left diagonal | Thin grid, partly overprinted by Benina lettering; no obvious transport crossing or broad terrain band. No explicit pinned pair. |
| `A4828` | Left vertical | Blue railroad and dark dashed track cross. Pinned `A4828|A4829` has `railroad`, `track`. |

Key depicts railroad as solid blue line with cross-ties, track as single dark dashed line, slope as gold stippled band along hexside, escarpment as dark stippled band, and village/bir as blue dot. Gold ink and track/railway overlap at A4829/A4830; slope interpretation is visual, not a ruling on direction or effect. No `up` direction inferred. Blue dot and Benina label are distinct from rail and track; no airfield symbol is visibly centered in this crop. Prior [scenario review](2026-11-17-graziani-map-independent-review.md) records §60.5 listing Benina airfield at A4829, which should not be inferred solely from map artwork. Missing pinned pairs do not prove absence of features.

Pinned [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records A4829 as `clear`, settlement `village`, `coastal: false`; pale interior and blue dot fit the key. Both pinned [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) pairs have `side: null` and `up: null`. This is a bounded same-scan reading, not independent-edition verification or a rights grant.

## Reproduction

```sh
pdftoppm -f 1 -l 1 -r 300 -x 4700 -y 1950 -W 1500 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4829/crop
pdftoppm -f 1 -l 1 -r 240 -x 2500 -y 240 -W 1450 -H 1200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4829/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop SHA-256 `b14a77508e29d6fdd4d57d187a409141f39d0fa4eb0f207d3fdc74780d388fd3`; key crop SHA-256 `4ff87a9e643435f83c285feddbb2422be069874c8389b5f06bee018564acb8e4` (crop hashes renderer-dependent). Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crops remain outside Git. No runtime or data changes; no gameplay tests run.

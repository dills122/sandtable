# C3918: six-side visual source audit

**Finding:** Map C shows six exposed grid sides around C3918, with no visible route crossing or broad terrain band on those sides. Pinned side data has no pair containing C3918. This local source comparison does not establish legal movement, ZOC, or a complete edge certificate.

## Source observations

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1, using crops below. Printed `3918` identifies the central cell; surrounding printed labels identify all six neighbors. Directions refer to the displayed crop. Approximate vertices clockwise from top are `(830,770)`, `(1015,880)`, `(1015,1095)`, `(830,1200)`, `(640,1095)`, `(640,880)` in the map crop.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `C4017` | Upper-left diagonal | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |
| `C4018` | Upper-right diagonal | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |
| `C3919` | Right vertical | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |
| `C3818` | Lower-right diagonal | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |
| `C3817` | Lower-left diagonal | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |
| `C3917` | Left vertical | Thin grid visible; no visible route crossing or broad terrain band. No explicit pinned pair. |

Key depicts track as a single dark dashed line, road as paired solid lines, unfinished road as paired dashed lines, and ridge, slope, and escarpment as broad colored bands. Nearby dark dashed tracks lie outside C3918, including the route across C3919/C3818. Blue checked ink lies farther southeast and does not visibly touch C3918; the key's border example does not unambiguously identify those checks. No blue rail line or checked ink visibly overprints C3918 sides. Scan quality, labels, and adjoining linework still limit negative observations; an absent pinned pair is not proof that a side has no feature.

Pinned `hexes.v1.json` records C3918 as `clear`, settlement `null`, and `coastal: false`. Its exposed interior is pale and inland, with no visible settlement dot, consistent with the key's clear example. This is a bounded same-scan reading, not independent-edition verification or a rights grant.

## Reproduction

```sh
mkdir -p /private/tmp/c3918
pdftoppm -f 3 -l 3 -r 600 -x 5800 -y 7000 -W 1900 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3918/crop
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3918/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop SHA-256 `c0125876f7919daac930bbacce8782cad2c168f544b61bbaf6598a63c041966d`; key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894` (crop hashes renderer-dependent). Pinned [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. PDF and crops remain outside Git. No runtime or data changes; no gameplay tests run.

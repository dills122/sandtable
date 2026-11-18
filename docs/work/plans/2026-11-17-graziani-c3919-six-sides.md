# C3919: six-side visual source audit

**Finding:** Map C shows a single dark dashed track crossing C3919/C3818 and C3919/C3920, and blue checkered ink along C3919/C3819. Pinned side records list the two track pairs. This local visual reading does not establish legal movement, ZOC, or a complete edge certificate.

## Source observations

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, in crops reproduced by commands below. Printed `3919` identifies the central cell; surrounding labels identify all six neighbors. Directions describe displayed crop. Approximate C3919 vertices clockwise from top are `(800,770)`, `(990,880)`, `(990,1095)`, `(800,1200)`, `(610,1095)`, `(610,880)` in the map crop.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `C4018` | upper-left diagonal | Thin grid visible; blue Fort Capuzzo lettering overlaps near upper vertex. No visible track, paired road, or broad escarpment on exposed portions; no explicit pinned pair. |
| `C4019` | upper-right diagonal | Thin grid visible; blue name overlaps upper vertex. No visible track, paired road, or broad escarpment on exposed portions; no explicit pinned pair. |
| `C3920` | right vertical | Single dark dashed track crosses near upper vertex. Pinned pair lists `track`; blue checks touch lower vertex. |
| `C3819` | lower-right diagonal | Blue checkered ink follows side and conceals underlying line; no exposed track crossing or broad escarpment visible. No explicit pinned pair. |
| `C3818` | lower-left diagonal | Single dark dashed track crosses side, concealing grid at crossing. Pinned pair lists `track`. Blue checks touch lower vertex. |
| `C3918` | left vertical | Thin grid visible; no visible track, paired road, or broad escarpment on exposed portions; no explicit pinned pair. |

Key depicts track as a single dark dashed line, unfinished road as paired dashed lines, road as paired solid lines, and escarpment as broad dark shaded hexside ink. It depicts `Border` as a gray side and adjoining lines, not an unambiguous match for the blue checks. [Regional border evidence](2026-11-17-graziani-border-mark-next.md) supports a geographic interpretation but does not establish key-symbol identity by itself. No paired road or broad escarpment is visible on exposed C3919 sides; overprints prevent claiming underlying ink is absent. A missing pinned pair means no literal record, not proof of an unfeatured side.

Pinned hex record says `clear`, settlement `null`, and `coastal: false`; exposed interior appears pale, inland, and without a blue settlement dot. Fort Capuzzo lettering extends over the upper vertex; its dot lies in C4020, not C3919. This is a bounded same-scan reading, not independent-edition verification or rights grant.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6200 -y 7000 -W 1900 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3919/crop
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3919/key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `6fd0baa0f2630dffde21fb67bac36d7fea1af47a5b0043c3c71eed28f802a521`; key crop `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894` (crop hashes renderer-dependent). Pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. PDF and crops remain outside Git. No runtime or data changes; no gameplay tests run.

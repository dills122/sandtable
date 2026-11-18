# Sidi Azeiz C4218: six-side source comparison

**Finding:** Map C shows a ridge along both upper diagonals of C4218, and a dense junction of dark dashed tracks and blue cross-tied line at Sidi Azeiz. Six pinned pairs correspond to visible ridge or route leads, but overlapping ink and close vertex approaches limit exact crossing placement. No legal movement conclusion follows.

## Identity and source

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1. In the 500-dpi crop below, C4218's approximate vertices clockwise from top are `(830,770)`, `(985,850)`, `(985,1040)`, `(830,1125)`, `(675,1040)`, `(675,850)`. The partly covered `4218` label and Sidi Azeiz blue dot lie inside this cell; surrounding numbered cells support identity. Directions refer to the displayed image. The key depicts ridge as a gold band, escarpment as a dark band, track as black dashes, road as paired solid lines, and unfinished railroad as a blue dashed cross-tied line. Blue cross-tied ink here resembles the latter, though track overprint obscures sections. No blue checked border is apparent along these six sides; this is a local visual observation only.

| Neighbor | Shared side and visible marks | Pinned pair |
| --- | --- | --- |
| `C4318` | Upper-left diagonal follows the gold ridge band. Dark route and blue line converge below the band's left end; no separate crossing on the exposed diagonal is isolated. | `ridge` |
| `C4319` | Upper-right diagonal follows gold ridge; dark dashed track crosses toward C4319 near the middle, amid gold ink. | `ridge`, `track` |
| `C4219` | Right vertical side crosses dark dashed track near its middle. Blue place-name ink overlaps lower end. | `track` |
| `C4119` | Lower-right diagonal has dark dashed track and blue cross-tied line crossing close together near its middle. Junction and overprint make their individual boundaries less distinct. | `track`, `unfinished-railroad` |
| `C4118` | Lower-left diagonal is crossed by a dark dashed branch near its left portion. Branch meets dense junction just inside C4218. | `track` |
| `C4217` | Left vertical side is crossed by dark dashed track and blue cross-tied line close together near its middle. Label, linework and junction overlap. | `track`, `unfinished-railroad` |

No broad dark escarpment or paired solid road is independently visible crossing these sides. Ridge splash, overprint, and junctions obscure some underlying grid; negative observations do not certify feature-free sides. No checked border crossing is established. The paired solid road farther east is outside C4218. Pinned `hexes.v1.json` records C4218 as `clear`, `village`, and `coastal: false`; the blue dot and pale inland cell are visible, while ridge ink occupies its upper edge. Pair features and source marks are compared literally, without asserting route continuity, rule effects, ZOC, or edge admission. All six pair records have `up: null`.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 500 -x 4850 -y 4900 -W 1900 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4218
pdftoppm -f 1 -l 1 -r 70 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapkey70
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop SHA-256 `5be5783f648b8378348d1145cca1773459d1049bda501a4fd49f4e7aaab99472`; key page render SHA-256 `337402ef1f89db6a76a5ffdd79f04240c560e4f2c5e2cf2e81727f948b57d508` (render hashes depend on renderer). Pinned [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. This is same-scan visual inspection, not independent edition verification. No runtime changes or gameplay tests.

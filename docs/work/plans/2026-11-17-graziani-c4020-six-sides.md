# Fort Capuzzo C4020: six-side visual source audit

**Finding:** Original Map C places Fort Capuzzo's blue point within `C4020`. All six neighboring pairs appear in the pinned side data. The scan shows route marks, dark escarpment and blue checks around this hex, but overprints conceal portions of the grid and underlying marks. This is a bounded visual comparison, not a movement or direction ruling.

## Original map and pinned data

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. In the reproducible crop below, C4020's approximate vertices are `(389,275)`, `(483,225)`, `(577,275)`, `(577,385)`, `(483,440)`, `(389,385)`. Directions refer to this displayed crop, not geographic compass directions. Some labels and side marks are obscured; neighboring IDs use the printed grid sequence as well as visible numbers. Pinned entries below are literal records in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), whose source is `vassal:CNAv2.1.0`.

| Neighbor | Shared side | Visible source and pinned comparison |
| --- | --- | --- |
| `C4120` | upper-left diagonal | Paired dark road lines cross near the upper vertex. Pinned: `road`. |
| `C4121` | upper-right diagonal | Broad dark escarpment ink adjoins the side; dark dashed track crosses near its middle, with heavy overprint. Pinned: `escarpment`, `track`; `up: C4020`. |
| `C4021` | right vertical | Paired road lines cross near the middle; blue checks and dark/brown slope-area ink cover much of the side. Pinned: `road`, `slope`; `up: C4020`. |
| `C3921` | lower-right diagonal | Dark dashed track and blue crossed rail-like line cross near the lower vertex; blue checks run along much of the side. Pinned: `track`, `unfinished-railroad`. |
| `C3920` | lower-left diagonal | Dark dashed track crosses near the left end; Fort Capuzzo text and route ink overlap the junction. Pinned: `track`. |
| `C4019` | left vertical | Dark dashed track and blue crossed rail-like line pass across the side; ink overlaps. Pinned: `track`, `unfinished-railroad`. |

The blue checked line continues south along the regional Libya–Egypt boundary; its political-boundary interpretation is geographic, not a direct visual match to the key's gray `Border` sample. See [border review](2026-11-17-graziani-contact-border-rules-review.md). Track, railroad, road, checks, labels and escarpment overprints can hide coincident features; absence of a visible mark does not establish absence beneath them. Pinned `up` values are data, not independently established by this crop. Pinned [hexes.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) describes C4020 as `clear`, `village`, `coastal: false`; the point and label are visible, but this note does not certify terrain classification or legal movement.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3300 -y 3500 -W 800 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c4020/fort-300
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `12c4dee6eaef719c800c049db23aa15fb383d54971ae5c52cd41cfeee64afe97` (renderer-dependent); pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Same scanned source edition; no independent sheet, runtime changes or tests.

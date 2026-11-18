# C3617: six-side visual source audit

**Finding:** Original Map C shows track crossings on `C3617–C3718` and `C3617–C3518`, matching both explicit pinned side entries. Blue checkered ink overlays the `C3617–C3618` side; the other three sides show thin grid where exposed. Place-name ink obscures part of the cell label and side linework. This bounded reading does not certify hidden features, movement, ZOC, or a complete map.

## Original map observations

Inspected [Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, informed by prior same-scan reviews. The Libyan Omar blue dot lies in the central cell; its `3617` label is partly covered by the blue place name. Neighboring printed numbers and grid support the identification. The exposed interior is pale and unpatterned, consistent with clear; the pinned hex record gives `terrain: clear`, `settlement: village`, `coastal: false`. The key depicts Village/Bir as a blue dot, track as a single dark dashed line, and escarpment as a broad dark band with splash. Its gray `Border` sample does not visibly duplicate the blue checks; their political meaning is an inference from regional context, discussed in [border review](2026-11-17-graziani-border-mark-review.md).

Crop coordinates: central vertices approximately `(940,690)`, `(1125,795)`, `(1125,1015)`, `(940,1120)`, `(750,1015)`, `(750,795)`; directions refer to displayed crop.

| Neighbor | Shared side | Visible source and pinned comparison |
| --- | --- | --- |
| `C3717` | upper-left diagonal | Thin grid visible; no identified track or broad escarpment. No explicit pinned pair. |
| `C3718` | upper-right diagonal | Single dashed track crosses; dash covers the crossing. Pinned pair lists `track`. |
| `C3618` | right vertical | Blue checks cover the side; no explicit pinned pair. Checks and nearby name/track ink limit certainty about coincident features. |
| `C3518` | lower-right diagonal | Single dashed track crosses; dash covers part of the grid. Pinned pair lists `track`. Blue checks start at the adjacent endpoint, limiting endpoint inspection. |
| `C3517` | lower-left diagonal | Thin grid visible; no identified track or broad escarpment. No explicit pinned pair. |
| `C3616` | left vertical | Thin grid visible except where blue Libyan Omar name crosses it; no identified track or broad escarpment on exposed portions. No explicit pinned pair. |

Neighbor labels `3717`, `3718`, `3618`, `3518`, `3517`, and `3616` are visible, though name ink partly overlaps `3616`. Track and place-name overprints hide underlying linework. Absence of an explicit side record is not proof of source-clear terrain. Scenario fixed placement at C3617 is identified in [fixed contact inventory](2026-11-17-graziani-fixed-contact-inventory.md); this map check does not decide unit state or setup legality.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 600 -x 5500 -y 8050 -W 2100 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3617-six
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `fcd8416d356b2e88dc38a1bdfa1207942ed11de23320714800ba035eb405d143` (renderer-dependent); pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan and informed prior reading, not independent edition or rights verification. No runtime edits or gameplay tests.

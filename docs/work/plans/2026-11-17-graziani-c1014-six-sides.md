# Giarabub C1014: six printed sides

**Finding:** Original Map C shows four dashed track crossings around Giarabub and a brown slope-like band along two of those sides. These match four explicit pinned side records, including their two `slope` entries. The adjoining lower-left side meets band ink at its vertex; that overlap does not establish a third slope side. This local reading does not certify unobscured sides or movement.

## Six sides and pinned comparison

Inspected [original map and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. The Giarabub blue dot lies inside the cell whose `1014` label is partly covered by track and brown ink. Neighbor labels and grid support the identities below. Directions are image directions, not compass bearings. Coordinates are approximate pixels within the crop specified below.

| Neighbor | Crop side | Visible source evidence | Pinned side record |
| --- | --- | --- | --- |
| `C1114` | Upper-left diagonal `(514,650)`–`(700,550)` | Brown band follows side, stippling on C1014 side; dark dashed line crosses near `(590,610)`. Ink overlaps grid. | `slope`, `track`; `up: C1114` |
| `C1115` | Upper-right diagonal `(700,550)`–`(890,650)` | Dark dash crosses near `(830,620)`, aligned with dashed line in both cells. | `track` |
| `C1015` | Right vertical `(890,650)`–`(890,875)` | Exposed thin grid line; Giarabub dot and lettering lie close to it. No crossing apparent. | No explicit pair entry |
| `C0915` | Lower-right diagonal `(700,975)`–`(890,875)` | Dark dash crosses near `(850,900)` and continues into C0915; crossing close to vertex. | `track` |
| `C0914` | Lower-left diagonal `(514,875)`–`(700,975)` | Thin diagonal visible for most of side. Brown and stippled ink touch/obscure its upper-left end at shared vertex; no dashed crossing apparent. Whether any band belongs to this side is not resolved solely by vertex overlap. | No explicit pair entry |
| `C1013` | Left vertical `(514,650)`–`(514,875)` | Brown band follows side, stippling on C1014 side; a dark dashed track crosses near `(514,780)`. Label and route obscure parts. | `slope`, `track`; `up: C1013` |

Key depicts track as single dark dashes, slope as brown band with irregular stippled edge, and village/Bir as blue dot. The dot and mostly pale interior are consistent with pinned `C1014` hex record `terrain: clear`, `settlement: village`, `coastal: false`; adjacent salt-marsh pattern starts in C1015 and extends east. [Independent review](2026-11-17-graziani-c1014-six-sides-review.md) confirms the visible splash is toward C1014 on the C1114 and C1013 sides. [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) p. 14 §8.35 place the splash on the down side, supporting C1014 down and the respective neighbors up as pinned. Overprint and shared vertices limit precision; C0914's vertex remains unresolved and no full-side absence is certified. No explicit pair entry means only that this sparse file has no literal pair; it does not prove absence of all features. No complete route or legal movement is asserted.

## Reproduction and provenance

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Crop PNG SHA-256 `29fa2434e73413d554f2adc880954cef285eea792353ee251e69d71c815d6b6c`; renderer bytes may vary.

```sh
pdftoppm -f 3 -l 3 -r 600 -x 4600 -y 16600 -W 2200 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c1014-sides
```

Same scanned edition and informed prior map work; no independent physical sheet, exhaustive errata check, runtime change or gameplay test.

# C3618 Sidi Omar: six-side source audit

**Finding:** Map C shows a dashed track crossing `C3618–C3519`, matching the only explicit pinned side entry incident to C3618. Blue checks overlay three sides; name ink and track ink conceal small portions of others. This is a bounded visual reading, not proof of a route, movement, ZOC, or complete edge coverage.

## Source and pin comparison

Inspected [original Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, informed by earlier same-scan readings. Printed `3618`, surrounding numbers and grid identify the cell. Its Sidi Omar blue dot matches the key's Village/Bir symbol; exposed pale, unpatterned interior is consistent with clear terrain. Pinned `hexes.v1.json` records `clear`, `village`, `coastal: false`. Key depicts track as a dark dashed line and escarpment as a broad band with splash. The key's gray Border sample does not establish the meaning of the blue checks; see [border review](2026-11-17-graziani-border-mark-review.md).

In the crop below, approximate vertices are `(936,690)`, `(1120,795)`, `(1120,1015)`, `(936,1120)`, `(750,1015)`, `(750,795)`; directions refer to displayed crop.

| Neighbor | Shared side | Visible source and pinned comparison |
| --- | --- | --- |
| `C3718` | upper-left diagonal | Blue checks overlay grid; no explicit pinned pair. Coincident underlying features cannot be ruled out. |
| `C3719` | upper-right diagonal | Thin grid visible; no track or broad escarpment identified. Checks meet its upper endpoint. No explicit pinned pair. |
| `C3619` | right vertical | Thin grid visible except where Sidi Omar name crosses near upper end; no track or broad escarpment identified on exposed portion. No explicit pinned pair. |
| `C3519` | lower-right diagonal | Dark dashed track crosses near right endpoint, covering grid there; pin lists `track`. |
| `C3518` | lower-left diagonal | Blue checks overlay grid; no explicit pinned pair. Underlying linework cannot be fully checked. |
| `C3617` | left vertical | Blue checks overlay grid; no explicit pinned pair. Underlying linework cannot be fully checked. |

Neighboring printed IDs support this adjacency. Absence of a pinned pair does not prove absence of an obscured or subtle feature. The pin enumerates features, not every blank edge. Settlement identification does not establish setup or game effects.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 600 -x 5875 -y 8050 -W 2100 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3618-six
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `0b99c30bcd39a766ba3c7f958005a5b48b6cbe9172683af477e108a57ed57615` (renderer-dependent); pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan as prior work, not independent edition evidence. No runtime or data changes; no gameplay tests.

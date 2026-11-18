# C3920: six-side visual source audit

**Finding:** Map C shows a dark dashed track crossing the C3919/C3920 side and blue checkered ink along three sides of C3920. The pinned side data lists track on C3919/C3920 and C3920/C4020. Crowded linework at the Fort Capuzzo vertex prevents this crop from independently establishing the latter crossing. Blue overprints remain unclassified here; no movement, ZOC or other legal-edge conclusion follows.

## Source observations and comparison

Visually inspected [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, in the 600-dpi crop below. The printed `3920` identifies the central cell. Approximate crop vertices, clockwise from top, are `(930,720)`, `(1115,830)`, `(1115,1040)`, `(930,1150)`, `(740,1040)` and `(740,830)`. Directions describe the displayed crop. Neighbor identities come from visible numbers and grid; the `4020` label is partly covered by Fort Capuzzo ink.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `C4019` | upper-left diagonal | Dark dashed route runs close to and partly over this diagonal toward Fort Capuzzo; label and route obscure parts of the grid and vertex. No explicit pinned pair. A separate crossing cannot be established from this crowded image. |
| `C4020` | upper-right diagonal | Most of the diagonal is thin grid; dark route, blue cross-tied ink and name crowd its upper vertex. Pinned pair lists `track`, but the precise crossing versus vertex contact is unresolved visually. |
| `C3921` | right vertical | Blue checkered ink follows the side. No broad dark escarpment is visible on exposed portions. No explicit pinned pair. Upper vertex is crowded by blue cross-tied and dark route ink; do not infer a crossing there. |
| `C3820` | lower-right diagonal | Blue checkered ink follows the side; no dark dashed route crossing or broad escarpment is visible on exposed portions. No explicit pinned pair. |
| `C3819` | lower-left diagonal | Blue checkered ink follows the side; no dark dashed route crossing or broad escarpment is visible on exposed portions. No explicit pinned pair. |
| `C3919` | left vertical | Dark dashed route crosses near the upper end of the side; pinned pair lists `track`. Blue checks touch the lower vertex, not visibly the length of this side. |

No explicit pinned pair means no matching record in `hexsides.v1.json`, not certified absence of a feature. Checks and other overprints conceal underlying ink. A separate [border evidence review](2026-11-17-graziani-border-mark-next.md) discusses their possible identification; this audit does not settle their meaning or effect. The pinned hex record gives `clear`, no settlement and `coastal: false`; the central cell appears pale and inland in this crop, with no settlement dot visible inside its bounds. Fort Capuzzo dot lies in neighboring C4020, near the crowded vertex. This is a local same-scan reading, not an independent edition, full map validation or rights grant.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6450 -y 7050 -W 1750 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3920-six
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `dafac62c78c7fcbbca1a9a31d6b8e53ac1766f40758b817b402dd511660e61ce` (renderer-dependent); pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. PDF and crop remain outside Git. No runtime or data changes; no gameplay tests run.

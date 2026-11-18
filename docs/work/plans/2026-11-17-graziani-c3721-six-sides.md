# Sidi Suleiman C3721: six-side visual source audit

**Finding:** Original Map C shows a dashed track crossing the `C3620` and `C3821` sides of C3721; both match explicit pinned entries. The four other neighboring sides show thin grid where exposed and have no explicit pinned pair. Place-name ink covers part of the southern linework; no complete feature-free, legal-edge or route certification follows.

## Original map observations

Visually inspected [Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. The `3721` label and Sidi Suleiman blue dot lie in the central hex. In the crop below its approximate vertices are `(750,520)`, `(940,620)`, `(940,840)`, `(750,950)`, `(560,840)`, `(560,620)`; directions refer to the displayed crop. Key depicts a track with single dark dashes, unfinished railroad with blue cross-ties, and escarpment with a broad dark band and splash.

| Neighbor | Crop side | Source observation and pinned comparison |
| --- | --- | --- |
| `C3820` | upper-left diagonal | Thin grid visible; no track or broad escarpment observed on exposed line. No explicit pinned pair. |
| `C3821` | upper-right diagonal | Single dark dashed track crosses, covering a short stretch of grid. Pinned pair lists `track`. Nearby blue cross-tied railroad is separate; do not infer it crosses this side from proximity. |
| `C3722` | right vertical | Thin grid visible; `Sidi` lettering overlaps near the lower vertex. Blue railroad lies in the neighboring hex. No explicit pinned pair; covered vertex remains unknown. |
| `C3621` | lower-right diagonal | Blue place-name ink overlaps part of side and bottom vertex; exposed grid appears thin. No explicit pinned pair; hidden section unknown. |
| `C3620` | lower-left diagonal | Dark dashed track crosses; dash overprints the line and nearby name ink approaches its lower endpoint. Pinned pair lists `track`. |
| `C3720` | left vertical | Thin grid visible; no identified track or broad escarpment on exposed line. No explicit pinned pair. |

No explicit pair record means only that the sparse file lacks one; it does not certify an unmarked source edge. Track dashes and name overprint conceal underlying line segments, and nearby blue railway and distant escarpment cannot be transferred to an edge by proximity. Movement and ZOC require independent terrain, unit and rule checks.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 600 -x 7000 -y 7900 -W 1800 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3721-six
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `e937ab6fa944699e35d3bc193076c56c8943800cf0946857a2d0dbe7608ad1b9` (renderer-dependent); pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same source edition and informed prior map work, not an independent physical sheet. No runtime changes or tests.

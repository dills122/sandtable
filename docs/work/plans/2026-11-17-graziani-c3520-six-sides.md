# C3520: six-side visual source audit

**Finding:** Original Map C shows dark dashed track crossings on `C3520–C3620` and `C3520–C3419`, matching the two explicit pinned side entries. The four remaining sides show thin grid on visible portions and have no explicit pair entry. This local reading does not certify hidden ink, full edge coverage, a route, movement or ZOC legality.

## Original map observations

Visually inspected [Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. In the crop below, `3520` labels the central hex. Its approximate vertices are `(870,665)`, `(1060,765)`, `(1060,985)`, `(870,1090)`, `(680,985)` and `(680,765)`. Directions refer to the displayed crop. The key depicts track as a single dark dashed line; escarpment is a broad dark band with splash; its gray `Border` sample differs from the nearby blue checks.

| Neighbor | Shared side | Visible source and pinned comparison |
| --- | --- | --- |
| `C3619` | upper-left diagonal | Thin grid visible; no track or broad escarpment seen. Nearby Sidi Omar name and track lie in adjoining territory. No explicit pinned pair. |
| `C3620` | upper-right diagonal | Dark dashed track crosses; the dash overprints the crossing. Pinned pair lists `track`. |
| `C3521` | right vertical | Thin grid visible; no identified feature on the exposed side. No explicit pinned pair. |
| `C3420` | lower-right diagonal | Thin grid visible; no identified track or wide escarpment on exposed side. No explicit pinned pair. |
| `C3419` | lower-left diagonal | Dark dashed track crosses; dash covers part of grid. Pinned pair lists `track`. This is the Sheferzen contact side; see [prior check](2026-11-17-graziani-map-first-rtr-side.md). |
| `C3519` | left vertical | Thin grid visible; a separate dashed track is inside C3519 and crosses another side, not visibly this vertical one. No explicit pinned pair. |

Track ink hides the short underlying stretches it crosses. For the other sides, visible thin line and absence of a pinned entry are bounded observations, not proof that no subtle or obscured source feature exists. The blue checkered line remains west of these sides; its political-boundary interpretation and possible effects are separately assessed in [border review](2026-11-17-graziani-contact-border-rules-review.md). Unit state, terrain entry, ZOC eligibility and any coincident feature require independent checks.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6500 -y 8400 -W 2100 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3520-six
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `a35a4376afc3ac2dfaa6047ac020216853f118040d710b9a8f07ea0d74eb8b6b` (renderer-dependent); pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same source edition and informed prior reading; no independent sheet, runtime edits or tests.

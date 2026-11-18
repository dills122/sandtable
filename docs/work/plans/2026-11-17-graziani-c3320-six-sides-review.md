# C3320 six sides: independent visual review

**Disposition:** The [six-side audit](2026-11-17-graziani-c3320-six-sides.md) identifies the six locally adjacent labels correctly and appropriately reserves overprinted pixels. Visible linework does not establish a track crossing any of the six sides; this is a bounded visual observation, not a complete feature-free or legal-edge certificate.

## Original map check

Re-rendered [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 at 600 dpi, crop `-x 6500 -y 9050 -W 2100 -H 1800`. The readable `3320` label lies in the central hex. In crop coordinates approximately: top vertex `(870,660)`, upper-left `(680,765)`, upper-right `(1060,765)`, lower-left `(680,985)`, bottom `(870,1090)`, lower-right `(1060,985)`. The neighboring labels and exposed sides are:

| Neighbor | Check |
| --- | --- |
| C3419 | Upper-left diagonal; blue `Bir el Sheferzen` lettering overlays a substantial part and its endpoint. Thin line visible between marks; hidden section unresolved. |
| C3420 | Upper-right diagonal; visible portions appear thin, with text near its left/top vertex. Hidden and crowded vertex not certified. |
| C3321 | Right vertical; name lettering intersects its upper portion. Visible remainder is thin grid. |
| C3220 | Lower-right diagonal; visible narrow grid, no clear track or broad escarpment on exposed stretch. |
| C3219 | Lower-left diagonal; visible narrow grid. Dashed track crosses the adjacent `C3319–C3219` side west of C3320's lower-left vertex; its neighboring presence is not a C3320 crossing. The dash pattern and shared vertex do not justify extrapolating hidden or missing marks. |
| C3319 | Left vertical; thin grid visible with lettering near upper endpoint. Nearby track remains to its west and does not visibly cross it. |

The blue checkered line follows sides farther west, not an exposed C3320 side. Its identity and effects remain separately interpreted. The terrain key distinguishes a dark single-dash track, broad dark escarpment, and gray `Border` example; overprint still prevents conclusions about concealed linework. Literal search of pinned `hexsides.v1.json` found no `C3320` entry, which does not establish feature-free source coverage.

## Provenance

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `2df1b51c6287e3ae34d15def957b3df0c2ac7acd4186736987c0b9607f2120ac`; pinned side JSON SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same source scan, not independent edition; informed by the audited note. No runtime changes or tests.

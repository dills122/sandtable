# E1829: six-side visual source audit

**Finding:** Map E shows Cairo city artwork over E1829 and neighboring cells, with river and transport artwork concentrated around its eastern and southern boundaries. A road visibly crosses the E1830 side, but its pinned pair lists only minor river; this is a candidate road omission. Printed black training-area silhouettes obscure parts of the artwork. The pinned side data records five pairs; identifying every feature at crowded vertices remains uncertain. This is not an occupancy or legal-movement ruling.

## Source observations

Inspected [original maps and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 5 and 1. Directions below refer to displayed map crop, not compass bearings. E1829's printed number is obscured; cell identification follows visible neighboring numbers and hex-grid continuity. Approximate central vertices in `cairo.png`, clockwise from top: `(310,262)`, `(348,283)`, `(348,327)`, `(310,348)`, `(273,327)`, `(273,283)`. Some labels also overlap artwork; neighbor identification uses grid sequence where obscured.

| Neighbor | Shared side | Visible marks and pinned side data |
| --- | --- | --- |
| `E1929` | Upper-left diagonal | Grid line visible through pale background; river artwork reaches upper vertex. No explicit pinned pair. |
| `E1930` | Upper-right diagonal | Road and railroad cross the diagonal east of its upper vertex in blue river artwork. City and silhouette overprint obscures other portions of the routes. Pinned pair has `road`, `railroad`. |
| `E1830` | Right vertical | Blue water band follows side; dark parallel road lines visibly cross the side near its middle and continue east through E1830. Silhouettes obscure portions. Pinned pair has `minor-river` only, making road a candidate omission; scan alone does not establish a data correction. |
| `E1730` | Lower-right diagonal | Blue river band follows side, with city artwork and silhouettes around it. Pinned pair has `major-river`. |
| `E1729` | Lower-left diagonal | Dark parallel road lines and blue railway cross or converge near side; river and city artwork crowd its lower vertex. Pinned pair has `road`, `railroad`. |
| `E1828` | Left vertical | Blue cross-tied railroad crosses; city marks and silhouettes overlap. Pinned pair has `railroad`. |

Map A key identifies blue building clusters as major city, green fill as delta, dark parallel lines as road, blue cross-tied line as railroad, blue river bands as major or minor river, and black figures as training area. Printed silhouettes are map artwork, not evidence of a specific scenario unit. Nile artwork, lines, city blocks, hex edges and silhouettes overlap heavily; major/minor identification and exact endpoint attribution at junctions require care. No absence claim follows from lack of a pinned pair.

Map A's Summary of Important Locations lists Cairo at `E1730/1829/1830/1930/1931`, marking Major City and Victory Condition, with dashes for Port and Assigned Garrison. This lists a multi-hex location; it does not choose a scenario setup hex. Pinned [`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records E1829 as `major-city` terrain and settlement, `coastal: false`. All five pinned [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) records have `side: null` and `up: null`. An [independent review](2026-11-17-graziani-e1829-six-sides-independent-review.md) checked the same scan and prompted the two transport clarifications. This is a same-scan visual reading, not independent-edition verification or a rights grant.

## Reproduction

```sh
mkdir -p /private/tmp/cairo-e1829
pdftoppm -f 5 -l 5 -r 120 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-e1829/map-e-120
pdftoppm -f 1 -l 1 -r 120 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-e1829/map-a-120
python3 - <<'PY'
from PIL import Image
p = '/private/tmp/cairo-e1829/'
Image.open(p+'map-e-120.png').crop((1900,2650,2570,3300)).save(p+'cairo.png')
a = Image.open(p+'map-a-120.png')
a.crop((900,100,1740,800)).rotate(270, expand=True).save(p+'key-upright.png')
a.crop((540,1550,1400,2350)).rotate(270, expand=True).save(p+'location-summary.png')
PY
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Cairo crop `d0fd72449a450ac48a1085590fa5877a739a702d200f2a76fa62ba9658e2639f`; key crop `a522c9ed3d1c74a8f0359a603062a7d53f5ea6440d6a85eb23fae343a05468b4`; location-summary crop `155e00c423a0d8f4bd967a13bf7dc385434e5c0ec527351fffc8d97ecb606218` (crop hashes renderer-dependent). Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crops remain outside Git. No runtime changes or gameplay tests.

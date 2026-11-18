# Map E rail sweep B: Jebel Hamzi and delta branches

**Date:** November 18, 2026. **Disposition:** Fifteen previously unreviewed pinned pairs checked in one bounded crop. Twelve show a source-positive crossing; three remain uncertain near vertices. All fifteen have a literal `railroad` pin. These observations do not establish an exhaustive rail inventory, a route, construction condition, occupancy, or an unblocked result.

Inspected [original Map E scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 5, and the p. 1 terrain key, with the [pinned inventory](2026-11-18-graziani-pinned-map-e-rail-inventory.md) and [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) available. This is an informed same-scan comparison, not independent-edition corroboration. Printed cell numbers and shared grid identify pairs. The key distinguishes solid blue cross-tied railroad from broken blue cross-tied unfinished railroad. The local centerlines appear continuous at these crossings at this scale; overprint and scan quality limit finer distinctions. Coordinates below are approximate pixels in the unrotated 2400 × 1900 crop, not geographic directions. `Positive` means visible cross-tied rail crossing the named side; `unknown` means attribution is not secure. Neither is a complete classification of all side features.

| Pinned pair | Scan observation | Pin features |
| --- | --- | --- |
| `E1927–E2026` | Unknown: line meets diagonal near vertex around (1230, 1440). | `railroad` |
| `E2025–E2026` | Positive: crosses vertical side near (1068, 1355). | `railroad` |
| `E2025–E2125` | Positive: crosses diagonal near (935, 1280). | `railroad` |
| `E2124–E2125` | Positive: crosses vertical side near (783, 1200). | `railroad` |
| `E2124–E2223` | Unknown: line passes close to grid vertex near (660, 1120). | `railroad` |
| `E2223–E2323` | Positive: crosses diagonal near (560, 960). | `railroad` |
| `E2323–E2422` | Positive: crosses diagonal near (470, 805). | `railroad` |
| `E2421–E2422` | Positive: crosses vertical side near (310, 690). | `railroad` |
| `E2422–E2523` | Unknown: line approaches shared diagonal close to its vertex near (485, 650). | `railroad` |
| `E2523–E2623` | Positive: crosses diagonal near (550, 480). | `railroad` |
| `E2623–E2723` | Positive: crosses diagonal near (550, 310). | `railroad` |
| `E2723–E2823` | Positive: crosses diagonal near (545, 150). | `railroad` |
| `E2226–E2326` | Positive: crosses diagonal near (1120, 960). | `railroad` |
| `E2326–E2425` | Positive: crosses diagonal near (1035, 790), with river ink nearby. | `railroad` |
| `E2425–E2526` | Positive: crosses diagonal near (1015, 635), beside river artwork. | `railroad` |

All 15 records have `side: null` and `up: null`. Source-positive means local printed ink only; it does not prove service, construction history, bridge condition, legal movement, continuous network or control. Unknown is not absence. This selection excludes recent Cairo/Nile and Suez reviews and makes no claim about omitted sides or other sheets. A §8.78 result also needs separately justified geographic bounds, applicable rail policy and fixed-snapshot Axis combat occupancy.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 300 -x 3800 -y 5700 -W 2400 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 200 -W 1400 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/kafr-review-key
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle.png /private/tmp/kafr-review-key.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256 in command order: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, `ee0df7def4b2ffc194b9c55b822f3e0291e9b1770305cbf70644ef36390bd0b1`, `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`, `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer. Crops remain outside Git. Documentation only; no runtime or map-data changes and no gameplay tests.

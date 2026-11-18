# Map E rail sweep C: independent visual review

**Date:** November 18, 2026. **Result:** Eight rail crossings supported; one remains unknown. No contradiction found in the nine pinned railroad entries. Review used the same scan and crop as the sweep, after reading its claims; it is neither blind nor independent-edition corroboration.

## Evidence and scope

Reviewed [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5, against the terrain key on page 1. The key depicts railroad as a continuous blue line with cross-ties, unfinished railroad as broken blue line with cross-ties, and road as paired dark lines. Hex labels and grid establish the pairs below. Coordinates refer to the sweep's unrotated 950 × 940 detail crop, with origin at upper left. Road and river pins were not independently evaluated.

| Pair | Independent source reading | Pin comparison |
| --- | --- | --- |
| `E2823–E2923` | Blue cross-tied line crosses diagonal near `(400, 830)`. | `railroad` supported |
| `E2923–E3023` | Cross-tied line crosses diagonal near `(400, 730)`, over blue river artwork. | `railroad` supported |
| `E2824–E2924` | Cross-tied line crosses diagonal near `(540, 840)`; river bend crowds the side. | `railroad` supported |
| `E2924–E3024` | Cross-tied line crosses diagonal near `(530, 730)`. | `railroad` supported |
| `E3023–E3024` | Cross-tied ink approaches the shared vertical near `(494, 650)`; river and paired road obscure the crossing near the upper vertex. Exact side attribution remains **unknown**. | `railroad` unresolved, not contradicted |
| `E3023–E3123` | Cross-tied line crosses diagonal near `(405, 620)`; junction and road overprint limit clarity. | `railroad` supported |
| `E3024–E3025` | Cross-tied blue line crosses vertical near `(620, 680)` alongside paired dark road. | `railroad` supported |
| `E3025–E3026` | Cross-tied blue line crosses vertical near `(745, 680)` alongside paired dark road. | `railroad` supported |
| `E3026–E3027` | Cross-tied blue line crosses vertical near `(870, 680)` amid road and river artwork. | `railroad` supported |

Checked [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json): all nine exact pairs occur once with `railroad`, `side: null`, and `up: null`. Additional features match the sweep's table; this check does not validate those features. These are local visual readings only. No unpinned sides were surveyed; no network, route, construction, damage, service, occupancy, or rules outcome is established.

## Source pin and reproduction

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; detail crop `ef0ec377e5653855e225d59889f6438a31856691d28efed5e4eaf1c90bfe1ae5`; key crop `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`; hexside pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop generation commands appear in [sweep C](2026-11-18-graziani-map-e-rail-sweep-c.md); renderer and image library may change PNG bytes. No runtime or map-data changes; no gameplay tests.

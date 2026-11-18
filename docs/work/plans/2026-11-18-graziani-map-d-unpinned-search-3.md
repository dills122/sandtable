# Map D bounded unpinned rail search: D3516

**Date:** November 18, 2026. Inspected all six D3516 sides, including the documented missing-rail leads in the [worklist](2026-11-18-graziani-map-d-rail-worklist.md). Two blue rail crossings are source-supported but absent from the literal pinned features. The diagonal is vertex-qualified; the vertical crossing is in its interior. Neither observation admits a data correction or proves other unpinned features absent. This same-scan search is bounded to D3516, not a full-sheet inventory.

| Literal sorted pair | Blue rail reading | Complete literal pin and other observations |
| --- | --- | --- |
| `D3516|D3517` | Blue cross-tied centerline crosses the shared near-vertical side well between vertices, near `(855, 688)` in the crop. Local line appears continuous, supporting a candidate missing `railroad`; see [targeted conflict review](2026-11-18-graziani-d3516-rail-conflict.md). | No literal pair. Road and coast crowd upper vertex; road attribution remains unsettled. |
| `D3516|D3615` | Blue centerline appears to cross the upper-left diagonal near its upper vertex and continues through D3516: candidate missing rail, qualified by crowded grid, slope and other ink. | `track` only. Dark ink near junction does not independently settle the pinned track crossing. |
| `D3516|D3616` | No distinct blue centerline crossing established on the upper-right diagonal; nearby rail lies within D3516 below it. Vertex, road and overprint limit absence claim. | `track` only; dark marks at junction leave its exact-side attribution uncertain. |
| `D3515|D3516` | No blue rail crossing identified on exposed vertical side. A blue branch lies in adjacent D3515; do not assign it to the vertical side from proximity. | `slope`; brown band lies along the side. Direction or complete feature inventory not assessed. |
| `D3415|D3516` | No blue rail crossing identified on exposed lower-left diagonal. | `slope`, `track`, `up: D3415`; brown band and dark dashed track appear on that side. Slope direction not reassessed. |
| `D3416|D3516` | No blue rail crossing identified on exposed lower-right diagonal. | No literal pair. Missing pair does not establish clear terrain or absence of other features. |

All six sides and their vertices fit within the crop. D3516's vertical D3517 side runs approximately `(855, 605)` to `(855, 790)`; its shared D3615/D3616 vertex is near `(700, 510)`. Approximate coordinates do not imply survey precision. Blue ties, dark road and dark dashed track are different marks; neither adjacency nor a pin label independently assigns one feature to a crossing. Exposed-portion observations do not certify absence beneath overprint. No statement about construction, usability, control or a complete route follows.

[Original Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Exact incident literal pairs, features and `up` checked in JSON. Reproduce the same 500-dpi crop used by the targeted review:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 4195 -y 7050 -W 1400 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-d3516-batch
shasum -a 256 /private/tmp/graziani-d3516-batch.png
```

Crop SHA-256 `bb50045612a467a9a673038c78198aa45ad018d867430fe0cabf20ff5e208268`; renderer affects PNG bytes. The embedded image is 300 ppi, so output at 500 dpi interpolates existing pixels. Informed same-scan inspection, not an independent edition. Documentation only; no data or runtime edits, and no gameplay tests.

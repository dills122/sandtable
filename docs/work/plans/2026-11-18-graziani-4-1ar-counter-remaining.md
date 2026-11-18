# 4/1AR: remaining counter-sheet inventory

**Result:** The remaining scanned sheets expose shared Axis/neutral markers in Sections 5 and 9, but I found no additional readable `4/1AR` or `4/2AR` face in those marker areas. Section 4 is marked German and contains German land/air faces; no readable exact `4/1AR` or `4/2AR` was identified there either. Sections 6–8 are marked Commonwealth/British/Indian/South African/Australian and were classified by sheet overview, not exhaustively transcribed. This does not prove absence from all physical counters, backs, illegible areas, corrections or editions, and does not resolve scenario punctuation.

| PDF area | Visible layout and bounded result |
| --- | --- |
| p. 1 Section 1, two land blocks | Previously checked by the [Section 1 review](2026-11-18-graziani-4-1ar-source-next-review.md); no readable `4/1AR` identified there. I checked layout only in this pass, not every label anew. |
| p. 1 Section 2, both blocks; p. 2 Section 3, both blocks | Previously swept and [independently reviewed](2026-11-18-graziani-4-1ar-counter-sweep-review.md). Section 2 left block row 8 column 1 is the known `4/1AR`, followed by `1/2AR` through `4/2AR`. Section 3 includes Italian aircraft, SGSU, Blackshirt and shared markers. No second readable `4/1AR` was found in those bounded sweeps. These are the same faces, not additional copies. |
| p. 2 Section 4 | Inspected both 10-column blocks at 200 dpi. Sheet is labeled **GERMAN**; displayed faces are German formations, artillery, personnel and SGSU/replacement/transport symbols. No exact `4/1AR` or `4/2AR` identified in the readable labels. Yellow highlights and small letters limit a glyph-by-glyph negative. |
| p. 3 Section 5 | Inspected both blocks at 200 dpi. Axis area contains numbered dump-like discs, trucks, ships, repair and vehicle markers; neutral area contains airfield/other markers; remainder is labeled German or Allied/Commonwealth and has named non-Italian faces. No readable exact `4/1AR` or `4/2AR` identified. Shared Axis markers could be used by Italians, but do not bear a distinct Italian artillery identity. |
| p. 3 Section 6 and p. 4 Sections 7–8 | Overview shows Commonwealth aircraft, logistics, fleets and British, Indian, South African and Australian land groups. No Italian-labeled block was identified in the overview. These pages were **not** systematically searched at legible glyph resolution; do not turn this classification into a face-by-face negative. |
| p. 5 Section 9 front and p. 6 Section 9 back | Inspected both at 200 dpi. Axis, Allied and neutral areas show generic discs, prison/POW, weather, construction, reserve and other markers; no named Italian artillery face or readable exact `4/1AR` or `4/2AR` identified. The printed p. 6 header specifically calls it Section 9 **Back**. Shared or blank marker symbols are not evidence of a distinct named artillery unit. |

Section 1–8 backs are not independently established by this six-page scan inventory; do not assume these displayed fronts exhaust physical counter identities. Small letters, yellow highlighting, dark backgrounds, scan quality and image/page edges may hide or truncate details. The negative concerns only an exact readable face label, not semantic aliases, unlabeled duplicate pieces or a physical-sheet count. The prior [OA comparison](2026-11-18-graziani-4-1ar-counter-sweep.md) identifies distinct `4/1AR` and `4/2AR` rows and dates; that is not evidence that one scenario occurrence should be replaced by the other. No source correction is inferred.

## Reproduction and hashes

[Counter PDF](https://spigames.net/PDFv10/CNA_Counters.pdf) has six pages and prints nine section types; Section 9 front/back are on separate pages. PDF SHA-256 `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`. Full page overviews at 80 dpi were used for classification; detailed crops at 200 dpi:

```sh
pdftoppm -f 2 -l 2 -r 200 -x 0 -y 1400 -W 2200 -H 1500 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/counter-rem-sec4
pdftoppm -f 3 -l 3 -r 200 -x 0 -y 0 -W 2200 -H 1400 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/counter-rem-sec5
pdftoppm -f 5 -l 5 -r 200 -x 0 -y 0 -W 2200 -H 1400 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/counter-rem-sec9front
pdftoppm -f 6 -l 6 -r 200 -x 0 -y 0 -W 2200 -H 1400 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/counter-rem-sec9back
```

Crop SHA-256 respectively `abcead0c7feabfc575f96e7f95d4568526af37c593a8f61640a36ee112c1850f`, `354bb21dc4ece538f5acf8011799eab02f41d14cca9b900f1959ace42cd8b102`, `6bcf0984dde8c90c4e321b54dbc3d0825d04eecf844f4bd0d729bd31983f985d`, `a36f8b5e8a5d61ab82a0d462a253df9ded250924b27c6bbc710d9c22bcafe0b5` (renderer-dependent). This is a same-scan, informed visual inventory. No runtime/data changes or gameplay tests.

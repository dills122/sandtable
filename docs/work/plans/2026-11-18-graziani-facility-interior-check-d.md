# Landing-strip interiors: five-cell map check

**November 18, 2026.** Informed visual second reading of the five numbered cells in [landing-strip batch A](2026-11-18-graziani-landing-strip-edges-a.md), using its existing crops and the p. 1 key from the same [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf). This is not an independent map edition. Scenario names and coordinates come from the linked list; a locality dot does not establish a landing-strip symbol, capacity or control. Source and crop hashes checked below.

| Cell | Grid and interior observation | Uncertainty |
| --- | --- | --- |
| C4108 Bir el Gubi | The printed 4108 and adjacent 4208, 4109 and 4008 identify the cell. A blue dot associated with Bir el Gubi lies inside, partly crossed by dashed track ink. Exposed interior is pale; green wadi artwork and intersecting tracks crowd its western side. | The key's dot is shared by village/bir; the printed name itself reads Bir el Gubi. Overprinting limits reading of underlying details; pale interior alone does not certify all terrain. |
| E3109 Burg el Arab | 3109, between 3108 and 3110 below 3209, contains a dark blue/green round mark beside the Burg el Arab label. Brown patterned artwork and pale patches are crossed by track and route ink. | Dot is overlapped by dark route ink and label; this limits detail and color reading. The key depicts rough in brown, but mixed patterns and crossings do not determine a complete terrain class. |
| E3007 El Hamman | 3007 between 3006 and 3008 contains a blue dot near its upper boundary, alongside road ink and near blue railroad; El Hamman lettering overlaps surrounding grid. Exposed interior is pale. | Dot is close to the boundary and route overprint; no inferred terrain or side-crossing certification follows from the locality association. |
| C4414 Gambut | Numbered 4414 between 4413 and 4415, below 4514/4515, contains the blue dot by Gambut. Pale central artwork with brown ridge-like bands and a dashed track is visible. | Brown bands follow grid in a crowded region. Do not equate the pale center with a complete terrain classification or infer a separate gravel pattern from the pin. |
| B5410 Maraua | The 5410 cell between 5409 and 5411, below 5510/5511, contains the blue/green Maraua dot beside the paired road and dashed tracks. Brown-toned fill is visible, with surrounding mixed brown artwork. | Dot and routes overlap; scan color and mixed artwork limit terrain classification. Near-vertex route assignment remains as qualified in the batch review. |

The visual [map key](https://spigames.net/PDFv10/CNA_Maps.pdf) p. 1 depicts village/bir as a shared blue dot and airfield as a distinct blue symbol. No separate airfield symbol was identified in these bounded crops; this is **not** proof that none exists beneath overprint or elsewhere. None of these observations certifies full terrain, side features, placement, or legal movement. The earlier [batch A review](2026-11-18-graziani-landing-strip-edges-a-review.md) focused on side comparisons; this check addresses the visible interior marks separately.

## Reproduction

Source PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Existing 300-dpi 1000×1000 crops and page-1 key were visually opened; hashes match prior records. Reproduce facility crops with `pdftoppm -f PAGE -l PAGE -r 300 -x X -y Y -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/CELL`:

| Cell | Page | X | Y | Crop SHA-256 |
| --- | ---: | ---: | ---: | --- |
| C4108 | 3 | 1048 | 3196 | `0c3d708d5460e95eba94ab4810ad345d9160d9a8c3528ad42258df58deabd0b3` |
| E3109 | 5 | 1120 | 4756 | `fbc1f3514dfb768a04bbdf59b1b7bb9e1eea57f91267449cfe16dd1b150f8304` |
| E3007 | 5 | 748 | 4912 | `8782bfabd3e3436f442cb7a53575e564bd9d5565454956ca0fa4f8cc79218dda` |
| C4414 | 3 | 2164 | 2728 | `f91afd09a662910f6e13478c321bfd1e9bfdb10ffe9a3b271361894254ab36f4` |
| B5410 | 2 | 1480 | 1060 | `83da49888aa9a07543465faf63985984ea8d8411e844c226482a52f3c9d66100` |

Key crop command: `pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey`; SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`. PNG hashes depend on renderer. No runtime/data edits or gameplay tests.

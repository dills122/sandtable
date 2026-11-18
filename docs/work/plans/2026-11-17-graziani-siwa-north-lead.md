# Siwa north lead: C0426 continuation

**Date:** November 17, 2026. **Status:** local candidate ink reading; no route or movement proof. Continues only the C0426 lead from the [Siwa branch batch](2026-11-17-graziani-siwa-branch-batch.md), on the same scan. Image directions follow the rendered sheet.

| Observation | Source reading and competing interpretation | Pinned comparison |
| --- | --- | --- |
| C0426 → C0527 | Paired dashes emerge upward from brown artwork in C0426 and appear to cross the upper-right side into C0527. The crossing is very close to the C0426/C0427/C0527 vertex: one dash lies nearer the vertex and artwork obscures its approach. A C0427 side attribution for part of the ink remains a competing reading; the scan alone does not settle a vertex convention. | `C0426|C0527`: `unfinished-road`. |
| C0527 junction candidate | Paired dashes continue toward C0627; a single-dashed lead approaches from C0626. The nearest single dash ends short of the paired marks, so an apparent fork is plausible but its exact join is not visible. Stop at this first candidate fork and gap; outgoing leads are not traced here. | `C0527|C0627`: `unfinished-road`; `C0527|C0626`: `track`. These records compare the visible leads, not proof of their interior join. |

The preceding C0327–C0426 paired line was recorded in the earlier batch; its precise join through C0426's brown artwork is not newly certified. Brown interior artwork and its relation to the route and sides remain unclassified. Other terrain, side features, vertex ownership and off-route continuations are unknown; sparse pinned records are VASSAL-derived comparisons, not independent original-sheet evidence or a complete feature inventory. The [key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1, distinguishes paired-dashed unfinished road from single-dashed track. Nothing here establishes connectivity to Tobruk, convoy legality, or its impossibility. A separate challenge review and further source/state analysis remain needed.

**Reproduction:** [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3. Render and extract the junction crop with:

```sh
pdftoppm -f 3 -l 3 -r 600 -x 9000 -y 16000 -W 3000 -H 4100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-north
python3 -c 'from PIL import Image; im=Image.open("/private/tmp/siwa-north.png"); im.crop((550,2600,1300,3400)).save("/private/tmp/siwa-north-junction.png")'
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/siwa-north.png /private/tmp/siwa-north-junction.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; 600 dpi render `4f4954cc28fb3ea8c71a521c85eda1d9c2f2dafba6cd8252b00390233ed8cbde`; junction crop `46187d5fed8808f64b74bf93a6629db073820eca45e9936c52c66aea66e14aec`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Image hashes depend on renderer and extraction. Same scan, not an independent edition or physical sheet. No runtime or map-data change; no gameplay test.

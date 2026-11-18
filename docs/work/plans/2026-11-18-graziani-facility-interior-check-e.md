# Facility interior source check: B5917, C4119 and D3416

**Result:** Same-scan visual rereading supports the `B5917` Zt. Tert dot inside its numbered cell, the predominantly pale `C4119` interior, and brown rough-like artwork in `D3416`. No separate landing-strip symbol was identified in the exposed portions. These are bounded observations, not complete terrain or facility certifications.

| Numbered cell | Interior observation and uncertainty |
|---|---|
| `B5917` | Printed `5917` lies in the cell bounded by numbered `5916/5918` and the surrounding grid. A blue-green dot under the **Zt. Tert** label lies inside that cell, over brown terrain artwork and the paired route. The key's dot is **Village/Bir** and does not itself distinguish those two settlement types. The first reading similarly identifies name and dot. Brown ink, route and lettering obscure portions; this check does not classify the entire cell or establish a landing-strip symbol. |
| `C4119` | Printed `4119` sits in a pale cell bounded by the neighboring `4118`, `4120`, `4219` and `4019` cells; blue cross-ticked and dark dashed routes cross its interior. The Sidi Azeiz dot above is in `C4218`, not `C4119`. I see no distinct locality dot or landing-strip symbol in the exposed target; route and numeral ink prevent a categorical absence claim. This is consistent with the first reading's pale interior and no identified settlement. |
| `D3416` | Partly overprinted `3416` sits below Gerawla `D3516` and beside `D3417`; grid continuity identifies the target. Its interior has substantial brown fill resembling the key's **Rough** sample, pale patches and a darker slope-like band along the lower portion. Gerawla's dot is in `D3516`, not this cell. No distinct locality dot or landing-strip symbol was identified within exposed `D3416`; the number and terrain artwork conceal portions. The first reading's rough-cell observation is supported as a visual lead, not a whole-hex or precedence rule. |

The [map key](https://spigames.net/PDFv10/CNA_Maps.pdf) p. 1 depicts Village/Bir, clear/rough and the different line and air-facility symbols. A dot is not itself a landing strip, and an unobserved symbol is not proof that the scenario-listed facility is absent. The [first-reading batch](2026-11-18-graziani-landing-strip-edges-b.md) and [independent side challenge](2026-11-18-graziani-landing-strip-edges-b-review.md) separately discuss side ink and pinned fields; this pass does not certify edges, movement, control, ownership or capacity. Scenario facility names/coordinates are carried from those notes, not independently re-read here.

## Reproduction and scope

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; inspected key p. 1 and independently re-rendered the prior 500-dpi crops:

```sh
pdftoppm -f 2 -l 2 -r 500 -x 4574 -y 587 -W 1400 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/if-e-b5917
pdftoppm -f 3 -l 3 -r 500 -x 5295 -y 5415 -W 1400 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/if-e-c4119
pdftoppm -f 4 -l 4 -r 500 -x 4195 -y 7320 -W 1400 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/if-e-d3416
```

Crop SHA-256 respectively `2aefd896fff564b5da9137d55a261af2c4495a33ace41e0095108be739f4c8f3`, `9a3e01666d254df652303c643aad867ba60cd5f5813d4705aa8260c6aa640da6`, `201e1041bfa6396dc98158d51782097002c93ca604837ce110e1f1b4d8e3246d` (renderer-dependent). Same scan, not an independent physical edition. No map/data/runtime edits or gameplay tests.

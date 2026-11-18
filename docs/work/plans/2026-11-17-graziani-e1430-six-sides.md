# E1430 Helwan: six-side original-map reading

**Finding:** Map E prints Helwan in E1430 with major-city blocks and Commonwealth training-area figures. Broad blue river artwork occupies its western side and much of the upper-left side; paired road lines meet the lower sides amid dense overprint. Pinned data records four of six pairs, including a `minor-river` on E1330; the visible blue artwork and congestion do not establish that minor-river classification. This is a bounded source reading, not a proposed data correction or legal movement decision.

## Cell and sides

On PDF p. 5, the `1430` numeral is partially overprinted by blue water and figures, but adjacent numbered cells identify the Helwan hex. In the 400-dpi crop below, approximate vertices clockwise from top are `(620,570)`, `(746,642)`, `(746,782)`, `(620,854)`, `(493,782)`, `(493,642)`. Directions refer to the rendered crop. Blue building blocks match the p. 1 key's **Major City**; three soldier figures match **Training Area**, with `CMNWLTH` overprint. `Helwan` name, route lines, figures, text, and river obscure parts of cell and edges. Pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) says `terrain: major-city`, `settlement: major-city`, `coastal: false`.

| Neighbor | Visible side and pinned data |
| --- | --- |
| E1530, upper-left diagonal | Broad blue river artwork overlaps much of side toward top vertex; grid and blue bank intersect. Pinned `major-river`. |
| E1531, upper-right diagonal | Grid is visible along much of side; blue bank reaches top vertex and city blocks lie nearby. No literal pinned pair; endpoint overlap limits negative claims. |
| E1431, right vertical | Grid visible along side; large `Helwan` name begins to its right. No literal pinned pair. |
| E1331, lower-right diagonal | Paired dark lines run from city across this side into E1331, consistent with key's road. Pinned `road`; figures, lettering and adjacent blue artwork crowd its western portion. |
| E1330, lower-left diagonal | Paired dark route lines cross near left endpoint; blue water occupies much of side toward lower vertex and `CMNWLTH` lettering obscures the junction. Pinned `minor-river`, `road`. Road reading is supported, but blue water here continues the broad river artwork, and this crop does not independently establish a separate minor-river symbol or exact boundary classification. |
| E1429, left vertical | Blue river artwork covers much of side; paired road and blue cross-tied railway lie nearby to the west, with lines crowding lower endpoint. Pinned `major-river`. Do not infer a road or railway crossing this side from proximity alone. |

Key distinguishes broad blue **Major River** from narrower **Minor River**, paired solid dark **Road**, and blue cross-tied **Railroad**. A missing pair is a literal data observation, not proof of a featureless edge. Source artwork can obscure underlying marks, especially at shared vertices. The [setup inventory](2026-11-17-graziani-fixed-contact-inventory.md) cites E1430 for the Helwan alternative; this reading neither selects a setup location nor establishes occupancy, edge legality, ZOC, network connectivity, or terrain effects.

## Source and reproducibility

[Original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 5 Map E and p. 1 terrain key. `/private/tmp/sandtable-maps.pdf` SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.

```sh
pdftoppm -f 5 -l 5 -r 400 -x 7000 -y 10000 -W 1300 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1430/helwan400
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e1430/key
```

PNG SHA-256 in command order: `9e4e85d861d6bb7be270a276f9d1793b83fdc97fa85851878c93e0b78573f180`, `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`. Renderer bytes may vary. Crops stay outside Git pending rights. Pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Inspection used existing inventory and pinned data, not a blind reading or independent edition. [Independent same-scan review](2026-11-17-graziani-e1430-six-sides-independent-review.md) confirmed the other side observations and corrected the E1431 text placement. No runtime changes or gameplay tests.

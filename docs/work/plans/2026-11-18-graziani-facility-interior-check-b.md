# Facility interior source check: El Berca, Martuba and B5825

**Result:** Fresh 600-dpi readings of the retained map scan support an El Berca dot in `A4728` and a Martuba dot in `B5526`. The inspected `B5825` interior is crossed by road and brown artwork; the Derna dot and anchor are in neighboring `B5925`. No distinct airfield symbol was identified in these crops. This is a bounded observation, not certification of complete terrain, facility location or absence of symbols beneath overprint.

| Cell and grid evidence | Interior observation and limits |
|---|---|
| `A4728` El Berca | The printed `4728` numeral is in the cell below `4828`, beside `4729` and above `4628` in the displayed crop. A blue dot beside the El Berca lettering lies inside that cell, immediately by paired road ink. Exposed ground is pale; dark road, blue cross-ticked railway, dashed ink and nearby city/figure graphics crowd parts. The blue water and port graphic to the northwest belong to the Benghazi area; proximity does not assign that port symbol to El Berca. The [first reading](2026-11-18-graziani-air-facility-edges-batch.md) also identifies the dot/name. No distinct airfield symbol is established from this view. |
| `B5526` Martuba | Printed `5526` sits in the cell between `5525` and `5527`, with `5626` above and `5426` below-right in the displayed crop. Blue/green dot under Martuba name lies inside `5526`, over brown fill alongside paired dashed and single dashed route marks. The [first reading](2026-11-18-graziani-air-facility-edges-batch.md) identifies the dot/name; the blue-dot key describes village/bir without distinguishing which. Fill and crossing ink are visible, but their proportions and terrain precedence are not adjudicated. No separate airfield symbol is established. |
| `B5825` unnamed scenario airfield | Numeral `5825` is partly crossed by the paired road; adjacent numbered `5925`, `5824`, `5826`, `5725` and `5726` support the grid location. Brown ground and slope-colored artwork lie under the road and number. The **Derna** dot and port-anchor graphic lie in `B5925`, not within the bounded `B5825` interior. I did not identify a distinct locality dot or airfield symbol inside exposed `B5825`; road and terrain overprint prevent a claim that every covered mark is absent. The scenario names an unnamed airfield at B5825; that source assignment is distinct from detecting its symbol on the map. |

The [map key](https://spigames.net/PDFv10/CNA_Maps.pdf) p. 1 distinguishes the locality dot, terrain and line symbols; dots do not themselves identify an air facility, type, capacity or current control. These crops do not settle full terrain classification, any edge's legal crossing, coast status, map data correction or placement legality. `A4728` observations do not resolve the separate scenario `B4827` versus map `A4827` Benghazi conflict. Same-scan reading, not independent physical-map provenance.

## Reproduction and provenance

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map p. 1 and p. 2 with 600-dpi render:

```sh
pdftoppm -f 1 -l 1 -r 600 -x 9980 -y 4640 -W 1600 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/finterior-a4728
pdftoppm -f 2 -l 2 -r 600 -x 9120 -y 2040 -W 1600 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/finterior-b5526
pdftoppm -f 2 -l 2 -r 600 -x 8360 -y 1070 -W 1600 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/finterior-b5825
```

Crop SHA-256 respectively `8a1fa4ee82bfc9365d961a2a81152d57927bb48ddd707999a95dd30159307c37`, `9dfc128aa7433fd08598e95181b8307ffa749906783eee46288bac5509885788`, `9d2b07b8bac6349bc16cf062451160b3ee825c60d59c2678d25e42098d9d72c5` (renderer-dependent). No scenario-book reinspection, data/runtime edits or gameplay tests.

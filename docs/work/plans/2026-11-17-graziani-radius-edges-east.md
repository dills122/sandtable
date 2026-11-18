# C0716 radius: eastern nine-cell edge audit

17 November 2026. Visual reading of [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, against pinned `vassal:CNAv2.1.0` side data. Same scan as earlier radius work; not independent-edition verification. C0914–C0918 and C1014–C1017 are the nine assigned cells, including sides to neighbors outside the disk. Crop below contains all nine and their six sides.

Each entry identifies the neighbor across a side, in displayed clockwise order **upper-left, upper-right, right, lower-right, lower-left, left**. `T` = dashed track visibly reaches/crosses side; `V` = track approaches shared vertex; side attribution uncertain; `B` = brown edge artwork visible; `M` = salt-marsh pattern reaches side (interior pattern, not a claim of a distinct edge rule); `?` = side classification unknown. Multiple marks can coexist; all entries, including marked ones, retain unknowns for other features. `?` does not mean clear, impassable, or a verified absence. Brown artwork's exact rule class and direction remain open. In particular, marks near a vertex may belong to either adjoining side.

| Cell | UL | UR | R | LR | LL | L |
| --- | --- | --- | --- | --- | --- | --- |
| C0914 | C1013 B | C1014 ? | C0915 ? | C0814 ? | C0813 B | C0913 B |
| C0915 | C1014 T | C1015 ? | C0916 ? | C0815 T | C0814 ? | C0914 ? |
| C0916 | C1015 M | C1016 M | C0917 M | C0816 M | C0815 V | C0915 ? |
| C0917 | C1016 M | C1017 M | C0918 M | C0817 M | C0816 M | C0916 M |
| C0918 | C1017 M | C1018 M | C0919 ? | C0818 M | C0817 M | C0917 M |
| C1014 | C1114 B,T | C1115 T | C1015 ? | C0915 T | C0914 ? | C1013 B,T |
| C1015 | C1115 ? | C1116 ? | C1016 M | C0916 M | C0915 ? | C1014 ? |
| C1016 | C1116 ? | C1117 ? | C1017 M | C0917 M | C0916 M | C1015 M |
| C1017 | C1117 ? | C1118 ? | C1018 M | C0918 M | C0917 M | C1016 M |

`M` indicates patterned ink visibly touching the shared grid area, not whole-hex terrain assignment. Blue Giarabub and El Qaqa lettering obscures parts of C1015 and the C08/C09 boundary; numbering, dashed ink, brown shading and scan resolution also limit precise edge attribution. C0915's dashed route passes close to the C1014/C1015/C0915 vertex and the C0915/C0916/C0815 vertex. Therefore V on C0815|C0916 is a vertex-qualified candidate, not proof of crossing; adjacent T marks at that vertex also have uncertain precise attribution. Brown ink on C0914 has an interior component; treating all of it as a side feature would need rule and clearer-source review.

## Pinned-side comparison

Pinned pairs touching these nine cells: `C0813|C0914` slope up C0813; `C0913|C0914` slope up C0913; `C0914|C1013` slope up C1013; `C1013|C1014` slope up C1013 plus track; `C1014|C1114` slope up C1114 plus track; `C1014|C1115`, `C0915|C1014`, `C0915|C1015`, `C0815|C0915`, and `C0815|C0916` track. Every listed record has `side: null`; tracks have `up: null`. Brown and dashed artwork supports candidates for the corresponding marks except `C0915|C1015`: dashed route approaches its common vertex, but this crop does not independently establish that it crosses this particular side. The pinned `up` directions are data claims, not verified by this visual pass. Missing pair records do not establish absence; `M` is not compared as a pinned hexside feature. No edits to pinned data follow from this comparison.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -scale-to 10000 -x 1800 -y 7900 -W 1600 -H 1050 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-east-edges
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `ac30b21674039dfd6b0bc7f82b3372d720881228f53f972d6d512f270690be6e` (renderer-dependent); pinned `hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned `hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. No runtime or data changes; no gameplay tests.

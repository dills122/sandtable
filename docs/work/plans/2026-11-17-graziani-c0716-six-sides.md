# El Qaqa C0716: six printed sides

**Finding:** Map C shows an oasis at C0716 and a horizontal dashed track crossing its left and right sides. A diagonal track reaches its upper vertex from C0815; ink and lettering obscure whether it also crosses the adjoining C0816 side. Pinned data records all four pairs as track. The image alone does not independently establish both upper-side crossings.

## Source observations

Inspected [original map and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1. The `0716` label is partly covered by oasis artwork and track, but surrounding printed numbers and grid identify the cell. Image directions and approximate coordinates refer to the crop below; C0716 vertices clockwise from top are `(617,620)`, `(805,725)`, `(805,945)`, `(617,1050)`, `(425,945)`, `(425,725)`. The key shows oasis as blue palms and track as single black dashes. Pinned pair entries below are literal records in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), whose declared source is `vassal:CNAv2.1.0`.

| Neighbor | Side | Visible evidence | Pinned record |
| --- | --- | --- | --- |
| C0815 | Upper-left | Diagonal dashed route reaches the side near its top vertex, around `(598,632)`. Route ink overlaps the grid. | `track` |
| C0816 | Upper-right | Diagonal route and blue lettering overlap the shared top vertex. No separate crossing away from that vertex is discernible; its attribution to this side remains uncertain. | `track` |
| C0717 | Right | Horizontal dark dash crosses near `(805,800)` and continues east. | `track` |
| C0616 | Lower-right | Thin grid line visible; no obvious route crossing or broad side band. | No explicit pair entry |
| C0615 | Lower-left | Thin grid line visible; no obvious route crossing or broad side band. | No explicit pair entry |
| C0715 | Left | Horizontal dark dash crosses near `(425,795)` and continues west, with palm artwork nearby. | `track` |

Blue palm artwork matches the key's oasis symbol; the exposed interior is pale. Pinned [hexes.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) records `clear`, `oasis`, and `coastal: false`. Neighboring salt-marsh graphics and southern desert fill are distinct from the central interior. Sparse missing pair entries do not prove absence of features; scan quality, printing overlap and vertex proximity limit negative readings. This local visual comparison does not establish legal movement or certify a complete route.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 600 -x 5250 -y 17500 -W 1800 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c0716-sides
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c0716-key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `b8a497d9a10e3fe8b92bf1018a68157135eed87d2e892c073b903662c1e20e9c`; key crop `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894` (PNG bytes depend on renderer). Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crops remain outside Git. Same-scan reading; no independent physical sheet, runtime change or gameplay test.

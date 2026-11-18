# Independent review: Siwa C0127 six sides

**Date:** November 18, 2026. **Result:** source reading supported, within stated limits. Fresh visual review of [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, using the same scan and independently rendered 600-dpi crop; this is not an independent edition.

The oasis graphic and partly obscured `0127` mark identify the bottom-row hex. Four printed cells share its sides: C0226 upper-left, C0227 upper-right, C0126 left, and C0128 right. Both lower sides border blank sheet margin; no printed neighboring cell or apparent route crossing appears there. This observation does not establish a board limit, physical impassability, or absence of adjoining coverage.

| Side | Visual check | Pinned pair check |
| --- | --- | --- |
| Upper-left, C0226 | Single dashed lead crosses side and continues west; oasis art obscures its lower continuation. | `track` |
| Upper-right, C0227 | Paired dashes and a separate single dashed lead cross close together. Their exact relationship beneath the artwork is unclear. | `unfinished-road`, `track` |
| Left, C0126 | No route crossing apparent; desert-colored fill lies within adjacent hex. | No explicit pair |
| Right, C0128 | No route crossing apparent; salt-marsh pattern lies within adjacent hex. | No explicit pair |
| Lower-left and lower-right | Blank sheet margin beyond both boundaries; no apparent route crossing. | No printed neighbor to pair |

Checked [hexside pins](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json): only two entries contain `C0127`, with features shown above. [Hex pins](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) identify C0127 as `clear` and `oasis`. Sparse pair omissions do not establish clear terrain or prohibit movement. Artwork, overprinting, and scan quality limit claims about junctions and unmarked features; this review does not establish route connectivity or movement legality.

Reproduced crop using `pdftoppm -f 3 -l 3 -r 600 -x 9200 -y 19300 -W 2500 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-review-independent`. Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; hex pins `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; side pins `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No runtime or map-data changes; no gameplay tests.

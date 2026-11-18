# Graziani landing-strip cells: first ten six-side readings

**Date:** November 18, 2026. Bounded same-scan reading of the first ten further §60.5 landing-strip targets in the [extent crosswalk](2026-11-18-graziani-scenario-extent-crosswalk.md), not an independent edition, route, placement or movement certification. The [scenario inventory](2026-11-17-graziani-objective-supply-extent-seeds.md) transcribes the facility list; a location dot on the map is not itself a landing-strip symbol or proof of current control, capacity or aircraft. Printed numbers and names in crops identify the listed locations, except the precise location of El Agheila's dot at a vertex deserves further review. Map sheets A–E are PDF pp. 1–5. Map key on p. 1 distinguishes road, track, railroad, slope, ridge, wadi and coast. Blue railroad markings and nearby lettering can obscure underlying ink.

## Six sides and pinned comparison

Columns are upper-left, upper-right, right, lower-right, lower-left, left in displayed crops. Each entry gives neighbor followed by `source / pin`. `–` means no explicit pin pair, **not clear**. `u` means no obvious distinct crossing or side band seen on exposed grid at this scale; it does not certify absence, terrain, traversal or a feature-free edge. `?` marks overlaps, near-vertex crossings or an incomplete classification. Features named in source column are visual observations, not route claims; all readings remain provisional. Coast is irregular artwork, not necessarily a side feature. Pin feature names follow [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json).

| Cell, scenario name | UL | UR | Right | LR | LL | Left |
| --- | --- | --- | --- | --- | --- | --- |
| B0707 Augila | B0806 track / track | B0807 slope / slope | B0708 track / track | B0607 u / – | B0606 track / track | B0706 u / – |
| C4108 Bir el Gubi | C4207 track / track | C4208 u / – | C4109 track / track | C4008 track / track | C4007 wadi / wadi | C4107 wadi, track / wadi, track |
| E3109 Burg el Arab | E3208 road, slope? / road, slope | E3209 railroad, road junction? / railroad | E3110 u / – | E3009 u / – | E3008 track / track | E3108 railroad / railroad |
| A1816 El Agheila | A1916 sea-side grid / – | A1917 sea-side grid / – | A1817 coast intersects / – | A1717 road near vertex? / road | A1716 road near vertex? / road | A1815 coast intersects / – |
| E3007 El Hamman | E3107 name and road junction? / – | E3108 railroad / railroad | E3008 u / – | E2908 u / – | E2907 u / – | E3006 railroad / railroad |
| D3323 Fuka | D3422 road near vertex? / road | D3423 coast, road near vertex? / – | D3324 road, railroad / road, railroad | D3223 u / – | D3222 track / track | D3322 railroad, road near vertex? / railroad |
| C4414 Gambut | C4514 ridge / ridge | C4515 ridge, track / ridge, track | C4415 u / – | C4315 ridge / ridge | C4314 ridge, track / ridge, track | C4413 u / – |
| B4933 Gazala | B5032 coast? / – | B5033 coast near side? / – | B4934 road / – | B4833 road near vertex? / – | B4832 road near vertex? / – | B4932 coast and road near vertex? / – |
| B5410 Maraua | B5510 u / – | B5511 u / – | B5411 road / road | B5311 track / track | B5310 track near vertex? / – | B5409 road / road |
| D3520 Matten Baggush | D3619 sea-side grid / – | D3620 coast intersects / – | D3521 coast intersects / – | D3420 track or road near vertex? / – | D3419 track, coast near vertex? / track | D3519 coast and road near vertex? / coast |

Gazala's road crosses the right vertical, while the crowded bend at its lower vertex makes attribution to adjacent diagonals uncertain. The [independent review](2026-11-18-graziani-landing-strip-edges-a-review.md) confirms the B4933–B4934 road but leaves the upper-right coast-side attribution uncertain. Its six incident pin pairs are absent: compare rather than silently filling those entries. The scenario spells the other facility Matten Baggush; the map prints Maaten Baggush. Its and El Agheila's dots sit close to grid junctions; labels and facility-list coordinates are retained without changing their adjacent sides. At Burg el Arab and Fuka, road and railway cross or converge near grid vertices; independently check exact pair assignment before using these observations. Yellow fill around Augila is interior artwork, not an automatic side feature. Ridge/slope names follow map-key comparison and remain provisional where small ink overlaps.

Pinned interiors respectively: B0707 desert/village; C4108 clear/bir; E3109 rough/village; A1816 clear/village/coastal; E3007 clear/village; D3323 clear/village/coastal; C4414 gravel/village; B4933 clear/village/coastal; B5410 rough/village; D3520 rough/village/coastal. These are data comparisons, not independent adjudication of mixed terrain or country; other listed pins say noncoastal. §60.5 says Egypt facilities initially Commonwealth and Libya facilities Italian, but this batch does not independently settle political membership or subsequent control.

## Reproduction and provenance

[Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [scenario PDF](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crops are 300 dpi, 1000×1000 pixels; PNG hashes depend on renderer. Reproduce each with `pdftoppm -f PAGE -l PAGE -r 300 -x X -y Y -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/CELL` using the following parameters:

| Cell | PAGE | X | Y | Crop SHA-256 |
| --- | ---: | ---: | ---: | --- |
| B0707 | 2 | 820 | 8680 | `2c1e74dd1e46530d236ca2a0cbfdf50fd4a438c71fac8529c56574d215f9a2d4` |
| C4108 | 3 | 1048 | 3196 | `0c3d708d5460e95eba94ab4810ad345d9160d9a8c3528ad42258df58deabd0b3` |
| E3109 | 5 | 1120 | 4756 | `fbc1f3514dfb768a04bbdf59b1b7bb9e1eea57f91267449cfe16dd1b150f8304` |
| A1816 | 1 | 2560 | 6928 | `d77a953882a5594929a99534d2adf1d5fe7ffadec9330c56eddd72ccddaa5320` |
| E3007 | 5 | 748 | 4912 | `8782bfabd3e3436f442cb7a53575e564bd9d5565454956ca0fa4f8cc79218dda` |
| D3323 | 4 | 3748 | 4672 | `857f20d41f45acefc10a275f7ba133e2b8ee8c3a9611e8c06a404c98d2e99b53` |
| C4414 | 3 | 2164 | 2728 | `f91afd09a662910f6e13478c321bfd1e9bfdb10ffe9a3b271361894254ab36f4` |
| B4933 | 2 | 5590 | 1930 | `fec50d96e104e8e2f6587a5163e7f30916940eade2123a633728cba10cd2608b` |
| B5410 | 2 | 1480 | 1060 | `83da49888aa9a07543465faf63985984ea8d8411e844c226482a52f3c9d66100` |
| D3520 | 4 | 3172 | 4204 | `55d5bc8b4996a45aa8dae6eddd1e316469c7ee6e7609ae53fd0472747c6f51a8` |

Crops remain outside Git. No runtime or data changes; no gameplay tests.

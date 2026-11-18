# Map D east of Fuka: bounded route continuation

**Date:** November 17, 2026. **Status:** informed same-scan source reading;
not independent-edition corroboration, a certified route, or a map-data
correction. This extends the [Matruh east rail ledger](2026-11-17-graziani-matruh-east-rail-batch-next.md)
from D3323 toward El Daba. That ledger's earlier crossings and the links
between them retain their own qualifications.

## Source and method

Inspected the [original five-sheet map scan](https://spigames.net/PDFv10/CNA_Maps.pdf),
Map D, PDF page 4; its terrain key is on PDF page 1. Local PDF SHA-256:
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
North is up in this render. Cell labels and grid identify the horizontal
sequence; blue cross-tied ink and paired dark lines identify the railroad and
road respectively. Both source and pinned records were available before this
reading, so it is not blind. Reproduce the crop with:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 6750 -y 7850 -W 2300 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-continuation-east
```

Crop SHA-256 `076776d0dc630c06a389927196ee5817b191f3e72e95c8d16471082e4771e3dc`;
renderer differences can change PNG bytes. Coordinates below are approximate
pixels in that crop, not geographic coordinates. Printed numerals overlap
some route ink; the line can be followed on both sides of each crossing.

## Six consecutive local sides

Each row records **both** an apparent blue railway crossing and an apparent
paired dark road crossing of the shared vertical side. The first road crossing
is vertex-adjacent, as the [visual review](2026-11-17-graziani-continuation-map-review.md)
notes; it should not be treated as an unambiguous interior crossing. The
[pinned side data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
contains `road` and `railroad` for each pair, consistent with this reading;
its SHA-256 is `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

| Pair | Approximate side x in crop | Qualification |
| --- | ---: | --- |
| D3323–D3324 | 497 | At Fuka approach; one road stroke merges with the upper vertex, so its precise crossing remains qualified. |
| D3324–D3325 | 812 | Printed 3325 overlies the lines near crossing. |
| D3325–D3326 | 1127 | Galal name and printed 3326 crowd crossing. |
| D3326–D3327 | 1442 | Printed 3327 overlaps lines; their continuation remains visible. |
| D3327–D3328 | 1757 | Ghaza lettering and 3328 overlap nearby route ink. |
| D3328–D3329 | 2072 | Printed 3329 overlaps lines; right edge of crop truncates the next cell. |

The paired lines and cross-tied line continue through these cells in this
crop. This is evidence for six local crossings, not a completed
Alexandria–Matruh railway survey or proof of legal service. The previous
D3322–D3323 observation, junctions nearer Matruh, further sides east of
D3329, the D/E seam and Alexandria terminal still need their own evidence.
Coast, other features, rail construction or damage, control, occupancy,
ZOC, and other legal requirements are not adjudicated here. No absence of a
feature is inferred. The review is same-scan corroboration, not independent
edition evidence or admission.

Documentation only; no pinned data or runtime changes, and no gameplay tests.

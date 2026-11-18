# C4706 / C4707 escarpment direction

**Finding:** The original Map C scan shows escarpment splash extending into C4707 from its shared vertical side with C4706. Under Land rules §8.35, splash identifies the down side, so the source supports **C4707 down and C4706 up**. The pinned record agrees. No direction correction is indicated for this edge.

## Source reading

Inspected [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and terrain key p. 1; SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. In the unrotated 500-dpi Tobruk crop, C4707 is the pale hex below Tobruk and immediately right of C4706; its own number is obscured by blue railway marks, so identification relies on the surrounding numbered grid, as documented in the [six-side reading](2026-11-17-graziani-c4707-six-sides.md). The broad dark vertical band follows their common side. Its irregular dark splash projects to the right into C4707, visible along the middle of the band; nearby lower-vertex slope and railway ink do not obscure that middle segment. The map key illustrates the same broad dark escarpment symbol and splash.

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 14 §8.35, describe splash contours on the down side; SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. This source observation supports the direction for this side only. It does not certify adjoining sides, route crossings, legal vehicle movement, or ZOC for a particular unit.

Reproduce the original crop and detail (renderer output may vary):

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1300 -y 3000 -W 2600 -H 2300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4707/tobruk
magick /private/tmp/graziani-c4707/tobruk.png -crop 650x800+450+1000 +repage /private/tmp/graziani-c4707/detail.png
```

Inspected detail SHA-256 `7f8381de215e388fa01421750a41f4762a119a2eb3d05c530dca7c3cece86828`; crop coordinates are from the six-side reading. Source PDF and crops are not committed.

## Pinned comparison

After source reading, checked [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. `C4706|C4707` records `escarpment` and `up: C4706`, consistent with the visible splash and §8.35. No runtime or map data changed; no gameplay tests run.

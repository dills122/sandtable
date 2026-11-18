# Map D east of El Daba: road and rail continuation

**Date:** November 17, 2026. **Status:** informed reading of the same map
scan, not independent-edition corroboration, a certified route, or a data
correction. This continues the [D3323–D3329 reading](2026-11-17-graziani-continuation-map.md).
The road and railroad do not cross every subsequent side together.

## Source and method

Inspected [original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf),
Map D, PDF page 4; the terrain key is on page 1. Source PDF SHA-256:
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
North is up. Paired dark lines denote road and blue cross-tied line denotes
railroad. Numerals, a track, and linework obscure portions of crossings.
Both scan and pinned data were available before reading; this was not blind.
Reproduce the overlapping crop with:

```sh
pdftoppm -f 4 -l 4 -r 500 -x 8750 -y 7850 -W 2300 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-east-next
```

Crop SHA-256 `4d7fd50c901eccdd81acb251491f6d5d12842dc74e3550777dca566a387a1f35`;
renderer differences can change PNG bytes. Pixel positions below are
approximate and refer to this crop. Compared against
[pinned side data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json),
SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

## Local side observations

| Apparent crossing | Scan observation | Pinned record |
| --- | --- | --- |
| D3329–D3330 | Road and railroad cross vertical side near x=386. | Both. |
| D3330–D3331 | Road approaches near x=700; its lower stroke merges with the lower vertex, leaving exact side attribution uncertain. Rail bends south earlier. | Road. |
| D3330–D3230 | Railroad crosses diagonal near x=630, y=570. | Railroad. |
| D3230–D3231 | Railroad crosses vertical side near x=858, y=640, close to upper vertex. | Railroad. |
| D3331–D3231 | Road crosses diagonal near x=900; crowded ink and nearby vertex warrant care. Railroad appears south of the vertex, following D3230–D3231 instead. | Road and railroad. Railroad on this particular side is not established by this reading. |
| D3231–D3232 | Road and railroad appear to cross vertical side near x=1170, y=650–690; printed 3232 and converging lines crowd it. | Railroad only; apparent road crossing needs review. |
| D3232–D3133 | Road and railroad cross diagonal near x=1400, y=840; lines and track converge nearby. | Both. |
| D3133–D3134 | Road and railroad cross vertical side near x=1643, y=950–990 in printed overlap. | No pair found; `D3133|E` carries both as an open side. Relation to D3134 and the adjoining sheet remains unverified. |

This comparison identifies discrepancies or uncertainties, not proof that
pinned data is wrong: printed overlap, vertex conventions, sheet-edge handling,
and extraction conventions need separate review. No absence of other features is
inferred. Trace beyond the printed overlap, matching Map E cells and seam,
Alexandria terminal, earlier D3322–D3323 crossing and Matruh junctions remain
open. Construction or damage, control, occupancy, ZOC, and other legal-service
requirements are outside this visual reading. No independent edition checked.

Documentation only; no pinned data or runtime changes, and no gameplay tests.

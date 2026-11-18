# PR #377 Mechili bounds: source review

**Disposition:** The [Mechili note](2026-11-17-graziani-pr377-mechili-bounds.md) correctly reads all six cells and computes each conditional lower bound of 41. Together with the seven committed graph paths, a same-metric interpretation conditionally places all seven targets at least 24 hexes from Mechili. These are distinct cells in one inconsistent chart, not independent source confirmation or an admitted geometry policy.

## Source and calculation

Visually checked [Air/Logistics §37.42](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25. Mechili is `B4921`, Fort Maddalena `C3019`, Wadi Natrun `E2418`, Bir-el-Qatrani `D2903`, and Barce `B5504`; none carries the major-city asterisk. The three pairs are:

| Intermediate | Mechili cell | Fort cell | Difference |
| --- | ---: | ---: | ---: |
| Wadi Natrun | 109 | 68 | 41 |
| Bir-el-Qatrani | 58 | 17 | 41 |
| Barce | 20 | 61 | 41 |

The footnote specifies shortest hex distance, origin excluded and destination included, with an all-sea grid where necessary. If either pair correctly represents distances to the same fixed intermediate in one undirected metric, reverse triangle gives `d(Mechili, Fort) ≥ |d(Mechili, X) − d(Fort, X)| = 41`. The three disjoint pairs are not separate editions, independent measurements, or proof of chart consistency. The direct Mechili–Fort cell is also **41**; agreement with a lower bound does not validate that cell or exclude correlated mistakes. The chart has other unresolved inconsistencies.

The [independent Soluch bounds review](2026-11-17-graziani-pr377-alternate-distance-bounds-review.md) reproduced BFS on commit `35e32e8051141f1a67a87011cd483bb5021bb51b` hex IDs using its `NeighborsOf` rule: `C3520=5`, `C3020=1`, `C3721=7`, `C3922=9`, `C3926=11`, `C4131=17`, `C3320=3` from C3019. This verifies graph arithmetic, not every source edge or shared chart identity. If those are valid paths in the same metric as either correct chart pair, triangle inequality yields `d(Mechili,target) ≥ 41 − path_length ≥ 24 > 1`. No exact local shortest distance is required; a valid path suffices.

The committed implementation uses the direct published Mechili–Fort anchor and conflict gate, not these alternate pairs. This analysis does not establish source correctness, map-seam correspondence, adjacency for other targets, legal movement or ZOC. Source and path assumptions require independent policy review. Air scan SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; same edition. No runtime changes or gameplay tests.

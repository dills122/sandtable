# PR #377: conditional Mechili–Fort bounds

**Finding:** Three distinct pairs of printed chart cells each conditionally imply `d(Mechili, Fort Maddalena) ≥ 41`. Combined with valid same-metric Fort-to-target paths of at most 17, this would imply each of the seven named forward hexes is at least 24 hexes from Mechili. The pairs come from the same inconsistent chart and do not independently prove any value, certify map edges, or supply a new runtime proof.

## Source cells

Visually inspected [Air/Logistics §37.42 Land Distance Chart](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25, Air scan SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Labels are Mechili `B4921`, Fort Maddalena `C3019`, Wadi Natrun `E2418`, Bir-el-Qatrani `D2903`, and Barce `B5504`; none carries the major-city asterisk.

| Intermediate | Chart cell for Mechili | Chart cell for Fort | Absolute difference |
| --- | ---: | ---: | ---: |
| Wadi Natrun | Wadi row / Mechili column **109** | Wadi row / Fort column **68** | 41 |
| Bir-el-Qatrani | Bir row / Mechili column **58** | Bir row / Fort column **17** | 41 |
| Barce | Mechili row / Barce column **20** | Fort row / Barce column **61** | 41 |

The footnote defines shortest hex distances excluding origin and including destination, assumes sea hexes where necessary, and states same-numbered hexes on adjacent sheets are 33 apart. For each pair separately, assuming both numbers refer to the same fixed endpoints in one undirected shortest-path metric, reverse triangle inequality gives `d(Mechili, Fort) ≥ |d(Mechili, X) − d(Fort, X)| = 41`. These are distinct cell pairs but one chart and one edition, not independent source confirmation. Its separate direct Mechili–Fort entry is 41; equality with a conditional lower bound does not verify that cell or actual geometry. Chart triangle violations elsewhere remain unresolved; see [distance review](2026-11-17-graziani-pr377-distance-triangle-review.md).

## Local paths and scope

Commit `35e32e8051141f1a67a87011cd483bb5021bb51b` describes its C-sheet graph BFS from `C3019` as `C3520=5`, `C3020=1`, `C3721=7`, `C3922=9`, `C3926=11`, `C4131=17`, and `C3320=3`. If each is a valid path connecting the same physical Fort and target in the chart metric, its length is an upper bound on local shortest distance. Therefore `d(Mechili, target) ≥ 41 − 17 = 24 > 1` for all seven. These paths are model-derived, not source-certified. The [independent Mechili review](2026-11-17-graziani-pr377-mechili-bounds-review.md) reports a separate BFS reproduction of the committed graph via its linked Soluch review; individual source edges and identity with the chart metric remain unverified necessary premises.

The commit's implementation uses the direct Mechili–Fort published entry, not these alternate pair derivations. No claim follows for other graph-reachable hexes, legal movement or ZOC; a later source policy and independent corroboration must assess the premises. No runtime edits or tests.

# PR #377: conditional alternate Soluch–Fort bounds

**Finding:** Two pairs of printed §37.42 values independently yield the same **conditional** lower bound of 60 hexes from Soluch to Fort Maddalena. If either pair and its common fixed-endpoint metric are correct, and each relevant committed C-sheet path is valid in that same metric, all seven named forward hexes are more than one hex from Soluch. This is an alternative argument, not a correction of the chart, proof that any cell is true, or a runtime change.

## Chart checks

Visually inspected [Air/Logistics Land Distance Chart §37.42](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25, same scan SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Its rows and stepped columns show:

| Printed row, column | Value |
| --- | ---: |
| Bir-el-Qatrani `D2903`, Soluch `A4130` | 77 |
| Bir-el-Qatrani `D2903`, Fort Maddalena `C3019` | 17 |
| Soluch `A4130`, Benghazi `A4827` | 7 |
| Fort Maddalena `C3019`, Benghazi `A4827` | 67 |

No cited endpoint carries the major-city asterisk. Footnote defines shortest hex distance excluding origin and including destination, allowing assumed sea hexes and specifying a 33-hex distance between same-numbered adjacent-sheet hexes. If the first pair is correct in a common undirected metric, the reverse triangle inequality gives `d(Soluch, Fort) ≥ |77 − 17| = 60`; if the second pair is correct, it gives `≥ |67 − 7| = 60`. They are separate chart-cell pairs in one source, not independent editions or independent confirmation of physical geometry. Either conditional pair suffices; neither confirms its own premise.

The chart contains other triangle inconsistencies, including one involving its printed direct Soluch–Fort 61; see [triangle review](2026-11-17-graziani-pr377-distance-triangle-review.md). These alternatives do not choose the erroneous cells or guarantee that the chart as a whole is consistent. A source correction, country/map identity or metric interpretation can affect multiple entries together. The scenario's separately printed Benghazi spelling differs from the chart's `A4827`; this check uses the chart endpoint and does not equate it to the scenario spelling by assumption.

## Comparison with committed paths

Commit `35e32e8051141f1a67a87011cd483bb5021bb51b` describes `C3019` local graph path lengths to `C3520=5`, `C3020=1`, `C3721=7`, `C3922=9`, `C3926=11`, `C4131=17`, `C3320=3`; its `FtMaddalenaMapDistances` BFS traverses `Cna1979HistoricalMap.NeighborsOf`. If those are actual paths from the same Fort endpoint in the metric used above, they are upper bounds on the corresponding shortest distances. Thus each target is at least `60 − 17 = 43` hexes from Soluch, greater than the adjacency threshold 1. Exact local shortest paths are unnecessary: a valid path of at most 17 suffices. This review reports committed path values and code method; it does not independently validate every source edge or graph-to-chart identity.

The commit's `TrySoluchMechiliToFtMaddalenaBound` uses the direct published anchor and candidate conflict gate, not either new derivation. This note does not claim the alternative is implemented, valid for Mechili or every graph-reachable C hex, or sufficient for legal movement/ZOC. Applying it requires an explicit source/geometry policy and verification of each assumption. No runtime edits or tests.

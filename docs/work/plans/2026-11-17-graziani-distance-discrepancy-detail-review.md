# Barce–Bir-el-Qatrani discrepancy: source review

**Disposition:** The [detail note](2026-11-17-graziani-distance-discrepancy-detail.md) correctly transcribes the direct entry and six two-leg pairs. Conditional shortest-path inequalities conflict with 80; they bound the direct value above by 78 if the other entries and fixed endpoint meanings are correct. They do not prove an exact value of 78, identify the erroneous cell, or provide six independent source editions.

## Chart and arithmetic

Visually checked [Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25 §37.42, including the left row labels, stepped column headings and footnote. Bir-el-Qatrani is `D2903`; Barce is `B5504`; their intersecting cell prints **80**. For each intermediate below, the first cell is that intermediate's row at the Barce column; the second is the Bir-el-Qatrani row at that intermediate's column:

| Intermediate and printed location | First cell | Second cell | Sum |
| --- | ---: | ---: | ---: |
| Ben Gania `B3715` | 20 | 58 | 78 |
| Mechili `B4921` | 20 | 58 | 78 |
| `C2803` | 46 | 33 | 79 |
| Tobruk `C4807` | 40 | 38 | 78 |
| Ft. Maddalena `C3019` | 61 | 17 | 78 |
| Bardia `C4321` | 56 | 22 | 78 |

The 12 leg cells are distinct from one another and from the direct cell. They share Barce and Bir endpoints and one scanned chart, so “independent” means **cell-disjoint constraints**, not independent measurement, source, geography or transcription process. Both endpoint rows and each intermediate identify single labeled hexes without the asterisk used for a major-city hex; footnote specifies shortest distance excluding origin and including destination, allowing a sea grid where needed. Assuming each cell denotes a shortest distance in the same undirected hex graph with the specified endpoints, joining two legs gives a walk of their sum, so direct distance cannot exceed any sum. Five give `≤78` and one `≤79`, inconsistent with 80.

Under those assumptions and treating entries as separately editable cells, retaining direct 80 requires at least one changed leg in each of six disjoint pairs; those edits must raise the applicable sum sufficiently. This is a lower bound on changed **cells in this subset**, not six independent causal mistakes: a corrected coordinate, convention or source method could affect many cells. The inequalities supply no lower bound of 78; values below 78 can satisfy them. Replacing direct 80 by 78 repairs these six inequalities alone, not necessarily every chart constraint or the actual map. No independent map or errata correction was verified here.

Same-scan review of Air PDF SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; no separate edition, source-certified seam or runtime change. No gameplay tests.

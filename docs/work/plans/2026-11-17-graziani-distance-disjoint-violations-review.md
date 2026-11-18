# Distance chart disjoint violations: independent review

**Disposition:** Four non-starred examples and their cell-disjoint lower bound check out. Recomputed from the supplied JSON: 24 violating triangles, a seven-triangle cell-disjoint witness, and a seven-cell hitting set. Seven is the minimum number of cells needed to intersect these 24 violations, conditional on fixed endpoints and a common shortest-path metric. This does not establish that seven replacement values suffice to produce a metric.

## Visual check

Inspected [Air rules §37.42](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25, same scan SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Row/column intersections independently match all 12 values in the reviewed note: `75 > 57 + 17`, `67 > 24 + 42`, `130 > 20 + 109`, and `80 > 20 + 58`. Their four triples have no common pair-cell. These endpoints lack the major-city asterisk. Chart footnote states shortest hex distance, excludes origin and includes destination, with assumed sea grid where needed; those conventions permit concatenation and do not defeat the inequality under fixed endpoints.

## Recalculation from JSON

Enumerated all combinations of three of the 20 labels, marking a triangle if its largest pair value exceeds the sum of the other two; found 24. A disjoint packing of seven is:

1. Nofilia, C2803, Alexandria*.
2. El Agheila, C2803, Fort Maddalena.
3. Agedabia, Ben Gania, Bardia.
4. Soluch, Ben Gania, Wadi Natrun.
5. Soluch, Bir-el-Qatrani, Cairo*.
6. Barce, Ben Gania, Bir-el-Qatrani.
7. C2803, Wadi Natrun, Cairo*.

A separate recursive hitting-set search returned seven cells: C2803–Alexandria; El Agheila–C2803; Agedabia–Bardia; Soluch–Wadi Natrun; Bir-el-Qatrani–Cairo; Barce–Bir-el-Qatrani; C2803–Wadi Natrun. This set intersects every enumerated violation, so the packing's lower bound is attainable as a **hitting set**, not necessarily as a consistent corrected distance chart. A single source error can affect multiple cells, and modifying these seven could create new inequalities. Alexandria and Cairo have starred major-city entries; the seven-bound additionally assumes each denotes one consistent endpoint across relevant cells. No independent visual check of all 190 JSON values or alternate edition was performed.

This review checks the note's computation and bounded interpretation, not map geometry, a specific correction, or source-gameplay readiness. No runtime changes.

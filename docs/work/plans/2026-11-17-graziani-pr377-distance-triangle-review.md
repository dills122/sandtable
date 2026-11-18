# PR #377: Soluch–Fort Maddalena chart triangle

**Finding:** The original chart prints Soluch–Fort Maddalena **61**, Soluch–Wadi Natrun **130**, and Fort Maddalena–Wadi Natrun **68**. Under one fixed-endpoint shortest-distance metric, `130 > 61 + 68` is impossible. This shows an inconsistency involving the chart cell cited by PR #377; it does not identify which cell is wrong, prove the PR's separation result false, or exclude an independent witness.

## Source verification

Visually checked [Air/Logistics §37.42 Land Distance Chart](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 25, from `/private/tmp/graziani-chart-distance.png` rendered at 160 dpi. Air source SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`.

| Row and column | Printed number |
| --- | ---: |
| Fort Maddalena `C3019` row, Soluch `A4130` column | 61 |
| Wadi Natrun `E2418` row, Soluch `A4130` column | 130 |
| Wadi Natrun `E2418` row, Fort Maddalena `C3019` column | 68 |

None of these three endpoint labels carries the major-city asterisk. The footnote defines shortest distance in hexes, excluding origin and including destination, allowing an assumed sea grid where necessary; it also says corresponding numbered hexes on adjacent sheets are 33 hexes apart. For fixed locations in one common graph, joining two paths gives a walk of `61 + 68 = 129`, so the stated 130 cannot also be its shortest distance. Origin counting and the sea-grid allowance do not remove that inequality. At least one printed value or shared interpretation is inconsistent, but this triangle alone cannot select it. If 130 and 68 were independently established as correct, they would instead imply Soluch–Fort ≥62; that conditional does not establish either value.

## Bounded effect on PR #377

The [PR scope note](2026-11-17-graziani-pr377-research-scope.md) describes a triangle-bound exclusion using 61 for Soluch–Fort and a separately admitted C-sheet path. Its implication `distance(Soluch, target) ≥ 61 − path_length(Fort, target)` depends on the actual Soluch–Fort distance being at least 61, the path joining the correct fixed locations, and a shared metric. The chart's contradictory triple prevents treating all three values as a mutually consistent source-certified metric; it does **not** independently prove 61 is too high or that any resulting separation is false. Conversely, retaining 61 as a printed transcription does not prove the required geometric lower bound without a source policy or independent corroboration.

Check any alternate anchor, geometry witness, correction or unit-specific separation separately before deciding implementation impact. This review does not inspect every path, the PR's conflict gate, all alternate proofs, later errata, or runtime behavior; it asserts no bug and proposes no replacement value. Same edition as previous chart readers; no runtime changes.

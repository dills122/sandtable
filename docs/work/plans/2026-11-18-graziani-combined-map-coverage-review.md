# Graziani combined map coverage: arithmetic review

**Date:** November 18, 2026. **Disposition:** No arithmetic or overlap discrepancy found in the [combined coverage note](2026-11-18-graziani-combined-map-coverage.md). This checks documented target sets, source-table neighbor IDs, and proposed grid geometry; it does not independently inspect the map scan or certify the observations, adjacency, features, boundaries, or rules.

## Reconciliation

Reconstructed the fixed/city targets from the 39 rows of the [coverage index](2026-11-17-graziani-fixed-hex-review-coverage.md), the disk and outer envelope from the row ranges in the [work batch](2026-11-17-graziani-playable-map-work-batch.md), and the contact envelope from its listed ranges. Counts are 39, 37, 61, and 10 respectively. The disk and contact sets do not overlap; fixed/city intersects the disk at `C1014` and contact at `C3320`, `C3419`, and `C3520`. The 61-cell envelope intersects fixed/city only at `C1014`, and does not intersect contact. Thus `61 + 10 + 39 - 4 = 106` named targets, and `37 + 10 + 39 - 4 = 82` earlier six-side targets.

Extracted targets and neighbor rows from the [air](2026-11-18-graziani-air-facility-edges-batch.md), [landing A](2026-11-18-graziani-landing-strip-edges-a.md), and [landing B](2026-11-18-graziani-landing-strip-edges-b.md) ledgers. Each has ten distinct targets and 60 listed incidences; each target's six distinct neighbor IDs match the proposed staggered-grid calculation. The three target sets are disjoint. Their union and the two targets `C0127` and `C3618` are mutually disjoint and do not intersect the earlier 106 named targets. This gives **138 named targets** and **114 six-side targets**. The remaining 24 are halo interior observations, not six-side ledgers.

Six listed sides per target give `82 × 6 + 2 × 6 + 30 × 6 = 684` directed incidences. The two C0127 lower-margin entries lack printed neighbors, leaving **682 neighbor-named incidences**. This uses the earlier ledgers' documented six-side coverage and the two new source tables; it is not a count of independently verified features or clear sides.

## Pair checks

Using the proposed staggered grid, earlier six-side targets and facility targets share exactly six geometric pairs: `B5825–B5925`, `C4119–C4120`, `C4119–C4218`, `D3516–D3615`, `E3613–E3614`, and `E3614–E3714`. Earlier targets and the two added targets share `C3519–C3618` and `C3617–C3618`; facility and added target sets share none. Within the facility set only `E2132–E2133` and `D3416–D3516` have both endpoints as targets, giving `180 - 2 = 178` distinct unordered facility pairs. Facility source rows name both endpoints of both pairs. These calculations count geometry, not agreement between readings or reciprocal legal travel.

All eight pairs in the [Matruh ledger](2026-11-17-graziani-matruh-route-batch.md) touch an earlier or facility six-side target; `D3516–D3517` touches facility target `D3516`. They cannot be added as eight disjoint pairs to this scope. This check does not count other route research.

## Limits

Same-scan reviews are informed checks, not independent-edition corroboration. Obscured ink, source-versus-pin conflicts, and unrecorded features remain unknown where the underlying records say so. `C3419–C3320` remains obscured; the `D3516–D3517` rail correction and other facility conflicts remain qualified in the [facility audit](2026-11-18-graziani-facility-side-coverage-audit.md). The C0127 margin entries do not prove an impassable physical boundary, absence of another sheet, or an off-map rule. Scenario-printed `B4827` remains distinct from map-identified `A4827`.

No complete route, playable boundary, full-map certification, setup, movement, control, or ZOC follows from these counts. Documentation review only; no runtime or map data changed and no gameplay tests run.

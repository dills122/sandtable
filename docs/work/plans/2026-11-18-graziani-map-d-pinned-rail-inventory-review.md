# Review: Map D pinned rail candidate inventory

**Date:** November 18, 2026. **Disposition:** Literal inventory verified against pinned file; source comparisons accurately retain limits of linked observations. This review does not inspect the map scan or certify rail geometry, construction, service, or §8.78 eligibility.

## Independent data check

Read [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Selected each record once when its features contain `railroad` or `unfinished-railroad` and either endpoint matches `D` followed by four digits. Recomputed:

| Measure | Result |
| --- | ---: |
| Selected records | 42 |
| Records with `railroad` | 25 |
| Records with `unfinished-railroad` | 17 |
| Internal D–D records | 40 |
| Open records | 2 |
| Explicit D–other-sheet records | 0 |
| Distinct D endpoints across selection | 41 |
| Distinct D endpoints incident to `railroad` | 25 |
| Distinct D endpoints incident to `unfinished-railroad` | 17 |

The feature endpoint sets intersect only at `D3615`. Open records are `D3133|E` (`road`, `railroad`) and `D3601|W` (`unfinished-railroad`); both have null `b`. Direction labels do not supply other-sheet endpoints. Compared all 42 [inventory table](2026-11-18-graziani-map-d-pinned-rail-inventory.md#literal-selected-rows) entries programmatically with selected records in file order: keys, ordered feature lists, sides, and `up` values match exactly. Each key also matches its `a` and `b` or open-side fields. No omitted or extra selected rows found.

## Source comparison check

Read linked observations, not underlying scans. [Matruh extension](2026-11-17-graziani-matruh-east-extension.md) and [junction review](2026-11-17-graziani-d3615-east-junction-review.md) support the inventory's qualified discrepancy and uncertain-alternative language; neither proves absence on the disputed junction. [Matruh batch](2026-11-17-graziani-matruh-east-rail-batch-review.md) reports six locally supported rail sides with vertex and overprint qualifications; [Fuka batch](2026-11-17-graziani-fuka-east-rail-batch-review.md) reports seven successive sides. These are readings of the same scan, not independent-edition confirmation or route certification.

[East continuation](2026-11-17-graziani-continuation-map-east.md) does not establish rail on `D3231–D3331` or the relation between observed `D3133–D3134` and open `D3133|E`. [C/D seam transcription](2026-11-17-graziani-seam-cd-independent.md) reports nearby railway motifs but leaves precise side correspondence unresolved. [Rules analysis](2026-11-17-graziani-continuation-rules.md) identifies §8.47 as strongly supporting exclusion of unconstructed railroad, subject to an overriding rule. Inventory correctly leaves construction and independently qualifying track to separate evidence.

Counts classify pinned labels only. Missing records do not prove absent ink; observed ink does not establish current construction, occupancy, west relation, witness status, or legal rail use. No runtime or map-data changes; no gameplay tests.

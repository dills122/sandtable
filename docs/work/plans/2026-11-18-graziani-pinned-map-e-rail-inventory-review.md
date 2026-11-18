# Review: pinned Map E rail candidate inventory

**Date:** November 18, 2026. **Disposition:** Data-only counts and stated limits checked; no discrepancy found. This review did not inspect the printed map or independently certify the linked local readings.

Recomputed from [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98eb30d98e6c`, source `vassal:CNAv2.1.0`. Selecting each record incident to an E-prefixed endpoint and containing either raw rail feature produced 143 records, all with `railroad` and none with `unfinished-railroad`; 142 distinct E endpoints; 142 E-to-E records; and one open record, `E3101|W` (`a: E3101`, `b: null`, `side: W`). All other selected `side` values and all selected `up` values are null. No selected record joins E to another named sheet. Selected keys are unique.

Extracted the three candidate lists in the [crosswalk](2026-11-18-graziani-rail-eastern-witness-crosswalk.md): 36 distinct IDs. The two groups in the [fork followup](2026-11-18-graziani-rail-eastern-candidates-next.md) add five distinct IDs with no overlap. All 41 occur in the pin. Endpoint-membership partition across selected records is 38 with two reviewed IDs, eight with one, and 97 with none. The inventory's 101 other IDs match the computed set exactly, without duplicates. Membership counts do not certify individual sides.

The [inventory](2026-11-18-graziani-pinned-map-e-rail-inventory.md) correctly limits these results to pinned records. Missing or unfinished features do not establish source completeness, actual construction state, route continuity, occupancy, or an unblocked result. Crosswalk and fork qualifications remain applicable; no map scan or independent-edition evidence was examined for this review.

Documentation-only review; no runtime or map-data changes, and no gameplay tests.

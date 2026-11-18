# Siwa west leads: independent scan review

**Date:** November 17, 2026. **Scope:** Challenge review of [west leads](2026-11-17-graziani-siwa-west-leads.md), using the same Map C scan and pinned sides, not an independent map edition. No runtime or data changes.

The four proposed local crossings are supported as bounded visual observations. Neither crowded vertex establishes an onward side. Map A's terrain key (PDF p. 1) labels the single thick dashed mark `Track`; the parallel thin dashed mark is `Unfinished Road`. The two west leads use the former pattern. This distinguishes the route marks from thin hex grid, brown terrain ink, blue oasis artwork, and hex numbers; it does not establish legal movement or a connected route.

| Candidate | Scan check | Pinned features |
| --- | --- | --- |
| C0224–C0324 | Thick dash crosses the slanted shared side above the oasis artwork. Further dashes toward C0225 are interrupted by gaps and overlap the artwork. | `track` |
| C0325–C0424 | Dashes approach the shared slanted side from both sides, with a gap at the side. Crossing is inferred from their alignment, not ink directly on the side. | `track` |
| C0424–C0524 | Thick dash crosses the slanted side over brown terrain ink. | `slope`, `track` |
| C0524–C0523 | Thick dash crosses the shared vertical side near its upper vertex; another dash lies inside C0523. | `track` |

At C0324's upper-left vertex, a thick dash approaches the grid junction from C0324 and dark dash-like ink continues across brown terrain near the C0323–C0423 side. Grid and terrain overlap the junction, and dash spacing leaves the exact passage unresolved. Do not attribute an onward crossing to C0324–C0323 or C0324–C0423 from this scan. Pinned C0324–C0423 has `slope` without `track`; C0323–C0324 has no record.

At C0523's upper-left vertex, the thick dashed mark approaches the junction shared with C0622 and C0623. Ink and grid meet at the vertex; scan does not settle which side owns the crossing. Pinned C0523–C0622 and C0523–C0623 both list `track`, but cannot settle that visual attribution. Pinned data is VASSAL-derived comparison, not independent confirmation; an absent record is not proof of absence. No conclusion about joins beyond these marks, a Siwa–Tobruk route, convoy eligibility, or movement legality follows.

**Evidence:** Inspected PDF pp. 1 and 3 and the 600 dpi crop cited in the lead. SHA-256 values checked: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `69261669fdcc4b7f8274ee0b1d538a4d03aa3c217b6b39fc12dee20562456a2d`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. No gameplay tests; documentation-only review.

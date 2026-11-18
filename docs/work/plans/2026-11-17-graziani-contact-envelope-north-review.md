# Independent review: Sheferzen north contact envelope

**Date:** November 17, 2026. **Disposition:** No discrepancy found in the 24 side observations or the pinned comparison. This is a second visual reading of the same scan, not independent edition corroboration or certification of legal crossings.

Reviewed the [north ledger](2026-11-17-graziani-contact-envelope-north.md) and [C3320 record](2026-11-17-graziani-c3320-six-sides.md) against Map C, PDF p. 3, at the ledger's 600 dpi crop. PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Page-image directions and printed numbers support all neighbor pairs and side positions. The following rows independently account for all 24 directed sides; exposed grid means no identified feature on the visible portion, not proof of an empty side.

| Cell | Six sides in upper-left, upper-right, right, lower-right, lower-left, left order |
| --- | --- |
| C3219 | C3319 track; C3320 exposed grid near crowded vertex; C3220 exposed grid; C3120 track near vertex; C3119 blue checks; C3218 blue checks. |
| C3220 | C3320, C3321, C3221, C3121, C3120 and C3219: exposed grid; nearby C3219/C3120 track does not visibly cross C3220/C3120. |
| C3319 | C3418 blue checks; C3419 track; C3320 exposed grid with name overprint at upper end; C3219 track; C3218 blue checks; C3318 blue checks. |
| C3321 | C3420 track; C3421 exposed grid; C3322 track; C3221 and C3220 exposed grid; C3320 exposed grid with name overprint near upper end. |

Blue checks meet the C3319/C3419 endpoint but do not visibly run along that track-bearing diagonal. The apparent track at C3219/C3319 passes west of the shared C3219/C3320 vertex; dash gaps and the crowded vertex do not establish a crossing of the latter. Lettering hides parts of C3319/C3320 and C3320/C3321. The four shared C3320 sides in this review agree with the prior record; its other two sides, C3419 and C3420, retain the prior record's overprint qualifications. No whole-cell terrain or rules conclusion follows from this inspection.

Compared `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` (SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`): literal matches involving the four cells are C3120|C3219, C3219|C3319, C3319|C3419, C3321|C3322 and C3321|C3420. Each records `track` with `up: null`; no literal C3320 record appears. Absence of a record does not certify absence of a source feature or establish its game effect. Blue checks and covered ink need separate interpretation and clearer evidence where applicable.

No runtime or data changes. No tests run; this was a source and pinned-file review.

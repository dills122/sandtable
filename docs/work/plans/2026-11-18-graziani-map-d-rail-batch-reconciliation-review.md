# Map D pinned rail batch reconciliation: arithmetic review

**Date:** November 18, 2026. The proposed provisional partition is **26 source-positive, 14 unknown, 0 not-yet-reviewed, and 2 open**, totaling 42. This follows the [worklist](2026-11-18-graziani-map-d-rail-worklist.md)'s existing 22 / 6 / 12 / 2 baseline; this audit checks the 12 transfers and their pin records, not the earlier 30 source readings anew. Categories concern the exact pinned rail class, not generic rail alone.

- [Batch A](2026-11-18-graziani-map-d-rail-batch-a.md): four move from not-yet-reviewed to unknown. Each has a generic rail observation but no established unfinished class; `D3710|D3711` also has a vertex-qualified crossing. Independent review was underway when this audit was written.
- [Batch B](2026-11-18-graziani-map-d-rail-batch-b.md): four move from not-yet-reviewed to source-positive, qualified by scan softness and overprint. Its [informed same-scan review](2026-11-18-graziani-map-d-rail-batch-b-review.md) supports the interrupted-centerline class reading and separately supports track on `D3602|D3603`.
- [Batch C](2026-11-18-graziani-map-d-rail-batch-c.md): four move from not-yet-reviewed to unknown. Generic crossings are observed, but the unfinished class is unsettled. Independent review of C remains outstanding; this reconciliation was written by C's reader and is not that review.

Arithmetic: `22 + 4 = 26` positive; `6 + 4 + 4 = 14` unknown; `12 - 4 - 4 - 4 = 0` not-yet-reviewed; 2 open unchanged. The three batches are mutually disjoint and exactly cover the 12 literal keys in the original worklist's not-yet-reviewed list. Direct JSON check found all 12 have two endpoints and include `unfinished-railroad`; `D3602|D3603` also pins `track`, and `D3611|D3711` and `D3710|D3711` also pin `road`. Other feature labels are separate issues and do not change the rail partition. Pinned hexside SHA-256 checked as `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

Counts remain provisional pending outstanding source reviews. This is a documentation and arithmetic audit, not a new edition, data change, or gameplay test.

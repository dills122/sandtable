# Review: additional Alexandria fork candidates

**Date:** November 18, 2026. **Disposition:** Agree with five additional candidate IDs and the qualified total of 41. North-branch side attribution remains limited by the nearby vertex. No correction identified within this bounded reading.

Reviewed [candidate note](2026-11-18-graziani-rail-eastern-candidates-next.md) against page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), using its two 700 dpi crops. This is a separate visual check of the same scan, not independent-edition corroboration; the candidate note, earlier review, and pinned data were visible. PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; detail crop `ae1c57c72c45956ec7959fc3d3633ae9fbd4494a7ce528e914aebda804e37b58`; western crop `cb6dd91bba2558c3585a48dab91f5570bce8d3c027eda2449decb84148cc3014`. Crop hashes depend on renderer; reproduction commands appear in the candidate note.

| Candidate | Check |
| --- | --- |
| `E3413` | Cross-tied ink crosses `E3412–E3413` near detail `(180, 895)`. Western crop identifies both hexes. |
| `E3414` | Same eastbound ink crosses `E3413–E3414` near detail `(620, 800)`, at river. |
| `E3415` | Same ink crosses `E3414–E3415` near detail `(1065, 715)`. |
| `E3516` | Ink crosses `E3415–E3516` near detail `(1380, 590)`, at river; junction lies inside printed `3516`. No onward side counted. |
| `E3513` | Northbound cross-tied ink crosses `E3412–E3513` diagonal near western crop `(660, 640)`, close to its eastern vertex. This proximity qualifies side attribution; no continuation through Alexandria inferred. |

Checked the five literal pairs in [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`: all contain `railroad`, `side: null`, and `up: null`; the `E3413–E3414` and `E3415–E3516` pairs also contain `minor-river`. Pinned agreement is comparison evidence, not independent scan confirmation.

Counted the three candidate lists in the [prior crosswalk](2026-11-18-graziani-rail-eastern-witness-crosswalk.md): 36 entries, all distinct. None of these five IDs appears there; union is 41. This checks list arithmetic, not every prior reading. Original crosswalk qualifications still apply, including registration and geographic west relation. Candidate ink does not establish construction, destruction policy, occupancy, Axis combat classification, or a fixed snapshot. An occupied qualifying eastern rail hex may block despite unknowns; absence from this incomplete candidate set cannot prove an unblocked result.

No runtime or map-data changes; no gameplay tests.

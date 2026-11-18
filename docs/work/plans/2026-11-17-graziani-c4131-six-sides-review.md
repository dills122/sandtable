# C4131 six sides: independent visual review

**Disposition:** The [source audit](2026-11-17-graziani-c4131-six-sides.md) correctly identifies the six neighboring labels and preserves coastal, track and road uncertainties. Strengthen its C4130 qualification: the inspected scan shows paired dashed strokes immediately west and paired strokes east of the side, but the crossing itself is crowded by the gray grid and name ink. It supports an aligned route lead, not a visibly uninterrupted pair proving either completed road or an actual crossing at that exact side.

## Source check

Visually inspected [Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, using the audit's 500-dpi crop. Approximate C4131 vertices are `(615,490)`, `(770,575)`, `(770,760)`, `(615,850)`, `(455,760)`, `(455,575)`. The dot lies inside C4131; shoreline traverses its northern interior. Directions refer to the displayed crop.

| Neighbor | Independent observation |
| --- | --- |
| `C4130` | Left vertical side: paired dashed strokes approach from the west, while paired road-like strokes continue east; the side crossing is crowded by grid and blue name ink and may coincide with a dash gap/transition. Exact finished versus unfinished status, and unbroken crossing under the overprint, remain unresolved. Pinned `road` is a candidate, not source-certified here. |
| `C4132` | Right vertical side: paired solid-looking route strokes cross near the name; shoreline meets the upper part of the same side. Pinned `road` agrees with this local appearance; text and other features require separate checks. |
| `C4030` | Lower-left diagonal: exposed grid appears narrow; adjacent route and text lie near its western vertex. No blanket no-feature conclusion. |
| `C4031` | Lower-right diagonal: a single dashed dark track extends from the dot area across toward C4031; dot/line overlap limits the exact crossing. Pinned `track` is supported as a local lead. |
| `C4230` | Upper-left diagonal lies in blue sea on the scan; grid visible. No inferred traversal or exhaustive all-sea-side rule determination. |
| `C4231` | Upper-right diagonal likewise lies in blue sea on the scan; grid visible. Same limit. |

Key distinguishes double solid road, double dashed unfinished road and single dashed track. An apparent route crossing is not itself legal entry, and shoreline, hidden marks and source-policy interpretation remain distinct from pinned data. The other three side pairs have no literal pinned entry according to the audited note; this review does not treat absent entries as source proof.

## Provenance

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `9b7e130a29b54b6e21db3237a35dfb3c55276650ddc16f2f636207f9fa438032`; pinned side file SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` as cited by note. Same-scan informed reinspection, not a separate edition. No runtime edits or tests.

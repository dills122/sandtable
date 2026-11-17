# C3921 six sides: independent visual comparison

**Disposition:** The six-side feature comparison is consistent with the
reproduced original Map C crop and pinned records. Both escarpments and both
unfinished-rail crossings are visible. The dark track's precise crossings at
C4020 and C3821 remain interpretive. Qualify the proposal's general statement
that neighbor numbers are legible except C3922: in this inspection **C4021's
label is also partly obscured** by border, terrain and road artwork; geometry
and the printed Sollum location support its identity. Neither obscured label
is a reason to invent a different cell, but record identity evidence separately
from the side's visible artwork.

## Reproduction and scope

Reproduced the [six-side follow-up](2026-11-17-graziani-c3921-six-sides-next.md)'s
500-dpi crop from [original maps](https://spigames.net/PDFv10/CNA_Maps.pdf)
PDF p. 3 with its exact parameters. Map PDF SHA-256 matches
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
rendered crop SHA-256 matches
`be5e91c052f0b937cf73616314840a7ef41d88801f22e6b7b197e1400242d94e`.
Also compared the earlier 300-dpi local crop and PDF p. 1 terrain key. The
pinned [hexsides file](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
hashes to `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
This is prompted reinspection of the same edition, not a blind independent
transcription, full terrain census or rights verification. Pixel coordinates
and side identity are visual observations, not a registered geometry proof.

## Side-by-side findings

| Neighbor | Independent observation | Pinned comparison and limit |
| --- | --- | --- |
| C3920 | Blue checkered ink follows the west shared side. No dark escarpment or blue river is visible there. | No feature record. Blue mark is not evidence of a source-certified clear side; its political-boundary reading and rule effect remain separate. |
| C4020 | Blue checks follow the northwest side; the blue cross-tied unfinished railroad intersects it. Dark dashed track approaches from both sides with a gap/overprint at the side. | `track`, `unfinished-railroad`. Railroad intersection is visible; an intended track connection is plausible but an unbroken dash is not shown. Blue check is additional source information. |
| C4021 | Dark stippled escarpment follows the northeast side; splash is on the northeast neighbor's side. The neighbor label is partly obscured in this crop. | `escarpment`, `up: C3921`, consistent with the art and Land §8.35's splash-on-down-side rule, subject to identity admission. |
| C3922 | Dark escarpment follows the east vertical side with splash east. Halfaya label and nearby marks obscure the printed number. | `escarpment`, `up: C3921`, consistent with the art and candidate identity; not an independently legible full cell number. |
| C3821 | Blue cross-tied unfinished railroad crosses the southeast side. Dark dashed track approaches the southeastern vertex and continues nearby; crossing ownership at the crowded vertex is not decisively visible. | `track`, `unfinished-railroad`. Railroad is supported; track requires an accepted vertex/gap interpretation, not a claim of plainly unbroken side contact. |
| C3820 | Central southwest side appears as thin gray grid line; blue checks meet its western endpoint and continue elsewhere. | No feature record. Do not infer an independently certified clear whole edge from a visually quiet center with a crowded endpoint. |

The source key differentiates unfinished blue cross-tied railroad from the
single dark dashed track; an unfinished railroad mark alone is not a usable
rail connection. The blue checkered mark has no direct matching blue swatch in
the key; the geographic-boundary interpretation is separately documented and
does not by itself give a cost or ZOC rule. §10.21(b) blocks ZOC through an
accepted escarpment in either direction, regardless of its up-side. This does
not prove movement, occupancy, ZOC on another side or scenario reachability.

The pinned file has four matching pair records and no C3920/C3921 or
C3820/C3921 pair record. A no-record convention is a dataset statement, not
proof no source mark exists; the C3920 blue feature illustrates the distinction.
No claim here upgrades any record or resolves all uninspected source features.

Before admission, independently resolve partly hidden labels, whether dark
dashes cross each disputed side or vertex, boundary meaning, feature and
terrain coverage, and unit-specific movement/ZOC conditions. Proposed checks
should vary each side separately, preserve both directions of accepted
escarpment ZOC blocking, test track crossing versus mere proximity, unfinished
rail, missing record versus source clear, and replay under the pinned version.
No pinned data, runtime or readiness flag changed; no gameplay tests ran.

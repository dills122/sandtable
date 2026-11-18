# C3922 six-side source review

**Disposition:** The [six-side note](2026-11-17-graziani-c3922-six-sides.md) correctly preserves an inferred C3922 locator and a candidate discrepancy between its C4021 side and pinned escarpment. The side's exposed segment looks thin, while the adjoining C3921/C3922 side has the broad dark band; road ink, shoreline and the shared vertex prevent certifying the contested side feature-free. The C3821 track crossing is especially close to the crowded western vertex and remains a lead rather than a fully isolated line.

## Same-scan checks

[Map C](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3, 500-dpi crop `(6100,6050,1800,1550)`: `3921` and `3923` are legible, and the intervening cell's number is hidden by `Halfaya Pass` and artwork. Its six neighboring cells follow the local grid as in the candidate, but the C3922 numeral itself is not visually confirmed. PDF p. 1 key differentiates the broad stippled escarpment band, gold slope, single dark dashed track, paired unfinished-road dashes, and blue shore/water.

| Neighbor | Independent visual reading | Pinned comparison |
| --- | --- | --- |
| C3921 | Broad dark escarpment band follows the left vertical side, with splash toward the candidate C3922 interior. | `escarpment`, `up C3921`; up direction not independently adjudicated. |
| C3821 | Gold slope-like band follows the lower-left diagonal. A single dashed track passes at the crowded western endpoint; precise crossing/vertex ownership has limited visibility. | `slope`, `track`, `up C3821`; conditional source support only. |
| C3822 | Paired short dark dashes cross the lower-right diagonal, partly obscured by the place name. | `unfinished-road`. |
| C3923 | Right vertical line passes through both blue water and pale land; exposed line is thin, and lettering is nearby. | No literal pair found. |
| C4022 | Upper-right diagonal runs through blue water, with shoreline and text at/near its western endpoint. | No literal pair found; no all-sea or empty-side certification. |
| C4021 | Upper-left diagonal runs from an escarpment junction toward coast; exposed portion appears thin, crossed by paired dashes near shoreline. The heavy band visibly follows the adjacent left side, not clearly this diagonal. Its junction, splash and overprints prevent conclusive negative identification. | `escarpment`, `unfinished-road`, `up C4021`; escarpment is a source/pinned candidate discrepancy, not a proven correction. |

The local dot associated with Sollum sits in the adjoining C4021 area under the grid reading; a name's lettering does not establish its location. No source observation above determines legality of movement, ZOC, a specific up/down direction, or route continuity beyond the inspected sides. Missing pinned pair entries are file facts, not proof of absent map features.

Checked literal pairs in `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; same independently rendered crop SHA-256 `39a47865e6cff7c3ee163084aeb78f16fc023d9adad6db8e094c53bfdac7bebc`. Same-scan informed recheck, not a new edition or full map audit. No runtime changes or tests.

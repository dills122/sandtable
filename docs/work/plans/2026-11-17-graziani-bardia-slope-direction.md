# Bardia C4321/C4220 slope direction

**Finding:** On the exposed portion of the brown band along C4321/C4220, irregular brown splash extends into **C4321**. C4220 is on the opposite side. Land §8.35 puts slope splash on the down side, so this supports **C4321 down and C4220 up**, consistent with pinned `up: C4220`. City blocks, figures and lettering overlap the band; this is a reading of the visible part, not a claim that the whole side is unobscured.

## Evidence

- [Original maps](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`: PDF p. 3, Map C, and p. 1 terrain key. Rendered p. 3 at 600 dpi using Poppler crop `-x 6800 -y 5800 -W 2000 -H 2000`. In that crop, Bardia city and AXIS artwork occupy C4321; its printed label is obscured, so identification follows the surrounding numbered grid. Printed `4220` lies in its lower-left neighbor and remains readable despite crossing marks. Their shared diagonal runs approximately from `(770, 1000)` to `(960, 1100)`, with C4321 above/right and C4220 below/left. Gold-brown ink follows the diagonal and its irregular exposed edge projects into C4321, particularly near its left portion. Artwork and the adjoining marked side crowd the junction, limiting interpretation of individual flecks.
- The terrain key distinguishes a one-sided brown splash-edged **slope** from a two-sided brown **ridge** and a dark gray **escarpment**. The visible brown band and one-sided splash on this segment support the slope reading; the adjoining marks and overprint limit a negative reading of other features.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 14 §8.35, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, state that slope and escarpment splash contours lie on the **down** side, while ridges have no direction. The up/down conclusion applies this convention, not geographic elevation inferred independently.
- Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`, records `C4220|C4321` as `slope`, `up: C4220`; direction agrees with the visible-part reading. No runtime data change follows from this check.

This check does not establish movement cost, road connectivity, combat, ZOC, or completeness of either hex's other sides.

[Independent six-side review](2026-11-17-graziani-c4321-six-sides-review.md) also notes city and AXIS overprint and does not independently establish the high side.

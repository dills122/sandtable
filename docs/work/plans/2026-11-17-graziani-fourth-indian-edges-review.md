# Fourth Indian local edges: independent visual check

**Disposition:** The reproduced Map D crops support the four bounded feature
readings in the [source comparison](2026-11-17-graziani-fourth-indian-edges-next.md).
Road and blue railroad marks cross both D3615–D3715–D3714 shared sides; the
slope splash supports D3613 and D3515 as the respective higher hexes. The
D3515/D3615 blue branch resembles unfinished railroad, but its junction and
linework are overprinted; wider connection and operational status remain
separate questions. No map admission or legal-movement result follows.

## Source and reproduction

Reproduced both exact 300-dpi and 500-dpi crops from the
[original map package](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 4,
using the commands and coordinates in the source comparison. The PDF SHA-256
matches `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
render hashes match respectively
`b689f05fdb1ddde578cd48055c7067175666d0c4d99b11e66aa16e509d0bdab6` and
`fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191`.
Compared Map A's printed terrain key and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14–15 §§8.35, 8.43 and 8.47, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
This is prompted inspection of the same scans, not a blind transcription,
independent source edition, registered overlay or rights verification.

## Independent observations

| Pair | Source crop | Interpretation and limit |
| --- | --- | --- |
| D3714/D3715 | Two dark parallel road strokes and a blue cross-tied line traverse their vertical shared side; Matruh name, dot and port artwork are close. | Road and railroad crossings are visible. A crossing does not establish full rail service, legal endpoint or a clear surrounding network. |
| D3615/D3715 | Dark double road and blue cross-tied railroad traverse the sloping shared side; brown fill, grid and junction artwork overlap nearby. | Both features are consistent with the pinned record; exact line intersections are approximate and other side features remain outside this check. |
| D3613/D3714 | Brown slope band follows their sloping side; irregular splash projects into D3714. Double-line road passes across the crowded band. | Under §8.35's splash-on-down-side rule, D3714 is down and D3613 up; road and slope costs require separate adjudication. |
| D3515/D3615 | Brown band follows their diagonal side, with splash toward D3615; blue broken/cross-tied branch meets it near the junction. | D3615 is down and D3515 up under §8.35. The branch resembles the key's unfinished railroad; stipple, ties and adjoining track obscure details, so do not infer the entire branch's exact construction or connectivity. |

The terrain key distinguishes solid cross-tied railroad from broken
cross-tied unfinished railroad and double-line road. Under §8.47, unconstructed
railroad hexes are ignored for all purposes; its separate provision treats
unconstructed **roads** as tracks. Do not read that road exception as making
unfinished railroad usable as a rail line. Source artwork alone does not
establish whether a section has subsequently been constructed or damaged.

The pinned [edge file](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
hashes to `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
The four entries and `up` values match the cited comparison. This check did
not independently classify D3615/D3616, the whole coastline, all road/rail
segments or incoming ZOC. Endpoint names and side location should still be
bound to an accepted map/content version and corroborated against applicable
state, route and movement rules.

Suggested unexecuted vectors: reciprocal feature lookup, both slope
directions, incomplete/unknown junction and rail construction state,
road-across-slope cost only after the relevant terrain ruling, and unknown
neighboring coverage without a default clear edge. No data, runtime or
readiness flag changed; no gameplay tests ran.

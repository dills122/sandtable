# Graziani eastern rail: additional Alexandria fork candidates

**Date:** November 18, 2026. **Disposition:** Five additional Map E hexes are locally supported printed-rail candidates beyond the 36 listed in the [eastern crosswalk](2026-11-18-graziani-rail-eastern-witness-crosswalk.md). This is a bounded same-scan comparison, not a complete eastern survey or an Axis witness.

## Source and readings

Compared page 5 of the [original map scan](https://spigames.net/PDFv10/CNA_Maps.pdf) with literal pairs in [pinned hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). Blue cross-tied ink marks rail. Pinned data and the earlier [fork followup](2026-11-18-graziani-alex-rail-fork-followup.md) and [review](2026-11-18-graziani-alex-rail-fork-followup-review.md) were available during inspection; this is neither blind nor independent-edition corroboration. Coordinates below refer to the crops in the reproduction commands.

| Group and new candidates | Local evidence and pinned comparison |
| --- | --- |
| East branch: `E3413`, `E3414`, `E3415`, `E3516` | Cross-tied line crosses `E3412–E3413` near detail `(180, 895)`, `E3413–E3414` near `(620, 800)`, `E3414–E3415` near `(1065, 715)`, and `E3415–E3516` near `(1380, 590)`. All four pairs contain `railroad`; the second and fourth also contain `minor-river`. `E3412` is already in the 36. The junction in `E3516` ends this bounded reading; no onward side is counted. |
| North branch: `E3513` | Cross-tied line crosses the `E3412–E3513` diagonal near western crop `(660, 640)`, near its eastern vertex; the pinned pair contains `railroad`. Proximity to the vertex limits side attribution. No continuation through Alexandria is asserted. |

All five pairs have `side: null` and `up: null` in pinned data. Together with the prior 36, these yield **41 distinct candidate IDs**, subject to the original crosswalk's qualifications and the north-branch vertex qualification above. Geographic ordering east of `D3714` remains conditional on sheet registration and the chosen west relation. Candidate rail ink alone does not establish construction or destruction policy, actual occupancy, Axis combat classification, or a fixed snapshot. A qualifying occupied eastern rail hex may block despite unknowns; absence from these candidates cannot prove an unblocked result.

## Reproduction and limits

```sh
pdftoppm -f 5 -l 5 -r 700 -x 5450 -y 10400 -W 2200 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-detail
pdftoppm -f 5 -l 5 -r 700 -x 4900 -y 10400 -W 1800 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-review-west
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/alex-fork-detail.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; detail SHA-256 `ae1c57c72c45956ec7959fc3d3633ae9fbd4494a7ce528e914aebda804e37b58`; hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop hashes depend on renderer. Source and crops remain outside Git. No runtime or map-data changes; no gameplay tests.

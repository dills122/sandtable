# Fourth Indian local map edges: original-source check

**Result:** Original Map D shows the road and blue railroad passing through the
two sides D3615–D3715–D3714, consistent with pinned feature records. Two nearby
slope sides also show source-readable down sides: D3714 on D3613/D3714 and
D3615 on D3515/D3615, supporting the pinned higher sides D3613 and D3515
respectively. This is a narrow observation of four sides, not certification
of the full road or railroad network, legal travel, current occupancy or an
entire source turn.

## Source and reproduction

- Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF
  p. 4 Map D and p. 1 terrain key, `/private/tmp/sandtable-maps.pdf`, SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`,
  verified before inspection. Map key distinguishes solid double-line road,
  solid blue cross-tied railroad, broken blue cross-tied unfinished railroad,
  and brown splash-edged slope. Inspected local Map D at 300 dpi with a 500-dpi
  close check. In default Poppler rotation north is up, east is right:

```sh
pdftoppm -f 4 -l 4 -r 300 -x 1950 -y 3750 -W 1600 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/indian
pdftoppm -f 4 -l 4 -r 500 -x 3900 -y 6750 -W 1500 -H 1350 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/indian500
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

  Local PNG SHA-256 in command order:
  `b689f05fdb1ddde578cd48055c7067175666d0c4d99b11e66aa16e509d0bdab6`,
  `fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191`,
  `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`.
  PDF hash and crop coordinates identify the source; renderer versions may
  change PNG bytes. Scans and crops stay outside Git pending rights.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  printed/PDF p. 14 §8.35 and p. 15 §§8.43, 8.47, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  §8.35 places slope splash contours on the **down** side. §8.47 distinguishes
  unfinished railway from usable track; neither image nor a feature label
  decides a complete route.
- Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256
  `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
  Its origin and narrowly supported Fourth Indian consumer are documented in
  [historical map transcription](../../specs/graziani-historical-map-transcription.md).
  Earlier mapping and dataset records were known before this inspection; this is
  informed same-edition source checking, not a blind or independent edition.

## Side observations

Approximate coordinates below are pixels in the 500-dpi crop, used to locate
marks, not to claim georeferenced precision. Locators use visible printed
labels and immediate hex geometry; artwork overlaps some small numerals and
hexside ink. The original C/D seam is not involved.

| Pair and pinned feature | Source observation | Direction and limit |
| --- | --- | --- |
| D3714/D3715: `road`, `railroad` | Shared vertical side near `x=520`, `y=365–550`. Parallel dark road lines cross around `(520,460)`; blue cross-tied railroad crosses just below, around `(520,480)`. Matruh's name, village dot and port artwork lie nearby. | Both types visibly connect across this side. Do not identify a complete rail service merely from the two blue marks. |
| D3615/D3715: `road`, `railroad` | Shared diagonal side approximately `(680,640)`–`(835,550)`. The double dark road crosses its middle near `(755,595)`; the blue cross-tied rail crosses nearer its lower-left portion near `(705,625)`. The route enters the rough/coastal D3615 vicinity. | Both crossings are visually consistent with the pinned features. Some linework overlaps hex grid, brown fill and the railway junction; exact graphic intersections are approximate, not a full edge inventory. |
| D3613/D3714: `slope`, `road`, `up: D3613` | Diagonal side near `(204,550)`–`(365,640)` has a brown slope band. Its irregular splash extends into D3714, on the upper/right side of that edge. A double-line road crosses near the crowded slope around `(315,610)`. | §8.35 gives D3714 down and **D3613 up**, consistent with data. Road/slope intersection is visible, but terrain and cost consequences need their own rule/state checks. |
| D3515/D3615: `slope`, `unfinished-railroad`, `up: D3515` | Diagonal side near `(680,820)`–`(835,910)` carries a brown slope band with irregular splash toward D3615. A blue broken/cross-tied railway curves through that side near `(750,860)` toward the D3615 junction. | D3615 down and **D3515 up**, consistent with data. The small junction, stipple and rail ties overprint one another; individual line endings are not a proof of rail construction or connectivity beyond this local side. |

These directions are rule-based readings of the visible splash, not physical
elevation measurements. A fifth nearby pinned record D3615/D3616 contains
road and railroad; the shoreline and overlapping marks there were not
independently classified in this bounded pass. Do not extend a clean-edge,
coast or full traversability finding from the four reviewed sides to that
neighbor or other unexamined sides.

## Policy and test boundary

The two Fourth Indian road edges are already cited as limited opening-route
source evidence by the historical-map spec. This pass adds visual confirmation
of their railroad marks and two nearby slope directions; it does not expand the
existing movement policy. D3515/D3615 is **unfinished** railroad in the pinned
record and key reading, not an automatically operational rail segment. The
map also shows coastline, rough fill, a village and a road/rail junction; their
full classification, condition and rule effects must be assessed independently.

- Under a separately accepted local map version, verify reciprocal road and
  railroad feature lookup for D3615–D3715 and D3715–D3714. Test movement,
  stacking, destination eligibility, current track state, occupancy and ZOC
  separately; source art alone is not an action authorization.
- Verify D3613 → D3714 and D3515 → D3615 are down-slope, reverse directions
  up-slope, with the same `up` identity regardless of endpoint order. Check
  the unfinished rail feature remains distinct from finished railroad and no
  adjacent unreviewed side is inferred clear.
- Compare accepted road crossing a slope with off-road movement only after the
  applicable Land terrain rule and errata are admitted; avoid silently
  replacing the slash/overprint uncertainty with a universal terrain exemption.
- Retain original source locators and map hash in any later correction or
  admission, version content and replay, and reject unavailable surrounding
  map/ZOC coverage rather than inferring absence.

No pinned JSON, runtime, readiness flag or content identity was changed; no
gameplay tests ran. An [independent source check](2026-11-17-graziani-fourth-indian-edges-review.md)
corroborates the four local readings; it does not certify the wider junction or
network.

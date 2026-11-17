# Map C3921: six-side physical comparison

**Result:** The pinned dataset has four feature-bearing side records around
C3921 and no record for two neighboring pairs. The original scan corroborates
both escarpment sides and the unfinished-rail marks on the two track/rail
sides. The dark dashed track approaches those two railway crossings, but its
exact side contact is interrupted or crowded by other marks. A blue checkered
line follows two sides, including the one with no recorded feature; the
[separate boundary check](2026-11-17-graziani-border-mark-next.md) supports a
Libya–Egypt boundary interpretation without settling its rules effect. No
missing record is promoted to a source-certified feature-free edge.

## Source and reproduction

Original [map package](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map
C and p. 1 terrain key, `/private/tmp/sandtable-maps.pdf`, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Hash verified before inspection. Inspected the whole local region at 300 dpi,
then enlarged at 500 dpi to distinguish ink gaps and intersections. North is
up and east right in the default Poppler orientation:

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3000 -y 3350 -W 1550 -H 1350 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/halfaya
pdftoppm -f 3 -l 3 -r 500 -x 5750 -y 6200 -W 1500 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/c3921-500
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

Local PNG SHA-256 in command order:

```text
61217cc97a0b079f9d186a17d48bb2cca6619e085566aa5808aa116f32c1571d
be5e91c052f0b937cf73616314840a7ef41d88801f22e6b7b197e1400242d94e
8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894
```

PDF hash and render parameters are primary provenance; PNG bytes can differ by
renderer version. Scans/crops stay outside Git pending rights. Map key shows
single dark dashed `Track`, blue cross-tied `Unfinished Railroad`, dark
splash-edged `Escarpment`, and green `Wadi`; the key's gray `Border` sample
does not visibly reproduce the map's blue checkered pattern. The boundary
interpretation is geographic evidence, not a directly matched key swatch.

Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`,
was compared after identifying the six printed neighbors. This is a targeted,
informed reinspection of the same source used in the earlier
[Halfaya side check](2026-11-17-graziani-halfaya-edges-next.md), not a blind
review or a separate original edition.

## Six sides

Approximate pixel coordinates below refer to the 500-dpi crop; they locate
visible marks, not a registered map transform. C3921 is the central labeled
hex; its six vertices run approximately `(555,365)`, `(715,276)`, `(875,365)`,
`(875,545)`, `(715,635)`, `(555,545)` clockwise from upper-left. Neighbor
identities use visible numbers and same-sheet hex geometry; C3922 and C4021
are partly obscured, with the scenario's Halfaya/Sollum locators and adjacent
labels as corroboration. An edge record is unordered; `up` is its stated higher hex.

| Neighbor | Original map observation on shared side | Pinned record | Assessment |
| --- | --- | --- | --- |
| C3920, west | Vertical `(555,365)`–`(555,545)` carries blue alternating checkered line. No escarpment or water line is visible on the side; nearby railway/track and upper vertex are overprinted. | **No record.** | Dataset's documented no-recorded-feature convention does not preserve the visible blue mark. Strongly inferred political boundary; rules effect remains unresolved. No independent clear-edge certificate. |
| C4020, northwest | Blue checkered line follows sloping `(555,365)`–`(715,276)`. Blue cross-tied unfinished railway intersects it near `(600,340)`. The dark track approaches from both adjacent interiors, but a gap between dashes straddles the congested side near the railway/checkered overprint. | `track`, `unfinished-railroad`; `up: null`. | Railway intersection is visible. Track continuity is a plausible reading of the dashed alignment, not an unbroken ink intersection proved by this scan. Blue boundary is additional source metadata absent from the listed feature array. |
| C4021, northeast | Dark splash-edged escarpment follows `(715,276)`–`(875,365)`; splash extends toward C4021, with some upper vertex overprint. | `escarpment`, `up: C3921`. | Consistent: original Land §8.35 puts splash on the down side, so the C3921 side is up. No independent full terrain or movement certificate. |
| C3922, east | Dark escarpment follows vertical `(875,365)`–`(875,545)`, with splash toward C3922. Halfaya label and nearby road-like marks crowd the far side. | `escarpment`, `up: C3921`. | Consistent with escarpment and C3921 higher. Land §10.21(b) blocks ZOC through this side in both directions; movement requires separate unit and terrain rules. |
| C3821, southeast | Blue unfinished railway crosses sloping `(715,635)`–`(875,545)` around `(785,595)`. Dark dashed track approaches the **southeastern vertex** near the neighboring slope/label and continues into the C3821 vicinity. | `track`, `unfinished-railroad`; `up: null`. | Railway crossing visible. Track side/vertex ownership and exact contact remain uncertain under overprinting; the record is a plausible interpretation, not visually settled by this crop. |
| C3820, southwest | Sloping `(555,545)`–`(715,635)` shows a thin gray grid edge without an obvious terrain or transport symbol across its central section. Blue checkered boundary meets the western endpoint and continues along a different side; its endpoint is crowded. | **No record.** | No feature observed across the central side. Endpoint occlusion and bounded source scope prevent calling the whole side independently verified clear. |

The 500-dpi close crop is an aid to seeing dash/vertex relations; it is not a
new source or a resolution of occluded ink. The candidate source data's
`track` entries are not rejected simply because a dashed stroke can cross a
side during a gap. Conversely, drawing a straight line across the gap and
replacing both side uncertainties with verified crossings would overstate the
source. The blue checkered line is not identified as a river, escarpment or
automatic movement barrier by this inspection.

## Implications and review vectors

- Maintain six distinct source assertions. For C3920 and C3820, a missing
  feature record on known adjacent hexes means no feature **recorded** in the
  pinned transcription, not source-proven clear. Preserve C3920's visible blue
  mark and unresolved rules effect. Preserve C4020's blue mark even though it
  also has recorded track/rail features.
- Test `C3921|C4021` and `C3921|C3922` as escarpment with higher side C3921
  only under the accepted source data/version. ZOC is blocked by an
  escarpment in both directions under §10.21(b); do not infer the same for the
  other four sides or equate ZOC with movement legality.
- Distinguish unbroken railroad crossing from **track inferred through a gap**
  at C4020 and track **near a vertex** at C3821. Require independent ruling or
  stronger source evidence before using each disputed track crossing as a
  source-certified cost reduction. An unfinished railway is not a ready rail
  route. Check reciprocal lookup and source versioning without silently
  changing the embedded v1 data.
- Any full movement or ZOC decision also needs unit eligibility, actual
  location/occupancy, relevant terrain, accepted boundary semantics and
  complete surrounding coverage. A per-side visual comparison cannot
  establish total control or scenario reachability.

No pinned data, runtime, readiness or content identity changed; no gameplay
tests ran. This is six sides of one hex, not a full Map C edge census.
An [independent visual review](2026-11-17-graziani-c3921-six-sides-review.md)
corroborates the bounded feature comparison while identifying the C4021 label
qualification incorporated above.

# Map B slope directions: original-source check

**Finding:** Both previously unknown slope directions are visually discernible
in the hash-matched color scan. The splash contours lie on the B1705 side of
B1705/B1706 and on the B1805 side of B1706/B1805. Original Land §8.35 states
that slope splash contours are on the **down** side, so **B1706 is the higher
side of both slopes**. This is source evidence for a proposed correction, not
an admitted JSON change or proof of legal movement across either side.

## Sources and reproducibility

- Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), local
  `/private/tmp/sandtable-maps.pdf`, SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
  checked before inspection. Map B is PDF p. 2; Map A terrain key is PDF p. 1.
  Inspected the following crops at 300 dpi with the PDF's default display
  rotation, in which north is up and east is right:

```sh
pdftoppm -f 2 -l 2 -r 300 -x 400 -y 6900 -W 1400 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/slopes
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

  Local PNG SHA-256 respectively
`0d052a9899c50020a1c4d8cee54ffb8dce0c905849d6e1822a91e3ce951f1e10` and
  `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`.
  Render versions can change PNG bytes; PDF hash and crop parameters are the
  primary source identity. Scans and crops remain outside Git pending rights.
- Original [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  local `/private/tmp/sandtable-land-rules.pdf`, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
  visually checked printed/PDF p. 14 §8.35 and p. 15 §§8.43–.44.
  Section 8.35 expressly distinguishes slopes and escarpments with directional
  up/down sides from ridges with no direction, and places splash contours on
  the down side. The terrain key visually distinguishes brown slope, brown
  ridge and dark escarpment symbols.
- Embedded `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256
  `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`,
  has `features: ["slope"]` and `up: null` for both pairs. Source and
  [prior review](../../specs/graziani-historical-map-transcription.md) were
  consulted before this pass; this is an informed same-edition reinspection,
  not a blind transcription or a second original edition.

## Local visual evidence

Coordinates below are approximate pixels from the 1,400 × 1,300 Map B crop,
not a calibrated geometry transform. The visible printed labels identify
B1705, B1706 and B1805; these are neighboring cells on one sheet, not a seam.

| Shared side | Visible source geometry | Source direction |
| --- | --- | --- |
| B1705/B1706 | Roughly vertical side from `(595,615)` to `(595,725)`; brown slope band follows the line and its irregular stippled splash extends **left into B1705**. The B1706 interior is on the right. | B1705 down; **B1706 up**. |
| B1706/B1805 | Sloping side from approximately `(595,615)` to `(690,560)`; brown slope band follows the side, and irregular splash extends **above into B1805**. B1706 is below/right. | B1805 down; **B1706 up**. |

Yellow salt-marsh pattern and nearby dark dashed track overlap the surrounding
artwork, particularly near vertices. Enough slope band and splash remain visible
on each side to distinguish which neighboring cell receives the splash; the
local lines are brown slope, not the gray/black escarpment sample. The shared
vertex and track do not justify assigning the track to either exact crossing
without a separate side-level check. Compare the key and §8.35 together: the
higher-side conclusion is a rule-based interpretation of visible splash, not
an independent topographic height measurement.

## Admission and proposed vectors

A future independently accepted map correction can set `up` to `B1706` for
both unordered pairs under a **new versioned map/content identity**, after
normal discrepancy, provenance, rights, boundary and replay review. The
existing pinned v1 remains unchanged and must remain available to its existing
replays. The source check does not resolve other terrain, movement or ZOC
coverage and does not admit a complete map.

- Given an accepted correction, test B1705 → B1706 and B1805 → B1706 as
  **up-slope**, and both reverse directions as **down-slope**. Confirm that
  unordered edge lookup preserves the same higher hex in either direction.
- Apply direction-specific terrain effects only after accepting the other
  required hex and edge facts. §8.43 allows slope crossing subject to costs;
  §8.44 separately restricts certain vehicles in salt marsh. This evidence
  does not establish a connected track, unit eligibility, movement cost,
  legality, friendly or enemy occupancy, or ZOC at either endpoint.
- Before that correction is admitted, `up: null` stays unknown: no default
  direction, assumed equal cost, silent upgrade of v1, or replay mutation.

An [independent source review](2026-11-17-graziani-slope-directions-review.md)
reproduces the crop and confirms both high-side readings against the same source
edition. No JSON, runtime, content identity or readiness flag was changed. No
gameplay tests ran.

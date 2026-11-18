# C4707 slope directions: source observation

**Result:** visible splash supports `up: C4606`, `up: C4607`, and `up: C4707` on the respective C4606, C4607, and C4807 sides. These readings agree with pinned values. Overprints obscure portions of the latter two sides; this is a visible-part reading, not a reconstruction of hidden marks or an independent map edition.

## Evidence and method

Inspected [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, Map C PDF page 3 and terrain key page 1. Rendered crop and key as documented in [C4707 six-side reading](2026-11-17-graziani-c4707-six-sides.md): their SHA-256 values are `39b116ace7edae2d4f5f5b5cfad5d0d550c2180c6144dd6d618eddc9e0a76958` and `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`. Inspected the detail at pixels `(450, 1000, 1100, 1800)` of the first crop, enlarged for visibility. C4707 is the pale hex below Tobruk C4807, between C4706 and C4708; its identifier is obscured by blue rail symbols. Neighbor identification follows the printed grid.

The key depicts slope as ochre one-sided splash. [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, printed/PDF page 14 §8.35, place slope splash contours on the down side. Thus the opposite endpoint is up. Directions below refer to the unrotated crop.

| Side | Visible evidence | Source reading | Pinned `up` comparison |
|---|---|---|---|
| C4707/C4606 | Lower-left diagonal has ochre band and distinct irregular splash extending into C4707, above the grid edge. | C4707 down; **C4606 up**. | C4606; agrees. |
| C4707/C4607 | Lower-right diagonal has ochre band and exposed irregular flecks inside C4707 above the edge; blue railway and black route marks cover part of the band and junction. | C4707 down; **C4607 up**, based on exposed splash. | C4607; agrees. |
| C4707/C4807 | Upper-right diagonal has ochre band and exposed irregular splash into the Tobruk C4807 hex above the edge, particularly toward the right; city/Axis artwork and converging route lines cover part of the side. | C4807 down; **C4707 up**, based on exposed splash. | C4707; agrees. |

Pinned values come from [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; they are comparison only, not evidence for the readings. No inference about route crossings, movement, ZOC, combat, or concealed markings follows. No runtime change or test needed for this documentation-only observation.

# C3320: six-side visual source audit

**Finding:** The six adjacent labeled cells are C3319, C3321, C3219, C3220, C3419 and C3420. The visible portions of C3320's sides look like thin grid lines; blue `Bir el Sheferzen` lettering overlaps the northern sides and vertices, so retain uncertainty where it hides the line. No explicit C3320 feature record appears in the pinned hexside file. An absent record is not proof that a side is feature-free or legal for a given unit.

## Original scan observations

[Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and terrain key p. 1: the key depicts a track as a single dark dashed line, escarpment as a thick dark band with splash, and its `Border` example as a plain gray grid-side illustration. The blue checkered line west of these hexes is a separate source mark; its political meaning and effects are addressed in [border review](2026-11-17-graziani-contact-border-rules-review.md), not determined by proximity. C3320's number is legible, and its exposed interior is unpatterned; [interior review](2026-11-17-graziani-south-contact-terrain-review.md) separately addresses that fact.

| Neighbor | Shared-side observation | Bounded conclusion |
| --- | --- | --- |
| C3419 | Northern-left diagonal; blue place-name letters overlap part of the side and its endpoints. Visible stretches are narrow grid ink; nearby blue dot lies in C3419. | Overprinted portion **unknown**; no complete no-feature certification. This is the Sheferzen contact side. |
| C3420 | Northern-right diagonal; visible grid line is thin, with name ink near its western endpoint. | No wide escarpment band observed on exposed portion; vertex/overprint remains uncertain. |
| C3321 | Right vertical side; thin grid line, with blue name lettering crossing near its upper portion. | Exposed portion shows no identified side feature; retain unknown under text. |
| C3220 | Lower-right diagonal; thin grid line visible between grid vertices. | No distinct track or wide escarpment seen on this line in crop; this is a visual observation, not full rules clearance. |
| C3219 | Lower-left diagonal; thin line visible. Nearby dark dashed track crosses a neighboring side to the west of the C3320 vertex, rather than clearly crossing this diagonal. | Do not promote the nearby track to this side; vertex and dash gaps warrant separate tracing. |
| C3319 | Left vertical side; narrow grid line visible, with the name's initial lettering overlapping near its upper endpoint. | The track is in C3319 and approaches the lower-left neighborhood; its presence in the neighboring hex alone does not establish a crossing of this vertical side. |

The current `hexsides.v1.json` has no entry with `C3320` as either endpoint (file SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`). That is a comparison with an absent literal record, not a source-certified assertion that all six sides are unmarked; the data's coverage and interpretation must be reviewed separately. The visible checkered line is west of C3319/C3419 and is not itself seen on a C3320 shared side, but obscured pixels still cannot be resolved by absence elsewhere.

## Reproduction and limits

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6500 -y 9050 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-c3320
```

Original Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; rendered crop SHA-256 `2df1b51c6287e3ae34d15def957b3df0c2ac7acd4186736987c0b9607f2120ac` (renderer-dependent). Neighbor labels and local geometry were visually inspected; independent review should trace each side and crowded vertices before admitting edge content. Terrain, unit eligibility, occupancy and movement/ZOC rules remain separate. No runtime changes or tests.

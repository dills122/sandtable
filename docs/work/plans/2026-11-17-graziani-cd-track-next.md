# Map C/D candidate track: C3533–D3501

**Result:** The original C sheet visibly prints a dark single-dashed track
crossing the shared side **C3533/C3534**; the D sheet prints a similar track
inside **D3501**, continuing into D3502. Under the existing 33-column
candidate, C3534 occupies the position represented by D3501, making these
observations consistent with the bounded C3533–D3501 track policy. The
D3501-side track dash stops just short of its clipped west print edge, and
physical identity/registration of the overlapping sheets is not established by
these marks alone. No contradiction with the two pinned `track` feature
records was found; those records are not independently conclusive proof of
continuous legal movement.

## Sources and reproducing the crops

Original [five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), local
`/private/tmp/sandtable-maps.pdf`, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`,
checked before inspection. PDF p. 3 is Map C; p. 4 is Map D; p. 1 has the
terrain key. Inspected the following 300-dpi crops and a 500-dpi close check.
Default Poppler page rotation places north up and east right. Create the output
directory first:

```sh
pdftoppm -f 3 -l 3 -r 300 -x 5500 -y 4050 -W 1050 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/crail
pdftoppm -f 4 -l 4 -r 300 -x 0 -y 4050 -W 1100 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/drail
pdftoppm -f 3 -l 3 -r 500 -x 10100 -y 7500 -W 800 -H 750 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/ctrack500
pdftoppm -f 4 -l 4 -r 500 -x 0 -y 7500 -W 850 -H 750 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/dtrack500
```

PNG hashes, in command order:

```text
44b0ed0137c3fa353e0bd07ad8317d2e022e72d33d748c17874096a667a23764
5d8e902c46bb4f361bb5efc765df775313d8f8488822b48265864995ecfe8be1
2495c3ce15f563545c72fb570f37ab1944757e9ace40aa7a64dd2261a7420d6c
fadf8ef1df68f5eb4c5823a49d9e83f40e3c9498a1200a23f3f09086d9b35105
```

The PDF hash and commands identify the source and view; renderer versions can
change PNG hashes. Source scans and crops stay outside Git. The key's single
thick dark dashed symbol is labeled `Track`; its paired thin dashed symbol is
`Unfinished Road`, and the cross-tied blue symbols are rail. This reinspection
was informed by prior [C/D strip](2026-11-17-graziani-seam-cd-independent.md)
and [rail](2026-11-17-graziani-cd-rail-followup.md) readings; it is not blind
or an independent publisher edition.

## Side-by-side physical observations

Approximate coordinates refer to the specified 500-dpi crops, measured by
visual inspection rather than image registration:

| Printed feature | Observation | Limit |
| --- | --- | --- |
| C3533 → C3534 | In the C crop, the solid dash spans the vertical side near `(296,214)`; printed `3533` lies to its left and `3534` to its right. The track proceeds through C3534. | This is a visible **within-C printed hexside crossing**. The independent alignment of C3534 and D3501 remains a separate question. |
| C3534 east edge | Last dark dash ends near `(599,214)`; right-side outline lies near `x=610`, leaving an apparent roughly 11-pixel gap. | A broken track pattern can end between dashes. This gap alone does not mean the track terminates or continues. This is C3534's far edge, **not** the C3533/C3534 shared side used by the candidate. |
| D3501 west edge | Near the clipped straight margin, D's gray outline/page edge lies around `x=20`; first dark dash begins around `(31,245)`, leaving an apparent roughly 11-pixel gap. The track is plainly inside D3501. | Dash gap and clipping leave literal ink contact unproved. A route could still be intended across the side; no absence or continuity follows from the gap alone. |
| D3501 → D3502 | A dash spans their visible shared vertical side around `(335,245)`; it continues east through D3502. | This is a second **within-D** observation, not direct proof of C/D identity. |
| Neighboring artwork | A brown slope band follows upper sloping sides of the C3533/C3534 area and the corresponding-looking upper sides near D3501/D3502. Blue unfinished railway and a separate dark track appear farther north in row 36. | These nearby features help locate the corridor, but do not by themselves classify the C3533/D3501 side, choose slope direction or authorize rail movement. |

At the observed C shared side, the track crosses near two-fifths of the way
from the upper vertex; D's track is near the middle of the clipped western
side. Approximate proportions and the different page scans do not constitute a
registered overlay, a measured physical defect or evidence the designer did
not intend a connection. A precise art match, complete hexside ownership and
an original-source resolution of §4.1's conflicting overlap wording remain
open. A map print edge is not automatically a prohibited game edge.

## Pinned data and policy comparison

`src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`,
has one-ended `C3533|E` with `track` and one-ended `D3501|W` with `track`.
The [historical-map spec](../../specs/graziani-historical-map-transcription.md)
records their C3533–D3501 crossing as a bounded source-turn policy, not a
general seam or all-map certificate. The retained canonical data omits C3534;
its `C3533|E` boundary representation must not be confused with the physical
rightmost line on the original C sheet, which is beyond printed C3534.

The visible C crossing and the D track are **consistent** with the candidate
feature label. The D dash does not itself reach the left print boundary, so a
claim of an unbroken ink stroke on both margins would overstate this evidence.
No data change, policy revocation or new route admission follows: unresolved
C3534/D3501 overlap identity, §4.1 and feature alignment require their own
source hierarchy and accepted policy. The prior bounded interpretation remains
as documented; it is not upgraded to general visual certification here.

## Proposed checks

- Preserve C3534 as a printed source locator even if canonical v1 omits it;
  compare its track-bearing C3533 shared side with D3501's **clipped** west
  side under the separately accepted overlap mapping.
- For the existing policy identity, verify reciprocal track metadata for the
  named candidate pair; avoid claiming either missing ink contact or pixel
  coincidence. A test of track benefit must also establish the applicable
  movement rules, terrain, occupancy and valid route; a line drawing alone is
  insufficient.
- Remove or vary a boundary feature in a synthetic input and verify it is
  surfaced for review rather than silently unioned with another sheet. Test
  unresolved alias, unknown edge, changed content identity and replay without
  treating uninspected adjacent seams as clear.

No runtime or pinned JSON changed; no gameplay tests ran. Scope is one track
candidate, not a full C/D edge census or independent original edition.
An [independent visual review](2026-11-17-graziani-cd-track-review.md)
reproduces the crops and corroborates the local track observations, while
leaving sheet overlap and uninterrupted seam continuity unproved.

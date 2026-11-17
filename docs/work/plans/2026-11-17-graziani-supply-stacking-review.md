# Independent review: abstract supply stacking

**Disposition:** RUL-GRA-08 is a bounded planning interpretation, not an official
correction or admitted game authority. Its Matruh example follows the cited
source under its selected three-unit on-map interpretation. Tighten the
exception's trigger and end condition before implementation.

## Source check

Independently hashed and visually inspected Land rules PDF p. 43, §32.14 and
adjacent §§32.12–32.18; scenario PDF p. 2, printed p. 26, §§59.62, 59.65–59.66;
and scenario PDF p. 5, printed p. 29, §60.92(C). The map PDF's terrain key was
visually checked for the major-city symbol. The map summary's classification of
Matruh was not independently rechecked in this review; the conclusion below
depends on the existing map transcription being correct. September errata were
searched as extracted text for the cited case numbers and stacking; this is not
a visual review of every errata page or a search for later corrections.

| Source | SHA-256 |
| --- | --- |
| `/private/tmp/sandtable-land-rules.pdf` | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |
| `/private/tmp/sandtable-scenarios.pdf` | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` |
| `/private/tmp/sandtable-maps.pdf` | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` |
| `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

§32.14 gives a supply unit zero stacking value, caps supply units at five in a
major-city hex and three in any other type of hex **off-map**, and permits
unlimited supply units in Tripoli/Tunisia region boxes. Extending three to
ordinary on-map hexes is a policy choice; the printed wording does not itself
state that extension. Scenario §60.92(C) places five regular supply units at
Mersa Matruh D3714, each with 40 ammo and 60 fuel. If D3714 is not a major city,
that fixed distribution exceeds the selected on-map limit. §59.65 permits
initial excess only when unavoidable due to scenario distribution instructions,
without penalty; it bars additional supply units in that hex, then requires all
normal stacking restrictions for that hex once its distribution meets them.
It also excludes motorization points transporting supply units from hex stacking
limits. Errata §32.0 warns that abstract rules were untested; no direct correction
to these cited cases appeared in the searched September errata text.

## Required clarifications

- State the exception is **per hex**, triggered only by excess unavoidable under
  scenario distribution instructions. The fixed five at Matruh qualify under the
  selected capacity and the stated map classification. Do not automatically excuse
  an avoidable excess, including optional added supply under §59.66 or player
  placement choices; evaluate those separately against applicable limits.
- State that the exception ends when that hex's supply distribution meets
  stacking restrictions, and from then on **all normal stacking restrictions**
  for that hex apply, permanently. RUL-GRA-08 currently mentions only enforcing
  the selected numerical limit after compliance; that underspecifies the source.
  A later attempt to exceed a limit cannot revive the original exception.
- Preserve the bar on additional supply units while the exception applies, even
  if the addition is subsequently offset; define any atomic redistribution and
  its legality explicitly rather than assuming an intermediate overstack is
  allowed. Test reduction to exactly three, subsequent legal addition after a
  further reduction, and an attempted return to four.
- Keep zero stacking value distinct from the separate supply-unit count cap and
  the rule for transporting motorization points. Keep dummy inclusion unresolved:
  §32.18 calls them dummy supply units and specifies movement, but does not
  expressly settle this count or initial exception. Avoid treating this review as
  an official ruling on them.
- Verify the Matruh symbol and major-city classification directly against the map
  before admitting the specific setup exception. The map key alone does not prove
  D3714's classification. Preserve the literal off-map alternative and limit
  the selected extension to the stated Land-only planning scope.

Tests should cover an avoidable versus unavoidable initial excess, no-penalty
operation, additions blocked during excess, exact compliance, permanent
post-compliance enforcement of all relevant restrictions, later re-entry,
transporting motorization points, dummy uncertainty, and deterministic replay.

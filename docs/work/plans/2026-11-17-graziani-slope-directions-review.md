# Map B slope direction: independent visual check

**Disposition:** Reproduced source crop supports **B1706 as up-side of both**
B1705/B1706 and B1706/B1805, conditional on the printed key and Land §8.35.
No contrary reading found in this bounded check. This corroborates a proposed
correction, not a map admission, movement result or independent edition.

## Source check

Reproduced the [proposal](2026-11-17-graziani-slope-directions-next.md)'s
300-dpi Map B PDF p. 2 crop using its exact `pdftoppm` parameters from
[original maps](https://spigames.net/PDFv10/CNA_Maps.pdf). The PDF SHA-256
matches `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
the rendered crop matches
`0d052a9899c50020a1c4d8cee54ffb8dce0c905849d6e1822a91e3ce951f1e10`.
Visually checked the printed terrain key on PDF p. 1 and
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) p. 14
§8.35; Land SHA-256 matches
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
This is prompted inspection of the same scan, not a blind transcription or a
second publisher source. No exhaustive map or errata audit was performed.

## Observations

| Side | Independent visual reading | Applying §8.35 |
| --- | --- | --- |
| B1705/B1706 | Both labels are legible in adjacent cells. Brown slope band lies on their shared near-vertical side; irregular splash projects left into B1705. | B1705 down, **B1706 up**. |
| B1706/B1805 | Both labels are legible in adjacent cells. Brown slope band follows their slanted shared side; irregular splash projects above into B1805. | B1805 down, **B1706 up**. |

The key identifies a one-sided brown splash as a slope, unlike the two-sided
ridge and dark escarpment samples. Land §8.35 states slope splash lies on the
**down** side; the up-side conclusion is therefore a rule-based reading of the
art, not an independently measured elevation. Salt-marsh ink and a dashed
track crowd nearby lines and vertices. Neither direction finding certifies a
track crossing, endpoint terrain, unit eligibility, movement cost or ZOC.

The referenced [pinned edge file](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
hashes to `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`;
its two specified edges have `features: ["slope"]` and `up: null`. Keep that
version and its replay identity intact. Before correction, independently
admit the source discrepancy, coordinate/edge semantics and rights; a future
version can set the higher endpoint to B1706 for both. Test lookup in both
orientations and preserve `unknown` under the existing pinned version. These
are proposed vectors, not executed gameplay tests. No code, data or readiness
flag was changed.

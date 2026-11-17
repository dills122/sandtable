# Graziani C/D seam: Piccadilly Circus railway and tracks

**Result:** At 300 dpi, the unfinished-rail marks reach the relevant printed
hexsides on both sheets, but their intersection positions visibly differ.
Nearby track and slope marks strengthen the alignment lead while retaining
separate uncertainties about printed sides and line endings. This does not
certify a new crossing, change the existing bounded C/D contract, prove a usable
rail route, or resolve Land §4.1's sheet-overlay wording.

## Sources, orientation and reproducibility

- Original five-sheet map PDF, `/private/tmp/sandtable-maps.pdf`, SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`,
  checked before this inspection. PDF p. 3 is Map C; p. 4 is Map D; p. 1
  contains the terrain key. C and D were inspected after reading the earlier
  [C/D strip record](2026-11-17-graziani-seam-cd-independent.md); this is a
  targeted reinspection, not a blind or separate-edition review.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  `/private/tmp/sandtable-cna-errata.pdf`, SHA-256
  `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`,
  checked before inspection. PDF p. 1, MAPS, adds an escarpment on D2228/2328,
  well away from the candidate. Cases §8.37, §8.71 and §8.73 on the same
  page respectively clarify track cost, Axis rail use under §54.4, and rail
  travelers' reorganization points. None of these entries corrects this seam
  or identifies a railway crossing. This is a bounded p. 1 visual check plus
  an extracted-text search for map, rail, hexrow, overlay and section; it is
  not proof that all later material or an unindexed correction was searched.
- At default PDF display rotation, sheet east is right and north is up. Crop
  coordinates below use the rendered 300-dpi page's top-left origin; small
  coordinate estimates in the table refer to each crop's upper-left corner.
  Reproduce directly (create the target directory first):

```sh
pdftoppm -f 3 -l 3 -r 300 -x 5500 -y 4050 -W 1050 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/crail
pdftoppm -f 4 -l 4 -r 300 -x 0 -y 4050 -W 1100 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/drail
pdftoppm -f 1 -l 1 -r 300 -x 2300 -y 300 -W 2000 -H 1300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/key
```

The respective local PNG SHA-256 values are
`44b0ed0137c3fa353e0bd07ad8317d2e022e72d33d748c17874096a667a23764`,
`5d8e902c46bb4f361bb5efc765df775313d8f8488822b48265864995ecfe8be1`, and
`2a9ec6a9440fcce8400eefea4ba9fa5b0d840073557ab3f10882f1b40ed2238e`.
Rendering version may change PNG bytes; PDF hash and commands identify the
source and view. Temporary scans/crops remain outside Git pending rights.

## Observed marks

Map A's key distinguishes a continuous blue railroad from a blue broken
centerline with cross-ties labeled unfinished railroad, and a single thick dark
broken line labeled track. The Piccadilly Circus blue symbol has cross-ties and
broken centerline, consistent with unfinished railroad. Brown stippled bands
following hex boundaries match the key's slope symbol; this observation does
not decide terrain effects or direction.

| Location | Direct observation in crop | Boundary limit |
| --- | --- | --- |
| C3632 → C3633 | Blue unfinished-rail symbol crosses their shared vertical side near crop `(640, 415)`; dark track runs nearby along the upper part of the cells. | This is an interior C-sheet side. Overprinted label and slope obscure some strokes; it proves neither legality nor cross-sheet alignment. |
| C3633 east | Blue cross-tied mark reaches the outer vertical hexside around `(832, 398)`. A dark track dash along the upper edge terminates around `(823, 377)`, a few pixels short of that hexside near its upper vertex. | C3633's vertical side is an exposed edge of the **printed grid**, while adjacent odd-row C3734 and C3534 extend farther east. A short dash gap cannot prove a break. |
| D3601 west | The blue centerline crosses D3601's left vertical side near `(106, 429)` and continues right through D3601; its leftward trace goes toward the straight page edge near `(10, 410)`. Dark track also crosses this side near `(106, 397)`. | D's page edge is left of the D3601 side; part of the rail mark is cut by that page edge. A page edge and a printed hexside are distinct. |
| D3601 → D3602 | Blue line and dark track both cross their shared vertical side around `(296, 430)` and `(296, 401)`, respectively. | Within-sheet evidence only; no rule effect inferred. |
| C3533 → C3534; D3501 | C's track crosses the C3533/C3534 shared side near `(738, 577)` and its last outer dash stops just short of C3534's right vertical side near `(927, 577)`. D3501 shows a corresponding-looking track; its first visible dash starts just inside the page edge near `(18, 598)`. | Neither interrupted dash alone proves contact with the margin or with the other sheet. D3501 is clipped on its west side. |
| Slopes around row 36 | On C, brown slope marks flank the upper and lower sloping sides of C3633, including sides toward C3733/C3734 and C3533/C3534. D's corresponding band flanks D3601 around D3701/D3702 and D3501/D3502. | These are local visual side leads; no exhaustive side classification or claim that both artworks agree at every endpoint. |

Coordinates are approximate visual measurements, not georeferenced image
registration. In particular, C's rail meets its exposed side near its upper
quarter, whereas D's rail crosses the D3601 west side closer to its middle.
The difference is visible relative to the respective side endpoints, not an
assertion of an exact physical offset or of an impossible intended connection:
linework, sheet printing, scan distortion and alignment may differ. The dark
track also has different edge proximity; dash pattern and a line at a vertex
make assigning a crossing more uncertain than reading its presence in a cell.

## Conditional geometry and admission boundary

Under the existing **33-column candidate**, C3633 east and D3601 west are
paired sides; C3534's position is the candidate overlap for D3501. Those are
hypotheses or previously bounded policy, not facts established by the two
crops. The original §4.1 wording conflict and incomplete edge/overlap ownership
remain open. Similar blue, black and slope motifs support a common intended
corridor, but their noncoincident side locations prevent claiming a precisely
registered rail line. No inference that the other feature must be present on a
missing side, no complete road/track classification, and no rail-use permission
follows from these images.

The [existing map pilot](2026-11-12-graziani-map-identity-pilot.md#independent-cd-sheet-edge-check)
records C3633–D3601 as an unfinished railway feature in a bounded source-turn
contract, not a usable railway. This review does not revoke or expand that
policy; it supplies a specific registration discrepancy for its reviewer.
Errata §8.71's Axis-use clarification does not convert an unfinished line into
an operational route. Track effects and interactions with slopes require their
own complete, applicable rules and edge records.

## Review vectors

- Preserve the two local source observations, their different side positions,
  and the unfinished classification; do not assert exact geometric coincidence
  or automatically promote to finished rail.
- Under a version explicitly accepting C3633–D3601 as a candidate adjacency,
  check reciprocal unfinished-rail feature records while keeping rail movement
  unavailable absent independently accepted rules and route state. A missing
  feature on one side must trigger review rather than automatic union.
- C3632–C3633 and D3601–D3602 are independently visible local rail-side
  observations; do not treat those as evidence that every neighboring side is
  rail, traversable or covered. Track dash gaps and adjacent slopes remain
  explicit, separate evidence.
- Verify that C3534's printed existence is retained in source provenance even
  when canonical policy omits overlap IDs. Unaccepted aliases, missing edges,
  and features outside reviewed coverage remain unknown rather than clear or
  impassable.

No runtime data, readiness flags or tests changed. No gameplay tests were run.

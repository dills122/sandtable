# Graziani playable map: bounded next work batch

**Date:** November 17, 2026. **Status:** research and certification plan; no
new map facts or release admission. The [map/control spec](../../specs/graziani-source-control-map.md#shortest-route-to-a-source-backed-playable-map)
leaves the scenario's playable extent and counts open. Start with the finite
areas below to make evidence and closure measurable. Neither area is a legal
scenario boundary; no artificial wall, empty exterior or absent enemy follows.

## Evidence and scope

The [pinned-map inventory](2026-11-17-graziani-map-coverage-inventory.md)
counts 7,073 candidate cells and 20,365 same-sheet candidate pairs, including
16,139 without an explicit feature record. These are data counts, not a
source-certified map or a complete set of legal neighbors. One-ended records
do not prove physical boundaries. The [coverage review](2026-11-17-graziani-map-coverage-review.md)
also identifies a Malta column-00 enumeration discrepancy; its scenario impact
is untested. Printed coordinate extrema are not rectangular map boundaries.
Original five-sheet scan: [map](https://spigames.net/PDFv10/CNA_Maps.pdf),
SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Secure a durable verified copy and settle rights separately; the temporary
copy used in research is not a durable input.

### Batch A: C0716 setup disk and incoming-neighbor ring

[Scenario §60.31 and source review](2026-11-17-graziani-c0716-radius-source.md)
support a Saharan Detachment placement within three hexes of C0716. Under the
explicit proposed inclusive distance interpretation, inventory the 37-cell
disk C04:14–17, C05:14–18, C06:13–18, C07:13–19, C08:13–18,
C09:14–18, C10:14–17. This is geometric candidate scope, not a legal placement
list. Add the distance-four ring for a finite 61-cell review envelope:

| Row | Inclusive columns in envelope |
| --- | --- |
| C03 | 14–18 |
| C04 | 13–18 |
| C05 | 13–19 |
| C06 | 12–19 |
| C07 | 12–20 |
| C08 | 12–19 |
| C09 | 13–19 |
| C10 | 13–18 |
| C11 | 14–18 |

Ring is a geometric one-neighbor halo around the proposed disk, useful for
checking possible incoming ZOC and edge completeness. It does not establish
where forces may originate, stop travel beyond it, or close a route. Record
all six side dispositions for every disk cell, including sides to the halo;
inventory halo interiors and any further sides needed by actual rules and
reachable forces. [RUL-GRA-23](2026-11-12-graziani-ruling-decisions.md#rul-gra-23-saharan-detachment-setup-radius-bounded-planning)
selects inclusion of origin and outer ring for planning; admission and source
alternatives remain open. The [37-cell interior audit](2026-11-17-graziani-radius-full-data-audit.md)
already compares pinned records with the scan but does not certify sides or
whole-hex assignment.

### Batch B: Sheferzen initial-contact envelope

[Scenario and map evidence](2026-11-17-graziani-map-sheferzen-initial-contact.md)
places opposing fixed forces at adjacent C3419 and C3320. Review their union
and one-neighbor halo: C3219–C3220, C3319–C3321, C3418–C3420,
C3519–C3520, ten candidate cells total. The shared side is partly obscured;
it is not certified clear. This finite envelope targets initial-contact and
incoming-ZOC evidence, not all possible legal opening moves. Check actual
force composition, qualifications, direction, entry and phase separately;
geometry alone proves neither ZOC nor compulsory combat.

## Priority queue and deliverables

1. **Discrepancy resolution, disk:** revisit C0718 oasis and C0818 marsh first;
   then C0817 and C0918 recorded oasis, and C0613–C0618 plus C0814/C0914/C1014
   boundary-art assignment. Seek a clearer source and applicable assignment
   rule, record positive marks, occlusion and disagreements. Do not change
   pinned data from an unresolved visual impression.
2. **Contact edge and halo:** reread C3419–C3320 through its full length and
   all five other sides of each origin. Enumerate every halo cell and side
   needed to evaluate both directional projections; resolve or retain unknown
   labels, terrain and potentially blocking features. Review any outside
   placement or force able to affect this area instead of assuming none.
3. **Route and junction:** use the documented D3615–D3715–D3714 opening
   road as a narrow seed. Review D3615/D3616 shoreline and junction,
   D3516/D3517 missing rail candidate and D3615 eastern junction overprint
   against [Fourth Indian evidence](2026-11-17-graziani-fourth-indian-edges-next.md)
   and its linked follow-ups. Trace the actual source-required route endpoints,
   branches, each intervening side and relevant occupancy/ZOC; local road or
   railway marks do not establish a complete route or railway service.
4. **Seams and broad placements:** prioritize whichever A/B, B/C, C/D or D/E
   joins an actual required route or placement needs; review both sides,
   registration, identity, crossing feature and rules before admission.
   [A/B El Abiar](2026-11-17-graziani-map-ab-el-abiar-identity.md) is a lead,
   not an alias. C/D has only the three bounded records described in the
   [transcription spec](../../specs/graziani-historical-map-transcription.md);
   unfinished railway is not an admitted rail route. Resolve or block the 11
   distance-chart disagreements, §4.1 wording, and source-coordinate conflicts
   relevant to each claim. Preserve scenario B4827 and map A4827 separately.
5. **Scenario-wide extent inventory:** enumerate fixed and selected setup,
   radius and anywhere placements, arrivals, objectives, facilities, supply
   origins and rule-reachable routes. The [fixed-hex index](2026-11-17-graziani-fixed-hex-review-coverage.md)
   has paired six-side readings for 33 explicit coordinates and additional
   city cells, but paired reading is not side certification; it excludes broad
   placements. Classify each source-defined domain and its boundary before
   proposing a finite scenario-playable area and counts.

Each batch produces a coordinate and side ledger with source hash, page/crop
locator, printed and canonical IDs, observation, reviewer, disagreement,
rules relevance, disposition and follow-up. Review obscured or conflicting
facts separately; informed checks of one scan are not independent editions.
Include physical boundary evidence, overlapping labels, all feature types and
directional higher sides. A second source check should challenge the ledger,
not merely repeat the candidate dataset.

## Coverage and closure contract

- Separate cell terrain, settlement and coast observations from side features.
  Recorded `clear` terrain is a cell assertion; an absent feature record on a
  known same-sheet pair means only no feature recorded by that transcription.
  Distinguish reviewed feature-free, reviewed feature-bearing, unknown or
  obscured, conflict, verified physical boundary, missing cell and unresolved
  seam. Preserve existing limited-consumer behavior; new coverage must not
  promote an empty feature list to independently verified clear.
- For every claimed route, enumerate endpoints, contiguous reciprocal edges,
  relevant features and direction, availability, terrain, applicable rule,
  occupancy and ZOC. Unknown segment or possible bypass leaves route closure
  unknown; distance or a matching local mark cannot close it.
- For a proposed playable boundary, identify all legal exits and entries,
  off-map areas, selectable placements and potential incoming units and ZOCs,
  including directions and enterability. Extend scope or retain a blocker if
  any could affect play. A one-ring halo supports local contact checks only;
  it does not prove movement or supply closure.
- Before runtime admission, version source and coverage identity, provenance,
  seam policy and any accepted correction; preserve old replay identities.
  Require unique mappings, reciprocal neighbors, six-side accounting, feature
  and direction validation, fail-closed unknown/conflict behavior, boundary
  ZOC, route closure and changed-identity replay tests. Check Malta separately.

**Certification milestones:** A reviewed local ledger can certify only its
enumerated facts and consumers. A bounded playable area additionally needs
closed legal movement, incoming ZOC, placements and required routes with all
relevant rules and state. Full-map certification requires a source-backed
inventory of all relevant sheets, boundaries, seams, corrections and features,
not extrapolation from these batches or third-party dataset counts. Scenario
readiness also depends on non-map rules, forces and lifecycle gates. No runtime,
data, readiness flags or gameplay tests changed in this planning batch.

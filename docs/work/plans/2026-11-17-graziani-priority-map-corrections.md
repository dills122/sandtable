# Priority map corrections: bounded coding handoff

November 17, 2026. These are **candidates**, not admitted runtime changes.
Recheck exact HEAD, map identity, records and source discrepancy before work.
This pass compared the cited source readings and reviews with the pinned data,
verified the hashes below, and visually inspected their existing crops. The
reviews are informed readings of the same scan, not independent editions.

| Unordered side | Pinned v1 | Conditional correction in a new map version | Evidence and limits |
| --- | --- | --- | --- |
| `B1705|B1706` | `slope`, `up: null` | Set `up: "B1706"`; preserve `slope`. | Splash lies in B1705; Land §8.35 puts splash on down side. [Reading](2026-11-17-graziani-slope-directions-next.md), [review](2026-11-17-graziani-slope-directions-review.md). |
| `B1706|B1805` | `slope`, `up: null` | Set `up: "B1706"`; preserve `slope`. | Splash lies in B1805; same rule and reviews. Neither slope reading certifies nearby track or movement legality. |
| `D3516|D3517` | Neither orientation has a record. | Represent positive `railroad` crossing, **only if representation preserves unknown membership of road and other features**. | Cross-tied centerline crosses side interior. Road, coastline and grid overlap at northern vertex. [Reading](2026-11-17-graziani-d3516-d3517-rail-next.md), [review](2026-11-17-graziani-d3516-d3517-rail-review.md). Missing record means unrepresented, not verified clear. |
| `D3231|D3232` | `railroad`, `up: null` | Add `road`, retaining `railroad`; do not infer additional features. | Paired dark road strokes and blue rail cross the side below its upper vertex; numeral partly overlaps road. [Reading](2026-11-17-graziani-map-el-daba-east.md), [review](2026-11-17-graziani-map-el-daba-east-review.md). |

Do not remove the pinned railway on `D3231|D3331` on this evidence: a
cross-tie and vertex overprint leave a separate centerline crossing unproved,
not disproved under every vertex policy. Likewise do not assign a road to
`D3516|D3517` from its crowded vertex. No observation certifies other features,
passability, route continuity, construction, ownership, occupancy, ZOC, weather,
legal timing or movement. Check these separately before route use.

## Provenance

- [Original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), Map B p. 2,
  Map D p. 4, key p. 1: SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  p. 14 §8.35: SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- [Pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json):
  SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
- Inspected temporary crops: B slopes SHA-256
  `0d052a9899c50020a1c4d8cee54ffb8dce0c905849d6e1822a91e3ce951f1e10`;
  D3516 SHA-256 `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`;
  El Daba 600-dpi review SHA-256
  `79aeaff39cff02536977e1475772721f8bce925d6d85ad8afab2973ae497af45`.
  Commands and coordinates are in linked readings; renderer output may vary.
  Hashes prove identity, not rights. Keep scans outside Git pending rights.

## Admission and validation

Admit discrepancy, source provenance, rights, coordinate and edge semantics
before coding. Preserve v1 bytes and identity for existing replays; issue a new
versioned map/content identity for accepted changes. Determine whether current
feature encoding can express a known positive with other features unknown. If
not, defer the D3516 record until a versioned coverage representation exists;
do not equate a partial list with verified absence. Review consumer effects,
authority and privacy boundaries and replay compatibility before admission.

Focused deterministic vectors: lookup each corrected side in both orientations;
test B1705 → B1706 and B1805 → B1706 as up-slope and reverse as down-slope;
verify v1 still returns unknown direction. Verify D3516 railway positive while
road remains unknown, and D3231/D3232 retains rail and gains road, without
certifying other features or routes. Include unknown-versus-clear and old/new
identity replay checks; test affected map and route consumers and boundary
behavior. Run applicable Core and consumer suites, report exact commands and
unrun coverage. No code, map data, policy or readiness flag changed here; no
gameplay tests ran.

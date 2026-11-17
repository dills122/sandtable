# Graziani pinned-map inventory: independent arithmetic review

**Disposition:** the inventory's file-level counts and stated provenance
limits reproduce. They do **not** certify all candidate pairs are enumerated by
the runtime neighbor method. Malta's column `00` makes that distinction
observable, as detailed below. This is an independent arithmetic check of the
same working-tree artifacts and candidate parity model, not an independent
physical-map transcription, coverage certification or gameplay test.

## Finding: candidate pairs differ from runtime enumeration

The inventory explicitly counts unordered same-sheet pairs derived from
existing JSON IDs; it does not claim to run compiled code. That arithmetic is
correct. However, [Cna1979HistoricalMap](../../../src/Cna.Core/Content/Cna1979HistoricalMap.cs)
`NeighborsOf` rejects any **candidate** with column below 1, while the pinned
Malta data contains nine `Mxx00` hexes. Of Malta's 158 mathematical candidate
pairs, 25 touch column 00:

- 17 join column 00 to a positive column. `NeighborsOf` can enumerate them
  from the column-00 endpoint but not from the positive-column endpoint.
- Eight join two column-00 cells. Neither endpoint can enumerate the other.
- The remaining 133 have positive columns at both endpoints and are
  enumerable in both directions. Thus enumeration finds 150 distinct pairs
  in at least one direction, with 17 asymmetric pairs and eight omitted from
  both directions. These are static consequences of the checked source and
  JSON, not an executed runtime test.

`EdgeBetween` uses a separate parity predicate and accepts a known adjacent
column-00 pair, returning an empty feature list for a missing record. For
example, `M0500` and `M0501` satisfy that predicate, while
`NeighborsOf("M0501")` excludes `M0500`. Therefore no consumer should
substitute the inventory's 158 candidates for an observed reciprocal neighbor
enumeration. The inventory should add this caveat if its discussion is used to
scope implementation; its existing “not runtime execution” qualification
already prevents treating the raw 158 as a runtime test. Whether any Graziani
scenario action can reach Malta or depends on these methods was not examined;
this finding alone does not establish a gameplay defect.

## Independent count comparison

Recomputed with an unordered-pair comparison over existing IDs rather than the
inventory's neighbor-generation loop. For each pair, same-row adjacent means
column difference one; consecutive rows match equal columns or the parity
diagonal in the checked `SameSheetAdjacent` source. Results match every row:

| Sheet | Hexes | Candidate pairs | Internal feature records | Unrecorded candidate pairs | One-ended records |
| --- | ---: | ---: | ---: | ---: | ---: |
| A | 994 | 2,800 | 565 | 2,235 | 28 |
| B | 1,959 | 5,696 | 1,152 | 4,544 | 42 |
| C | 1,520 | 4,395 | 880 | 3,515 | 26 |
| D | 1,240 | 3,575 | 718 | 2,857 | 26 |
| E | 1,297 | 3,741 | 911 | 2,830 | 17 |
| M | 63 | 158 | 0 | 158 | 0 |
| Total | 7,073 | 20,365 | 4,226 | 16,139 | 139 |

No two-ended feature pair lies outside the chosen same-sheet candidate set;
no two-ended record crosses sheets. Confirmed 4,365 side records, all with at
least one feature, 288 with multiple features, no duplicate hex IDs or side
keys, no missing named endpoint, and 25 one-ended records whose `up` names a
different sheet. Those tests do not certify whether cross-sheet coordinates
or directional hints are physically correct. The two internal slope records
with `up: null` are `B1705|B1706` and `B1706|B1805`.

Independently reproduced all reported terrain, settlement, coastal and feature
occurrence totals. In particular the 150 non-null settlements are 130 village,
11 major-city, six oasis and three bir; 290 records are coastal. Explicitly
checked `C3920|C3921` absent, `C3921|C3922` escarpment with `up: C3921`, and
both listed Fourth Indian route pairs carry `road` and `railroad`. These data
checks do not inspect or approve the separately cited original-map images.

## Provenance and interpretation

Both JSON roots contain `sources` and their respective arrays, each naming
`vassal:CNAv2.1.0`; record shapes and fields match the inventory. The source
[transcription specification](../../specs/graziani-historical-map-transcription.md)
describes the pinned Living Rules revision and explicitly confines the absence
of a feature record to no feature *recorded* on a known adjacent same-sheet
pair. Its narrow route, contact and C/D seam policies are separate from general
map certification. The checked implementation likewise returns unknown for
missing/nonadjacent pairs and an empty recorded-feature list for a known
same-sheet pair with no entry. A hash pins bytes, not physical-source coverage,
permission, absence of omitted political markings or a clear legal edge.
One-ended records are not proof of external boundary or seam traversal.

SHA-256 independently reproduced: `hexes.v1.json`
`ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`, and
`hexsides.v1.json`
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
Inputs are the working-tree files; their hashes also match the checked loader's
constants. Compiled-resource loading, full geographic coverage, legal movement,
review of upstream rights and every original sheet were not tested. No data,
code or readiness state changed; no gameplay tests ran.

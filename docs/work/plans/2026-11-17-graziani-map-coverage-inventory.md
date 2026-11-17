# Graziani pinned map: quantitative coverage inventory

**Result:** Embedded v1 records contain **7,073 hexes**, including 63 Malta
records, and **4,365 feature-bearing hexside records**. They supply broad
candidate geometry and terrain, not a certified playable map. For the 20,365
same-sheet neighboring pairs derivable from the embedded coordinates, 4,226
have an explicit feature record and 16,139 do not. The latter count is
**not** a count of verified clear historical edges: the documented
transcription convention treats an absent record on a known adjacent
same-sheet pair as no *recorded* feature. Independent completeness and
coverage provenance remain absent. Cross-sheet pairs, unavailable hexes and
physical boundaries need separate treatment.

## Inputs and reproducible arithmetic

Files examined from the current working tree on November 17, 2026:

| Input | SHA-256 |
| --- | --- |
| `src/Cna.Core/Content/HistoricalMap/hexes.v1.json` | `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568` |
| `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` | `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` |

[Historical map transcription](../../specs/graziani-historical-map-transcription.md)
documents the pinned Living Rules revision and resource semantics;
[map/control boundary](../../specs/graziani-source-control-map.md) states the
separate coverage and admission work. Counts below inspect file contents, not
compiled-resource loading, runtime execution, original-sheet completeness,
legal movement, or the status of every independently reviewed source excerpt.

The following Python script reconstructs each unique **same-sheet** pair
between existing IDs. `RRCC` uses row parity: even rows have diagonal neighbors
in `CC` and `CC+1`; odd rows in `CC` and `CC-1`. This is the documented
candidate geometry, not a source-certified seam transform. Missing IDs and
cross-sheet candidates are intentionally excluded from the denominator.

An [independent arithmetic and consumer review](2026-11-17-graziani-map-coverage-review.md)
reproduces these counts but finds that the runtime neighbor enumerator excludes
column `00` as a candidate. In Malta this makes 17 of the 158 mathematical
pairs asymmetric and eight absent in both directions, while the separate edge
predicate recognizes known adjacent pairs. These are static source findings,
not an executed gameplay test or evidence of Graziani reachability.

```sh
python3 - <<'PY'
import collections, json
base = 'src/Cna.Core/Content/HistoricalMap/'
h = json.load(open(base + 'hexes.v1.json'))['hexes']
e = json.load(open(base + 'hexsides.v1.json'))['hexsides']
for sheet in 'ABCDEM':
    ids = {x['id'] for x in h if x['sheet'] == sheet}
    pairs = set()
    for cell in ids:
        row, col = int(cell[1:3]), int(cell[3:5])
        delta = 1 if row % 2 == 0 else -1
        neighbors = [(row, col - 1), (row, col + 1),
                     (row - 1, col), (row + 1, col),
                     (row - 1, col + delta), (row + 1, col + delta)]
        for nr, nc in neighbors:
            other = f'{sheet}{nr:02}{nc:02}'
            if other in ids:
                pairs.add(tuple(sorted((cell, other))))
    records = [x for x in e if x['a'][0] == sheet and x['b'] is not None]
    featured = {tuple(sorted((x['a'], x['b']))) for x in records}
    boundary = sum(x['a'][0] == sheet and x['b'] is None for x in e)
    print(sheet, len(ids), len(pairs), len(records),
          len(pairs - featured), len(featured - pairs), boundary)
print('feature occurrences:', collections.Counter(
    f for x in e for f in x['features']))
PY
```

## Counts and geographic scope

| Sheet | Hex records | Same-sheet candidate pairs | Explicit internal feature records | Candidate pairs without feature record | Feature records outside candidate pairs | One-ended side records |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 994 | 2,800 | 565 | 2,235 | 0 | 28 |
| B | 1,959 | 5,696 | 1,152 | 4,544 | 0 | 42 |
| C | 1,520 | 4,395 | 880 | 3,515 | 0 | 26 |
| D | 1,240 | 3,575 | 718 | 2,857 | 0 | 26 |
| E | 1,297 | 3,741 | 911 | 2,830 | 0 | 17 |
| M (Malta) | 63 | 158 | 0 | 158 | 0 | 0 |
| **Total** | **7,073** | **20,365** | **4,226** | **16,139** | **0** | **139** |

Every JSON side has at least one feature; 288 have multiple features. Of 4,365
records, 4,226 have two named endpoints and 139 have a null `b` and a side
direction. No two-ended record crosses sheets. All named `a`/non-null `b`
endpoints exist in the hex file; no duplicate hex IDs or side keys were found.
These are syntactic and geometry checks, not reciprocal source review. A
one-ended record documents a side feature at a recorded cell; it does **not**
certify the missing opposite cell or establish a physical game-map boundary.
Twenty-five one-ended records have `up` pointing at a different, cross-sheet
coordinate. Such directional hints do not themselves admit that coordinate as
an alias or a legal seam crossing.

Among hexes, 4,192 record clear terrain, 677 desert, 655 rough, 463 gravel,
346 sea, 325 delta, 266 salt marsh, 107 mountain, 25 swamp, 11 major city and
6 heavy vegetation. `coastal` is true on 290 records. There are 150 non-null
settlements: 130 village, 11 major city, 6 oasis, 3 bir. Values are transcription
assertions; e.g. `terrain: clear` is a recorded **cell terrain**, distinct from
an edge lacking a recorded feature. Malta's zero feature records do not prove
all 158 candidate edges source-clear.

Feature occurrences across all explicit records (multiple features can share
one record): 1,808 track, 744 slope, 703 wadi, 355 road, 216 escarpment,
205 minor river, 201 ridge, 183 railroad, 86 major river, 85 unfinished road,
55 unfinished railroad and 21 coast. Two internal slope records have unknown
higher side: `B1705|B1706` and `B1706|B1805`. A null `up` on a road or track
is not the same question as an unknown slope direction.
Subsequent [original-map review](2026-11-17-graziani-slope-directions-next.md)
and an independent second reading identify `B1706` as the high side of both;
the pinned JSON still contains nulls pending a versioned content correction.

Recorded ranges are A rows 01–56/columns 01–33, B 01–61/01–33, C 01–51/01–33,
D 01–42/01–33, E 01–44/01–34, Malta 05–13/00–06. These are observed minima
and maxima, **not** full rectangular coverage or a defined playable extent.
A–D omit printed 34-edge copies under this transcription's convention, with
clipped edges and overlap identities requiring separate source review. Sea,
coast, omitted coordinates, off-map boxes and incoming ZOC must not be assumed
complete merely because a row or column lies between the extrema.

## Provenance, absence and admitted scope

- Each JSON root contains only `sources` and its array; the shared source is
  `vassal:CNAv2.1.0`. Every hex record has exactly `id`, `sheet`, `terrain`,
  `settlement`, `coastal`; every side record has `key`, `a`, `b`, `side`,
  `features`, `up`. No per-cell or per-side scan locator, reviewer, coverage
  assertion, confidence, verified-clear or verified-boundary field is present
  in these records. The pinned revision and hash identify imported content,
  not verification of every original-sheet mark or permission to redistribute
  a scan.
- `C3920|C3921` has no explicit side record; it is an existing same-sheet
  candidate pair with no feature **recorded by this dataset**. The original
  scan's blue checkered boundary mark and its geographic interpretation are
  recorded in [boundary review](2026-11-17-graziani-border-mark-next.md).
  Therefore dataset absence does not mean every visible marking was
  source-transcribed or that scenario-specific geographic effects vanish.
  `C3921|C3922` explicitly records escarpment, `up: C3921`; this agrees with
  the bounded [physical scan check](2026-11-17-graziani-halfaya-edges-next.md),
  but does not certify its neighbors or a general control contract.
- The source map spec documents narrow actual consumers: the D3615–D3715–
  D3714 Fourth Indian route, five Cairo arrival hexes, limited same-sheet
  contact geometry, and three versioned C/D seam candidates. Both D route
  sides have explicit `road` and `railroad` records. These cases do not turn
  all pinned cells or terrain into accepted authority. The C/D policy identifies
  only C3533–D3501 and C4033–D3901 tracks and C3633–D3601 unfinished railway;
  two one-ended records are not themselves a two-ended source-certified seam.
  The independent [rail check](2026-11-17-graziani-cd-rail-followup.md)
  records an alignment-position discrepancy needing review. Rail route use
  remains unavailable under the documented bounded policy.
- Known same-sheet adjacency, missing feature record, missing coordinate,
  unknown side classification and an admitted feature-free side are distinct
  states. The current record shape has no explicit verified-clear/unknown
  discriminator. Preserve the existing loader convention for its existing
  consumers without describing its empty feature result as a newly verified
  source observation.

## Remaining work

Establish a reviewed scenario-reachable extent and boundary policy; source-check
hex and side features, including unrepresented political marks, coast and
transport; distinguish reviewed feature-free sides from unknown ones; reconcile
source §4.1, the 11 distance mismatches and each required seam; resolve the two
unknown slope directions; and determine incoming units/ZOC across any proposed
coverage boundary. Add versioned provenance/coverage semantics, legal
traversal and grouped-control integration before asserting playable map or
control completeness. Include duplicate/reciprocal topology, missing-edge,
seam, wrong-source and replay tests. Third-party apparent completeness or a
matching data hash cannot replace these gates.

No runtime, fixture, readiness flag or original data was edited. No gameplay
tests were run. Counts are reproducible file analysis, not an acceptance test.

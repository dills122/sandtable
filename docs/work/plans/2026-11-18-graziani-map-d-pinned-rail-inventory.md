# Map D pinned rail candidate inventory

**Date:** November 18, 2026. **Scope:** literal pinned-data inventory for Land §8.78 planning; no new source inspection. This is not a complete printed-map survey, a certified network, or proof of construction, damage, occupancy, geographic west, witness status, or legal rail use. Missing rows do not prove absent features. See [bounded eastern coverage](2026-11-17-graziani-rail-nonlocal-coverage.md) for witness and geographic qualifications.

## Selection and counts

Read [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Select rows containing `railroad` or `unfinished-railroad` with `a` or `b` matching `D` plus four digits. Count each row once and deduplicate endpoints. File identifies source as `vassal:CNAv2.1.0`; data labels are not independent source certification.

| Measure | Count |
| --- | ---: |
| Selected edge records | 42 |
| `railroad` records | 25 |
| `unfinished-railroad` records | 17 |
| Internal D–D records | 40 |
| Open records (`b: null`) | 2 |
| Explicit D–other-sheet pairs | 0 |
| Distinct D endpoints, all selected records | 41 |
| Distinct D endpoints incident to `railroad` | 25 |
| Distinct D endpoints incident to `unfinished-railroad` | 17 |

`D3615` belongs to both endpoint sets. Endpoints are candidates inferred from recorded incident features, not individually surveyed hexes. Open-side labels `E` and `W` are directions, not Map E or Map W endpoints. `D3133|E` and `D3601|W` have no `b`; no cross-sheet pair or alias follows. Counts do not settle inclusion in any §8.78 evaluation.

## Literal selected rows

`a` and `b` follow each key's order. Features retain file order. All `side` values are null except open rows; `up` appears when non-null. Labels are literal, not state or source adjudications.

| Key | Features | Open side | Up |
| --- | --- | --- | --- |
| `D3133|D3232` | `road`, `railroad` | — | — |
| `D3133|E` | `road`, `railroad` | `E` | — |
| `D3230|D3231` | `railroad` | — | — |
| `D3230|D3330` | `railroad` | — | — |
| `D3231|D3232` | `railroad` | — | — |
| `D3231|D3331` | `road`, `railroad` | — | — |
| `D3322|D3323` | `railroad` | — | — |
| `D3322|D3421` | `slope`, `railroad` | — | `D3421` |
| `D3323|D3324` | `road`, `railroad` | — | — |
| `D3324|D3325` | `road`, `railroad` | — | — |
| `D3325|D3326` | `road`, `railroad` | — | — |
| `D3326|D3327` | `road`, `railroad` | — | — |
| `D3327|D3328` | `road`, `railroad` | — | — |
| `D3328|D3329` | `road`, `railroad` | — | — |
| `D3329|D3330` | `road`, `railroad` | — | — |
| `D3419|D3420` | `railroad` | — | — |
| `D3419|D3519` | `railroad`, `road` | — | — |
| `D3420|D3421` | `railroad`, `road` | — | — |
| `D3421|D3422` | `railroad`, `road`, `slope` | — | `D3421` |
| `D3514|D3515` | `unfinished-railroad` | — | — |
| `D3514|D3613` | `unfinished-railroad` | — | — |
| `D3515|D3615` | `slope`, `unfinished-railroad` | — | `D3515` |
| `D3517|D3518` | `railroad`, `road` | — | — |
| `D3517|D3616` | `railroad`, `road` | — | — |
| `D3518|D3519` | `railroad`, `road` | — | — |
| `D3601|D3602` | `track`, `unfinished-railroad` | — | — |
| `D3601|W` | `unfinished-railroad` | `W` | — |
| `D3602|D3603` | `track`, `unfinished-railroad` | — | — |
| `D3603|D3604` | `unfinished-railroad` | — | — |
| `D3604|D3605` | `unfinished-railroad` | — | — |
| `D3605|D3606` | `unfinished-railroad` | — | — |
| `D3606|D3607` | `unfinished-railroad` | — | — |
| `D3607|D3608` | `unfinished-railroad` | — | — |
| `D3608|D3609` | `unfinished-railroad` | — | — |
| `D3609|D3710` | `unfinished-railroad` | — | — |
| `D3611|D3612` | `road`, `unfinished-railroad` | — | — |
| `D3611|D3711` | `road`, `unfinished-railroad` | — | — |
| `D3612|D3613` | `road`, `unfinished-railroad` | — | — |
| `D3615|D3616` | `railroad`, `road` | — | — |
| `D3615|D3715` | `road`, `railroad` | — | — |
| `D3710|D3711` | `road`, `unfinished-railroad` | — | — |
| `D3714|D3715` | `road`, `railroad` | — | — |

## Comparison with existing source readings

- [Matruh extension](2026-11-17-graziani-matruh-east-extension.md) reports source candidates `D3615–D3516` and `D3516–D3517` absent from this rail selection; the former is pinned as track only and the latter has no record. It retains `D3615–D3616` and `D3616–D3517` as uncertain alternatives. [Junction review](2026-11-17-graziani-d3615-east-junction-review.md) finds pinned `D3615–D3616` rail placement questionable, not disproved. `D3517–D3518` is near a vertex.
- [Same-scan review](2026-11-17-graziani-matruh-east-rail-batch-review.md) supports six recorded rail crossings from `D3518–D3519` through `D3322–D3323`, with crowded vertex/overprint qualifications. [Fuka review](2026-11-17-graziani-fuka-east-rail-batch-review.md) supports successive `D3323–D3330` crossings. These do not certify every selected row or connect all pieces.
- [East continuation](2026-11-17-graziani-continuation-map-east.md) supports several recorded sides but does not establish rail at `D3231–D3331`; its visible `D3133–D3134` reading has no literal pair and relation to open `D3133|E` remains unverified. [C/D seam review](2026-11-17-graziani-seam-cd-independent.md) describes nearby unfinished railway motifs but does not certify exact side identity; do not turn `D3601|W` into a cross-sheet connection.
- `D3421–D3422` and other rows outside these bounded comparisons remain data candidates; presence here is not a source review. Unfinished ink remains distinct from constructed railroad; [rules analysis](2026-11-17-graziani-continuation-rules.md) notes §8.47's instruction to ignore unconstructed railroad. Construction and independently qualifying overlapping track need separate evidence.

## Reproduce

Run from repository root with Python 3. Emits hash, selection, endpoint sets, classifications and full rows; no seam aliases or geographic ordering are inferred.

```sh
python3 - <<'PY'
import collections
import hashlib
import json
import re
from pathlib import Path

path = Path('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json')
raw = path.read_bytes()
rows = json.loads(raw)['hexsides']
is_d = lambda value: isinstance(value, str) and re.fullmatch(r'D\d{4}', value) is not None
rows = [row for row in rows
        if {'railroad', 'unfinished-railroad'} & set(row['features'])
        and any(is_d(row[field]) for field in ('a', 'b'))]
print('sha256', hashlib.sha256(raw).hexdigest())
print('selected', len(rows))
for feature in ('railroad', 'unfinished-railroad'):
    selected = [row for row in rows if feature in row['features']]
    endpoints = sorted({value for row in selected for value in (row['a'], row['b']) if is_d(value)})
    print(feature, 'rows', len(selected), 'D endpoints', len(endpoints), ','.join(endpoints))
endpoints = sorted({value for row in rows for value in (row['a'], row['b']) if is_d(value)})
print('all D endpoints', len(endpoints), ','.join(endpoints))
categories = collections.Counter('open' if row['b'] is None else
                                 'D-D' if is_d(row['a']) and is_d(row['b']) else
                                 'cross-sheet' for row in rows)
print('categories', dict(sorted(categories.items())))
for row in rows:
    print(json.dumps(row, separators=(',', ':'), ensure_ascii=False))
PY
```

Documentation only; no data or runtime changes, no gameplay tests. Source comparisons summarize existing notes rather than newly checking the scan.

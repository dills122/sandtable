# Graziani §8.78: pinned Map E rail candidate inventory

**Date:** November 18, 2026. **Disposition:** Reproducible inventory of pinned data, not certification of printed map, route, construction, occupancy, or an unblocked result. No map scan was inspected for this inventory. Existing local reviews supply the 41-candidate comparison set; their source, vertex and other qualifications still apply.

## Counts and comparison

Input: [`hexsides.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), source label `vassal:CNAv2.1.0`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98eb30d98e6c`. Select each record once if either endpoint starts with `E` and features include `railroad` or `unfinished-railroad`. Count distinct non-null E-prefixed endpoints, not side keys or directions.

| Pinned selection | Count |
| --- | ---: |
| Incident side records | 143 |
| Records with `railroad` | 143 |
| Records with `unfinished-railroad` | 0 |
| Distinct Map E endpoint IDs | 142 |
| E-to-E records | 142 |
| Open records | 1 |
| Endpoint IDs in existing reviewed 41-candidate set | 41 |
| Other pinned E endpoint IDs | 101 |

The open record is `E3101|W` (`a: E3101`, `b: null`, `side: W`); `W` is a side marker, not an endpoint ID. Other 142 records have null `side`; all 143 have null `up`. There are no selected cross-sheet endpoint records. These facts describe the pin only; they do not establish where the open side leads, sheet registration, or actual geometry.

The 41 are the 36 IDs from the [eastern witness crosswalk](2026-11-18-graziani-rail-eastern-witness-crosswalk.md) plus five from the [fork candidate followup](2026-11-18-graziani-rail-eastern-candidates-next.md). All occur as pinned endpoints. The 143 records partition by membership of their endpoints in that 41-ID set: 38 have both endpoints in it, eight have exactly one, and 97 have neither. This is **not** a side-review count: two reviewed endpoint IDs do not establish that their shared side was reviewed or certified; the open side has only one endpoint.

The other 101 pinned E IDs, as candidates for further evidence, are:

```text
E0127 E0227 E0328 E0428 E0529 E0628 E0729 E0828 E0929 E1029 E1130
E1827 E1927 E1928 E2025 E2026 E2124 E2125 E2223 E2226 E2323 E2326
E2418 E2419 E2421 E2422 E2425 E2428 E2429 E2520 E2521 E2523 E2526
E2529 E2530 E2531 E2623 E2624 E2625 E2628 E2631 E2632 E2633 E2723
E2725 E2728 E2732 E2823 E2824 E2827 E2828 E2829 E2830 E2831 E2905
E2923 E2924 E2927 E2930 E3023 E3024 E3025 E3026 E3027 E3030 E3118
E3123 E3128 E3130 E3217 E3221 E3222 E3228 E3230 E3318 E3320 E3321
E3328 E3329 E3330 E3331 E3417 E3418 E3419 E3420 E3421 E3426 E3427
E3431 E3432 E3433 E3517 E3522 E3523 E3526 E3613 E3615 E3623 E3624
E3714 E3715
```

## Interpretation limits

- Missing pin means **unknown**, not no rail. Zero selected unfinished records does not prove no unfinished rail exists on Map E, nor does `railroad` establish construction or destruction state.
- Pinned side pairs are candidate graph edges, not verified geographic adjacency, a continuous historical line, or a geographic east/west ordering. Clipped and vertex crossings and crowded junctions need source review; the linked 41 retain their specific qualifications.
- Open side and off-map extent remain unresolved; no other-sheet inventory or D/E seam equivalence follows from this E-only selection. Other Map D branches and sheets may matter under an independently justified geographic bound.
- Section 8.78 also requires applicable construction and destroyed-rail policy, fixed snapshot occupancy, and Axis combat classification. Neither omission from this inventory nor a disconnected-looking pin establishes absence or an unblocked result.

## Reproduce

Run from repository root. The script prints the selected records and their features, endpoint sets and partition counts, so output can be audited against the JSON without relying on ordering or an assumed graph traversal.

```sh
shasum -a 256 src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
python3 - <<'PY'
import json
from collections import Counter

path = 'src/Cna.Core/Content/HistoricalMap/hexsides.v1.json'
reviewed = set('''
E3101 E3001 E3002 E3003 E3004 E3005 E3006 E3007 E3108 E3109 E3209
E3210 E3211 E3312 E3412 E1828 E1729 E1829 E1930 E1931 E2031 E2132
E2133 E2134 E1629 E1530 E1429 E1330 E1229 E2029 E1929 E2129 E2229
E2329 E2128 E2127 E3413 E3414 E3415 E3516 E3513
'''.split())
records = json.load(open(path))['hexsides']
rail = {'railroad', 'unfinished-railroad'}
selected = [r for r in records
            if rail.intersection(r['features'])
            and any(isinstance(r[k], str) and r[k].startswith('E')
                    for k in ('a', 'b'))]
ids = {r[k] for r in selected for k in ('a', 'b')
       if isinstance(r[k], str) and r[k].startswith('E')}
print('selected records:', len(selected))
print('rail feature counts:', dict(Counter(f for r in selected
                                          for f in r['features'] if f in rail)))
print('E endpoints:', len(ids), 'reviewed:', len(ids & reviewed),
      'other:', len(ids - reviewed), 'reviewed missing:', sorted(reviewed - ids))
print('endpoint membership partition (0, 1, 2):',
      [sum(sum(r[k] in reviewed for k in ('a', 'b')) == n for r in selected)
       for n in range(3)])
print('other E IDs:', ' '.join(sorted(ids - reviewed)))
for r in selected:
    print(r['key'], 'a=', r['a'], 'b=', r['b'], 'side=', r['side'],
          'up=', r['up'], 'features=', ','.join(r['features']))
PY
```

Documentation-only inventory; no runtime or map-data changes and no gameplay tests.

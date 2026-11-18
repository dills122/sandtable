# Map E pinned rail: sweep completion reconciliation

**Date:** November 18, 2026. **Disposition:** Documentary reconciliation of selected pinned records, not a new scan reading or a complete map survey. All 46 fresh pairs in the [reviewed worklist](2026-11-18-graziani-map-e-rail-remaining-worklist-review.md) now have readings in sweeps D–F and their reviews. The 143 selected records partition as follows:

| Classification | Records | Basis |
| --- | ---: | --- |
| Source-positive | 122 | Prior 81, plus 13 in [D](2026-11-18-graziani-map-e-rail-sweep-d.md), 12 in [E](2026-11-18-graziani-map-e-rail-sweep-e.md), and 16 in [F](2026-11-18-graziani-map-e-rail-sweep-f.md); qualifications in source notes remain binding. |
| Unresolved named pair | 19 | Prior 15, plus one in D and three in E, listed below. |
| Apparent source/pin mismatch | 1 | `E1927–E1928`: D and its [review](2026-11-18-graziani-map-e-rail-sweep-d-review.md) report no visible crossing on this named side despite its railroad pin. |
| Open side | 1 | `E3101\|W`: destination and printed crossing unresolved; not a named pair. |
| **Total** | **143** | **142 distinct named pairs plus one open record.** |

The [E review](2026-11-18-graziani-map-e-rail-sweep-e-review.md) retains three unknowns; the [F review](2026-11-18-graziani-map-e-rail-sweep-f-review.md) supports all 16 local readings, including a provisional near-vertex reading and overprint qualifications. The reviews challenge the same scan with candidates and prior readings visible; they are not blind or independent-edition corroboration. The mismatch is a bounded scan observation requiring further adjudication, not a data correction. The separate source-positive, non-rail-pinned `E2927–E3027` discrepancy lies outside this selected pin partition.

## Unresolved named pairs

```text
E0929–E1029
E1130–E1229
E1927–E2026
E1929–E2029
E1930–E2029
E2124–E2223
E2422–E2523
E2428–E2429
E2529–E2628
E2829–E2830
E2829–E2930
E2905–E3004
E3023–E3024
E3210–E3211
E3320–E3419
E3320–E3420
E3513–E3613
E3613–E3714
E3714–E3715
```

The [worklist](2026-11-18-graziani-map-e-rail-remaining-worklist.md) locates the original 15; D and E locate the four additions. Unknown means insecure side attribution, not a positive crossing or proof of absence. Source-positive means a reported crossing of that exact side, subject to each reading's qualifications; it does not certify connected junctions, a complete route, construction or damage state, service, control, occupancy, or an unblocked legal outcome. No omission survey was performed: completing selected pins does not prove there are no missing pins or rail on unpinned sides, nor establish seams or off-map continuation.

## Reproduce set reconciliation

Run from repository root. This reuses the worklist's executable baseline and manually encodes D/E exceptions; it checks set membership and row identity, not visual truth. Source and review row labels were also compared manually. If tables or classifications change, revisit the exceptions and prior manual chains; a passing count cannot establish source completeness or observation meaning.

```sh
shasum -a 256 src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
python3 - <<'PYCODE'
import re
from pathlib import Path
work = Path('docs/work/plans/2026-11-18-graziani-map-e-rail-remaining-worklist.md').read_text()
exec(work.split("python3 - <<'PYCODE'\n")[1].split('\nPYCODE')[0])
assert len(sel) == 143 and len(actual) == 142
assert len(positive) == 81 and len(unknown) == 15
assert len(sel) - len(actual) == 1
fresh = actual - positive - unknown
seen = set()
for suffix in 'def':
    base = Path(f'docs/work/plans/2026-11-18-graziani-map-e-rail-sweep-{suffix}')
    def pairs(path):
        return {pair(*m) for m in re.findall(r'\| `?(E\d{4})[–-](E\d{4})`? \|', path.read_text())}
    rows = pairs(base.with_suffix('.md'))
    assert rows == pairs(Path(str(base) + '-review.md'))
    assert len(rows) == {'d': 15, 'e': 15, 'f': 16}[suffix]
    assert not rows & seen and rows <= fresh
    seen |= rows
    uncertain = {'d': ['E2829 E2830'], 'e': ['E2829 E2930', 'E3320 E3419', 'E3320 E3420'], 'f': []}[suffix]
    u = {pair(*s.split()) for s in uncertain}
    n = {pair('E1927', 'E1928')} if suffix == 'd' else set()
    assert u | n <= rows
    positive |= rows - u - n
    unknown |= u
    if suffix == 'd': mismatch = n
assert seen == fresh
assert not (positive & unknown or positive & mismatch or unknown & mismatch)
assert positive | unknown | mismatch == actual
print('positive', len(positive), 'unresolved', len(unknown), 'mismatch', len(mismatch), 'open', len(sel) - len(actual))
for p in sorted(unknown): print('–'.join(p))
PYCODE
```

Pin SHA-256: `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Reconciliation inherits the worklist review's uniqueness check and limits on manually encoded earlier chains. No new crop inspected, runtime or map-data changes, or gameplay tests.

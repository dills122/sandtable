# Map E pinned rail: unknown recheck reconciliation

**Date:** November 18, 2026. **Disposition:** Documentary set reconciliation, not a new scan reading or map-data correction. The [pinned sweep completion](2026-11-18-graziani-map-e-rail-pinned-sweep-completion.md) and [review](2026-11-18-graziani-map-e-rail-pinned-sweep-completion-review.md) partition 143 selected records as 122 source-positive, 19 unknown named pairs, one apparent source/pin mismatch and one open side. Rechecks [A](2026-11-18-graziani-map-e-rail-unknown-a.md), [B](2026-11-18-graziani-map-e-rail-unknown-b.md), and [C](2026-11-18-graziani-map-e-rail-unknown-c.md), with their respective [A review](2026-11-18-graziani-map-e-rail-unknown-a-review.md), [B review](2026-11-18-graziani-map-e-rail-unknown-b-review.md), and [C review](2026-11-18-graziani-map-e-rail-unknown-c-review.md), cover those 19 pairs exactly once. Seven are promoted to qualified source-positive readings, treated as provisional here; twelve remain unknown.

| Selected-record classification | Before | After | Change |
| --- | ---: | ---: | --- |
| Source-positive | 122 | 129 | Seven qualified promotions; prior qualifications remain binding. |
| Unknown named pair | 19 | 12 | Seven promoted, twelve retained. |
| Apparent source/pin mismatch | 1 | 1 | `E1927–E1928` remains a bounded observation, not a correction. |
| Open side | 1 | 1 | `E3101\|W`: destination and printed crossing unresolved. |
| **Total** | **143** | **143** | **142 distinct named pairs and one open record.** |

## Seven provisional promotions

```text
E0929–E1029
E1130–E1229
E1927–E2026
E1929–E2029
E2124–E2223
E2422–E2523
E3023–E3024
```

A calls its five crossings positive and qualified; B and C explicitly call theirs provisional. All seven retain the specific road, overprint, line-identity or near-vertex limits in their source notes and reviews. C's review describes the center of `E3023–E3024` as obscured and supports alignment, not proof of an uninterrupted printed stroke.

## Twelve still unknown

```text
E1930–E2029
E2428–E2429
E2529–E2628
E2829–E2830
E2829–E2930
E2905–E3004
E3210–E3211
E3320–E3419
E3320–E3420
E3513–E3613
E3613–E3714
E3714–E3715
```

Unknown means neither secure side attribution nor proof of absence. These rechecks and visual challenges used the same scan, with candidates and earlier readings visible; they are neither blind nor independent-edition corroboration. A local side crossing does not establish connected junctions, a complete route, service, construction or damage state, control, occupancy, or movement legality. No omission survey was performed: this selected-pin partition does not establish missing-pin completeness or rule out rail on unpinned sides. The separate unpinned `E2927–E3027` discrepancy remains outside it.

## Reproduce set reconciliation

Run from repository root. This executes the completion note's baseline script, including its manually encoded earlier chains and D/E exceptions, then checks each recheck and review row, status label, uniqueness and resulting partition. The seven expected promotions are explicitly encoded; status checks do not interpret evidence text or prove visual truth. Baseline limits and manual source-chain qualifications remain binding.

```sh
python3 - <<'PYCODE'
import contextlib, hashlib, io, re
from pathlib import Path
root = Path('docs/work/plans')
prefix = '2026-11-18-graziani-map-e-rail-'
completion = (root / (prefix + 'pinned-sweep-completion.md')).read_text()
script = completion.split("python3 - <<'PYCODE'\n")[1].split('\nPYCODE')[0]
with contextlib.redirect_stdout(io.StringIO()):
    exec(script)
pin = Path('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json')
assert hashlib.sha256(pin.read_bytes()).hexdigest() == '124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c'
assert (len(positive), len(unknown), len(mismatch), len(sel) - len(actual)) == (122, 19, 1, 1)
assert mismatch == {pair('E1927', 'E1928')}
assert [(r['a'], r['b'], r['side']) for r in sel if r['b'] is None] == [('E3101', None, 'W')]
listed = completion.split('## Unresolved named pairs')[1].split('```text\n')[1].split('\n```')[0]
assert {pair(*x.split('–')) for x in listed.splitlines()} == unknown
promoted = {pair(*s.split()) for s in [
    'E0929 E1029', 'E1130 E1229', 'E1927 E2026', 'E1929 E2029',
    'E2124 E2223', 'E2422 E2523', 'E3023 E3024']}
seen = set()
for suffix in 'abc':
    rows_by_doc = []
    for review in ('', '-review'):
        doc = (root / (prefix + f'unknown-{suffix}{review}.md')).read_text()
        rows = re.findall(r'^\| `(E\d{4})[–-](E\d{4})` \| ([^|]+) \|', doc, re.M)
        pairs = [pair(a, b) for a, b, _ in rows]
        assert len(pairs) == len(set(pairs)) == {'a': 6, 'b': 6, 'c': 7}[suffix]
        for (a, b, label) in rows:
            p = pair(a, b)
            label = label.lower()
            assert ('unknown' in label) == (p not in promoted)
            if p in promoted:
                assert 'positive' in label or 'support' in label
        rows_by_doc.append(set(pairs))
    assert rows_by_doc[0] == rows_by_doc[1]
    assert not seen & rows_by_doc[0]
    seen |= rows_by_doc[0]
assert seen == unknown and promoted <= unknown
remaining = unknown - promoted
positive |= promoted
assert not (positive & remaining or positive & mismatch or remaining & mismatch)
assert positive | remaining | mismatch == actual
assert (len(positive), len(remaining), len(mismatch), len(sel) - len(actual)) == (129, 12, 1, 1)
for label, items in [('PROMOTED', promoted), ('UNKNOWN', remaining), ('MISMATCH', mismatch)]:
    print(label, len(items))
    for p in sorted(items): print('–'.join(p))
print('OPEN 1\nE3101|W')
PYCODE
```

Documentation only; no new crop inspected, runtime or map-data changes, or gameplay tests.

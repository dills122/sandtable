# Map E pinned railroad: remaining source worklist

**Date:** November 18, 2026. **Disposition:** Planning inventory, not a new scan reading. Exact unordered pairs, not endpoint membership, control exclusion. Of 143 selected E-incident pinned records, 81 pairs have prior source-positive readings (including vertex-qualified positives), 15 pairs have explicitly uncertain source attribution, 46 pairs have no source crossing assessment in the cited reviews, and one is an open side. Categories describe those notes, not truth of the map. No new crop was inspected here.

## Next three bounded batches

The following partition is a deterministic lexicographic workload, not a claim of geographic order, route continuity, or a single-crop fit. Reviewers should choose crops from printed grid, split a batch if needed, and record each exact side as positive, negative, or uncertain with location and limitations. Candidate pin and prior notes are visible, so comparison is informed. The source locator for every fresh pair below is **Map E, PDF page 5; exact crop and crossing location not yet established**; terrain key is PDF page 1. [Original scan](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Pin SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Existing crop locators may help find areas but do not certify unassessed sides.

```text
BATCH D 15
E1827–E1828
E1827–E1927
E1927–E1928
E2127–E2226
E2418–E2419
E2419–E2520
E2421–E2521
E2520–E2521
E2526–E2625
E2624–E2625
E2624–E2725
E2725–E2824
E2728–E2828
E2828–E2829
E2829–E2830
BATCH E 15
E2829–E2930
E2930–E3030
E3030–E3130
E3118–E3217
E3123–E3222
E3130–E3230
E3217–E3318
E3221–E3222
E3221–E3321
E3230–E3331
E3318–E3417
E3320–E3321
E3320–E3419
E3320–E3420
E3328–E3329
BATCH F 16
E3328–E3427
E3329–E3330
E3330–E3331
E3331–E3431
E3417–E3418
E3417–E3517
E3418–E3419
E3420–E3421
E3421–E3522
E3426–E3427
E3426–E3526
E3431–E3432
E3432–E3433
E3522–E3523
E3615–E3715
E3623–E3624
```

In particular, [delta review](2026-11-18-graziani-delta-rail-source-batch.md) identifies `E3328–E3329` and `E3329–E3330` as pinned leads while stopping before either crossing; they remain fresh candidates. Likewise, `E3516–E3517` and `E3516–E3615` are **not** fresh: later [Kafr el Douwar junction review](2026-11-18-graziani-kafr-el-douwar-junction-review.md) observes crossings despite earlier fork-trace stop.

## Previously inspected, unresolved: revisit separately

```text
UNKNOWN 15
E0929–E1029
E1130–E1229
E1927–E2026
E1929–E2029
E1930–E2029
E2124–E2223
E2422–E2523
E2428–E2429
E2529–E2628
E2905–E3004
E3023–E3024
E3210–E3211
E3513–E3613
E3613–E3714
E3714–E3715
```

Locators: lower Nile [sweep A](2026-11-18-graziani-map-e-rail-sweep-a.md) for `E0929–E1029`, `E1130–E1229`; [sweep B](2026-11-18-graziani-map-e-rail-sweep-b.md) for `E1927–E2026`, `E2124–E2223`, `E2422–E2523`; [sweep C](2026-11-18-graziani-map-e-rail-sweep-c.md) for `E3023–E3024`; [Cairo extension](2026-11-17-graziani-cairo-rail-extension.md) for `E1929–E2029`, `E1930–E2029`; [river junction](2026-11-18-graziani-cairo-river-junction.md) for `E2428–E2429`; [Cairo north followup](2026-11-18-graziani-cairo-nile-rail-followup.md) for `E2529–E2628`; [coast review](2026-11-17-graziani-east-rail-coast-batch-review.md) for `E2905–E3004`; [Alexandria extension review](2026-11-17-graziani-alex-coastal-rail-extension-review.md) for `E3210–E3211`; [E3613](2026-11-17-graziani-e3613-six-sides-review.md) and [E3714](2026-11-17-graziani-e3714-six-sides-review.md) for the final three Alexandria pairs. Linked notes provide PDF and crop commands or references. Uncertain means unresolved, not absent or positive.

Source-positive group comprises exact sides in the coastal and Alexandria extensions, Cairo seed and extensions, fork and Kafr reviews, Suez and delta batches, and sweeps A–C. The reproducible explicit chain set below records exact pairs; qualifications in linked notes remain binding. `E3101|W` is the one open record; its destination and printed crossing remain unresolved and are not an unordered pair. `E2927–E3027` is a separately source-positive, **non-rail-pinned** discrepancy in the delta batch, outside this selected railroad-pin partition. Missing pins, including this discrepancy, do not imply clear terrain or no rail. No omission survey, seam, construction, service, occupancy, or unblocked outcome follows.

## Reproduce pair partition

Run from repository root. Script checks pair membership against the pin, extracts sweep table pairs and explicitly encodes other reviewed chains. Review lists must be updated when new source evidence appears; a source-qualified positive does not settle adjacent sides. The two unpinned side leads at the Cairo vertex remain outside the selected set. All selection and classification reflects cited notes as of this date.

```sh
shasum -a 256 src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
python3 - <<'PYCODE'
import json,re
from pathlib import Path
p=Path('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json')
records=json.loads(p.read_text())['hexsides']
rail={'railroad','unfinished-railroad'}
sel=[r for r in records if rail.intersection(r['features']) and any(isinstance(r[k],str) and r[k].startswith('E') for k in ('a','b'))]
def pair(a,b): return tuple(sorted((a,b)))
def chain(s):
    ids=s.split(); return {pair(a,b) for a,b in zip(ids,ids[1:])}
positive=set()
for s in ['E3101 E3001 E3002 E3003 E3004 E3005 E3006 E3007 E3108 E3109 E3209 E3210','E3211 E3312 E3412 E3413 E3414 E3415 E3516','E3412 E3513','E3517 E3516 E3615','E1828 E1829 E1729 E1629 E1530 E1429 E1330 E1229','E1829 E1930','E1931 E2031 E2132 E2133 E2134','E2029 E2129 E2229 E2329','E2127 E2128 E2129','E2428 E2529','E2429 E2530 E2531 E2631 E2632 E2633','E2632 E2732 E2831 E2830','E2628 E2728 E2827 E2927','E3027 E3128 E3228 E3329']:
    positive |= chain(s)
unknown={pair(*s.split()) for s in ['E3210 E3211','E1930 E2029','E1929 E2029','E2428 E2429','E2529 E2628','E2905 E3004','E3513 E3613','E3613 E3714','E3714 E3715']}
for suffix in 'abc':
    doc=Path(f'docs/work/plans/2026-11-18-graziani-map-e-rail-sweep-{suffix}.md').read_text()
    pairs={pair(*m) for m in re.findall(r'\| `?(E\d{4})[–-](E\d{4})`? \|',doc)}
    uncertain={'a':['E0929 E1029','E1130 E1229'],'b':['E1927 E2026','E2124 E2223','E2422 E2523'],'c':['E3023 E3024']}[suffix]
    u={pair(*x.split()) for x in uncertain}
    assert u <= pairs
    unknown |= u; positive |= pairs-u
assert not positive & unknown
actual={pair(r['a'],r['b']) for r in sel if r['b'] is not None}
print('selected',len(sel),'pairs',len(actual),'positive',len(positive & actual),'unknown',len(unknown & actual),'unreviewed',len(actual-positive-unknown),'open',len(sel)-len(actual))
print('nonpinned tagged',sorted((positive|unknown)-actual))
remaining=sorted(actual-positive-unknown)
for label,items in [('UNKNOWN',sorted(unknown & actual)), ('BATCH D',remaining[:15]), ('BATCH E',remaining[15:30]), ('BATCH F',remaining[30:])]:
 print(label, len(items))
 for x in items: print('–'.join(x))
PYCODE
```

Documentation only; no runtime or map-data changes, gameplay tests, or independent source verification.

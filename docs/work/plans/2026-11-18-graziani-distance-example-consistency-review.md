# Distance example consistency: arithmetic review and visual limit

**Disposition:** Arithmetic and scope checks support [candidate](2026-11-18-graziani-distance-example-consistency.md)'s bounded comparison, but this pass **does not independently verify the Map E labels or shared-side chain visually**. The crop rendered, but image output was unavailable and computer-use transport closed during review. A separate reader must check the native scan before accepting the visual observation. No source correction, global metric or seam registration follows.

- The previously inspected Air/Logistics PDF p. 24 example prints four 33-hex terms, zero northwest from `E2629` to `E3425`, then 8 to `E3433`, 9 to Ismailia and total **149**. Arithmetic is `132 + 0 + 8 + 9 = 149`; replacing zero with 8 makes **157** only as a counterfactual, not the printed answer or a proved shortest distance.
- Under the explicitly assumed regular grid `x = 2 * column - row % 2`, `y = row`, each adjacent pair in the candidate's nine-label sequence has distance 1 and its endpoints have distance 8. The first numeric component changes from 26 to 34, so the claimed lower bound follows **if** every regular-grid adjacency changes it by at most one and both printed labels identify distinct cells. This is a model calculation; it does not verify the printed labels, actual grid, terrain, crossing, a nonlocal transition or source intent.
- Independently recomputed `graziani-distance-independent.json` as a symmetric lower-triangle matrix: 20 labels, 190 cells, 1,140 unordered triples and **24 violating triples**. None of `E2629`, `E3425`, `E3433` or Ismailia is in its labels. These are 24 matrix-triple violations under that transcription; the local worked-example issue is separate and does not create a 25th audited matrix triple. This calculation does not independently retranscribe or validate every printed chart value.

Reproducible arithmetic:

```sh
python3 - <<'PY'
import itertools, json
j = json.load(open('docs/work/plans/graziani-distance-independent.json'))
d = lambda a, b: j['lower_triangle'][max(a, b)][min(a, b)] if a != b else 0
triples = list(itertools.combinations(range(len(j['labels'])), 3))
violations = [t for t in triples if any(d(x, y) > d(x, z) + d(z, y)
              for x, y, z in ((t[0], t[1], t[2]), (t[0], t[2], t[1]), (t[1], t[2], t[0])))]
print(len(j['labels']), sum(map(len, j['lower_triangle'])), len(triples), len(violations))
PY
```

Air PDF SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; map PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same source packages; no independent edition, exhaustive chart rereading, new errata or ground-route inspection. Documentation only; no data/runtime changes or gameplay tests.

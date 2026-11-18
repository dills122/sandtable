# Tobruk–Ben Gania distance example: source and arithmetic review

**Disposition:** Supports [worked-example comparison](2026-11-18-graziani-map-distance-example-next.md)'s printed readings and model arithmetic. The offset result is conditional on the stated regular-grid model and does not establish physical-sheet registration, edge identity or a legal route. Same-scan informed review, not an independent edition.

- Air/Logistics PDF p. 24, Example 1, prints Tobruk to Ben Gania as either **31 hexes west to B4809** or **31 southwest to B1725**, with Ben Gania on the diagonal intersecting both paths. The adjacent PDF p. 25 §37.42 identifies Tobruk `C4807`, Ben Gania `B3715`, and their matrix distance **31**. It also states the chart's origin-exclusive, destination-inclusive counting and hypothetical sea-grid convention. The worked example says shortest distance is not the actual flight path; neither construction point is an instruction to fly or move via it.
- Recomputed the candidate's doubled-coordinate formula `x = 2 * (column + N * sheet) - (row % 2)`, `y = row`, `d = max(abs(dy), (abs(dx) + abs(dy))/2)`, with B=1 and C=2. For N=33, `C4807` to `B4809`, `B1725` and `B3715` each gives 31; `B4809` to `B3715` gives 11, and `B1725` to `B3715` gives 20. The two B construction points share a regular-grid diagonal with Ben Gania between them; these extra lengths are not part of the printed 31-distance route.
- For N=32/33/34, the row-aligned `C4807`–`B4809` distances are **30/31/32**, and `C4807`–`B1725` distances **31/31/32**, respectively. Thus the first leg distinguishes N=33 from these two neighboring offsets only under the stated candidate's coordinate, parity and named-cell assumptions. Agreement with the same package's example and matrix is not independent confirmation, a proof that no other transform fits, or a resolution of Land §4.1's overlay wording and other printed distance contradictions.

Reproduce the arithmetic:

```sh
python3 - <<'PY'
def point(sheet, row, col, offset):
    return 2 * (col + offset * sheet) - row % 2, row
def dist(a, b):
    dx, dy = abs(a[0] - b[0]), abs(a[1] - b[1])
    return max(dy, (dx + dy) / 2)
for n in (32, 33, 34):
    t, x, y, b = (point(*v, n) for v in
                  ((2, 48, 7), (1, 48, 9), (1, 17, 25), (1, 37, 15)))
    print(n, *(dist(*pair) for pair in ((t, x), (t, y), (t, b), (x, b), (y, b))))
PY
```

Visually checked [Air/Logistics](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF pp. 24–25, example and §37.42, and carried the [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) §4.1 overlay qualification from the prior registration check without re-rendering that page. Verified Air SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`, Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and map package `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. No new seam-crop, errata or physical-edition inspection. Documentation only; no data/runtime changes or gameplay tests.

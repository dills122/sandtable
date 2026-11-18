# Map E unpinned railroad search: eastern delta strip

**Date:** November 18, 2026. **Disposition:** No missing railroad pin found in this very small strip. Two visible railroad crossings both have literal railroad records. No conclusion about the rest of Map E; this is an informed reading of one scan, not independent-edition corroboration or a map-data correction.

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf). The fixed search window is `x=970..1180, y=460..530` pixels, measured from the upper left of the 200-dpi context crop below. It follows the blue, transversely ticked line east across the `E3431`–`E3433` strip. Printed grid and labels in the context crop identify the two shared sides. Search includes every visible rail/grid crossing inside that window, including both endpoints; the known `E2927–E3027` discrepancy lies outside it. Adjacent roads, river, and continuation beyond the window are outside this search.

| Visible crossing | Approximate context-crop pixel | Literal JSON records for unordered pair |
| --- | --- | --- |
| `E3431–E3432` | `(1002, 503)` | `E3431|E3432`: `features: ["railroad"]`, `side: null`, `up: null` |
| `E3432–E3433` | `(1128, 480)` | `E3432|E3433`: `features: ["railroad"]`, `side: null`, `up: null` |

Both crossing positions lie on vertical shared sides, away from vertices. No other line/grid crossing is visible within the specified window; crop limits truncate the line at both ends. Scan thickness and similar colors limit interpretation; this records visible crossing evidence, not operational status, connectivity through junctions, or absence of other features. No positive discrepancy or unresolved crossing was found in this window. Wider unpinned-side search remains open.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 3000 -y 2700 -W 1200 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/delta-search
python3 - <<'PY'
import json
h = json.load(open('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json'))['hexsides']
for pair in [('E3431', 'E3432'), ('E3432', 'E3433')]:
    print(pair, [v for v in h if {v['a'], v['b']} == set(pair)])
PY
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/delta-search.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; context crop `509cc8d35d3c77a7a9b4d12d42fd9dbe701ac32cc9d088a67cdfd5ae9103c091`; pinned JSON `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer. Documentation only; no gameplay tests.

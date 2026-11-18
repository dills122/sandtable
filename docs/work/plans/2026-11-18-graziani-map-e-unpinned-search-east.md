# Map E unpinned railroad search: Heliopolis eastern strip

**Date:** November 18, 2026. **Disposition:** No unpinned railroad crossing found in this bounded strip. Three visible crossings have literal railroad records. This is an informed reading of one scan, not independent-edition corroboration or a survey of the remainder of Map E.

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), with its page 1 key for the blue transverse-ticked railroad symbol. The search window is `x=1330..1715, y=520..610`, measured from the upper left of the 200-dpi context crop below. It follows the Heliopolis line across `E2031–E2132`, `E2132–E2133` and `E2133–E2134`, including all visible rail/grid crossings within that rectangle. The nearby `E2233` hex is north of the line; the window includes its southern neighborhood but does not establish a crossing into it. Printed labels and grid continuity identify the pairs; pinned data and prior readings were available during inspection.

| Visible crossing | Approximate context-crop pixel | Literal unordered-pair record |
| --- | --- | --- |
| `E2031–E2132` | `(1349, 598)` | `E2031|E2132`: `features: ["railroad"]`, `side: null`, `up: null` |
| `E2132–E2133` | `(1463, 544)` | `E2132|E2133`: `features: ["railroad"]`, `side: null`, `up: null` |
| `E2133–E2134` | `(1588, 533)` | `E2133|E2134`: `features: ["railroad"]`, `side: null`, `up: null` |

The diagonal crossing lies near, but visibly east of, its western vertex; the other two cross vertical sides. No additional railroad/grid crossing is visible within this rectangle. The window truncates the line at its southwest end. The [independent review](2026-11-18-graziani-map-e-unpinned-search-east-review.md) finds its eastern endpoint close to, but not visibly reaching, the sheet edge; no continuation is established. Scan thickness and print limit interpretation; no unseen continuation, off-map facility connection, operational condition, or absence elsewhere follows. No unresolved crossing was identified inside this window. The previously searched `E3431–E3433` window is outside this crop.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 200 -x 2600 -y 4050 -W 1750 -H 1400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/e-east-unpinned
python3 - <<'PY'
import json
h = json.load(open('src/Cna.Core/Content/HistoricalMap/hexsides.v1.json'))['hexsides']
for pair in [('E2031', 'E2132'), ('E2132', 'E2133'), ('E2133', 'E2134')]:
    print(pair, [v for v in h if {v['a'], v['b']} == set(pair)])
PY
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/e-east-unpinned.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; context crop `cdebc04ec593d0a74b9ca6cd486d7c15b89a8f77d98792f9edd1dc516dfe67a9`; pinned JSON `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG bytes depend on renderer. Documentation only; no gameplay tests.

# D3609 six-side rail survey: independent review

**Date:** November 18, 2026. Rechecked the [D3609 survey](2026-11-18-graziani-map-d-3609-source.md) against the original [Map D scan and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 4 and 1, and literal pin file. Same scan, not an independent edition. **Disposition:** agree with both qualified rail crossings and the bounded negative observations on the other four sides; no material disagreement.

| Literal pair | Independent observation and pin comparison |
| --- | --- |
| `D3608|D3609` | Blue cross-tied line crosses the shared vertical side well inside its endpoints. Nearby centerline has repeated gaps consistent with the key's unfinished symbol; numeral, grid and tie obscure portions. Pin contains `unfinished-railroad`. |
| `D3609|D3709` | Brown band follows the diagonal. No blue rail crossing found in exposed portion; grid and brown overprint qualify the reading. Pin contains `slope`, `up: D3609`; neither its direction nor absence of every other feature is certified here. |
| `D3609|D3710` | Blue cross-tied line crosses the diagonal between endpoints, closer to the D3610-side vertex. Repeated local gaps support unfinished class; a tie and the grid crowd the crossing. Pin contains `unfinished-railroad`. |
| `D3609|D3610` | No blue centerline crossing identified on the vertical side. Blue line instead crosses the adjacent diagonal above the upper vertex; a tie and nearby grid qualify that vertex. No literal pair found. |
| `D3509|D3609` | No blue rail crossing identified in the exposed lower-left diagonal. No literal pair found. |
| `D3510|D3609` | No blue rail crossing identified in the exposed lower-right diagonal. No literal pair found. |

The key distinguishes continuous blue railroad from interrupted blue unfinished railroad; the local gap readings are qualified by image softness and overprint. Negative readings concern exposed blue rail only, not absence of other features or proof of clear terrain. A missing pin is not itself source evidence. No conclusion follows about construction, operational use or routes.

Reproduce the same-source view:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 2900 -y 8200 -W 1500 -H 1450 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/mapd-3609-independent
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pin file `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Both checked; pin query returned exactly the three entries stated above for D3609. The 600-dpi output enlarges the 300-ppi embedded image. Documentation only; no runtime/data edits or gameplay tests.

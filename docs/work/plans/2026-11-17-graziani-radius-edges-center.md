# Graziani: C0716 radius edges, columns C07–C08

17 November 2026. Visual audit of [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, against pinned [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json), source `vassal:CNAv2.1.0`. Thirteen cells, including sides to cells outside the disk. This is a same-scan reading informed by [C0716 six-side review](2026-11-17-graziani-c0716-six-sides.md), not an independent edition or movement ruling.

Each entry gives neighbor followed by visual/pinned status. Order is upper-left, upper-right, right, lower-right, lower-left, left as printed. `T` = visible dashed track crossing; `V` = route reaches a vertex or overlaps grid and assignment to that particular side is uncertain; `S` = brown slope-like band along side; `B` = blue/black border mark along side; `U` = no positive crossing or side mark established, **unknown**, not clear. After slash, `t` and `s` are literal pinned `track` and `slope`; `–` means no explicit pair record, not proof of absence. Features may coexist; terrain artwork and vertex qualifications follow the table.

| Cell | Six sides: neighbor visual/pinned |
| --- | --- |
| C0713 | C0812 T/t; C0813 V/t; C0714 T/t; C0613 U/–; C0612 U/–; C0712 U/– |
| C0714 | C0813 U/–; C0814 U/–; C0715 T/t; C0614 U/–; C0613 U/–; C0713 T/t |
| C0715 | C0814 U/–; C0815 U/–; C0716 T/t; C0615 U/–; C0614 U/–; C0714 T/t |
| C0716 | C0815 T/t; C0816 V/t; C0717 T/t; C0616 U/–; C0615 U/–; C0715 T/t |
| C0717 | C0816 U/–; C0817 U/–; C0718 T/t; C0617 U/–; C0616 U/–; C0716 T/t |
| C0718 | C0817 U/–; C0818 U/–; C0719 T+B/t; C0618 U/–; C0617 U/–; C0717 T/t |
| C0719 | C0818 B/–; C0819 U/–; C0720 T/t; C0619 U/–; C0618 B/–; C0718 T+B/t |
| C0813 | C0913 U/–; C0914 S/s; C0814 S/s; C0714 U/–; C0713 V/t; C0812 U/– |
| C0814 | C0914 U/–; C0915 U/–; C0815 U/–; C0715 U/–; C0714 U/–; C0813 S/s |
| C0815 | C0915 T/t; C0916 V/t; C0816 U/–; C0716 T/t; C0715 U/–; C0814 U/– |
| C0816 | C0916 U/–; C0917 U/–; C0817 U/–; C0717 U/–; C0716 V/t; C0815 U/– |
| C0817 | C0917 U/–; C0918 U/–; C0818 U/–; C0718 U/–; C0717 U/–; C0816 U/– |
| C0818 | C0918 U/–; C0919 U/–; C0819 U/–; C0719 B/–; C0718 U/–; C0817 U/– |

A dash crosses C0812–C0713; the track then runs close to the C0713–C0813 edge and toward its right vertex. Separate crossing of that edge is not discernible. At C0815's top vertex, track overlaps the junction of C0915 and C0916; the image does not independently establish both pinned crossings. The same qualification applies at C0716's top vertex, where diagonal track and lettering overlap C0816; the C0815 side has visible track immediately adjacent to the vertex. Horizontal dashes crossing C0718–C0719 overlap the border. The blue/black band continues along C0719–C0818 and C0719–C0618; its absence from these pair records does not settle whether this dataset models borders elsewhere or imply a data defect. Verify border meaning and schema before any change.

Brown band follows C0813–C0914 and C0813–C0814; its source-key classification and uphill direction need separate validation. Salt-marsh polygon fill reaches or crosses portions of boundaries near C0715/C0814 and C0816–C0818 and their northern neighbors; labels and grid overlap limit exact edge attribution. These are interior artwork observations, not asserted impassable side features. Oasis art and lettering obscure parts of C0716–C0718 and C0816–C0818. `U` includes all other marks potentially concealed or too faint to identify; no negative terrain certification follows. Hex interior comparison is documented in [radius data audit](2026-11-17-graziani-radius-full-data-audit.md).

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 600 -x 4700 -y 16000 -W 3200 -H 4200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/radius-center
pdftoppm -f 3 -l 3 -r 600 -x 3900 -y 17000 -W 1600 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/radius-center-west
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; center crop `8ec6464e4dcc250b7ee3224e3db7220515e72ce4f390ee35478c8cd0d8fbf235`; west crop `45e0edce8357363cfedd70a299a6901917a7f3211e86422bc03abd2862471649` (PNG bytes renderer-dependent). Pinned hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Crops and PDF outside Git. No runtime or data changes; no gameplay tests.

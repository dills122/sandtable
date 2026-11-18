# Graziani C0716 radius: western edge source audit

Reviewed 17 November 2026 against [original Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pages 3 and 1. This batch covers C0414–C0417, C0514–C0518 and C0613–C0618, including sides against cells outside the radius. Directions refer to the displayed scan, not compass bearings. Neighbors identify each of six sides; repeated shared sides are the same printed line, not independent observations.

**Reading:** Yellow desert fill covers C04 and C05 cells and extends irregularly into C06 cells. This fill crosses grid boundaries; its presence is an area observation, not a distinct side feature or a ruling about terrain assignment. A blue checked line follows C0618/C0719 and C0618/C0619. Elsewhere among the 90 listed side incidences, exposed grid lines show no obvious route crossing or broad side band at this scan scale. No distinct positive route crossing or side-terrain mark is assigned in this batch. Fine marks, overlaps and exact terrain effects remain unknown on **every** side; lack of a visible mark is not evidence of clear terrain or complete feature absence. This is a same-scan visual reading, not a second edition or legal-movement certification.

The pinned [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) contains **no pair entry involving any assigned cell**. It declares source `vassal:CNAv2.1.0`; sparse missing records cannot certify clear edges. Except for the two checked-line sides identified below, each side in the table has the same status: exposed grid and yellow fill in the vicinity as described above; no obvious crossing or broad band observed; side classification and any small or obscured mark **unknown**, and no pinned entry. On the two checked-line sides, coincident features remain **unknown** under the overprint. The checked line is interpreted geographically as the Libya–Egypt boundary in the [regional review](2026-11-17-graziani-saharan-radius-border.md), but the key does not reproduce its blue checks; its independent movement effect is not established. Column C06's pale/yellow irregular boundary is interior artwork and intersects the grid in places; do not treat it as an identified route or side band.

| Cell | Upper-left | Upper-right | Right | Lower-right | Lower-left | Left |
| --- | --- | --- | --- | --- | --- | --- |
| C0414 | C0514 | C0515 | C0415 | C0315 | C0314 | C0413 |
| C0415 | C0515 | C0516 | C0416 | C0316 | C0315 | C0414 |
| C0416 | C0516 | C0517 | C0417 | C0317 | C0316 | C0415 |
| C0417 | C0517 | C0518 | C0418 | C0318 | C0317 | C0416 |
| C0514 | C0613 | C0614 | C0515 | C0414 | C0413 | C0513 |
| C0515 | C0614 | C0615 | C0516 | C0415 | C0414 | C0514 |
| C0516 | C0615 | C0616 | C0517 | C0416 | C0415 | C0515 |
| C0517 | C0616 | C0617 | C0518 | C0417 | C0416 | C0516 |
| C0518 | C0617 | C0618 | C0519 | C0418 | C0417 | C0517 |
| C0613 | C0713 | C0714 | C0614 | C0514 | C0513 | C0612 |
| C0614 | C0714 | C0715 | C0615 | C0515 | C0514 | C0613 |
| C0615 | C0715 | C0716 | C0616 | C0516 | C0515 | C0614 |
| C0616 | C0716 | C0717 | C0617 | C0517 | C0516 | C0615 |
| C0617 | C0717 | C0718 | C0618 | C0518 | C0517 | C0616 |
| C0618 | C0718 | C0719 (blue checks) | C0619 (blue checks) | C0519 | C0518 | C0617 |

## Reproduction and limits

```sh
pdftoppm -f 3 -l 3 -r 600 -x 4100 -y 18100 -W 3500 -H 2400 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/radius-west
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c0716-key
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `7a7770b4fff4f2aa7c6f13146b57f4087bdac9b4372a6b52021c88b6c0771084`; key crop `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894` (PNG bytes depend on renderer). Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; hexsides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crops remain outside Git. No runtime changes or gameplay tests.

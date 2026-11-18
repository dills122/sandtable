# Graziani: eastern C0716 halo interiors

**Date:** 17 November 2026. Visual inventory of [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, informed by the [disk audit](2026-11-17-graziani-radius-full-data-audit.md) and [C0919 review](2026-11-17-graziani-c0919-oasis-review.md). Same scan, not an independent edition. Printed four-digit numbers below carry sheet prefix `C` in canonical IDs.

Subtracting the 37-cell disk from the 61-cell envelope in the [batch plan](2026-11-17-graziani-playable-map-work-batch.md) yields 24 ring cells. This ledger covers the 13 with first coordinate 07–11: C0712, C0720, C0812, C0819, C0913, C0919, C1013, C1018, and C1114–C1118. This geometric selection is not a legal placement or movement boundary.

| Cell | Scan interior and limitations | Pinned terrain; settlement | Comparison |
| --- | --- | --- | --- |
| C0712 | Pale interior; Bir Bu Salama lettering overlaps part. No distinct settlement symbol apparent in exposed area. | `clear`; null | No positive conflict observed; lettering limits negative reading. |
| C0720 | Pale interior with dashed track; no distinct settlement symbol apparent. | `clear`; null | No positive conflict observed; track and number obscure small areas. |
| C0812 | Pale interior and blue dot at Bir Bu Salama; dashed track nearby. | `clear`; `village` | Dot supports settlement. Text, track and number obscure parts. |
| C0819 | Salt-marsh polygon pattern; brown band at eastern edge; blue boundary artwork nearby. No distinct settlement symbol apparent in exposed area. | `salt-marsh`; null | Marsh supported; assignment and effects of edge artwork remain open. |
| C0913 | Pale interior; dashed track crosses it. No distinct settlement symbol apparent. | `clear`; null | No positive conflict observed; track and number obscure small areas. |
| C0919 | Blue palm-and-water oasis symbol; number and blue boundary artwork overlap symbol or edge. | `clear`; null | Positive missing-oasis candidate; see linked review. `clear` not independently adjudicated. |
| C1013 | Pale interior; dashed track and brown edge artwork near C1014. No distinct settlement symbol apparent. | `clear`; null | No definite conflict; precise artwork assignment remains open. |
| C1018 | Salt-marsh polygons; blue lettering and neighboring boundary artwork overlap portions. No distinct settlement symbol apparent in exposed area. | `salt-marsh`; null | Marsh supported; overprint limits negative reading. |
| C1114 | Brown patch extends into interior; dashed track crosses nearby. No distinct settlement symbol apparent. | `rough`; null | Brown artwork consistent with rough; whole-cell assignment and nearby edge artwork remain open. |
| C1115 | Pale interior, dashed track, and nearby brown artwork. No distinct settlement symbol apparent. | `clear`; null | No definite conflict; track and number limit reading. |
| C1116 | Pale interior and dashed track; no distinct settlement symbol apparent. | `clear`; null | No positive conflict observed; track and number obscure small areas. |
| C1117 | Pale interior and dashed track; Ain Melfa lettering overlaps part. No distinct settlement symbol apparent in exposed area. | `clear`; null | No positive conflict observed; overprint limits negative reading. |
| C1118 | Pale interior, blue dot and Ain Melfa lettering; dashed track and nearby brown/blue boundary artwork. | `clear`; `village` | Dot supports settlement; overprint and edge artwork limit remaining interpretation. |

Pale appearance is not a rules finding of `clear`; absence of an apparent symbol is limited by scan resolution and overprint. Brown bands, dashed tracks and blue boundary marks are observations, not a complete obstacle or six-side inventory. Whole-hex terrain assignment, obstacle classification and direction require applicable rules and clearer source checks. Pinned JSON identifies `vassal:CNAv2.1.0` as source, so C0919 is a cross-source discrepancy candidate, not proof of error. No correction or runtime admission follows. Other ring cells, side dispositions, opposing force reach, legal setup, movement and ZOC remain unverified.

## Reproduction

Source PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Rendered crop SHA-256 `55d4123771b88fc244711ea45e91307c814f1ca928e58df21e8443e4c2d04f08`; renderer and encoding can change PNG bytes.

```sh
pdftoppm -f 3 -l 3 -scale-to 7000 -x 600 -y 4700 -W 3100 -H 2200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-halo-east
```

Documentation-only review; no gameplay tests.

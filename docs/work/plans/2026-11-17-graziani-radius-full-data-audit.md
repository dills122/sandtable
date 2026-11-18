# Graziani: full C0716 radius interior data audit

Reviewed 17 November 2026 against [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, and its terrain key on p. 1. Compared all 37 geometric candidates in the [radius source review](2026-11-17-graziani-c0716-radius-source.md) with the pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json). Visual reading uses the same scan and was informed by the [interior transcription](2026-11-17-graziani-c0716-radius-interiors.md), its [review](2026-11-17-graziani-c0716-radius-interiors-review.md), and the earlier [four-cell comparison](2026-11-17-graziani-radius-data-review.md); it is neither blind nor an independent edition. The data identifies its source as `vassal:CNAv2.1.0`, so a difference from this scan is a cross-source discrepancy candidate, not proof of a transcription error.

## Priority candidates

| Priority | Hex | Printed evidence | Pinned record | Disposition |
| --- | --- | --- | --- | --- |
| 1 | C0718 | Blue palm-and-water oasis symbol in the cell, crossed by a dashed track. | `clear`, no settlement | Visible symbol supports a missing-oasis candidate. Track and number obscure part of symbol. |
| 1 | C0818 | Salt-marsh polygons extend into the cell; blue lettering covers part of interior. | `clear`, no settlement | Visible marsh conflicts with `clear` if this artwork qualifies the cell under the applicable assignment convention. Overprint limits negative settlement claims. |
| 2 | C0918 | Salt-marsh polygons visible; no distinct oasis symbol apparent. C0919 has a separate visible oasis. | `salt-marsh`, `oasis` | Marsh supported; recorded oasis lacks visible symbol support in this cell. Verify source and assignment before removal. |
| 2 | C0817 | Salt-marsh polygons visible; lettering obscures part of interior. No distinct oasis symbol apparent in exposed area. | `salt-marsh`, `oasis` | Marsh supported; recorded oasis lacks visible symbol support, but overprint prevents proving absence. |

Priority orders review by positive visible evidence first, then unsupported records; it does not assert gameplay severity or authorize edits. The [independent visual review](2026-11-17-graziani-radius-data-review-independent.md) also examined these four cells using the same scan.

## Complete comparison

Ranges are inclusive; `—` means null settlement. “Supported” means visible artwork agrees with the stated field at this scan's resolution; it does not establish whole-hex rules classification or prove that obscured marks are absent.

| Cells (count) | Pinned terrain; settlement | Map appearance and assessment |
| --- | --- | --- |
| C0414–C0417 (4), C0514–C0518 (5) | `desert`; — | Yellow desert fill supports terrain. No settlement symbol apparent. |
| C0613–C0618 (6) | `desert`; — | Yellow desert and uncolored portions share each cell. Desert artwork present; whole-hex assignment depends on boundary convention. No settlement symbol apparent. |
| C0713–C0714 (2), C0719 (1), C0813 (1), C0815 (1), C0915 (1) | `clear`; — | No colored interior terrain symbol apparent; no settlement symbol apparent. This is not proof of blank terrain or absence of line features. |
| C0715, C0717 (2) | `salt-marsh`; — | Marsh artwork supports terrain; no settlement symbol apparent. |
| C0716 (1) | `clear`; `oasis` | Oasis symbol supports settlement; track crosses symbol. Nearby name alone does not assign further symbols. Clear terrain is not independently adjudicated. |
| C0718 (1) | `clear`; — | Oasis symbol visible; priority 1 candidate above. |
| C0814 (1) | `salt-marsh`; — | Marsh artwork supports terrain; brown ridge/slope artwork enters western edge. Its classification and interaction with interior terrain remain open. |
| C0816 (1) | `salt-marsh`; — | Marsh artwork supports terrain; lettering masks part of interior and limits negative settlement reading. |
| C0817 (1) | `salt-marsh`; `oasis` | Marsh supported; oasis unsupported in exposed area, priority 2 above. |
| C0818 (1) | `clear`; — | Marsh visible, priority 1 above; lettering limits negative settlement reading. |
| C0914 (1) | `gravel`; — | Brown artwork extends from western boundary into interior. Visual appearance alone does not settle whether `gravel` or a side feature is the correct classification. No settlement symbol apparent. |
| C0916–C0917 (2) | `salt-marsh`; — | Marsh artwork supports terrain; no settlement symbol apparent. |
| C0918 (1) | `salt-marsh`; `oasis` | Marsh supported; oasis lacks visible symbol support, priority 2 above. |
| C1014 (1) | `clear`; `village` | Blue village/bir dot supports settlement. Brown ridge/slope at western edge needs separate classification; clear interior terrain not independently adjudicated. Map label is Giarabub; scenario uses Giaribub. |
| C1015–C1017 (3) | `salt-marsh`; — | Marsh artwork supports terrain. Label partly obscures C1015; no settlement symbol apparent in exposed areas. |

Total: 37 records. No additional definite interior discrepancy identified at this resolution. This is not a clean bill of health for `clear`, `gravel`, or null settlements: scan quality, labels, tracks and hex numbers mask portions, and artwork can cross hex boundaries. Separate terrain and settlement fields permit an oasis and marsh in one cell; labels alone do not establish settlement location. C1018 and C0712 are outside this geometric disk.

## Scope and reproducibility

Priority follow-up: check original or clearer editions and Vassal source for the four candidates; establish the map's whole-hex terrain and edge-artwork assignment convention before changing any record. Review C0613–C0618 and brown artwork at C0814/C0914/C1014 under that convention. No hexside, route, border, legal setup, stacking, movement, supply or gameplay audit is implied. The geometric radius itself assumes both origin and distance-three boundary included; no candidate is thereby established as a legal setup hex.

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; hex data SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Rendered Map C crop includes the complete disk:

```sh
pdftoppm -f 3 -l 3 -scale-to 5000 -x 500 -y 3400 -W 2000 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-full-radius
```

No data or runtime changes; no gameplay tests.

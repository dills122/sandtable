# Graziani: independent radius data visual review

Reviewed 17 November 2026 against [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 3 (Map C), and page 1 terrain key. This is a separate visual comparison of the same scan after reading the [data review](2026-11-17-graziani-radius-data-review.md), not a blind reading or an independent edition. Key shows yellow polygonal salt-marsh artwork and a blue palm-and-water oasis symbol; blue lettering alone does not assign an oasis symbol to a hex.

| Hex | Independent visual observation | Comparison with pinned data |
| --- | --- | --- |
| C0718 | Blue palm-and-water symbol lies in hex, with dashed track crossing it and label partly covered. | `terrain: clear`, `settlement: null`: visible symbol supports the missing-oasis discrepancy candidate. |
| C0818 | Salt-marsh pattern extends into hex, with blue “Giarabub Oasis” lettering covering part of interior. | `terrain: clear`, `settlement: null`: visible marsh supports a terrain review candidate. Artwork crossing an edge alone does not establish the applicable terrain assignment. Overprint limits negative claims about small symbols. |
| C0817 | Salt-marsh pattern visible; “Giarabub” and “El Qaqa” lettering cover parts of interior. No distinct palm-and-water symbol apparent in exposed area. | `terrain: salt-marsh`, `settlement: oasis`: artwork supports marsh; no visible oasis symbol supports the recorded settlement. Obscured area prevents proving absence. |
| C0918 | Salt-marsh pattern visible across interior; no distinct palm-and-water symbol apparent. Neighboring C0919 has a clear palm-and-water symbol. | `terrain: salt-marsh`, `settlement: oasis`: artwork supports marsh; recorded oasis lacks visible symbol support in C0918. Nearby lettering and C0919's symbol do not establish a symbol in C0918. |

These findings agree with the reviewed document as visual observations and discrepancy candidates. Hex data uses separate `terrain` and `settlement` fields, so visible marsh and an oasis record are not inherently contradictory. Scan, lettering, track, and boundary crossings limit negative readings and exact assignments. No terrain precedence, rule effect, setup eligibility, or data correction is adjudicated here.

Evidence: PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Map crop reproduced using the command in the reviewed document; terrain key inspected separately. No runtime changes or gameplay tests.

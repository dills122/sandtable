# Graziani: C0716 radius data review

Reviewed 17 November 2026 against [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 3 (Map C), and page 1 terrain key. Reading is informed by the earlier [radius interior transcription](2026-11-17-graziani-c0716-radius-interiors.md), uses the same scan, and is not an independent edition or blind transcription. The key distinguishes a blue palm-and-water **Oasis** symbol from yellow polygonal **Salt Marsh** artwork; blue place-name lettering is not itself the oasis symbol.

| Hex | Visible map interior | Pinned data | Assessment |
| --- | --- | --- | --- |
| C0718 | Blue palm-and-water oasis symbol, crossed by dashed track; printed `0718` partly overlaid. | `terrain: clear`, `settlement: null` | Oasis symbol visible; missing settlement is a source-data discrepancy candidate. |
| C0818 | Salt-marsh polygons extend inside the cell; blue “Giarabub Oasis” lettering masks part of it. | `terrain: clear`, `settlement: null` | Clear terrain conflicts with visible marsh artwork. The overprint limits a claim about obscured small marks. |
| C0817 | Salt-marsh polygons visible; “Giarabub” and “El Qaqa” lettering overlap parts of the cell. No distinct palm-and-water symbol apparent in exposed area. | `terrain: salt-marsh`, `settlement: oasis` | Marsh is supported. Recorded oasis is not supported by a visible symbol; lettering obscures part of the interior, so this scan alone does not prove absence under the overprint. |
| C0918 | Salt-marsh polygons visible across the interior; no palm-and-water symbol apparent. | `terrain: salt-marsh`, `settlement: oasis` | Marsh is supported; recorded oasis lacks visible symbol support. Adjacent C0919 contains a distinct oasis symbol; the nearby “Giarabub Oasis” lettering crosses cells and does not establish a symbol in C0918. |

These observations identify review candidates, not automatic edits or rulings on terrain precedence, setup eligibility, movement, or legal placement. Labels and the track can obscure small details. Exact treatment of artwork crossing a hex edge requires the applicable map transcription convention and rules; no such adjudication is made here.

## Evidence and reproducibility

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned `src/Cna.Core/Content/HistoricalMap/hexes.v1.json` SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Data records were inspected after the map reading. Rendered crop SHA-256 `1e8857e67f0a5369e03bdafbcb8c45183d969408d2a7004e73f12360abdb5c70`; renderer and PNG encoding can affect this hash. PDF hash anchors the source.

```sh
pdftoppm -f 3 -l 3 -scale-to 5000 -x 1000 -y 3950 -W 1000 -H 700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-radius-crop
```

Crop includes printed hex labels `0718`, `0817`, `0818`, `0918`, and neighboring oasis at `0919`. Original terrain key inspected separately on PDF page 1. No runtime changes or gameplay tests.

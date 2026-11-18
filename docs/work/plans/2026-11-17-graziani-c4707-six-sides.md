# Graziani C4707: six-side map reading

**Scope:** source observation for fixed Axis setup hex C4707, not a legal movement, supply, ZOC or contact finding. Scenario §60.31 places 64 Catanzaro there; see [setup inventory](2026-11-17-graziani-fixed-contact-inventory.md) and [Tobruk reachability](2026-11-17-graziani-tobruk-reachability.md). This is an informed reading of the same scan used in existing research, not independent transcription or a new edition.

**Direction follow-up:** Later original-map checks and independent reviews
support the three recorded [slope directions](2026-11-17-graziani-c4707-slope-directions.md)
and [escarpment direction](2026-11-17-graziani-c4707-escarpment-direction.md),
with label and overprint qualifications. They do not certify all side features.

**Source and reproducibility:** [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Map C PDF page 3 and terrain key page 1. Local crop, before any subsequent rescaling:

```sh
pdftoppm -f 3 -l 3 -r 500 -x 1300 -y 3000 -W 2600 -H 2300 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4707/tobruk
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 160 -W 1500 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-c4707/key
```

Crop SHA-256 values respectively `39b116ace7edae2d4f5f5b5cfad5d0d550c2180c6144dd6d618eddc9e0a76958` and `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`. Renderer bytes can vary. The inspected detail is pixels `(450, 1000, 1100, 1800)` of the first crop, SHA-256 `7f8381de215e388fa01421750a41f4762a119a2eb3d05c530dca7c3cece86828`; crops remain outside Git. Directions below refer to the unrotated PDF rendering, not compass bearings.

C4707 is the pale hex immediately below Tobruk C4807, left of C4708, and right of C4706; its identifier is obscured by blue rail symbols. The map key depicts a broad dark stippled band for escarpment, ochre stippled band for slope, paired short dashed black lines for unfinished road, and blue dotted line with crossbars for unfinished railroad. It distinguishes solid railroad from unfinished railroad. Route marks, city/Axis illustration and converging lines cover parts of the C4707/C4807 junction and nearby vertices.

| Neighbor | Visible side reading | Pinned hexside record |
|---|---|---|
| C4806 | Upper-left diagonal: narrow grid line, no obvious broad terrain band in exposed portion. | No record. |
| C4807 | Upper-right diagonal: ochre slope band; blue unfinished railway and black paired route lines converge into Tobruk at or near this side. Axis/city overprint and crowded vertex limit exact crossing assignment. | `slope`, `unfinished-railroad`, `unfinished-road`; `up: C4707`. |
| C4708 | Right vertical: thin grid line in exposed portion; route lines converge at upper endpoint, and lower end is crowded. Exact vertex assignment remains uncertain. | No record. |
| C4607 | Lower-right diagonal: ochre slope band, blue unfinished rail and black paired broken lines cross or converge near this side and its vertex. Overprinting limits precise separation. | `slope`, `unfinished-railroad`, `unfinished-road`; `up: C4607`. |
| C4606 | Lower-left diagonal: broad ochre stippled slope band. | `slope`; `up: C4606`. |
| C4706 | Left vertical: broad dark stippled escarpment band. | `escarpment`; `up: C4706`. |

Pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) reports `terrain: clear`, `settlement: null`, `coastal: false`; its SHA-256 is `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 is `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; both declare `vassal:CNAv2.1.0` as source. Absence of a record is not proof of a completely feature-free side. Directional `up` values are transcribed from pinned data, not independently established by this visual check. The scan supports the broad band readings; it does not settle all route crossings, terrain direction, rules effects, or hidden marks under overprint.

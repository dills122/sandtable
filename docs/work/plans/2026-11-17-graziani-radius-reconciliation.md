# Graziani: Map C radius reconciliation candidates

**Disposition, 17 November 2026:** retain pinned data unchanged. The printed
map supports two positive discrepancy candidates and leaves two recorded oasis
symbols unconfirmed. None is an admitted correction. The [earlier comparison](2026-11-17-graziani-radius-data-review.md),
[independent same-scan review](2026-11-17-graziani-radius-data-review-independent.md),
and [37-cell audit](2026-11-17-graziani-radius-full-data-audit.md) informed this
reading; it is neither blind nor a separate edition.

## Proposed field-level candidates

The [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1,
depicts **Salt Marsh** with yellow polygons and **Oasis** with blue palms and
water. Page 3 places those symbols on Map C. A blue place-name is not itself
an oasis symbol. The candidates below are proposed values for review, not
instructions to modify the pinned resource.

| Hex | Pinned `terrain`; `settlement` | Candidate | Evidence and remaining condition |
| --- | --- | --- | --- |
| C0718 | `clear`; `null` | `clear`; `oasis` | Palm-and-water symbol visibly inside cell; track and printed number overlap it. Strong positive symbol evidence. Preserve terrain pending separate review. |
| C0818 | `clear`; `null` | `salt-marsh`; settlement unresolved | Marsh polygons visibly extend into cell. Change terrain only if accepted whole-hex assignment convention classifies this extent as marsh. Lettering obscures part of cell; do not infer settlement absence. |
| C0817 | `salt-marsh`; `oasis` | Retain marsh; review `oasis` versus `null` | Marsh visible. No distinct oasis symbol apparent in exposed area, but names obscure part of cell. Do not remove solely on this scan. |
| C0918 | `salt-marsh`; `oasis` | Retain marsh; review `oasis` versus `null` | Marsh visible; no distinct oasis symbol apparent. Adjacent C0919 has its own visible symbol; lettering crossing cells does not locate another. Verify source and obscured detail before removal. |

`null` is the existing no-settlement value, not an uncertainty marker. A
possible removal cannot encode unresolved evidence as a fact. Track, hexside,
coast, setup and rules effects are outside this comparison. C0919 lies outside
the radius, but its visible symbol and pinned `clear`; `null` record merit a
separate coordinate and source check; do not treat its symbol as C0918's.

## Representation and admission

The pinned [hex records](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json)
store `terrain` and `settlement` separately. Thus an accepted marsh hex with
an independently located oasis can be represented as `terrain: salt-marsh`
and `settlement: oasis`; neither field cancels the other. This is a data-shape
observation, not a ruling on movement, precedence, or effects. The key names
the symbols but does not by itself establish how much edge-crossing artwork
qualifies a whole hex. The applicable source convention and rules, including
any interactions between terrain and settlements, remain unresolved.

Before admitting any change:

1. Compare a clearer original or independently sourced edition, preserving
   coordinate and symbol evidence, especially obscured C0817 and C0818.
   Check VASSAL source and upstream correction history for why these values
   differ; a difference from the scan alone is not proof of an upstream error.
2. Establish and record the applicable terrain and settlement assignment
   conventions, including boundaries and overlays. Independently review each
   proposed field and any contrary evidence. Record unresolved fields outside
   authoritative values until evidence supports a decision.
3. Apply any accepted corrections in a new map version and identity, preserve
   pinned v1 bytes for replay, and update hashes and relevant design documents
   as required by the [map transcription specification](../../specs/graziani-historical-map-transcription.md).
   Review affected consumers, deterministic tests, replay, and authority/privacy
   boundaries before enabling behavior. No legal placement or movement follows
   from these candidates alone.

## Provenance

Viewed hash-matched Map C crop and terrain key from the PDF used by the earlier
reviews; reproduction command is in the [data review](2026-11-17-graziani-radius-data-review.md).
PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
pinned hex JSON SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`.
JSON declares `vassal:CNAv2.1.0`; the specification identifies the pinned
Living Rules revision and its VASSAL-derived provenance. Upstream data conventions
and VASSAL correction history were not independently retrieved in this pass.
No data or runtime changes; no gameplay tests.

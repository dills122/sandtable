# Gazala B/C seam: same-scan review

**November 18, 2026.** Reviewed the [seam note](2026-11-18-graziani-gazala-bc-seam.md) against the Map B and C crops from the same source PDF. **Agree with its bounded disposition:** `B4934` and `C4901` are candidate corresponding positions under the hypothetical +33 column translation, not a proven alias or adjacency. This is a second reading of the same scan, not independent source or edition corroboration.

## Visible evidence

- On Map B, the printed `4933` cell contains Gazala's blue dot. Paired road strokes cross its right vertical side into `4934` and continue to the sheet edge. The `4934` cell has blue coast artwork at its upper edge and a dark band at its lower-right edge. `5033` is sea, and `5134` appears above; no `5034` label is visible in this crop.
- On Map C, the left image boundary cuts through the printed `4901` cell and its coastal and road approach. Paired strokes enter at that boundary and run east along the coastal strip. `5001` above is sea; `4801` below is land with dark artwork on its upper-left boundary. Eastward, the corridor runs toward the named Tobruk and port mark near `4807`. Tobruk orients Map C but is not a shared landmark visible on Map B.
- Gazala's dot and its rounded inlet are distinctive on B, but their counterparts are not visible in the clipped part of C. The coast, road and dark band in the two edge areas are useful leads; their shapes and placement do not demonstrate a common registered feature. Grid spacing, sea above, yellow pattern and dashed lines farther south are broad similarities, not unique anchors. No common set of measured vertices or matched distinctive marks has been demonstrated.

With columns expressed as the last two printed digits, `01 + 33 = 34`, so the proposed translation places `C4901` at the candidate position of `B4934`; `B4933` is one column west in that model. Arithmetic assumes the translation and does not establish it. Map C's cropped cell prevents testing the missing approach and coast; it does not establish their absence. Land §4.1's coordinate wording and edition/provenance remain unresolved. Neither a join nor a contradiction can be concluded from these crops alone.

## Pin and next evidence

Literal inspection of [pinned hexes](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) finds `B4933`, `C4901`, and `C4807`, but no `B4934` ID. A missing record is not proof of absent terrain or an alias. Keep the independently observed `B4933|B4934` road crossing separate from representation of its endpoint; do not infer cross-sheet continuity, a route, or legal movement. Seek full sheet-edge or independent physical/edition evidence, register multiple vertices and distinctive coast and road marks, reconcile clipped areas and the source rules, then establish representation policy before any versioned correction.

## Reproduction

Viewed `/private/tmp/gazala-review-600.png` and `/private/tmp/gazala-c-seam.png`; crop commands are in the [road candidate](2026-11-18-graziani-gazala-road-candidate.md) and [seam note](2026-11-18-graziani-gazala-bc-seam.md). SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; B crop `254cbb508b043794550ef1b96d425aaaba83ad26f820be498bd1567fa56e5c49`; C crop `65dd235a298ff7053764101a6a6dd86c504c43880907c8ff9d6cb8ac1773e2c4`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Hashes identify inspected bytes, not provenance, rights, or edition equivalence. No runtime/data changes or gameplay tests.

# Map E delta railway: Tanta branch review

**Date:** November 18, 2026. **Disposition:** Agree with the seven rail crossings in the [source batch](2026-11-18-graziani-delta-rail-source-batch.md). The `E2927–E3027` crossing is visibly cross-ticked rail, while its pinned pair has `unfinished-road` and `track`, with no `railroad`. This supports a source-versus-pin discrepancy for rail; it does not establish that the other pinned features are absent or authorize a data correction.

Viewed page 5 of the [map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf) using the batch's crop and printed hex labels. The blue line has repeated transverse ties, matching the railroad symbol; the nearby dark paired strokes are a separate route. Coordinates below are approximate pixels from the detail crop's upper left. Pin and first reading were available during this review, so this is an informed second reading of the same scan, not a blind or independent-edition check.

| Side | Review of crossing | Pinned features |
| --- | --- | --- |
| `E2628–E2728` | Ticked line crosses the diagonal near `(285, 920)`, above the lower crop edge. | `railroad`, `road` |
| `E2728–E2827` | Ticked line crosses near `(215, 780)`, below the branch junction. | `railroad`, `road` |
| `E2827–E2927` | Ticked line crosses near `(155, 635)`, beside dark route strokes. | `railroad`, `road` |
| `E2927–E3027` | Ticked line crosses the diagonal near `(140, 535)`, between its vertices and south of Tanta's dot. Labels and grid place the crossing on this pair; it is not merely an approach to the city or a vertex crossing. Adjacent dark strokes overlap the side; their full feature classification is outside this rail check. | `unfinished-road`, `track` |
| `E3027–E3128` | Ticked line crosses near `(200, 435)`, north of Tanta. | `railroad`, `road` |
| `E3128–E3228` | Ticked line crosses near `(265, 325)`, beside road strokes. | `railroad`, `road` |
| `E3228–E3329` | Ticked line crosses near `(345, 220)` before the crowded junction. | `railroad`, `road` |

All seven literal pinned pairs have `side: null` and `up: null`. Rail and road overlap at the junction in `E3329`; no onward crossing is assigned. This review does not resolve the previously uncertain `E2529` vertex or establish route service, control, occupancy, or other side features.

Reproduced the batch crop with `pdftoppm` at 200 dpi and PIL using its stated coordinates. SHA-256: PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; full crop `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`; detail `3320df8825718c8cfb548433ae549e4509da7f7bd2e24b7e179a35ae7c9daaa9`; [pinned hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PNG hashes depend on renderer and image library. No runtime or map-data changes; no gameplay tests.

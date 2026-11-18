# Gazala road: bounded source discrepancy candidate

**November 18, 2026.** Original Map B shows a solid road crossing the interior of `B4933|B4934`; the pinned v1 sides file contains no entry involving either cell. The [independent review](2026-11-18-graziani-gazala-road-candidate-review.md) also finds no `B4934` entry in pinned hexes. This is a candidate positive observation for a separately reviewed, versioned map, not an admitted correction or route proof. The existing [landing-strip reading and informed review](2026-11-18-graziani-landing-strip-edges-a-review.md) used this same scan; this check is not a second edition or blind reading.

## Source and local markers

Viewed [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 2, and its p. 1 key as described in the prior review. In the unrotated 600-dpi crop, the printed `4933` cell contains the blue Gazala village dot and label, with blue sea/coast artwork nearby. Its right vertical shared side with `4934` is crossed well away from either endpoint by paired continuous dark strokes, matching the key's solid road symbol. The strokes continue right across `4934` to the printed sheet edge; that visible end is a **sheet-edge lead**, not an established continuation or alias on Map C. The opposite approach bends through the crowded lower junction under the label; adjacent `B4932`, `B4832`, and `B4833` side assignments remain uncertain. The coast and yellow coastal artwork near the upper junction do not establish all side features or their legal effects.

[Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 4 / printed p. 28, §60.5, lists **Gazala B4933** under Air Landing Strips. It states facilities may be used by whoever controls them, and initially assigns facilities in Egypt to Commonwealth and in Libya to Italy. This separately identifies a scenario facility; the village dot itself is not a landing-strip symbol, and the list does not certify road connectivity, present control, capacity, or aircraft placement. Country identification and later control remain separate checks.

Pinned [hexes](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) record `B4933` as clear, village, coastal. Literal inspection of [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) found no record mentioning `B4933` or `B4934`; no record means unrepresented, not a source-certified empty side. These files cite `vassal:CNAv2.1.0`; comparison against the printed scan is not independent edition verification.

## Conditional admission and consumers

Consider a road-positive record on unordered `B4933|B4934` only after source provenance, rights, endpoint identity, B/C seam handling and side semantics are reviewed; the absent pinned `B4934` hex means this is not just an edge insertion. Preserve v1 bytes and identity for existing replays; use a new map/content identity for any admitted change. Following the [priority correction handoff](2026-11-17-graziani-priority-map-corrections.md), represent this as a **partial positive** only if all other feature membership remains unknown. If encoding or consumers interpret an omitted feature as absent, defer until versioned coverage can preserve unknowns. Do not invent adjacent sides, coastal-side membership, a Map B/C seam connection, or a complete road route.

The [terrain transcription policy research](2026-11-17-graziani-terrain-transcription-policy.md) notes Land §8.33 requires connected road/track hexes and a connecting side for its movement benefit, with other restrictions. A single positive side alone does not determine movement, supply, route reachability, weather, occupancy, control, ZOC, or legal timing. Before coding, review affected map and route consumers and authority/privacy boundaries; test both orientations, road-positive with other features unknown, unknown versus clear, old/new identity and replay, and affected consumers. Run relevant Core and boundary suites; disclose unrun coverage. These are documented admission requirements, not claims about current implementation.

## Reproduction and identity

```sh
pdftoppm -f 2 -l 2 -r 600 -x 11180 -y 3860 -W 2000 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gazala-review-600
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `254cbb508b043794550ef1b96d425aaaba83ad26f820be498bd1567fa56e5c49`; scenario PDF `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; viewed scenario page image `e2609e6d7f960e27846ccadbf7304079622fee12a376d5d30919600a9b7cc35f`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Rendered bytes depend on renderer. Hashes identify sources, not rights. Scans and crops remain outside Git. No map/runtime changes or gameplay tests.

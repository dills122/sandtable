# Commonwealth air supply: division-row scope

**Date:** November 17, 2026. **Disposition:** §32.37 supports a shell-inclusive western boundary, but the Matruh-to-Alexandria candidate remains conditional. A destination in the boundary row is not *farther west* than that row; a destination farther west fails the printed restriction. Map-sheet registration, whether a given HQ represents an occupying division, and the actual Organization Phase snapshot still need resolution. This is not a complete shipment witness.

## Source and comparison

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 43, §32.37. It prohibits a supply unit from being transported farther west, for Commonwealth, than the westernmost hexrow occupied by a friendly division, “shell or full.” Separately, the destination hex must contain a friendly division or brigade headquarters counter, not a battle-group HQ. A qualifying HQ or detached subordinate alone does not prove that a division occupies its row: §32.37 does not define how to locate a dispersed division or exclude every subordinate as evidence. Resolve formation occupancy using an applicable source or ruling. The boundary rule does not require the two non-shell divisions used by the separate §32.66 convoy chart. Do not substitute that chart's qualifying-pair predicate.

For an explicitly ordered west-to-east row coordinate `x`, let `W = min(x)` across all actual friendly divisions that occupy represented rows, including shells and full divisions. Under the destination-boundary reading, destination row `d >= W` passes this one restriction; `d < W` fails. Equality means same **hexrow**, not necessarily same hex. This algebra assumes a valid map-wide ordering and a source-supported division-occupancy interpretation. Section 32.37 does not specify an empty-set result or how to locate a dispersed formation; do not manufacture a boundary from a brigade, a name on a setup list, or a guessed position. With no established eligible division or unresolved placement, return unknown/pending ruling rather than silently allowing transport or asserting a source-specified rejection. This check alone does not authorize an endpoint or shipment.

Synthetic cases, with all other requirements held aside and increasing coordinates eastward:

| Friendly division rows | Destination | Boundary result | Reason |
|---|---:|---|---|
| Full 10 | 9 | Fail | Farther west than row 10. |
| Full 10 | 10 | Pass | Same row, even in a different hex. |
| Full 10 | 11 | Pass | East of boundary. |
| Shell 8, full 10 | 8 | Pass | Shell establishes westernmost boundary. |
| Shell 8, full 10 | 7 | Fail | West of both divisions. |
| Brigade HQ 8; only established division row 10 | 9 | Conditional fail | Fails if an accepted occupancy interpretation establishes no division at or west of 9; HQ alone does not settle that question. |
| Division location unknown or no established division | 10 | Unknown | No defined `W`; absence policy requires ruling. |

These examples address the destination against an extreme row, not a distance, a route, or travel through intervening hexes. The printed phrase describes the unit's transport; whether an origin already west of `W` can be moved eastward to a destination still west of `W`, and off-map box ordering or a division outside represented map scope, need an explicit interpretation before testing those cases as rules. Do not assign artificial zero/infinite values to boxes or unresolved sheets. Record both origin and destination and any adopted policy.

## Conditional candidate

[Scenario §60.41](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, places 7th Armored Division at D3612, 4th Indian Division at D3615, and 2nd New Zealand Division HQ among forces at Alexandria E3613 and/or E3714. Those are setup entries, not proof of the state at the actual transport time. New Zealand HQ's exact hex is not chosen; its presence as a qualifying destination HQ is separate from whether it establishes a division occupying the boundary row. Under an accepted interpretation treating that actual division as occupying its HQ row, a destination in that same hex necessarily satisfies the boundary even if it is a shell: the westernmost row is at least as far west as that division. This deduction does not require identifying every more-westerly division. It does require an actual friendly division at the chosen destination and an actual qualifying HQ at transport time. An independently established division farther west also suffices if its row is demonstrably at or west of destination.

Alternatively, under the **unverified** D-relative arithmetic `Dxxcc = cc`, `Exxcc = 33 + cc`, a surviving 7th Armored division at D3612 gives `W <= 12`, while Alexandria E3613 and E3714 have rows 46 and 47, respectively; both pass the boundary. Shell status does not change this result. This is an illustrative conditional computation, not certification of D/E registration, cross-sheet physical correspondence, division occupancy, or survival to the phase. The [D/E seam review](2026-11-17-graziani-continuation-de-seam-review.md) leaves registration unresolved. No particular destination hex, HQ placement, formation state, prior movement, or startup sequence is selected here.

The [witness review](2026-11-17-graziani-air-supply-witness-review.md) also requires actual cargo and payer balances, origin enemy-distance check, destination city status and capacity, timing and unused allowance, plus stage lock. Passing the division-row condition alone cannot establish legal Matruh D3714 to Alexandria transport. In particular, no conclusion about §32.14's disputed on-map stacking follows.

**Provenance:** Visual inspection of local Land and scenario scans, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. Same scan family as prior review; no independent edition, exhaustive errata survey, map registration proof, runtime/data change, or gameplay test.

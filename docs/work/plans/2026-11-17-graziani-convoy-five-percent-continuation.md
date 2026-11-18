# Graziani 5% convoy: six-turn search reduction

**Date:** November 17, 2026. **Disposition:** Reachability remains unknown. The source's two-division rule reduces a candidate position search to an order statistic, but neither a legal six-turn witness nor an impossibility proof follows. The numerical bands below are explicitly conditional on the same illustrative threshold and map assumptions as the [reviewed reachability note](2026-11-17-graziani-five-percent-reachability-review.md); they do not decide either assumption.

## Source-based reduction

[Axis chart §32.66](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF p. 34, defines a qualifying division as a non-shell Allied division east of, or in the same east–west hexrow as, at least one **other** non-shell Allied division. Given a complete, ordered snapshot of distinct non-shell Allied divisions, order their east–west positions west to east, retaining ties. If fewer than two exist, no division meets this condition. Otherwise, the **second position in that multiset** is the westernmost qualifying position: the first division has another at or west of it only when tied, and every division at or east of the second has another at or west of it. Ties count twice only for two distinct divisions. This is a deduction from the qualification predicate, not a ruling on how to compare positions across sheets or how chart thresholds work.

For example, positions `[10, 20, 30]` yield westernmost qualifying position `20`; `[10, 10, 30]` yield `10`. Removing a shell from either set requires re-sorting the remaining non-shell divisions. A third division farther west can move the second position west even if the previously considered Indian–Australian pair does not move. Thus examining only a named pair cannot establish the chart row when another non-shell division may exist. Conversely, a witness need not make every division enter the target band: it must establish the second position and the full relevant set at the convoy snapshot.

## Conditional bounds for the printed September routes

Scenario §60.37 limits the **preplanned remainder of September** to lanes 2, 3 and 6; §60.22 ends at GT6 OpStage 3. This limitation is not independently extended to October. The chart's 21–40 and following 41–80 thresholds are respectively `Dxx01` / `Cxx16` on route 2, `Dxx01` / `Cxx21` on route 3, and `Dxx11` / `Dxx01` on route 6. These are source-printed coordinates, not certified movement paths.

For a bounded search, **suppose** “beyond” means strictly west, read-down selects the westernmost threshold having at least one qualifying division strictly west, and use the illustrative coordinate transform `Ccol = col`, `Dcol = 33 + col`. Under those hypotheses, let `s` be the second-westernmost non-shell Allied position, counting ties. The chart selects 21–40 precisely when `s` is strictly west of the first threshold and not strictly west of the next. At equality with the next threshold, this hypothetical strict policy stays in the 21–40 row; a different equality policy changes the boundary.

| September lane | Conditional constraint on `s` | Same hypothetical numeric index |
|---|---|---|
| 2 | `Cxx16 ≤ s < Dxx01` | `16 ≤ s < 34` |
| 3 | `Cxx21 ≤ s < Dxx01` | `21 ≤ s < 34` |
| 6 | `Dxx01 ≤ s < Dxx11` | `34 ≤ s < 44` |

These are **necessary and sufficient only for the 21–40 chart row under the stated hypotheses and a complete ordered snapshot**, not for a legal game event. Since positions are integers under this illustration, the respective bands are `16–33`, `21–33`, and `34–43`. Route 3's band is a strict subset of route 2's; on an identical snapshot both select 21–40 when `s` is in `21–33`. Route 6's band is disjoint from both under the chosen transform. Routes and cargo remain separate player choices; this comparison does not create a convoy or mandate an attack. No inference about crossing a sheet seam, exact physical overlap, or lawful setup or movement follows from these indexes.

A candidate in any band requires at least two distinct non-shell divisions strictly west of the lane's first threshold: the second one and another at or west of it. It also requires **at most one** non-shell division strictly west of the next threshold; two would place `s` beyond that next threshold and select a later row under the hypothetical policy. This count formulation permits an efficient search without assuming which division is the comparator and correctly handles same-row ties. Unknown position or shell status prevents an exact count and must not be silently excluded.

## Witness and negative-proof gates

A positive six-turn witness still needs an accepted coordinate/threshold policy; legally established division positions and attachments at an actual arrival, including all other relevant divisions; lawful movement and training history from scenario setup; a permitted planned nonempty convoy and lane at that same snapshot; Commonwealth attack; and sequential dice `55–64` for the common chart's 5% cell. The scenario starts at GT1 OpStage 1, while the normal sequence's preceding Naval Convoy Stage requires a separate startup decision; this analysis does not choose one. A later event needs its actual later state. Calculating physical losses separately requires the unresolved 5% allocation and rounding policy.

A negative proof must cover every lawful snapshot and convoy opportunity through GT6 OpStage 3, including October routes if allowed by the accepted rules, all division identities, shell changes, placements, movements and threshold cases. Showing one conditional pair outside a band or failing to find a route does not prove exclusion. Useful next evidence is a complete source-backed initial non-shell roster and bounded formation movement under a separately accepted map and startup contract; if either prerequisite remains unknown, return unknown rather than manufacture a transition.

## Provenance and scope

Visually checked hash-matched [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF p. 34 §32.66, SHA-256 `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`, and [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27 §§60.22, 60.37 and 60.41, SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. The [prior review](2026-11-17-graziani-five-percent-reachability-review.md) supplies common-chart, loss-policy and startup source checks. This is the same scanned edition, not an independent edition. No gameplay tests, runtime or source-data changes.

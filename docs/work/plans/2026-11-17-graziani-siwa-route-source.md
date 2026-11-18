# Siwa C0127: local convoy-route source leads

**Date:** November 17, 2026. **Status:** local map reading and research queue; no route, legal move, map correction, or victory result certified.

Scenario §60.81 Italian decisive names **both** Mersa Matruh and Siwa, with a combat unit that can be supplied by convoy **from Tobruk**. The scenario's §60.5 identifies Siwa C0127, Matruh D3714 and Tobruk C4807. Whether and how to bind a qualifying unit and supply to each place, and the directed convoy test, remain policy questions. See [endpoint review](2026-11-17-graziani-matruh-route-endpoints-review.md) and [direction review](2026-11-17-graziani-victory-convoy-direction-review.md). A Matruh witness cannot settle a separate Siwa obligation under the proposed per-objective reading. Neither a road nor an actual delivery is expressly selected here.

## Six sides of Siwa

Visually inspected [original Map C and key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1. The lower hex's partly overprinted `0127` label, surrounding grid and blue palm oasis identify Siwa. The key distinguishes single dashed track and paired dashed unfinished road. Below, image left/right follow the rendered sheet. Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) are sparse VASSAL-derived pair records, not independent original-sheet evidence. No entry does not establish a clear side.

| Side | Local observation | Pinned pair |
| --- | --- | --- |
| Upper-left, C0226 | Single dashed line crosses side and runs into oasis artwork; approaches west through C0226. | `track` |
| Upper-right, C0227 | Paired dashed line and single dashed line cross side close together, partly obscured by palm artwork; latter curves east through C0227. | `unfinished-road`, `track` |
| Left, C0126 | Shared line visible with no obvious route crossing; nearby interior has desert fill. | No explicit pair |
| Right, C0128 | Shared line visible with no obvious route crossing; neighboring salt-marsh pattern approaches it. | No explicit pair |
| Lower-left | Hex boundary exposed against blank sheet margin; no adjoining printed cell. | No pair |
| Lower-right | Hex boundary exposed against blank sheet margin; no adjoining printed cell. | No pair |

Pinned [hexes](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) describe C0127 as clear oasis, C0126 as desert and C0128 as salt marsh. This is a local feature comparison, not a complete classification of each side or proof that a truck may cross. Siwa is at the southern printed edge: blank margin is not a certified adjacent off-map hex, terrain, or a permitted detour. Confirm applicable board-limit rules and any other sheet coverage before using or excluding outside continuations. The upper-left track is a western branch; the upper-right has northward unfinished-road and eastward track leads. Their detailed junctions and onward connections require separate six-side examination. Do not infer that either reaches Tobruk or that marked routes exhaust off-road possibilities. Under Land §8.47 unconstructed road functions as track; whether that rule applies to a proposed hypothetical convoy requires the separately selected convoy policy.

## Next evidence

1. Independently check the C0226 and C0227 sides and junctions, including line crossings under lettering and oasis ink, then follow every relevant branch and possible off-road continuation under an accepted movement policy. Preserve unresolved crossings as unknown.
2. Survey connecting terrain, escarpments, crossings and state between the local leads and Tobruk C4807. For any candidate involving the C/D seam, resolve sheet identity and adjacency rather than joining similar edge marks; the [independent seam reading](2026-11-17-graziani-seam-cd-independent.md) reports a candidate overlap, not accepted crossings. A seam is not asserted as necessary for every Siwa–Tobruk candidate.
3. A positive witness needs ordered adjacent sides, applicable features and legal state from one snapshot. A negative result needs all eligible continuations and relevant outside coverage under an accepted finite domain; missing pair records or one blocked branch do not establish failure. Source control, direction, weather, occupancy, ZOC, range and whether hypothetical trace or shipment is required remain separate gates.

## Reproduction and limits

```sh
pdftoppm -f 3 -l 3 -r 600 -x 9200 -y 19300 -W 2500 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/siwa-detail
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `9a801bc94aa8d4ceca1f1635d0aa542a3d0c2520cb7a308ded329c65d6553004` (renderer-dependent). Scenario PDF `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; pinned sides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Scenario and Land descriptions rely on linked same-scan reviews, not a new independent edition. Map observation uses one scan, not a physical sheet. No runtime change or gameplay test.

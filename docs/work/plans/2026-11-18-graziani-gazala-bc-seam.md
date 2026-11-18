# Gazala B/C seam: endpoint evidence

**November 18, 2026.** Under the existing *hypothetical* 33-column translation, printed `B4934` and `C4901` occupy the same candidate position; `B4933` lies one column west. This arithmetic identifies `C4901` as the next cell to investigate, **not an admitted alias, adjacency, or replacement endpoint**. The [Gazala road review](2026-11-18-graziani-gazala-road-candidate-review.md) establishes a road crossing on Map B and a missing `B4934` pin record; it does not establish B/C registration. No map or runtime change follows.

## Source landmarks

Compared the original [map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), pages 2 and 3, using the same scan as the previous reviews. On Map B, `4933` contains the Gazala dot, and paired solid road strokes cross the vertical `4933|4934` side and extend through `4934` to its right edge. Blue coastal artwork enters the upper part of `4934`, and a dark band touches its lower right. Nearby `5033` is sea; `5134` is printed farther above. The scan does not show a `B5034` label: do not substitute it for `B4934`.

On Map C, `4901` is the first visible coastal land cell at the clipped left margin, with paired solid strokes entering from that margin and continuing east through the coastal strip. `5001` immediately above is sea; `4801` below is land with a dark band along its upper-left area. Farther east, the same visible road corridor approaches Tobruk; the printed grid and prior [Tobruk review](2026-11-17-graziani-c4807-six-sides-independent-review.md) locate `C4807`, independently identified as a major city and port by the map's location table. This is an orientation landmark, not proof of a complete route from Gazala or an exact sheet join.

The two coastal cells have road and dark-band motifs, consistent with the earlier [seam inventory](2026-11-17-graziani-seam-inventory.md) and [identity pilot](2026-11-12-graziani-map-identity-pilot.md#potential-map-seam-conflict). Their ink is not an unambiguous overlay: Map B shows a rounded coastal indentation and nearly horizontal road across `4934`; Map C's margin clips the road approach and nearby coast, with the road bending near the edge. No measured shared-vertex registration or uniquely matched feature establishes whether these depict the same cell, adjacent cells, or differently rendered material. Neighboring yellow terrain and dashed route leads southward are broad similarities, not independent identity proof. Land §4.1's printed coordinate wording also remains in conflict with the proposed interpretation; the same-number distance rule does not by itself prove a translation.

## Pin and disposition

Pinned [hexes](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) contain `B4933` as clear, village, coastal and `C4901` as clear, coastal, without settlement; `B4934` is absent. The printed Gazala dot is in `B4933`, so the `C4901` settlement field does not decide the proposed `B4934` correspondence. Pinned `C4807` is major-city and coastal. These records cite `vassal:CNAv2.1.0`; this source comparison does not establish edition equivalence. Missing IDs and sides mean unrepresented, not source-certified absence.

Retain the B road-positive observation separately from endpoint representation and other unknown side features. Before proposing admission, compare original full sheet edges or an independent physical/edition source, register several grid vertices and distinctive coast, road and terrain marks, reconcile clipped portions, source rules and edition provenance, and establish the pin's representation policy. A confirmed geometric correspondence alone would not settle all side features or legal movement, supply, control, weather, occupancy, ZOC or timing. Preserve v1 replay identity; use a separately reviewed version if evidence and representation support a correction.

## Reproduction

Viewed `/private/tmp/gazala-review-600.png` using the command in the [candidate](2026-11-18-graziani-gazala-road-candidate.md), and rendered Map C with:

```sh
pdftoppm -f 3 -l 3 -r 300 -x 0 -y 1750 -W 1800 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gazala-c-seam
```

SHA-256: map PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; B crop `254cbb508b043794550ef1b96d425aaaba83ad26f820be498bd1567fa56e5c49`; C crop `65dd235a298ff7053764101a6a6dd86c504c43880907c8ff9d6cb8ac1773e2c4`; pinned hexes `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Renderer affects crop bytes; hashes identify files, not rights or edition equivalence. No runtime/data changes or gameplay tests.

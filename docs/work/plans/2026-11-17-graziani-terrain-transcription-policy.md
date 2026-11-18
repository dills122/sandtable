# Graziani terrain transcription: artwork across hex boundaries

**Disposition:** No general whole-hex assignment threshold was found in the bounded printed material inspected below. The map key identifies the artwork and the rules attach effects to a terrain *hex*, but neither inspected source says that any intrusion, the center point, or a majority of a cell determines its terrain. Treat those thresholds as candidate project rulings, not printed rules. Keep C0818's pinned `clear` record unchanged pending an accepted policy and a source review; its visible salt-marsh artwork is a discrepancy candidate, not yet an admitted correction.

## Source evidence

- [Original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1 terrain key, distinguishes blank Clear, yellow polygonal Salt Marsh, yellow Desert, and blue palm-and-water Oasis; it depicts ridge, escarpment, slope and wadi along sides. PDF p. 3 Map C: salt-marsh polygons visibly enter C0818 from adjoining marsh, while blue Giarabub Oasis lettering obscures part of the cell. Artwork presence does not establish its share of the cell or its center classification. See [radius data review](2026-11-17-graziani-radius-data-review.md) and [same-scan independent check](2026-11-17-graziani-radius-data-review-independent.md). The key's sample drawing is illustrative; it gives no numerical coverage threshold.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed p. 14 §§8.31–.37, distinguish costs to enter hexes from costs to cross sides. §8.35 explicitly places slope/escarpment splash contours on the **down** side; ridge has no direction. That rule interprets directional *side* symbols and does not define area-terrain assignment. §8.33 requires connected road/track hexes and a connecting side for its movement benefit; exceptions and restrictions still apply.
- Land printed p. 15 §8.44 describes salt-marsh movement, abandonment and assault restrictions; §8.48 says oases do not affect movement. [Terrain Effects Chart](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 23, §8.37, lists Clear and Salt Marsh separately: respectively non-motorized entry 2 vs 3 CP, motorized 2 vs 2 CP, breakdown 4 vs 6, and close-assault shift none vs R1; both list stacking limit 6. Village/Bir/Oasis uses underlying terrain for all purposes. Chart footnotes and §8.44 impose vehicle and route restrictions; an overlay alone does not replace the underlying terrain.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), p. 1 §8.37, corrects the track entry: use half the terrain cost instead of the chart's printed 1 CP, and moves footnote 4 to Major City. It gives a Map D side correction, not an identified area-coverage rule. This check covers map key, Land §§4.1, 8.3–8.4, the chart and errata p. 1; it is not an exhaustive search of later editions, clarifications, all errata or designer rulings.

## Candidate assignment policies

| Candidate | Operational meaning | Consequence and unresolved issue |
| --- | --- | --- |
| Any area intrusion | Assign a terrain type whenever discernible interior artwork extends across a side into the cell. Ignore boundary-only contact. | Would classify visible marsh in C0818 as marsh if accepted; small print bleed and competing terrain can trigger multiple types. Requires minimum evidence, tolerance and precedence. No inspected source prescribes this rule. |
| Center | Assign type covering the geometric center. | Reproducible with clear, registered artwork; lettering, symbols and boundary-on-center cases require unknown/tie handling. C0818 center is not established by this check. No inspected source prescribes it. |
| Majority or plurality | Assign type with greatest estimated cell coverage; define whether blank means clear and specify ties. | Limits sliver effects but scan/printing and overprint make accurate areas difficult; C0818 area was not measured. No inspected source prescribes it. |
| Evidence-gated manual adjudication | Preserve visible marks and uncertainty; admit a single whole-hex terrain only when a versioned, reviewed case-specific ruling supplies sufficient evidence. | Avoids a silent global threshold; delays legal consequences for disputed cells. Recommend this conservative interim policy, without declaring the pinned `clear` source-correct. |

Each candidate must address multiple interior types, overprints, boundary-only contact, print registration/bleed, symbols on shared edges and missing imagery. Settlement symbols and true side features require separate classification; a name spanning cells is not a settlement locator, and splash artwork entering a neighboring cell is not automatically area terrain. Unknown is not clear. No side, route, country, ZOC, setup or legal movement is established by this proposal.

## Admission and test gates

1. Seek clearer original printing, official terrain assignment rule or specific ruling; record edition, page and exact coverage language. Independently inspect C0818 and nearby threshold examples at reproducible resolution, marking hex vertices, obscuration and any measured areas. Check upstream transcription conventions and corrections, without treating VASSAL-derived data as independent confirmation of the original.
2. Choose and version an explicit convention or bounded case ruling, including ties, overlays, uncertainty and conflicts. Compare C0818, C0613–C0618 desert boundaries and C0814/C0914/C1014 side-artwork cases; document both disagreements and retained records. Recheck settlement separately. If evidence remains insufficient, retain an explicit unresolved blocker and do not infer clearance from existing data.
3. Before any correction, bind new map bytes and policy to a new identity, retain v1 for replay, and review affected consumers and source-world gates. Test stable hashes, old replay, new policy binding, unknown/fail-closed behavior, positive and negative threshold examples, overprint/tie handling and overlay/hexside independence. Add focused deterministic movement, breakdown, combat and setup tests only for admitted rules and their affected consumers, including road/track exception and September errata; validate source-specific unit eligibility independently. Reassess reachable impacts and run affected Core and boundary suites. No runtime changes, source-map correction or gameplay tests are made here.

## Reproduction and provenance

Map crop used in the earlier review, independently viewed here:

```sh
pdftoppm -f 3 -l 3 -scale-to 5000 -x 1000 -y 3950 -W 1000 -H 700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-radius-crop
```

Source SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; Air/chart `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Hashes identify inspected scans, not independent editions or rights grants. Pinned data provenance and replay limits: [historical map transcription](../../specs/graziani-historical-map-transcription.md).

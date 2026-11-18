# Fort Maddalena: fixed starting pair and border side

**Finding:** Scenario-listed Fort Maddalena Garrison C3019 and 11th Hussars C3020 occupy adjacent hexes across a blue checkered side in original Map C. The side appears free of an escarpment band, but the blue mark has unresolved independent rules effects. A geographic reading of the line places the Italian fort west and the Commonwealth unit east of the Libya–Egypt boundary. Adjacency does not prove either unit qualifies to project ZOC, an attack obligation or legal final setup.

## Source evidence

Reverified [map](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. Inspected map PDF p. 3 and terrain key p. 1, scenario PDF p. 3 / printed p. 27 §§60.31 and 60.41, and Land p. 18 §10.21. Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c` was compared after source observation.

Scenario §60.31 places Fort Maddelena Garrison at C3019. Section 60.41 places 11th Hussars, R, 7Spt/7, with three medium trucks at C3020. Both are printed starting entries, not proof of final player setup or current unit strength.

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5400 -y 8600 -W 1900 -H 1650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-fort-contact/map
```

PNG SHA-256 `48a1dc731acd9137459c79e015e2f21984e8118b55378d079da18f0bac64bc2a`; source hash and crop parameters are primary, renderer bytes can vary. Image and scan remain outside Git. This targeted reading used prior [boundary source analysis](2026-11-17-graziani-border-mark-next.md), not a blind source entry or independent edition.

## Shared side and limits

In the crop, the blue Fort Maddelena dot lies in printed `3019` at approximately `(560,490)`; printed `3020` lies immediately east. Their shared vertical side runs approximately `(745,390)`–`(745,570)`, bearing blue alternating blocks. Its neighboring outlines and labels identify both cells without a seam assumption. The map key's escarpment is a thick dark band with a splash edge; no such band is visibly present on this side. The pinned data contains no literal `C3019|C3020` feature record, so it does not preserve this visible mark. No source-certified feature-free classification follows: the side is visibly marked and endpoint detail is crowded.

The checkered line can be traced south/north through the broader Map C context to the country-label area. The [prior country-label crop](2026-11-17-graziani-border-mark-next.md) shows `LIBYA` to its west and `EGYPT` east; the key's gray `Border` sample does not directly reproduce blue checks. Under that geographic interpretation C3019 is on the west/Libya side and C3020 the east/Egypt side. This is a source-based interpretation rather than an official annotation or a complete region census. An independent boundary/movement rule and any special scenario restrictions still need review.

Land §10.21(b) denies ZOC through escarpments; §10.21(c) separately requires the projecting unit to be able to enter the target. No escarpment band is seen, but the marked side, unit eligibility, entry and other rules preclude claiming positive ZOC. Do not convert a visible political-boundary interpretation into an automatic ZOC or movement prohibition. Verify each actual stack's organization, raw defense, cohesion, setup, phase and §§10.31–.32 before any initial compulsory-combat conclusion; other player-placed forces may matter.

Independent review should check coordinate labels, boundary location and shared-side features at higher resolution, then separately inspect both units' counters and OA strengths. Proposed tests: adjacency; preserve the blue mark and unknown rule status; evaluate opposing directions with qualified/unqualified stacks and known/unknown edge; region placement and scenario-explicit entries; and phasing combat obligations once accepted. No runtime, content, readiness or gameplay tests changed.

# Bir el Sheferzen initial contact: independent review

**Disposition:** corroborate the [contact lead](2026-11-17-graziani-map-sheferzen-initial-contact.md): the printed scenario locates opposing forces in adjacent C3419 and C3320. The shared side appears as a thin grid line without an obvious escarpment band; overprinted name and vertex prevent a complete clear-side certification. Adjacency alone does not prove either directional ZOC, compulsory combat, legal final setup or a historical outcome.

## Source check

[Scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27: §60.31 places Sheferzen Garrison at Bir Scheferzen C3419; §60.41 places the 2nd Rifle Brigade at C3320, with ten medium trucks. The slightly different spelling of the location on Map C does not establish a different place.

Re-rendered [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, at 600 dpi. Printed 3419 lies in the town-dot hex and 3320 in the immediately southeastern hex. Their visible common diagonal runs northeast from the junction at the left of the `el Sheferzen` text to the lower junction at the right of `Bir`; the grid line appears narrow where visible. Blue place-name ink crosses and partly obscures the diagonal and one endpoint. No comparable broad escarpment symbol is visible on the exposed parts; that observation is not a proof of absence beneath ink or of other side features. Blue checkered line and dashed track are nearby, but this reading does not certify their exact side and endpoint effects. The pinned `hexsides.v1.json` contains no literal `C3320|C3419` record; absence of a record is not source evidence of a clear edge.

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) PDF/printed pp. 17–18: §§10.11 and 10.15 constrain ZOC projection by unit/stack characteristics and raw defensive close-assault strength; §10.21(b–c) also restricts projection through escarpment and into hexes the projecting unit cannot enter. Section 10.31 concerns enemy units exerting ZOC on friendly combat units, while §10.32 supplies specified exceptions. Assess each direction against accepted starting composition, applicable setup choices, strength, terrain, timing and rules; a printed pair of neighboring counters is not itself proof of either projection or an attack. Full setup and per-side map/control coverage remain open.

## Reproduction and limits

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/sandtable-scenarios.pdf /private/tmp/sandtable-land-rules.pdf src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
pdftoppm -f 3 -l 3 -r 600 -x 6700 -y 9050 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-sheferzen-review
```

SHA-256: map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. This is an informed same-scan check, not a blind reading or independent edition; hashes do not grant rights. No runtime, readiness or gameplay tests changed.

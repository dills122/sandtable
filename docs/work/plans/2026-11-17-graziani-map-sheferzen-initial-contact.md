# Bir el Sheferzen: adjacent fixed starting forces

**Finding:** The scenario's Italian Sheferzen Garrison at C3419 and Commonwealth 2nd Rifle Brigade at C3320 are in adjacent printed map hexes. This establishes a fixed opposing setup pair that warrants initial-contact adjudication, unlike the nonadjacent C3920/C3922 pair. It does **not** prove either unit exerts ZOC, a compulsory attack, setup legality or actual combat. The shared diagonal appears to be a thin grid line without a visible escarpment, but settlement text overlaps it; do not treat the edge as comprehensively certified clear.

## Original evidence

Visually checked [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, [original map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map C and p. 1 key, and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 18, §§10.21 and 10.31–.32. SHA-256 verified before inspection:

- Scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
- Map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
- Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

Scenario §60.31 lists Bir Scheferzen (C3419), Sheferzen Garrison; §60.41 lists C3320, 2nd Rifle Brigade (I; 7Spt/7), with trucks. The printed starting locations are source facts, subject to other scenario and owner setup rules. The scenario's spelling differs slightly from the map's `Bir el Sheferzen`; no alternate location is inferred.

```sh
pdftoppm -f 3 -l 3 -r 300 -x 2850 -y 4000 -W 1500 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-contact-south/crop
pdftoppm -f 3 -l 3 -r 500 -x 5650 -y 7600 -W 1500 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-contact-south/detail
```

PNG hashes respectively `2bc19a4452604a4608240a2e7609241283f3e0cd78fdf10a837a465ea7d03327` and `3537b34255ebc13e296783e7882f42b33302ec0ee879730acdb1cb9068613331`. Renderer bytes can vary; coordinates and PDF hash identify the source. Crops stay outside Git. This targeted informed reading follows the earlier [Halfaya contact check](2026-11-17-graziani-initial-contact-followup.md), not a blind entry or new edition.

## Map and rule limits

The 500-dpi crop clearly prints `3419` by the Bir el Sheferzen dot and `3320` in the hex southeast. Their common diagonal runs approximately from `(335,580)` to `(490,490)` in that crop. The map key depicts an escarpment as a thick dark band with stippled down-side edge; the shared diagonal looks like a narrow grid line, not such a band. `Bir el Sheferzen` text covers part of this edge and its lower vertex. Blue checkered boundary marks and a dashed track occur nearby, but on other visible sides or interiors; endpoint overprint means no assertion of a completely feature-free edge. The pinned data has no explicit record for `C3320|C3419`, which is not independent proof of a clear side.

Land §10.21(b) bars ZOC through an escarpment and §10.21(c) independently requires the projecting unit to be able to enter the adjacent hex from its own position. The observed absence of an obvious escarpment is therefore not a positive ZOC certificate. Land §10.31 concerns enemy units exerting ZOC on friendly combat units; §10.32 and other rules give exceptions. Whether these starting forces satisfy applicable strength, cohesion, organization, entry, phase and other requirements requires separate source and state checks. The garrison name alone does not establish its combat strength or ability to project ZOC. Other units and player placements may change results.

Independent review should confirm source coordinates and shared side, trace its full length despite lettering, and determine each stack's actual accepted initial composition and ZOC qualification. Focused tests should exercise adjacent geometry, uncertain-edge behavior, each directional projection, phasing and applicable §10.31/.32 consequences under the accepted state; keep unverified facts unknown. No runtime, content, readiness or gameplay tests changed.

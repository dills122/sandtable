# Printed distance example: independent source review

**Date:** November 18, 2026. Rechecked the [distance-example note](2026-11-18-graziani-map-registration-source-next.md) against the same original Air/Logistics, Land and map scans. **Disposition:** the unusual zero wording and printed arithmetic are accurately transcribed. The worked example supplies directional counting evidence, not a seam registration or corrected global distance algorithm.

- Air/Logistics PDF p. 24, “Determining Shortest Distance,” Example 2, literally prints **“zero hexes northwest to E3425”** immediately after its `E2629` term. The example then gives 8 to `E3433`, 9 east to Ismailia and total 149. Its four preceding 33s give 132; `132 + 0 + 8 + 9 = 149`. Air p. 25 chart §37.42 names Agedabia `A2629`. This is an internally correct sum of the *printed* terms, not a verification of their geometry.
- The example says “East 33 hexes to B2629” and then adds three more 33-hex steps through `C2629`, `D2629` and `E2629`. Reading those successive terms as continuing east is supported by the sequence and Land §4.1's A-to-E arrangement; the word “East” is printed only once before the first step, not repeated before every term. This supports the proposed directional counting interpretation without converting a distance to a unique seam alias or ground connection.
- On Map E, `2629` and `3425` are separately printed in distinct visible cells, with intervening rows and hexes. The map therefore strengthens the note's ordinary-reading qualification: zero does not represent a normal positive separation between those two identified printed cells. Their map positions do not independently specify whether the example's number, locator, intended starting point or some other text is mistaken. Do not replace the zero with an invented correction or treat the whole example as a geometric oracle.
- Air §36.53 on p. 24 lists Ismailia off map with entry/exit `E3433` and distance 9, matching the final leg. Section 37.42's chart footnote separately says same-numbered positions on adjacent sheets are 33 hexes apart and explains its inclusive/exclusive distance convention. Neither passage proves a road, rail, edge traversal or sheet registration. Land §4.1's literal `01xx`/`39xx` overlay wording remains unreconciled with the physical-edge evidence.

For a reproducible map view at the embedded 300-ppi scale:

```sh
pdftoppm -f 5 -l 5 -r 300 -x 4500 -y 4500 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/registration-delta-review
```

A scoped exact-string search of `docs/work/plans/*.md` for `E2629`, `E3425`, `zero hexes northwest` and `Agedabia to Ismailia` found only the reviewed note at review time. Other existing notes do document Agedabia, Ismailia and the §37.42 footnote separately; this search does not establish that no different transcription or reference exists elsewhere. The note's example locator is consequently a newly indexed local finding within that search scope, not a new edition or newly discovered map.

Reproduced the bounded text extraction search of September errata for `37.4`, `2629`, `3425`, `Ismailia`, `Agedabia`, `shortest`, `distance` and `149`; all returned zero matches. This checks extracted text only, not all visual, indirect or later corrections.

[Air/Logistics](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf) SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; [map](https://spigames.net/PDFv10/CNA_Maps.pdf) `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. All hashes checked. Same scans, not independent edition; no code/data edits or gameplay tests.

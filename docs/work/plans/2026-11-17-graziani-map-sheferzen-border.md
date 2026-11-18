# Sheferzen shared side and nearby national boundary

**Finding:** In the original Map C scan, C3419 and C3320 lie on the same, eastern side of the nearby blue checkered line; that line follows C3418/C3419's western vertical side, not their own shared diagonal. Interpreting the line as the Libya–Egypt boundary places both listed hex interiors on the Egypt side. This is an evidence-supported geographic interpretation, not a certified setup policy. The C3419/C3320 diagonal is a narrow grid line where visible, but blue place-name overprint conceals part of it and prevents complete clear-side certification.

## Sources and visual method

[Original map](https://spigames.net/PDFv10/CNA_Maps.pdf), Map C PDF p. 3 and terrain key p. 1; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27 §§60.31, 60.41; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 18 §10.21. SHA-256 verified immediately before inspection:

- Map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
- Scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
- Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.

```sh
pdftoppm -f 3 -l 3 -r 600 -x 6600 -y 9000 -W 2300 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/sheferzen-border/detail
pdftoppm -f 3 -l 3 -r 150 -x 700 -y 2850 -W 2100 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/sheferzen-border/context
```

PNG hashes respectively `f98260b5837166d7a34186152f22f6fb10c502c0973d1bda6565c59b9986f488`, `96242a8e0d55466588802f52c37030931c5b3e89ee3b3f8698e814e18559019b`. Source hash and crop parameters identify evidence; renderer versions can alter PNG bytes. Scans and crops remain outside Git. Prior [initial-contact check](2026-11-17-graziani-map-sheferzen-initial-contact.md), its [review](2026-11-17-graziani-map-sheferzen-initial-contact-review.md), and [boundary analysis](2026-11-17-graziani-border-mark-next.md) were known; this is an informed same-scan reinspection, not a blind reading or independent edition.

## Local side and mark

In the 600-dpi crop, printed `3419` is beside the Bir el Sheferzen dot; `3320` is in the adjacent southeast hex. Their shared diagonal runs approximately `(580,815)`–`(770,710)`, pixels within the crop. Visible sections look like ordinary narrow grid stroke, unlike the key's broad, irregular dark escarpment band. The name `Bir el Sheferzen` crosses the diagonal and its southwestern endpoint, so its hidden length and vertex are not independently verified feature-free. A dark dashed track passes through C3419 and across its southwest side toward C3319; another dashed route is in C3420 and farther east. Neither visible continuation proves a crossing of the C3419/C3320 diagonal. The blue checkered line is separated from that diagonal; it is not seen following its visible length. The key distinguishes a green wadi, blue river, cross-tied railway and dark escarpment; do not infer absence of any hidden feature from these visual distinctions.

The checkered line follows the **C3418/C3419** vertical side near `x=395`, `y=490–710`. It then continues southwest from that side's lower vertex and down the western side of C3319. C3419 and C3320 interiors lie east of the line, with C3320 beyond C3419. The country-label crop shows the same kind of line between large printed `LIBYA` west and `EGYPT` east. Its geographic identification is strong but interpretive: the key's `Border` symbol does not visibly reproduce the blue checks. No new country classification for other hexes, control ownership, or complete boundary census follows.

Scenario §60.31 expressly places the Italian Sheferzen Garrison at C3419 and separately lists some forces for placement `Anywhere in Libya`; §60.41 places 2nd Rifle Brigade at C3320. If the proposed geographic interpretation and a hex-interior region policy are accepted, C3419 and C3320 are Egypt-side and a Libya-only placement would not qualify merely by being adjacent to the line. The explicitly printed garrison placement is separate evidence and must not be erased by that geographic inference. Verify all applicable scenario exceptions, cell classification and individual unit permissions before concluding whether additional units can co-locate; geographic position does not establish final setup or unit ZOC. Land §10.21(b–c) distinguishes escarpment blocking from unit entry requirements; this inspection neither supplies an unconditional ZOC nor establishes a political-border movement rule.

Independent review should verify all three grid rays at the western endpoint, country-label continuity, and precise side identity. Keep hidden overprint, region admission and any coincident line features unknown until reviewed. Proposed tests: Libya-only placement on both sides of a source-accepted border; separate explicit C3419 scenario placement; diagonal ZOC with unknown versus accepted terrain; setup and disclosure with additional actual units. No runtime, source content, readiness or gameplay tests changed.

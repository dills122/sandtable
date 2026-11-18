# Halfaya–Sollum shared side: escarpment review lead

**Finding:** The source image raises a possible false-positive escarpment on the pinned `C3922|C4021` edge. The heavy escarpment bands visibly follow the two sides meeting at the western endpoint of that edge; the diagonal from that endpoint toward the coast appears as a thin grid line rather than a continuing band. The crowded vertex, labels, shoreline and dashed road require independent review before any correction. If confirmed, the discrepancy matters to the escarpment-specific ZOC block, but does not by itself prove positive ZOC or traversability.

## Source and reproduction

[Original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map C; original key PDF p. 1, and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 18 §10.21(b). SHA-256 verified before inspection:

- Map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
- Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- Pinned `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json` `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.

```sh
pdftoppm -f 3 -l 3 -r 500 -x 6100 -y 6050 -W 1800 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/halfaya-c3922/crop
pdftoppm -f 18 -l 18 -r 200 -png -singlefile /private/tmp/sandtable-land-rules.pdf /private/tmp/halfaya-c3922/rules
```

Crop PNG SHA-256 `39a47865e6cff7c3ee163084aeb78f16fc023d9adad6db8e094c53bfdac7bebc`; rules PNG `c7bd9744997ec3128ae7ded8829788ae3865ecd325f79e54ccb6b99430920b89`. Renderer versions can change PNG bytes. Coordinates below are approximate crop pixels; files remain outside Git. Prior [C3921 six-side note](2026-11-17-graziani-c3921-six-sides-next.md) and pinned record were known before inspection; this is informed same-scan review, not blind or a new edition.

## Bounded observation

C3921 is legible west of the named Halfaya Pass; C3922 is the neighboring hex east, with label obscured. Sollum's dot is in the northern adjoining hex C4021, whose number is also obscured; adjacent printed labels and prior scenario locators support identities rather than providing two fully legible numerals. Under that reading, the C3922/C4021 shared diagonal runs from the three-hex vertex approximately `(520,515)` northeast to `(680,425)`. The pinned edge has `escarpment`, `unfinished-road`, and `up: C4021`.

The key's escarpment is a wide dark band with irregular splash on its down side. Map C shows that band on C3921/C4021 approaching the vertex from northwest and on C3921/C3922 departing south. From the same vertex toward the coast on the candidate C3922/C4021 side, the line appears narrow and largely free of the wide band. A small splash near the endpoint belongs visually to the adjoining bands but overlaps the vertex, so a short indistinct mark cannot settle side ownership. Paired dark dashed marks intersect the diagonal near the coastline, consistent with an unfinished-road lead; dash gaps, the shoreline and overprint prevent certifying exact road continuity from this inspection alone. No claim is made that the side is otherwise feature-free.

Land §10.21(b) says ZOCs do not extend through escarpment hexsides. If independent review confirms this diagonal lacks an escarpment and accepts the cell identities, the pinned escarpment would incorrectly invoke that particular two-way block there. A positive projection still requires qualified source unit, entry legality, other features, occupancy, rules and certified map/control coverage. Do not infer either direction's legal movement or a route from this observation.

## Follow-up

Independent visual review should trace all three grid rays from the vertex, separately identify the black bands, verify C4021 and C3922 using wider printed labels and scenario locators, and compare a higher-resolution crop. If the discrepancy is accepted, assess versioned provenance and replay, reciprocal edge lookup and tests for both directions of the §10.21(b) rule. Keep this candidate unresolved pending review; no runtime data, readiness or gameplay tests changed.

# Map C blue checkered line: boundary evidence

**Finding:** The blue checkered overprint on C3920/C3921 is continuous with a
hexside-following line that the original map places between its large `LIBYA`
and `EGYPT` labels. This strongly supports identifying it as the Libya–Egypt
boundary. The inspected map key does not visibly reproduce that blue pattern
beside its `Border` sample, so geographic identity remains an interpretation
of the printed layout rather than an explicit blue-symbol legend. No general
movement or ZOC effect for this particular mark was established.

## Source, visual observations and reproducibility

[Original map package](https://spigames.net/PDFv10/CNA_Maps.pdf),
`/private/tmp/sandtable-maps.pdf`, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`,
verified before inspection. PDF p. 3 is Map C and p. 1 contains the terrain
key and §8.37 chart. Inspected p. 3 as a whole at 60 dpi, the local C3920–C3922
strip at 300 dpi, and the area with the country labels at 150 dpi. The relevant
line follows hexsides from the C3920/C3921 side south past the Sidi Omar area,
then between the printed country names in the C20xx area and continues toward
Giarabub. In the country-label crop the blue pattern and a green wadi coexist
along part of the line near C2018–C2320. The coexisting green feature prevents
assuming every boundary side has the same terrain or crossing cost.

The northern crop and the key can be reproduced using the commands in the
[Halfaya review](2026-11-17-graziani-halfaya-edges-next.md#original-sources-and-reproduction)
and the key command below. For wider country-label context:

```sh
pdftoppm -f 3 -l 3 -r 150 -x 700 -y 2850 -W 2100 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/bordercontext
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/fullkey
```

Local PNG SHA-256 respectively
`96242a8e0d55466588802f52c37030931c5b3e89ee3b3f8698e814e18559019b` and
`8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`.
Crops are reproducibility aids; renderer versions can alter PNG bytes. Source
scans remain outside Git pending rights.

The p. 1 terrain key shows `Border` with a gray hexside and adjoining outlines;
it does **not** visibly show the conspicuous alternating blue blocks found on
Map C. The same key distinguishes green wadi, blue rivers, blue cross-tied
railroad, and gray escarpment. None looks like the blue checkered pattern.
The key therefore does not itself provide a clear direct symbol match. The
placement of the line between large country labels, its continuity to the
northern side, and its location along hexsides together make political boundary
the strong reading. They do not establish a publisher-defined rule for it or
prove every individual neighboring cell's national status without inspecting
that cell and its applicable scenario instructions.

## Rules check and limits

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  `/private/tmp/sandtable-land-rules.pdf`, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  Visually inspected PDF/printed p. 7 §4.1, p. 14 §§8.31–.37, p. 15 §§8.41–.43,
  and p. 18 §10.21. The movement cases describe terrain/hexside costs and
  exclusions but do not identify a separate political-border movement cost
  in those passages. §10.21(a)–(b) names all-sea, major-river, lake and
  escarpment sides, not a political border; §10.21(c) independently restricts
  projection where the unit cannot enter the adjacent hex. The inspected
  §8.37 chart has no explicit political-border row. These observations do not
  prove a global absence of other restrictions or that blue alone can be
  discarded from every action's rules.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
  Visually checked p. 1 MAPS and §8.37; bounded text search for `border`,
  `frontier`, `Libya`, `Egypt`, `3920`, `3921` and terrain/hexside terms found
  no explicit blue-line clarification. The correction to tracks remains
  relevant when an actual track is used. Extracted text may omit information;
  later corrections and all scenario-specific conditions were not exhausted.
- [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
  Scenario §60.31, PDF p. 3 / printed p. 27, uses geographic placement terms
  including `Anywhere in Libya`; the existence of a geographic boundary can
  matter for such setup predicates even if no independent movement penalty is
  found. This pass did not adjudicate all such placement rules or neighboring
  cell membership.

**Policy boundary:** Preserve the blue mark as source metadata with a
Libya–Egypt boundary interpretation and confidence, rather than erase it as
an unmarked side or treat it as water, escarpment or a stand-alone impassable
barrier. A project may later adopt a versioned no-additional-cost reading for
ordinary movement once applicable rules and errata scope are reviewed; that
would be an interpretation, not an explicit rule established here. Evaluate
coincident wadis, escarpments, tracks and scenario-specific geographic
conditions independently. Existing Halfaya C3920/C3921 positive movement and
ZOC conclusions remain conditional; no new runtime admission follows.

## Suggested review vectors

- Preserve the blue boundary annotation on C3920/C3921 in source provenance;
  do not classify it as a river, escarpment or automatic impassability merely
  from color. Check its geographic interpretation independently from the
  label syntax of the key.
- For a separately accepted ordinary-movement policy, compare otherwise equal
  clear hexsides with and without the boundary, then add a coincident wadi as
  a separate effect. The test must not assume that all border sides are clear.
- Test Libya-only scenario placement against an explicitly reviewed regional
  classification; an unknown or straddling cell must not silently qualify.
  ZOC still requires a qualified exerting unit and legal entry under §10.21(c).

This same-edition visual reading does not establish a complete boundary census,
country control, legal movement, scenario reachability or rights to distribute
scans. No code, readiness flag, or gameplay test changed.
An [independent source check](2026-11-17-graziani-border-mark-review.md)
agrees that the political boundary is plausible, while cautioning that the
key's `Border` label may mean a generic hexside. Neither source proves an
unconditional positive ZOC across the marked side.

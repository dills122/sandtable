# Halfaya local edges: source evidence and conditional ZOC

**Result:** Map C prints an escarpment along the C3921/C3922 shared side.
Under Land §10.21(b), that side blocks ZOC in **both directions**, even where
some movement across an escarpment might otherwise be possible. C3920/C3921
is a different shared side with a blue checkered overprint; no escarpment is
visible there. The latter observation does not by itself prove an eligible
unit's legal movement or ZOC. C3920 and C3922 are not adjacent. These are
bounded source facts and conditional rules consequences, not a complete map,
scenario setup certificate, or admitted runtime change.

## Original sources and reproduction

- [Map package](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 Map C,
  p. 1 terrain key and §8.37 chart, `/private/tmp/sandtable-maps.pdf`, SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
  Verified hash before the 300-dpi inspection. The following crop uses PDF's
  default display rotation, in which east is right and north is up:

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3000 -y 3350 -W 1550 -H 1350 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/halfaya
```

  Rendered PNG SHA-256
  `61217cc97a0b079f9d186a17d48bb2cca6619e085566aa5808aa116f32c1571d`;
  PDF hash and crop parameters are the primary reproduction evidence because
  rendering versions may change image bytes. Source and crop remain outside Git.
- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed
  and PDF pp. 14–15, §§8.31–.37, 8.42, 8.47; pp. 17–18, §§10.11–.15, 10.21;
  SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  Visually inspected hash-matched pages for this review.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
  Visually checked PDF p. 1, MAPS and §8.37, and searched extracted text for
  §8.3, §8.4, §10.2, escarpment, terrain, track, border, Halfaya and `3920`–
  `3922`. The MAPS entry adds D2228/D2328 escarpment, not these C sides.
  §8.37 corrects track cost: tracks halve terrain cost, rather than costing a
  fixed 1 CP. Search found no explicit correction naming these three C cells;
  OCR gaps, other rule references and later corrections remain possible.
- Scenario §60.31 / §60.41 setup and §60.81 Halfaya locator are as documented
  in [initial-contact review](2026-11-17-graziani-initial-contact-followup.md),
  rather than freshly transcribed here. The current task's sources are the
  same original edition already reviewed, not an independent edition.

## Map observations

Crop coordinates below are approximate pixels within the reproducible image,
not calibrated GIS measurements. Neighboring printed cells place C3921 between
C3920 and C3922:

```text
C3920  |  C3921  |  C3922 (Halfaya Pass)
       blue       dark escarpment
       overprint  on shared side
```

- **C3920/C3921:** The vertical shared side runs roughly from `(783,590)`
  to `(783,700)`. Both cell interiors have the pale background shown as clear
  in the key, though C3921 contains overprinted linework. A blue alternating
  checkered mark follows the side. It is visibly distinct from the dark
  escarpment and green wadi samples in the key; this check does **not** assign
  the blue mark's meaning or decide any political-border rule. No black
  escarpment, blue major-river or sea symbol appears along this side. A nearby
  blue cross-tied line and dark track lie near C3921's upper vertex; their
  crowded endpoint does not establish a road/track crossing of this particular
  vertical side. Do not convert unclassified blue ink or an overprinted vertex
  into an assumed clear, featureless edge.
- **C3921/C3922:** Their vertical shared side is approximately `(973,590)`
  to `(973,700)`. A thick dark-gray band with stippled/splash edge follows
  the side, matching the escarpment sample in the terrain key. Its splash lies
  on the C3922 side, giving the down side there under §8.35; the C3921 side is
  up. Artwork and label overlap make the C3922 printed number hard to read;
  consecutive numbering, adjacent hexes and scenario §60.81 support the cell
  identity. The visual classification does not depend on an assumed sheet
  seam or on reinterpreting the nearby track as an escarpment crossing.
- **C3920/C3922:** C3921 lies between them. No shared hexside exists; neither
  can exert a one-hex ZOC directly into the other from those positions. This
  says nothing about other neighboring units or setup choices.

## Rule consequences and remaining gates

| Directed relation | Bounded source-backed result | Still required |
| --- | --- | --- |
| C3920 → C3921 | No §10.21(a) or (b) blocker is visible on their common side; a ZOC can extend only if the source is otherwise qualified and may enter the target under §10.21(c). | Resolve the blue mark and any applicable rule; verify unit/formation, actual strength and cohesion, target terrain and entry restrictions. Do not infer a legal move solely from the absence of an escarpment. |
| C3921 → C3920 | Same visible side and same conditional result, assessed for the unit **at C3921**. | Assess that unit independently; reverse projection does not follow automatically from forward projection. |
| C3921 → C3922 | **No ZOC through this escarpment** under §10.21(b). | Adjudication must still establish accepted map identity and state. This conclusion does not itself determine combat through other hexes. |
| C3922 → C3921 | **No ZOC through this escarpment** under §10.21(b). | Same map and state qualification. The rule's prohibition is not directional. |
| C3920 ↔ C3922 | No direct ZOC: nonadjacent. | Other units, paths, and successive movements are separate. |

Land §10.11–.15 also restricts which stacks exert ZOC, including the raw
close-assault threshold and cohesion; a terrain certificate does not establish
those facts. §10.21(c) independently excludes a target that the exerting unit
could not enter from its current position. The project still needs an accepted
interpretation of entry restrictions and qualified formation state; do not
infer unconditional positive ZOC from matching hexes alone.

§8.31–.37 distinguishes hex and hexside costs and says road/track benefit
requires a connecting feature across the side. The printed terrain chart's
clear row is 2 CP for both motorized and non-motorized units before other
applicable rules; do not claim that number is the final cost of this move
without resolving the blue overprint and other conditions. §8.35 puts the
escarpment's splash on the down side; §8.42 prohibits vehicles moving **up**
and explicitly allows vehicles down via a crossing track, with stated
additional costs. That case alone does not explicitly settle every no-track
downhill case; check the terrain chart and applicable restrictions separately.
These directional movement rules do not override §10.21(b)'s ZOC block
in either direction. Whether a particular non-motorized move is legal, its
complete costs, and whether any track actually crosses this specific side are
not certified here. §8.47 distinguishes unfinished railroad and roads; the
nearby rail mark alone does not grant a connecting road bonus.

## Focused proposed vectors

- Feed a source-accepted escarpment edge C3921/C3922 and independently
  qualifying opposing units: `ZOC(C3921,C3922) = false` and reverse `false`,
  including a hypothetical traversable track; ordinary adjacency still true.
- Feed C3920/C3921 as adjacent with the blue mark preserved as unresolved:
  return unknown for a full movement/positive-ZOC decision until feature,
  unit and entry policies are accepted. Under a separately accepted no-effect
  interpretation and a qualifying unit that can enter, test positive
  projection in each direction independently; test failed cohesion/strength or
  prohibited entry separately.
- Assert C3920/C3922 are nonadjacent and direct ZOC false; never use this
  local result to classify wider setup as contact-free. Test other neighboring
  forces, reciprocal edges, observation policy and combat obligations only
  with their own accepted source/state data.

An [independent source review](2026-11-17-graziani-halfaya-edges-review.md)
corroborates the bounded geometry and ZOC conclusion, while flagging the
downhill qualification corrected above and its own unverified terrain-key
comparison. No runtime, content or readiness flag changed; no gameplay tests
were run.

# Benghazi A4827: six-side source inspection

**Scope:** Map A artwork and pinned map data, not movement legality. The city
hex identification is supported by its six numbered neighbors and Map A's
location summary; its own number is obscured. Scenario §§60.31 and 60.92(B)
print Benghazi `B4827`. Existing RUL-GRA-05 selects `A4827` as a provisional
project interpretation; retain the source conflict, do not alias the two
coordinates, and do not treat this inspection as a publisher correction.
See [coordinate review](2026-11-17-graziani-map-next-review.md).

## Source and reproducible crops

[Original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1,
Map A, local `/private/tmp/sandtable-maps.pdf`, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Default PDF rotation, Poppler `-scale-to`, no crop editing:

```sh
pdftoppm -f 1 -l 1 -scale-to 12000 -x 5500 -y 2500 -W 1100 -H 1100 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4827/city
pdftoppm -f 1 -l 1 -scale-to 7000 -x 1500 -y 150 -W 3100 -H 3250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4827/full-key-summary
pdftoppm -f 1 -l 1 -scale-to 7000 -x 850 -y 2500 -W 1500 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/a4827/summary
```

City crop SHA-256 `2fcb4c44948159a8b5feb36746f5187542fb176250cd82990f6bf60917bc1e1e`;
full-key/context crop SHA-256 `60bb1273946b98fe770923349fe808ad6e167cd1f23b0feb90e632cdcfa10f3c`;
full-summary crop SHA-256 `93a67ce6178021fdfec7090c9e4a57fa6a331824c08dd111eafc8bc1cb1a5f46`.
Crop pixels use rendered page origin; rendering versions can change PNG hashes.
Source scan and crops remain outside Git.

## Artwork and sides

Directions below mean positions in the unrotated **city crop**, not compass
directions. These are visual observations, not side-terrain classifications.
The hex center is covered by port, city and black training-area figures and
`AXIS` overprint; coastline and route strokes cluster at its right-hand vertex.

| Neighbor | Crop position | Visible evidence on shared side |
| --- | --- | --- |
| `A4927` | upper left | Blue sea on both sides; no obvious route or other crossing. |
| `A4928` | upper right | Blue sea along most of side; coastline and parallel black road strokes converge near right vertex. Their side assignment at that crowded vertex is uncertain. |
| `A4828` | right | Coastal junction and blue railroad and black dashed track-like line extend into `4828`. Railroad/track approach side through overprinted city; individual black strokes and whether road crosses this side cannot be established confidently at junction. |
| `A4728` | lower right | Coastline, parallel black road and blue railroad converge at or near this side; heavy overprinting makes exact railroad and coastline crossings uncertain. El Berca dot and `4728` appear in neighboring hex. |
| `A4727` | lower left | Blue sea for most of side, coast reaches it close to lower vertex; no obvious route crossing. |
| `A4826` | left | Blue sea both sides; large Benghazi label partly covers neighbor number and side. No obvious route crossing in exposed portion. |

Absence of an obvious ink mark is not proof of a clear, traversable, or
complete side. `4927`, `4928`, `4828`, `4728`, `4727` are readable; `4826` is
partly obscured. Blue railroad extends from city toward `4828`/Benina and
`4728`/El Berca; dashed track-like strokes accompany portions of it. The
parallel solid black route bends through the coastal junction. Art overprint
prevents confidently assigning every feature to a precise hexside.

The printed terrain key distinguishes sea, coast, major city, port, road,
unfinished road, track, railroad, unfinished railroad, and training area.
The soldiers match its training-area pictogram; `AXIS` is printed directly below
those figures. Their operational meaning and permitted use require rules;
the picture alone supplies no legality. Map A's Summary of Important Locations
lists Benghazi `A4827`, marks major city, port and victory condition, and does
not mark assigned garrison. This summary column does not erase the scenario's
separately named Benghazi Garrison.

## Pinned data comparison

`src/Cna.Core/Content/HistoricalMap/hexes.v1.json` and `hexsides.v1.json` cite
`vassal:CNAv2.1.0`; they are a separate transcription, not this original scan.
The hex record gives `A4827` terrain and settlement `major-city`, coastal true.
The six neighbor records identify `A4927`, `A4928`, `A4826`, `A4727` as sea;
`A4828` clear/coastal; `A4728` clear/coastal with village. Sea-colored artwork
extends into parts of some coastal cells, so this is a data comparison, not a
rule for assigning whole-hex terrain from color.

The pinned side file has only two records mentioning `A4827`:
`A4728|A4827` has `road`; `A4827|A4828` has `road` and `railroad`.
Both have `side: null` and `up: null`. No records mention the other four
neighbors. That is a statement about this file's contents, not proof of an
empty side or inaccessible edge. The scan supports road/rail activity near
the junction but the overlap prevents independent certification of all pinned
features or exclusion of a track. In particular, the blue line toward `4728`
deserves targeted source/rules review before interpreting its lack in that
edge record. No side here is certified legal for movement, supply, ZOC, or
control; no runtime data changed. No tests run for this source note.

[Independent review](2026-11-17-graziani-a4827-six-sides-review.md) flagged
the earlier key label and clipped crop; this revision checks the complete key
and summary directly and corrects the label to **Unfinished Road**.

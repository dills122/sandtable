# Sidi Barrani C4131: six-side source audit

**Finding:** Original Map C supports a road-like paired-line crossing on each east/west side and a single-dashed track lead toward C4031; these correspond to three pinned pairs. The C4130 crossing is crowded by name ink and a transition from nearby paired dashes to paired solid strokes, so whether the exact side is finished road requires independent inspection. Coastal and overprinted portions remain unknown; no legal edge follows.

## Source observations and pinned comparison

[Map C and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 3 and 1, show Sidi Barrani's blue dot within numbered C4131 and shoreline through the northern interior. Neighbors identified by labeled grid are C4130, C4132, C4030, C4031, C4230 and C4231. Directions below are page-image directions. Key depicts road as paired solid lines, unfinished road as paired dashes, and track as single dashes; coastline and text can obscure a crossing.

| Neighbor | Visible side | Pinned pair |
| --- | --- | --- |
| C4130 | Left vertical side has paired route strokes crossing on land; name and nearby transition from paired dashes to solid strokes obscure exact classification at the crossing. | `road`; verify finished versus unfinished source mark at side, do not silently correct. |
| C4132 | Right vertical side bears paired solid road-like strokes through name ink on land; shoreline crosses higher on this side. | `road`. |
| C4030 | Lower-left diagonal is visible as a narrow grid stroke, with route and name ink near its western vertex. | No literal record; no conclusion about hidden vertex or full edge. |
| C4031 | Lower-right diagonal meets the blue dot and a single dark dashed track; dash extends into C4031. | `track`; dot/vertex partly conceals exact crossing. |
| C4230 | Upper-left diagonal lies in the blue sea portion; thin grid line visible. | No literal record; do not infer a legal traversal or certify all-sea side from this limited check. |
| C4231 | Upper-right diagonal lies in blue sea; thin grid line visible. | No literal record; same limitation. |

Current `hexsides.v1.json` contains only the three named pairs involving C4131, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Absence is not a source certificate. The [forward interior review](2026-11-17-graziani-forward-hex-terrain-review.md) separately confirms the village dot, pale land and coast. Preserve road, shoreline and concealed features independently; §10.21 and unit-specific entry remain separate rule questions.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 500 -x 9000 -y 5450 -W 1800 -H 1700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-sidi4131
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `9b7e130a29b54b6e21db3237a35dfb3c55276650ddc16f2f636207f9fa438032` (renderer-dependent). Independent review should verify all vertices and the C4130 route transition before any content decision. No runtime changes or gameplay tests.

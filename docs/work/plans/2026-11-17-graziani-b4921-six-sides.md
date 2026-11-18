# Graziani B4921: six-side source comparison

**Finding:** Original Map B places Mechili's blue village/bir dot in B4921's rock/gravel-patterned hex, amid six apparent single dashed track spokes. Pinned v1 records five track sides; the upper-right spoke appears to meet the B5021 side very close to its vertex, but exact attribution is uncertain. This comparison does not establish movement, supply, ZOC or contact legality.

## Local evidence

Inspected [original map and terrain key](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF pp. 2 and 1. The existing [fixed setup inventory](2026-11-17-graziani-fixed-contact-inventory.md) lists B4921 among explicit Axis setup hexes; that inventory was consulted, not independently retranscribed here. In the 500-dpi crop, B4921's approximate vertices clockwise from top are `(735,510)`, `(895,600)`, `(895,780)`, `(735,865)`, `(580,780)`, `(580,600)`. Directions describe this crop, not compass bearings. Neighbor labels appear within the adjoining cells. The key shows rock/gravel as yellow circles and stipple, village/bir as a blue dot, and track as a single heavy dashed line. B4921 shows the pattern and a blue dot; Mechili name and dark junction overprint obscure part of its interior. No sea is visible in this hex. Pinned [hex record](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) says `terrain: gravel`, `settlement: village`, `coastal: false`.

| Neighbor | Visible shared side in crop | Pinned pair |
| --- | --- | --- |
| `B5020` | Upper-left diagonal: single dashed route crosses toward Mechili. | `track` |
| `B5021` | Upper-right diagonal: dashed route from Mechili approaches and appears to cross immediately beside right-hand vertex, where grid and ink overlap. Its exact side attribution remains uncertain. | No record found for pair. |
| `B4922` | Right vertical: dashed route crosses; Mechili lettering overlaps nearby. | `track` |
| `B4821` | Lower-right diagonal: dashed route crosses amid Mechili lettering. | `track` |
| `B4820` | Lower-left diagonal: dashed route crosses near its lower endpoint; dashes and grid overlap. | `track` |
| `B4920` | Left vertical: dashed route crosses; a gap between dashes and grid thickness limit exact crossing. | `track` |

Each of the five recorded pairs has `side: null` and `up: null`. Absence of a B5021 pair is a statement about this pinned file, not proof that the side is clear: the route runs extremely near the three-hex vertex. The central dot, text, overlapping dashes, and scan quality prevent treating this visual reading as a certified edge transcription. No wadi or ridge band is apparent on these six sides; that negative observation has the same overprint limits. Same-edition informed inspection is not independent edition verification.

## Reproduction and provenance

```sh
pdftoppm -f 2 -l 2 -r 500 -x 5800 -y 3300 -W 1800 -H 1900 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4921-six
pdftoppm -f 1 -l 1 -r 200 -x 1500 -y 160 -W 1500 -H 800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/b4921-key
```

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; map crop `9862076a9f9c4d7c1ad167c76a4d0c2c14e36f47d2d2fcce8057427f2a3fedd6`; key crop `3d850fc643d4d90930c3aa35b1c67f63a59f0e4d590091cb1707abf3d8b79d30`. PNG bytes may vary by renderer. Pinned hexes SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`; [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; both declare `vassal:CNAv2.1.0` as source. Crops remain outside Git. No runtime changes or gameplay tests.

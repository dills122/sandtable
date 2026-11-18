# Saharan Detachment radius: regional boundary check

**Disposition:** The blue checked line on [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, passes through the proposed C0716 radius disk along hexsides. Under the existing geographic interpretation of that line as the Libya–Egypt boundary, **C0719 is on its Egypt side and the other 36 candidates are on its Libya side**. This is a visual, conditional region classification, not an admitted legal setup list or a certified boundary polygon. The separate Saharan setup row does not itself say “in Libya”; the inspected movement sources do not give the political line an independent crossing prohibition or cost.

## Map evidence

The [37-cell disk](2026-11-17-graziani-c0716-radius-source.md) assumes both origin and distance-three ring are included; that interpretation remains unresolved. In an enlarged Map C rendering the checked line follows the west side of C0719, separating it from C0718, and continues along C0719's northwest and southwest diagonals. It runs east of the other candidates, including C0618, C0818, C0918 and C1017. Thus the line touches candidate sides, rather than dividing a candidate interior in this reading. C0719 is on the opposite side from C0716; there is no claim that touching a boundary makes a hex part of both countries.

The checked line continues through the larger map region where `LIBYA` and `EGYPT` labels lie on opposing sides, Libya west and Egypt east. This supports national identity but the PDF p. 1 key's gray `Border` example does not visibly reproduce the blue checks. See [regional evidence](2026-11-17-graziani-border-mark-next.md) and its [review](2026-11-17-graziani-border-mark-review.md). Country labels and cell membership are therefore geographic inferences, not an explicit matching legend or complete certified region census. Visual overprints may conceal coincident features, so classification says nothing about their terrain or traversability.

## Setup and movement

[Scenario §60.31](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, lists Saharan Det (I) and five light trucks in a distinct **Within 3 Hexes of C0716** row after a separate **Anywhere in Libya** row. It does not explicitly append the latter phrase to the Saharan row. Section 60.34 separately restricts two player-placed dumps to map C in Libya; that dump rule does not define this unit's placement. This supports keeping a Libya-only filter unproven, not declaring C0719 legal: other setup, organizational, occupancy, terrain and scenario rules or corrections may apply. The fixed Giaribub garrison C1014 and C0716 supply dump are separate entries.

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 14 §§8.31–.33, make movement dependent on unit and terrain and discuss entry prohibitions and connected roads/tracks. Section 8.19 separately prohibits Commonwealth land units moving west of Marble Arch A2109; it is not a general Libya–Egypt crossing rule. The map's §8.37 terrain chart and Land §10.21 ZOC exceptions were checked in [border-effects research](2026-11-17-graziani-fort-maddalena-border-effects.md): neither names a political boundary as an independent cost or ZOC exception. These are bounded source checks, not proof of unrestricted movement, ZOC, setup, or every unit's eligibility. Initial placement is not a movement route, and salt marsh, desert, tracks, units and coincident hexside features still need independent adjudication.

## Reproduction and limits

Visual reading used the same original scans as prior research, not an independent edition or blind transcription. The regional crop can be reproduced with:

```sh
pdftoppm -f 3 -l 3 -r 150 -x 900 -y 3900 -W 1600 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/radius-border
```

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. The local scan, endpoint choice, line identity, per-cell classification and legal placement remain separate review gates. No runtime change or gameplay test was made.

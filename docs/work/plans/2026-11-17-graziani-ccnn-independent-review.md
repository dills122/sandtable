# Independent CCNN source review — 2026-11-17

Scope: independently inspect original Axis charts, Land rules, and September 1979 errata before comparing existing project research. PDF page numbers below are one-based; Land pages also carry the same printed numbers. This is a source reading, not a game ruling or an implementation change.

## Source identity

SHA-256 hashes computed on local originals and subsequently matched against project source inventory:

| Source | Local file | SHA-256 |
|---|---|---|
| Axis charts | `/private/tmp/sandtable-axis-charts.pdf` | `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca` |
| Land rules | `/private/tmp/sandtable-land-rules.pdf` | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |
| September 1979 errata | `/private/tmp/sandtable-cna-errata.pdf` | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

## 1st CCNN division HQ

- Axis OA §4.44b, PDF p. 17, row for 1st CCNN Div HQ: counter abbreviation `1 CCNN`, ID `g`, TOE and weapon system `N`, arrival `D`. Same sheet lists 219th and 233rd Legion HQs, each ID `g`, separately and explicitly as `1 × 65/17 Gun`, arrival `D`. Division artillery regiment separately lists 6 × 75/27, 3 × 100/17, and 2 heavy AA points. These are game TOE strength points, not necessarily individual physical guns.
- Axis PDF p. 4 OA key: `N` for non-tank/non-gun units means maximum TOE strength points on the unit characteristics chart for its ID code. It also states that tank and gun TOE points, even assigned to non-tank/non-gun units, are always listed by number and weapon system. `D` means deployed at the start of the first Operations Stage of Game Turn One.
- Axis §4.46b PDF p. 26: HQ `g` has CPA 20, no intrinsic listed combat values, and may assign one artillery TOE strength point; maximum column 1. This is an assignment capacity and identifies no model of gun. Land §4.46 p. 10 says applicable HQs take characteristics from their assigned weapon-system TOE points. Land p. 10's OA explanation similarly says normal units arrive at maximum but weapon-system points present on arrival are listed by number and type.
- September errata PDF p. 1 §4.44b lists several OA corrections; none identifies this HQ's gun. Its §4.46 note gives no specific HQ correction. This is a bounded review of the relevant published corrections, not evidence no other correction exists.

Two readings remain possible: literal `N` refers to the maximum of one artillery point, but leaves type unspecified and fails the explicit listing convention; alternatively, `N` applies to a non-gun HQ without establishing an assigned gun, with `g` only allowing later assignment. The source does not choose between them. Neither an initial zero nor an initial 65/17 is established. Neighboring legion equipment supplies no basis to transfer its weapon type to the division HQ. An explicit versioned ruling or further authoritative source is needed for initial strength and type.

## Engineer parenthesized combat strength

- Axis PDF p. 17 lists 201st Engineer **Battalion**, ID `bbb`, `N`, `D`. Italian characteristics §4.46b PDF p. 27 gives `bbb` CPA 25, close assault offensive/defensive `0/(1)`, and maximum TOE `(1)`. Keep both parentheses in transcription; Land §4.46's explanation of parenthesized maximum expressly addresses headquarters, not engineer battalions.
- Land §3.4 p. 6 gives a general defensive use condition for parenthesized ratings. September errata PDF p. 1 clarifies §3.4: use only when attacked alone in a hex or with other units having only parenthesized strengths.
- Land §23.11 p. 35 originally says engineers may use parenthesized strength only when stacked with a friendly combat unit. September errata PDF p. 2 explicitly corrects this to **not** stacked with a friendly combat unit. The errata also permits engineers to enter friendly-occupied, enemy-controlled hexes. The original classifies engineers as non-combat units and denies them zones of control; Land §3.23 p. 6 allows case-dependent treatment of some non-combat units for specified purposes.
- Axis characteristics key PDF p. 27 separately describes parenthesized ratings in terms of other units in the hex with ammo and non-parenthesized numbers for that rating; replacement points do not prevent use. This is not identical to corrected §3.4 or the specific §23.11. Apply neither the chart key alone nor an unconditional defense value without an explicit reconciliation. For a conservative reading, corrected §3.4's attacked/stack condition and corrected §23.11's engineer-specific friendly-combat-unit condition both matter. The exact interaction with case-specific combat classification and the chart key's ammo qualifier remains an interpretation requiring adjudication, especially mixed stacks. Parenthesized defense does not authorize an offensive assault.

## Comparison after independent reading

Existing `2026-11-16-graziani-source-material-research.md` agrees on ambiguous initial 1st HQ gun strength/type and the engineer-specific errata. Its citation calls Land §23.11 printed p. 36; the inspected hash-matched rules place §23.11 at bottom of printed/PDF p. 35, continuing §23.1 on p. 36. Existing research already records the chart key's distinct ammunition condition and unresolved interaction. No independent historical source was used to fill the game chart gap.

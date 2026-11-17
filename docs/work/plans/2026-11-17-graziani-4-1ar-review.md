# Independent source check: repeated 4/1AR

**Finding:** The scenario prints `4/1AR` twice, but the checked OA has one
4th Battalion, 1st Artillery Raggruppamento row and the inspected physical
counter block has one `4/1AR` counter. This supports one identified starting
battalion; it does not establish an official correction of the second printed
occurrence or prove a complete inventory of all counter sheets.

## Source observations

- Scenario §60.31, printed p. 27 / PDF p. 3, “Anywhere in Libya” reads
  `1/1AR; 2/1AR; 3/1AR; 4/1AR; XXI Cp; XXVI Cp 4/1AR; XXI Cp (Ar).`
  There is a space but no visible semicolon between `XXVI Cp` and the second
  `4/1AR`. September errata §60.31, PDF p. 3, explicitly identifies the
  *XXI Corps Artillery* as listed twice. It does not mention 4/1AR or specify
  replacement punctuation. Do not extend that correction to claim publisher
  intent for the latter repetition.
- Axis OA PDF p. 16, **Unassigned Gun Units**, identifies 4/1AR once as the
  fourth battalion of 1st Artillery Raggruppamento, ID `mm`, **3 × 100/17
  Howitzer**, arrival code `D`; the three other 1st Raggruppamento battalions
  have distinct 1/1AR, 2/1AR and 3/1AR rows. OA key PDF p. 4 defines D as
  deployed GT1 OpStage 1. Note c says these battalions are independent and
  the raggruppamento is administrative without a formal HQ. XXVI Corps
  Artillery appears as a separate row and identity. The OA does not support
  interpreting the second 4/1AR occurrence as an XXVI subordinate solely
  because the scenario omits punctuation.
- Original counter scan PDF p. 1, Counter Section 2, **left block**, the
  two rows directly below the tank group: `1/1AR`, `2/1AR`, `3/1AR` occupy
  the last three positions of the first row; **`4/1AR` is first counter in the
  next row**, immediately left of `1/2AR`, `2/2AR`, `3/2AR`, `4/2AR`. Its
  visible face has artillery symbol, battalion mark, and stacking value 1.
  One `4/1AR` face is visible in this local group. This visual evidence is
  stronger than a bare OA match but is not an exhaustive search of every
  counter sheet or confirmation of later physical editions.

Reproduction on the inspected PDF, 180 dpi, crop coordinates relative to
rendered page (north/up as Poppler renders it):

```sh
pdftoppm -f 1 -l 1 -r 180 -x 20 -y 2000 -W 950 -H 230 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/graziani-mapcheck/ar4countercrop
```

The resulting local crop SHA-256 is
`2bac704af31930c4253fee410965d19adae2943b715dc3d48b98b5a21f59dadc`;
renderer versions may alter PNG bytes. The crop stays outside Git pending
rights. Full counter PDF hash below identifies the scanned source.

## Consequences and tests

Preserve both scenario text occurrences, their exact adjacency and the separate
XXI erratum as evidence. A one-battalion setup candidate has support from the
OA row and physical counter; do not create two independent 4/1AR units simply
because two scenario mentions appear. Do not silently replace the second
mention with `4/2AR` or another identity: OA lists those 2nd Raggruppamento
units separately at GT26, and neither the punctuation nor the XXI erratum
establishes that substitution. The unrestricted Libya location remains a
player choice under applicable setup and stacking rules; no exact hex is
implied.

Test a parser retaining both occurrences but linking a selected one-unit
interpretation to one OA identity. A second distinct counter or proposed
replacement ID must require independent source evidence; a placement request
cannot duplicate the same identity. Verify the exact 3 × 100/17 composition,
not the similarly numbered `4/2AR` row's 3 × 105/28. Keep uncertainty over
whether there is a missing delimiter or other intended text visible in the
source record.

## Sources

[Scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3, SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF pp. 4,
16, `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`;
[counter scan](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 1,
`bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`;
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3, `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Compared existing [artillery source note](2026-11-17-graziani-corps-artillery-next.md)
and its review. Same-edition inspection, no runtime changes or gameplay tests.

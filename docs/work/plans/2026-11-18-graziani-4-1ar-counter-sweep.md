# SU-02: additional Section 2 and 3 counter-face sweep

**Result:** In the inspected printed faces of Counter Section 2, I identify **one** `4/1AR`, the previously recorded artillery counter, and no second face bearing that readable label. In the inspected Section 3 faces, including areas outside the previously reviewed Blackshirt block, I identify no `4/1AR` label. This is a visual, same-scan result: it does not prove a physical inventory across all sections, backs, printings, replacement sheets or obscured material, and it does not identify the intended meaning of the repeated scenario text.

## Coverage and observations

[Counter PDF](https://spigames.net/PDFv10/CNA_Counters.pdf) p. 1 Section 2 and p. 2 Section 3 each print **200 pieces** and “quantity of sections of this identical type: 1”; the header also states nine sections of all types in the game. These are printed section quantities, not independent proof of a particular physical set's contents. For orientation below, rows are counted top to bottom and columns left to right within each displayed ten-column block, not printed game coordinates.

| Area | Inspection partition and result |
| --- | --- |
| Section 2 left block, rows 1–4 | `185 Folg`, 1/2 Libyan, Maletti and associated infantry/HQ faces. Visible labels in these four rows do not show `4/1AR`. |
| Section 2 left block, rows 5–8 | Maletti/LTC armor, Sahara and artillery groups. **Row 8 column 1** is `4/1AR`; columns 2–5 read `1/2AR`, `2/2AR`, `3/2AR`, `4/2AR`. The preceding row ends `1/1AR`, `2/1AR`, `3/1AR`. The other readable faces in these rows do not show another `4/1AR`. This rechecks the earlier positive crop; it is not a new independent counter. |
| Section 2 left block, rows 9–10 | Tobruk `31 GaF`, `32 GaF`, Marines and coast-defense group; no additional readable `4/1AR`. |
| Section 2 right block, rows 1–4 | Tobruk/Bardia, Bersaglieri, Libyan, garrison and artillery faces; no readable `4/1AR`. |
| Section 2 right block, rows 5–8 | Armor and corps artillery, GaF, garrison, artillery and SGSU faces; no readable `4/1AR`. `XXVICp` and other corps artillery are distinct printed labels, not a guessed linkage from the scenario punctuation. |
| Section 2 right block, rows 9–10 | Remaining garrison and SGSU faces; no readable `4/1AR`. |
| Section 3 left block, rows 1–4 | Italian SGSU and aircraft faces, plus neutral markers; no readable `4/1AR`. |
| Section 3 left block, rows 5–8 | Italian aircraft alongside the previously reviewed 1 CCNN/Blackshirt faces; no readable `4/1AR`. |
| Section 3 left block, rows 9–10 | Italian replacement markers, tank silhouettes, SGSU and `San Giorgio`; no readable `4/1AR`. |
| Section 3 right block, rows 1–4 | Neutral track, marker and airfield faces; no readable `4/1AR`. |
| Section 3 right block, rows 5–8 | Previously checked 2/4 CCNN and other Blackshirt land-counter faces, with neutral symbols at the upper-right edge; no readable `4/1AR`. |
| Section 3 right block, rows 9–10 | German aircraft faces; no readable `4/1AR`. Section 4 begins **below** this block and is outside this sweep. |

Rendered the full displayed Section 2 and Section 3 at 200 dpi, including the edges of the two 10-by-10 block layouts. Some yellow highlighting, dark background, fine type, borders and overlapping page/crop edges reduce readability; no claim that every glyph or reverse face was independently deciphered. The search is for a visible face whose **label reads** `4/1AR`, not all semantically equivalent abbreviations or unlabelled duplicate counters. The earlier [Section 1 and Blackshirt review](2026-11-18-graziani-4-1ar-source-next-review.md) independently covered its stated areas; this sweep supplements it within the same PDF, not an independent physical edition.

## OA comparison and remaining conflict

[Axis OA](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF p. 16 visibly distinguishes `4/1AR`, ID `mm`, **3 × 100/17 Howitzer**, arrival `D`, from `4/2AR`, also `mm`, **3 × 105/28 Gun**, arrival `1/26`. The adjacent counter labels are different, as are OA weapons and dates. Section 2's single identified `4/1AR` face is consistent with one candidate identity; it does not prove which scenario occurrence is erroneous, that `4/2AR` was intended, or that the missing separator implies an attachment. Preserve both scenario occurrences and the separate explicit XXI Corps erratum as described in the [prior source note](2026-11-17-graziani-4-1ar-next.md). A publisher correction, clearer source or independently authenticated additional counter could discriminate; none is supplied by this bounded visual sweep.

## Reproduction and provenance

```sh
pdftoppm -f 1 -l 1 -r 200 -x 0 -y 1350 -W 2200 -H 2100 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/sweep-section2
pdftoppm -f 2 -l 2 -r 200 -x 0 -y 0 -W 2200 -H 1700 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/sweep-section3a
pdftoppm -f 16 -l 16 -r 180 -png -singlefile /private/tmp/sandtable-axis-charts.pdf /private/tmp/sweep-axis-oa16
```

Verified local SHA-256: counters `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`; Axis charts `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`. No new scenario or errata reinspection in this pass; those claims are attributed to the cited earlier review. No runtime/data edits or gameplay tests.

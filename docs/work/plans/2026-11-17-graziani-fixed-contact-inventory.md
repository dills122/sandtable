# Graziani fixed opposing setup: adjacency inventory

**Finding:** Three same-sheet fixed starting-coordinate pairs are adjacent on the printed Map C: `C3419–C3520`, `C3419–C3320`, and `C3019–C3020`. The first pair is an additional contact lead: the Italian Sheferzen Garrison at C3419 is also adjacent to the Commonwealth 1st Royal Tank Regiment at C3520. Adjacency does not establish ZOC, qualifying strength, final setup, combat obligation or reachability. This is a bounded inventory of fixed opposing scenario locations, not a census of optional placements or a certified full map.

## Source and method

Visually checked [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27 §§60.31 and 60.41, and [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3. SHA-256 verified: scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. This is informed same-scan research, not a blind entry, independent edition or rights grant.

For the three positive pairs, reproduce local map strips:

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5650 -y 7600 -W 1500 -H 1250 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-contact-south/detail
pdftoppm -f 3 -l 3 -r 500 -x 5400 -y 8600 -W 1900 -H 1650 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-fort-contact/map
```

PNG SHA-256 respectively `3537b34255ebc13e296783e7882f42b33302ec0ee879730acdb1cb9068613331` and `48a1dc731acd9137459c79e015e2f21984e8118b55378d079da18f0bac64bc2a`. Renderer versions may alter bytes; hashes and coordinates identify source. Crops remain outside Git.

## Fixed-coordinate inventory

Transcribed Axis explicit hexes: `C4218`, `C4120`, `C4020`, `C3920`, `C3919`, `C3918`, `C3617`, `C4707`, `C4507`, `C4321`, `C4807`, `B4827`, `B5504`, `B5925`, `B4921`, `C1014`, `C3419`, `C3019`, `C1715`, `A4829`, `A4130`. `B4827` is the scenario's Benghazi spelling, retained as printed despite the known `A4827` map conflict. Tripoli box, Tripolitania, `Anywhere in Libya`, `Anywhere on Map A or B`, and a three-hex-radius placement are not fixed map-hex entries and are excluded.

Commonwealth explicit hexes: `C4131`, `C3926`, `C3922`, `C3721`, `C3520`, `C3320`, `C3020`, `D3714`, `D3612`, `D3615`, `E1829`, and `E1430` for the specified Helwan alternative. Alexandria is `E3613` and/or `E3714` (possible listed hexes, not a claim both are occupied); Cairo and/or Helwan placement is an owner choice and Cairo is not reduced here to an invented single hex. Other `Anywhere on Map D or E` entries are excluded. Placement choices, attachments, trucks and scenario restrictions remain separate.

For a reproducible **candidate** check of C-sheet coordinates, compare rows and columns as printed: cells in one row are neighbors at one column difference; cells in consecutive rows are neighbors when `abs((2*colA - (rowA mod 2)) - (2*colB - (rowB mod 2))) == 1`. This parity convention is checked locally against the printed positive grid and is not a source-certified full-sheet geometry or seam admission. Exhaustive pair comparison of the above explicit C coordinates under that candidate gives exactly the three pairs below. Different-sheet pairs are not certified nonadjacent from this check; none of the listed C/D placements is near the C/D cut under the existing 33-column candidate. A full source-backed cross-sheet exclusion and unspecified Cairo choices remain open.

| Axis → Commonwealth | Printed units | Visual edge observation and limits |
| --- | --- | --- |
| `C3419–C3520` | Sheferzen Garrison → 1st RTR (`7/7`, 5 medium trucks). | Map labels `3419` and `3520` occupy adjacent hexes; their common diagonal runs approximately `(335,220)`–`(490,310)` in the first crop. A dashed track crosses near this side; its exact intersection and all other side features need independent review. Do not certify track cost or clear terrain here. |
| `C3419–C3320` | Sheferzen Garrison → 2nd Rifle Brigade (`7Spt/7`, 10 medium trucks). | Shared southeast diagonal; blue name ink partly hides its lower endpoint. See [local source](2026-11-17-graziani-map-sheferzen-initial-contact.md) and [independent check](2026-11-17-graziani-map-sheferzen-initial-contact-review.md). No complete clear-edge certificate. |
| `C3019–C3020` | Fort Maddalena Garrison → 11th Hussars (`7Spt/7`, 3 medium trucks). | Shared vertical side bears a blue checkered mark; see [Fort source check](2026-11-17-graziani-map-fort-maddalena-contact.md). Border interpretation and rules effects remain open. |

Scenario lists the 2nd Libyan Division C3920 and Commonwealth C3922, but C3921 lies between them; that specific pair is not adjacent, as [previously checked](2026-11-17-graziani-initial-contact-followup.md). No conclusion about global contact follows from that one pair.

## Remaining checks

Independently recheck the complete source list, C3419/C3520 labels and diagonal, and same-sheet comparison. Obtain each actual stack and optional placements, terrain, ZOC eligibility, phasing and §10.31/.32 exceptions before adjudicating; the scenario's `N`, `D`, counter values and chart categories are not substitutes for final state. Cross-sheet transform, country boundaries, Cairo alternatives and unenumerated setup choices require separate coverage. Proposed tests should assert all three local adjacency pairs, known/unknown side data, optional co-location and both ZOC directions without leaking hidden unit choices. No runtime data, readiness or gameplay tests changed.

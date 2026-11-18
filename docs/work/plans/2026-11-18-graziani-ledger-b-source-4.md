# VI-03: Sidi Omar objective and neighboring name

**Result:** Original scenario and location-summary records support **Sidi Omar C3618** as the location associated with the Commonwealth decisive objective. The Map C crop shows the Sidi Omar settlement dot in numbered 3618, and a separate nearby dot under the **Libyan Omar** label in the neighboring cell. The [review](2026-11-18-graziani-ledger-b-source-4-review.md) identifies that cell as C3617 from the surrounding grid sequence; its own numeral is partly obscured. This is a bounded local name/locator comparison, not proof of possession, supply, legal access or victory.

## Source comparison

I visually inspected the original [scenario book](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF p. 4 / printed p. 28, [Air/Logistics location summary](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf) PDF p. 26, and [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3. The [victory matrix](2026-11-12-graziani-victory-matrix.md) already lists the named Commonwealth decisive objectives, and [C3618 source review](2026-11-18-graziani-c3618-six-sides-review.md) independently reads the same map scan. This check explicitly joins the victory name, two published locators and the neighboring distinct map label; it does not repeat or certify the six side readings.

| Record | Observation and limit |
| --- | --- |
| Scenario §60.81 | The Commonwealth decisive row says take and hold Bardia, Fort Maddalena, **Sidi Omar**, Sollum and Siwa, with supply as above. The row does not print a coordinate next to Sidi Omar. It does not import the tactical row's Halfaya Pass as an additional listed location. |
| Scenario §60.5 | Its Air Landing Strips list prints **Sidi Omar C3618**. This is a source locator for the same spelled name, not a rule that an air facility automatically satisfies victory. |
| Air/Logistics location summary | PDF p. 26 prints **Sidi Omar C3618** and marks its Victory Condition column. This is a separate printed reference in the same edition; it does not itself determine the required control or supply state. |
| Map C | A blue dot beneath the **Sidi Omar** label lies in the cell printed `3618`. A separate blue dot beneath **Libyan Omar** lies in the neighboring cell identified as `3617` by the grid sequence; its numeral is partly obscured by lettering. The dots and grid cells are distinct in the inspected crop. |

The repeated name and matching coordinate are strong source evidence for associating the objective with C3618, while §60.81 itself names the place without a locator. Preserve **Libyan Omar C3617** as a separate map label; similar names do not make its dot an alternate victory hex. This reading does not establish that every map-side feature is clear or that the target can be reached or held; blue boundary marks, track and their rules remain subject to their own review. The [scenario extent crosswalk](2026-11-18-graziani-scenario-extent-crosswalk.md) already identifies C3618 as a victory candidate; this is a focused source attribution, not runtime admission.

## Reproduction and limits

Map crop: `pdftoppm -f 3 -l 3 -r 600 -x 5875 -y 8050 -W 2100 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/sidiomar-check`. Local PDF SHA-256 verified: scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; Air/Logistics `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; maps `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same edition and informed map rereading, not an independent edition or exhaustive errata/alternate-location search. No code/data changes, route proof or gameplay tests.

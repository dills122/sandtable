# Benghazi coordinate: independent ruling review

**Scope and result, November 17, 2026:** Review of RUL-GRA-05's named-city
interpretation, not a general map or seam review. Recommend retaining A4827 as
Benghazi for the two named setup rows as a **versioned project interpretation**;
this review does not establish an official correction or authorize release
admission. The scenario's printed B4827 must remain in source provenance.

## Source observations

Local copies were SHA-256 checked before inspection against the research queue;
PDF page numbers below are one-based. Map and scenario pages and the distance
chart were visually inspected. The errata's four pages were text-searched for
Benghazi, 4827, and the two relevant case numbers; the search is bounded to that
copy and does not establish absence from other errata.

| Original source | Locator and observation | SHA-256 |
| --- | --- | --- |
| [Five map sheets](https://spigames.net/PDFv10/CNA_Maps.pdf) | PDF p. 1, Map A, Summary of Important Locations: Benghazi **A4827**, marked major city, port and victory condition. The map depicts Benghazi on Map A's coast; city artwork obscures its printed hex digits, so the summary is the legible coordinate evidence. | `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030` |
| [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) | PDF p. 3, printed p. 27, §60.31: **Benghazi (B4827)**, Benghazi Garrison. The same page's §60.34 names Benghazi in the normal supply table without a coordinate. | `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66` |
| Same scenario booklet | PDF p. 4, printed p. 28, §60.92(B): **Benghazi (B 4827)**, one regular supply unit, 10 ammo and 10 fuel each; §60.92 says this Land-only table replaces §60.34. These are two distinct setup provisions, not two supplies to add together. | same as above |
| [Air and Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) | PDF p. 25, §37.42 Land Distance Chart labels **Benghazi (A4827)**. Its footnote states that corresponding numbered hexes on adjacent map sections are 33 hexes apart; it does not authorize identifying A4827 with B4827. | `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4` |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | PDF p. 3, §60.31 clarifies duplicated XXI Corps artillery under “Anywhere in Libya”; it does not address Benghazi. Search of this four-page copy found no Benghazi, 4827 or §60.92 entry. | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |

Map A and §37.42 are mutually corroborating printed references, but neither is a
second edition or an official adjudication of the scenario text. The two
scenario occurrences may be a repeated letter error; repetition could also
reflect a shared source. These observations do not prove the publisher's intent.

## Review disposition and consequences

For a named-city interpretation, the city location summary and map's placement
make A4827 more coherent than interpreting the named Benghazi rows at B4827.
The literal-reading alternative preserves both scenario coordinates but leaves
the named city displaced from the map's stated city. Deferral remains valid
until the project accepts a ruling. This review supports RUL-GRA-05's scoped
choice, independently of whether that choice is later admitted; it does not
claim authority to change project status.

If accepted and versioned, apply the choice separately to the §60.31 garrison
and §60.92(B) replacement supply row. Preserve both B4827 source readings and
the selected A4827 interpretation in provenance; leave B4827 a distinct hex,
not an alias. Check city identity and placement, distinct B4827 handling,
Land-only replacement versus normal supply, content/ruling hash and replay or
old-version behavior. Missing or contradictory map identity must reject rather
than silently relocate. Relevant implementation scope is scenario setup and
supply content, named-location/map identity validation and their deterministic
setup, contract/hash and replay tests; no runtime content is changed here.

This choice proves no seam crossing, movement, control, supply route or complete
map coverage. It does not resolve the 11 distance discrepancies or the printed
chart's triangle violations. Existing readiness and admission gates remain
unchanged; rights and any later authoritative clarification remain open.

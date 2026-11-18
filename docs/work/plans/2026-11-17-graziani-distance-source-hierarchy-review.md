# Distance source hierarchy: independent review

**Disposition:** The [source-hierarchy note](2026-11-17-graziani-distance-source-hierarchy.md) accurately reports the checked chart, six contradictions and bounded errata result. No checked correction selects 78, 80, or a particular erroneous cell. Preserve the distinction between printed source fact, conditional mathematical inconsistency, and a separately justified correction.

## Chart and footnote

Visually checked [Air/Logistics PDF](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) pp. 24–25, SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Section 37.42 prints Barce `B5504`–Bir-el-Qatrani `D2903` as **80**. The six non-starred intermediate pairs read Ben Gania `20+58=78`, Mechili `20+58=78`, C2803 `46+33=79`, Tobruk `40+38=78`, Fort Maddalena `61+17=78`, and Bardia `56+22=78`. They are distinct leg cells in the same chart, not independent source editions.

The footnote specifies shortest hex distance, origin excluded and destination included, assuming a sea grid where needed, and same-numbered hexes on neighboring sheets 33 apart. Page 24's axis procedure distinguishes shortest distance from actual flight path; its Tobruk–Ben Gania example gives 31, matching the chart. Under one common fixed-endpoint undirected graph, each two-leg walk upper-bounds the direct shortest distance; origin/destination convention counts the intermediate correctly once. An 80 shortest path cannot coexist with any reported 78/79 leg sum. The inequality does not select a wrong cell or prove 78 exact; errors in shared endpoint identity or assumptions could affect multiple entries.

## Errata check

Visually inspected all four pages of [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Page 1 map correction concerns D2228/2328 escarpment, not a distance value. Page 2 §37.31 addresses missions at major-city facilities; page 3 §39.11 references that correction. Page 4 provides omitted divisional OA tables. No identified §37.42 or Barce–Bir distance correction appears in this packet. This is not an exhaustive later-edition or external ruling search.

The note's hierarchy is appropriately evidential: source preserves printed 80 and legs; inconsistency is conditional on endpoint and graph interpretation; any specific corrected value needs separately traceable support. No runtime changes or tests.

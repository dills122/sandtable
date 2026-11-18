# Barce–Bir-el-Qatrani: distance-source hierarchy

**Finding:** The original §37.42 chart defines a common shortest-hex distance, including an assumed sea grid where needed. Its Barce `B5504`–Bir-el-Qatrani `D2903` cell prints **80**, while six pairs of other cells each sum to at most **79** (five sum to 78). Under a single fixed-endpoint undirected shortest-path metric these cannot all be correct. The sea-grid allowance does not itself explain the inconsistency. No examined official correction selects a cell or tells players which value prevails.

## Original chart and calculation

Visually inspected [Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) PDF p. 25 §37.42 and PDF p. 24 “Determining Shortest Distance,” SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. The table's stepped headings and Bir-el-Qatrani row show **80** at Barce. Neither endpoint nor any of the intermediate endpoints below is marked with the chart's major-city asterisk. The original [independent cell check](2026-11-17-graziani-distance-discrepancy-detail-review.md) transcribes the six sums as follows:

| Intermediate | Barce → intermediate | Intermediate → Bir-el-Qatrani | Sum |
| --- | ---: | ---: | ---: |
| Ben Gania B3715 | 20 | 58 | 78 |
| Mechili B4921 | 20 | 58 | 78 |
| C2803 | 46 | 33 | 79 |
| Tobruk C4807 | 40 | 38 | 78 |
| Fort Maddalena C3019 | 61 | 17 | 78 |
| Bardia C4321 | 56 | 22 | 78 |

The chart footnote says count the shortest distance in hexes, excluding the origin and including the destination; the same-numbered hex on an adjacent sheet is stated to be 33 hexes distant. It says to cut across the Mediterranean using an assumed all-sea hex grid where necessary. The preceding page gives a hex-axis construction for shortest distance, explicitly distinguishes it from actual flight path, and its Tobruk–Ben Gania example gives **31**, agreeing with that table cell. That example supports use of the chart as a shortest-distance reference, but does not validate these disputed six triangles or map seams.

For any one of the six intermediates, if both printed leg values are valid shortest paths in one shared undirected hex grid, joining the legs gives a walk no longer than the printed sum; the same origin-excluded convention counts the intermediate once across the concatenated walk. Sea hexes add permitted paths to that grid, and do not add a mandatory per-leg fee or make the concatenated walk longer. Consequently 80 cannot be the shortest value when a 78/79 walk exists. Pair-specific grids, differing endpoint identities or a non-metric interpretation would change that inference, but the inspected caption and footnote do not specify those mechanisms. The table itself does not say which direct or leg cell is wrong. Six sums are not six independent editions: they share one chart and several underlying assumptions.

## Errata and priority

Visually inspected all four pages of [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Page 1 corrects map D hexside `2228/2328` to escarpment; that concerns terrain, not a chart distance or general seam registration. Page 2 §37.31 concerns air missions at major-city facilities; page 3 §39.11 points to that addendum. Neither addresses §37.42, Barce or Bir-el-Qatrani. The inspected four-page packet gives no identified correction selecting the direct cell or legs; this is not proof that later or unlocated corrections do not exist.

The chart is direct printed evidence of **what the table says**; a two-leg inequality is conditional evidence of internal inconsistency, not an official replacement number or a general hierarchy overriding a printed entry. The separate [model comparison](2026-11-17-graziani-map-distance-decision.md) is a candidate geometry comparison, not independent publisher correction. Preserve the original 80, the conflicting legs, endpoint and common-grid assumptions, and an unresolved status until a traceable correction or independently verified source geometry supports a policy. Do not silently select 78 or suppress the discrepancy. No runtime changes or tests.

Independent same-scan check: [distance-source hierarchy review](2026-11-17-graziani-distance-source-hierarchy-review.md). Its conditional inconsistency uses the same fixed endpoints and a common undirected graph including the stipulated sea grid; no reviewed official correction picks a cell or replacement value.

# E1829 six-side audit: independent review

Reviewed Map E crop, Map A terrain key and location summary, and pinned hex and side JSON. No occupancy or movement conclusions.

## Finding

- **E1830 transport crossing deserves explicit treatment.** On the right vertical shared side, a dark double road line visibly crosses the river and hex edge near mid-side, with silhouettes obscuring parts of the line. It continues east through E1830. The pinned pair contains only `minor-river`. The draft's “cross or obscure” and “prevents a confident negative finding” understate this apparent visual/pinned difference. State the visible road crossing and the data difference, without inferring legal movement or asserting a data correction from the scan alone.
- **E1930 wording could locate crossing more precisely.** Road and railroad pass across the upper-right diagonal in its interior, east of the upper vertex; river artwork also covers this diagonal. Silhouettes obscure other portions of the routes. “Converge near upper vertex” risks placing all three at the vertex and overstating obscuration of the visible crossing.

## Side-by-side evidence

| Neighbor | Review |
| --- | --- |
| E1929 | Upper-left diagonal follows grid sequence. River artwork reaches its upper end; no pinned pair. Absence of a record does not establish absence of a feature. |
| E1930 | Upper-right diagonal; road and rail visibly cross in the river area. Pinned pair `road`, `railroad`. |
| E1830 | Right vertical; water follows side and road crosses it. Pinned pair `minor-river`, no road. |
| E1730 | Lower-right diagonal; river follows side. Pinned pair `major-river`. |
| E1729 | Lower-left diagonal; dark road and blue railroad cross the side. Pinned pair `road`, `railroad`. |
| E1828 | Left vertical; blue cross-tied railroad crosses side. Pinned pair `railroad`. |

Central printed label and several adjacent labels are obscured; placement follows legible numbering and grid continuity. Key supports road, railroad, city, river and training-area symbol readings; silhouettes overlap artwork and are not evidence of scenario units. Location summary lists Cairo as E1730/1829/1830/1930/1931 with major city and victory-condition marks, and dashes for port and assigned garrison. Pinned E1829 is major-city terrain and settlement, non-coastal; all five recorded pairs have null `side` and `up`. Source PDF, crops, and both JSON hashes match draft. This is one scan, not independent edition verification.

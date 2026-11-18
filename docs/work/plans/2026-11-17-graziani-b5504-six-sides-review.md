# Independent review: Barce B5504 six sides

Reviewed the original Map B scan and terrain key against the six pinned `hexsides.v1.json` records. The draft's observations are supported, with one material source discrepancy requiring follow-up: the `B5404|B5504` route looks like continuous paired road lines, while the pinned record says `unfinished-road`. The key depicts unfinished road as two dashed lines. Overprinting at the junction and side crossing prevents a confident identification of every individual stroke; the scan alone does not establish which source should govern a data correction.

| Neighbor | Independent visual check | Pinned record |
| --- | --- | --- |
| `B5603` | Dark escarpment band follows upper-left diagonal side. | `escarpment`, `up: B5504` |
| `B5604` | Dark escarpment band follows upper-right diagonal side. | `escarpment`, `up: B5504` |
| `B5505` | Continuous paired road lines cross right vertical side; label and other linework overlap. | `road` |
| `B5404` | Continuous paired dark lines cross lower-right diagonal, resembling road; additional overlapping strokes complicate the crossing. | `unfinished-road` |
| `B5403` | Heavy dark dashes and blue cross-tied line cross lower-left diagonal together, matching track and railroad symbols respectively. | `track` |
| `B5503` | Continuous paired road lines cross left vertical side, partly overprinted by number. | `road` |

The key shows the shape of an escarpment but does not itself label high and low sides. Band location alone therefore does not independently verify either pinned `up` value. The blue route toward `B5403` is visible in addition to the pinned track; this check does not establish whether another data source represents it. The scan identifies local printed symbols, not movement legality or complete connectivity.

Compared PDF p. 2 and p. 1 key using the draft's 600 dpi crops. Confirmed SHA-256 values for scan (`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`) and pinned side file (`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`). Pinned source is `vassal:CNAv2.1.0`; scan and transcription are different source artifacts, not proof of an error in either edition.

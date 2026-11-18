# Halfaya C3922: six-side source audit

**Finding:** Under the locally supported identification of C3922, the six neighbors are C3921, C3923, C3821, C3822, C4021 and C4022. Original scan shows an escarpment on the C3921 side, a slope/track lead on C3821, and paired dashed road leads near C3822 and C4021. The C4021 junction is crowded and its pinned escarpment conflicts with the separately reviewed visual lead. Several segments are hidden by name ink, coastline or water; none is certified as a legal movement or ZOC edge.

## Identity and original marks

[Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3; terrain key p. 1. C3921 and C3923 are legible, but C3922's numeral is covered by `Halfaya Pass` and overprinted features. Its location between them and neighboring numbered grid, with Sollum dot's C4021 reading, is an inferred identification, not a direct complete numeral. Page-image directions below follow the original crop. The key uses a wide dark band for escarpment, a gold band for slope, single black dashes for track and paired black dashes for unfinished road; blue depicts water and shoreline. See [Halfaya–Sollum side lead](2026-11-17-graziani-map-halfaya-sollum-side.md) for the C4021 junction.

| Neighbor | Original visible observation | Pinned pair |
| --- | --- | --- |
| C3921 | Left vertical side has a wide dark escarpment band, with splash into the C3922 side. | `escarpment`, up C3921. |
| C3821 | Lower-left diagonal carries a gold slope-like band and a dark single dashed track crossing near its crowded western vertex. Name and adjacent bands overlap. | `slope`, `track`, up C3821. |
| C3822 | Lower-right diagonal is crossed near name text by paired dark dashes, consistent with unfinished road. | `unfinished-road`. |
| C3923 | Right vertical side passes through shoreline/water and land, with `Halfaya Pass` lettering near lower end. No distinct additional band is clear in exposed stretch. | No literal record; underlying/overprinted detail remains unknown. |
| C4022 | Upper-right diagonal is largely in blue water, with shoreline and `Sollum` lettering near western endpoint; thin grid line visible elsewhere. | No literal record; do not infer all-sea classification or no other feature from a partial visual check. |
| C4021 | Upper-left diagonal runs from crowded escarpment vertex toward coast. Paired dashes cross near shoreline, a lead for unfinished road. Exposed diagonal appears thin rather than carrying the wide adjacent escarpment bands, but endpoint splash, road and text obscure exact ownership. | `escarpment`, `unfinished-road`, up C4021. Escarpment is a **candidate discrepancy**, not a correction or proof of a feature-free side. |

Literal pair comparison used `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. The listed `up` values are pinned data, not independently established directional movement permissions. Missing pair records do not certify absence; shore, name and vertex ink conceal parts of source. Land §10.21 makes terrain and unit-specific entry material, but this image review determines neither projection nor movement. Coordinate identity, each edge and reciprocal rule need independent verification before admission.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 500 -x 6100 -y 6050 -W 1800 -H 1550 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-six3922
```

Original map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop SHA-256 `39a47865e6cff7c3ee163084aeb78f16fc023d9adad6db8e094c53bfdac7bebc` (renderer-dependent). Informed same-scan review, no independent edition, runtime changes or gameplay tests.

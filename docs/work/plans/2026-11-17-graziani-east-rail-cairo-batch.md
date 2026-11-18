# Graziani §8.78: Cairo rail branch survey

**Date:** November 17, 2026. **Disposition:** Eight further nearby sides inspected in original Map E and pinned data. Heliopolis branch supplies three clear local rail-side candidates; Nile-side continuation supplies five, some with vertex or overprint uncertainty. These are finite printed-ink observations, not an eastern-rail census, a usable route, or a finding of an Axis witness.

## Source and pinned comparison

Read [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), page 5, against the railroad symbol in its page 1 key, and literal pairs in pinned [hexside data](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). Source crop is displayed with Cairo upright. Numbering follows printed labels and grid continuity; printed city blocks and training-area silhouettes obscure parts of Cairo. The seed sides `E1828–E1829`, `E1729–E1829`, `E1829–E1930`, and `E1931–E2031` were separately reviewed in [Cairo](2026-11-17-graziani-e1829-six-sides-independent-review.md) and [Heliopolis](2026-11-17-graziani-e1931-six-sides-review.md); they are not re-certified by this batch.

| Side | Original map observation | Pinned features |
| --- | --- | --- |
| `E2031–E2132` | Blue transverse-ticked line runs from Heliopolis across diagonal toward Abassia. | `railroad` |
| `E2132–E2133` | Blue ticked line crosses vertical side toward Almaza. | `railroad` |
| `E2133–E2134` | Blue ticked line crosses vertical side east of Almaza, continuing toward map edge. | `railroad` |
| `E1729–E1629` | Blue ticked line runs south from Cairo and crosses diagonal; nearby city and river artwork crowd northern end. | `railroad` |
| `E1629–E1530` | Blue ticked line crosses diagonal near its eastern vertex beside Nile artwork; exact vertex attribution warrants review. | `railroad` |
| `E1530–E1429` | Blue ticked line crosses diagonal near its eastern vertex, west of Nile. | `railroad` |
| `E1429–E1330` | Blue ticked line follows close to eastern vertices and crosses diagonal near road, river and Helwan overprint; exact side attribution remains qualified. | `railroad`, `road` |
| `E1330–E1229` | Blue ticked line crosses diagonal close to eastern vertex; road and Nile artwork crowd the vicinity. | `railroad`, `road` |

All listed pinned records have `side: null` and `up: null`. Pinned road entries are reported literally; this batch does not adjudicate road crossings, river classification, other terrain or absence of additional features. Apparent side crossings do not independently establish the construction or damage state of any rail, line identity, current occupancy, combat-unit status, or service. Printed figures are training-area artwork, not scenario occupants. Ink near a vertex may instead belong to an adjoining side; retain those qualifications rather than treating every endpoint as certified.

## Witness and ordering limits

Local printed rail membership can support a candidate endpoint only subject to the side reading and construction interpretation. Whether any candidate lies east of the selected Matruh used hex depends on the eventual geographic west relation and D/E registration, neither established here. No continuity to the coastal line is assumed or required by the proposed reading of §8.78. Unknown rail construction, destruction policy, occupant, combat classification and snapshot remain unknown; no negative witness conclusion follows from this bounded list. See [eastern coverage](2026-11-17-graziani-rail-nonlocal-coverage.md) for rule and privacy limits. Same-scan readings are not independent-edition verification or a rights grant.

## Reproduction

```sh
pdftoppm -f 5 -l 5 -r 160 -x 2700 -y 3400 -W 1300 -H 1200 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/cairo-rail-wide
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/cairo-rail-wide.png src/Cna.Core/Content/HistoricalMap/hexsides.v1.json
```

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; crop `baa773640a918e4481de4929765c1f0376bf5d53cf70d159d487509202d703b8` (renderer-dependent); pinned hexsides `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. PDF and crop remain outside Git. No runtime or map-data changes; no gameplay tests.

# Matruh route: bounded source and control ledger

**Date:** November 17, 2026. **Status:** source evidence only; no map-data
correction, route admission or certification. Scope is the two-side
D3615–D3715–D3714 seed and the eastern junction through D3517. This is not
the complete route required by any movement, supply or rail rule. See the
[work batch](2026-11-17-graziani-playable-map-work-batch.md) for closure criteria.

## Evidence and coordinate identity

Source is the [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf),
Map D PDF p. 4 and terrain key PDF p. 1. Verified local PDF SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Key distinguishes paired dark road, blue cross-tied railroad, broken blue
unfinished railroad and single dashed track. Printed numbers below use the
canonical `D` sheet prefix; identification follows visible labels and their
grid geometry, not a georeferenced survey. In the north-up 500-dpi crop,
coast, labels and junction ink overlap grid. Pixel locators are approximate;
cross-ties or marks touching a three-cell vertex do not alone assign all
three sides. The D3713 label is obscured and identified by sequence, not a
direct reading. None of this establishes water-side passability.

This batch rechecked the existing 500-dpi context, 800-dpi junction and
D3714 crops after reading earlier comparisons. It is an informed same-scan
check, not blind transcription or independent-edition corroboration. The
[Fourth Indian reading and review](2026-11-17-graziani-fourth-indian-edges-next.md),
[D3615 review](2026-11-17-graziani-d3615-six-sides-review.md),
[junction review](2026-11-17-graziani-d3615-east-junction-review.md),
[D3516/D3517 review](2026-11-17-graziani-d3516-d3517-rail-review.md), and
[D3714 review](2026-11-17-graziani-d3714-six-sides-review.md) retain their
own detailed locators and qualifications.

## Side ledger

Pair order is immaterial to the physical side; arrows do not assert legal
travel. Pinned comparison is `hexsides.v1.json`, SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
An absent record means unrepresented, not verified clear.

| Printed pair / canonical pair | Source observation and pinned comparison | Disposition and follow-up |
| --- | --- | --- |
| 3615/3715 / D3615–D3715 | Road and cross-tied rail cross diagonal, consistent with pinned `road`, `railroad`; grid, brown fill and route ink overlap. | Positive local feature evidence. Check other features and legal state separately. |
| 3715/3714 / D3715–D3714 | Road and cross-tied rail cross vertical side near Matruh; pinned `road`, `railroad`. | Positive local feature evidence; endpoints alone do not establish a complete network. |
| 3615/3616 / D3615–D3616 | Road crosses vertical side above junction. Pinned `road`, `railroad`; continuous visible rail instead appears southwest of shared vertex. Coast, numerals and overprint limit negative reading. | Road supported; rail placement is a conflict candidate, not conclusively absent. Retain pending adjudication. |
| 3615/3516 / D3615–D3516 | Blue cross-tied centerline appears to cross diagonal near crowded vertex; pinned `track` only. | Missing rail candidate supported by informed same-scan review. Exact track crossing remains unknown; no automatic correction. |
| 3516/3616 / D3516–D3616 | Dark marks meet crowded common vertex; pinned `track`. | Crossing neither confirmed nor disproved; vertex touch insufficient. |
| 3516/3517 / D3516–D3517 | Blue cross-tied centerline crosses side interior around `(580,570–580)` in 800-dpi crop; pinned pair absent. Road and coast meet grid at northern vertex. | Missing rail candidate corroborated. Road membership, other features and passability remain unknown. |
| 3515/3615 / D3515–D3615 | Broken blue rail and slope band cross; splash toward D3615. Pinned `unfinished-railroad`, `slope`, `up: D3515`. | Consistent local reading; unfinished branch is not usable rail by graphic appearance. |
| 3613/3714 / D3613–D3714 | Road crosses slope; splash toward D3714. Pinned `road`, `slope`, `up: D3613`. | Source direction uses Land §8.35 down-side convention; legal cost and route remain separate. |

The [next eastern side](2026-11-17-graziani-d3517-d3518-rail-next.md) has
an informed source reading of road and rail matching pinned D3517–D3518;
rail lies near a vertex and that note calls for independent review. It does
not close the network. D3615 is printed with rough artwork and coastal
vicinity; D3714 has the Matruh Village/Bir dot and separate port anchor.
Terrain, shoreline and overprints do not certify all six sides or current
control. Other sides and possible branches remain outside this ledger.

## Route and control blockers

The two positive seed crossings give local road and rail marks between D3615
and D3714 via D3715. They do not establish a source-required route's actual
endpoints, all intervening and reciprocal sides, branch/bypass coverage, or
full terrain and coast effects. Trace each relevant rule's origin and
destination and every side beyond this seed before asserting connectivity or
closure. Preserve obscured and unreviewed features as unknown.

[Scenario §60.7 and rail review](2026-11-13-graziani-rail-scope.md) identify
the starting railroad endpoint at Mersa Matruh, not complete topology or
service. Distinguish printed finished/unfinished features from actual track
construction, damage and repair. Confirm applicable phase, actor, capacity,
control, occupancy and ZOC from accepted rules and authoritative state at the
relevant time; do not infer them from the source artwork or initial labels.
The [rail ZOC review](2026-11-17-graziani-rail-zoc-policy-review.md) retains
unresolved interpretation and privacy gates. A positive crossing is not a
legal move, a controlled route, or proof that any opponent or ZOC is absent.

Before changing pinned features, independently challenge the local side
attribution, accept provenance and a versioned correction, check reciprocal
identity and replay impact, and test fail-closed unknowns. A local correction
cannot itself certify route closure or scenario readiness.

## Reproduction

Existing temporary crops, inspected in this batch and independently hashed:

| Locator | SHA-256 |
| --- | --- |
| Map D 500 dpi, `-x 3900 -y 6750 -W 1500 -H 1350` | `fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191` |
| Map D 800 dpi, `-x 7500 -y 11800 -W 1300 -H 1100` | `7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549` |
| Map D 500 dpi, `-x 3750 -y 6350 -W 1550 -H 1550` | `51e978cf0d3627bfd7c413b76dc481303c40dbdc111433769ee28c2189471ae3` |

Use `pdftoppm -f 4 -l 4 -r <dpi> <crop options> -png -singlefile`
with the verified PDF; complete commands and key crop are in the linked
readings. PNG bytes can vary by renderer; PDF hash and crop coordinates
identify source. Temporary files are not durable inputs; rights and durable
source storage remain separate work. No runtime or map data changed; no
gameplay tests run.

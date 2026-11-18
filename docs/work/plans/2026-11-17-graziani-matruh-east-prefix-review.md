# Source review: Matruh eastbound prefixes

**Date:** November 17, 2026. **Disposition:** Support the scoped visual
prefixes and their uncertainty. No reviewed observation establishes a complete
route, a legal move, or an approved map correction. This is an informed
recheck of the same scan and crops after reading the proposal, not a blind
transcription or independent edition.

| Crossing | Source recheck and pinned comparison |
| --- | --- |
| D3714–D3715 and D3715–D3615 | Context crop shows paired road and blue cross-tied rail across both boundaries. Both records contain `road` and `railroad`. |
| D3615–D3616 | Paired road crosses the vertical side. Pinned data also records `railroad`, but the visible blue centerline turns across the southwest diagonal; crowded ink, coast and label preclude a categorical absence finding. |
| D3615–D3516 | Blue centerline crosses the diagonal near its crowded vertex and continues southeast. Pinned entry contains `track` only; missing rail remains a candidate. Marks at the adjacent D3516–D3616 vertex do not establish that side's track or road. |
| D3516–D3517 | Blue centerline crosses the vertical side interior; neither orientation has a pinned entry. Missing rail remains a candidate; road and other features remain unverified. |
| D3517–D3518 | Road and blue centerline cross the vertical side, the latter close to its southern vertex. Pinned entry contains `road` and `railroad`. Its connection to Matruh depends on the preceding candidate sides. |

The D3616–D3517 road is crowded at a vertex; do not assign that crossing
from this reading. A missing record means unrepresented, not clear. Unfinished
blue rail branches toward D3515; its depiction does not establish its use.
No inference about other branches, complete connectivity, terrain or control
follows from these local sides.

The original Land rules pp. 15–16 support the stated distinction between
printed infrastructure and construction, the named active line, rail phase,
load and movement restrictions, pickup/dropoff and the separate restriction
west of an Axis-combat-occupied rail hex. Section 32.34 on p. 43 specifies
two real and/or dummy Supply Units as total load in any one direction per
OpStage under normal rail rules; its relation to general bidirectional use
remains a question. Section 32.16 defines a supply-range trace in movement
costs, with impassable terrain and enemy ZOCs unoccupied by friendly units
excluded. It does not make that trace a rail movement. Errata and scenario
claims were not independently rechecked for this review; retain the cited
scope review and rulings as separate dependencies.

**Evidence:** Original [Map D and key](https://spigames.net/PDFv10/CNA_Maps.pdf),
PDF pp. 4 and 1, SHA-256
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
inspected existing 500-dpi context crop SHA-256
`fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191`,
800-dpi junction crop
`7407f6a0e63a0df36ffd69206e6cea6cc0c80891e0e2035db60cc6a5a6960549`,
and 800-dpi D3517 crop
`a3ac860b6a24d16b754b0df7e029d860d694bbe45f387590cda368ad48309d7d`.
Compared actual pinned `hexsides.v1.json` records, SHA-256
`124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
Inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 15–16 and 43, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
No map or runtime changes; no gameplay tests.

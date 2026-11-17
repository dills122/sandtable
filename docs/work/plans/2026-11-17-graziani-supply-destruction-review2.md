# Supply destruction: source audit

**Disposition:** checked die thresholds, modifier, repeat limit and separation
from capture against the [destruction note](2026-11-17-graziani-supply-destruction-next.md).
Core transcription is sound. Counter and cargo aftermath, all-unit CP basis,
and relationship between the roll-for-each instruction and target selection
remain policy questions; no separate successful or failed attempt is inferred.

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12, 16–17, 43–44 §§5.2, 9.1–.3, 32.12–.18, 32.56,
SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Visually checked [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF pp. 1–2 for §32.17 and §9.2, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
no direct amendment identified there. The §32.0 erratum warns abstract rules
were untested. Other errata or source cases were not exhaustively inspected.

## Source findings

* §32.17 permits players to attempt destruction of Supply Units they are with,
  paying half the CPA of all units in the hex, rounding upward, and rolling one
  die for each Supply Unit in the hex. Adjusted results 1–3 destroy that
  Supply Unit; all other results leave it unaffected. The text does not
  explicitly authorize selecting a subset while omitting other Supply Units in
  that hex, or determine all friendly/enemy and dummy cases. In particular,
  the per-unit repeat limit does not by itself establish a separately legal
  later attempt against a previously omitted counter.
* If the **sum of Stacking Points in the hex during the attempt is one or
  less**, add 1 to the die. On a six-sided die, total 1 gives success for raw
  1–2 and failure for 3–6; total 2 gives success for 1–3 and failure for
  4–6. Adjusted 7 is simply outside 1–3. Apply the modifier per die from the
  relevant attempt snapshot; do not calculate it from number of counters.
* §32.14 assigns Supply Units zero stacking value. §9.12–.13 distinguishes
  an HQ counter representing its parent from its attached components; §9.21
  makes attachment, not mere co-location, material; §§9.26–.28 define shells
  and unit equivalents. Thus a parent plus its represented children must not
  automatically be double-counted as independent stacks. Do not infer that a
  shell's organizational reduction alone mathematically halves the printed
  Stacking Point number: resolve the applicable stacking value under the
  actual counter/formation rules. Zero-value or informational markers do not
  automatically raise the total above one.
* Attempt timing is before any movement in a given sub-segment of a
  Movement/Combat Segment, with **one attempt per Supply Unit per sub-segment**.
  §5.2's segment vocabulary does not unambiguously map that term to an engine
  step. Record a reviewed timing identity rather than infer a fresh retry from
  a new command; do not invent a one-per-hex or stage-wide limit. The source
  says half CPA of all units in the hex, rounded upward, but does not expressly
  resolve rounding once versus per unit or represented-parent aggregation.
* A successful result destroys the Supply Unit; it cannot remain an ordinary
  usable source. The case does not separately enumerate cargo bookkeeping,
  destination of each ammo/fuel point, attached motorization or counter/event
  identity. A destruction record should account for unavailable stock without
  silently crediting it as transferable or immediately usable elsewhere. A
  failed roll leaves the Supply Unit unaffected by this destruction result;
  CP and the die attempt still occurred. §32.13 capture, §32.15 both-zero
  removal and §32.18 dummy discovery are separate rules. §32.56 addresses
  assigned motorization loss if its unit is eliminated and capture if its unit
  is captured; whether this destruction matches elimination and how dummies
  interact with an attempt require rulings.

## Conditional checks

1. Stack sum 1, raw 2/3: success/failure; sum 2, raw 3/4: success/failure.
   Verify zero-value Supply Units do not by themselves alter the modifier.
2. One parent with attached children versus separately represented units:
   source-certified stacking and CPA snapshots; distinguish shell size from
   Stacking Points. Two 15-CPA units expose aggregate 15 versus individually
   rounded 16 as competing readings, not a resolved result.
3. Two Supply Units in a hex: one die each under the printed procedure, with
   independently applied results and no repeat of either in the same identified
   sub-segment. An omitted target and a second command require a selection
   policy; do not assert such an omission is permitted.
4. Attempt after movement in the same timing unit must not pass §32.17's
   pre-movement condition. Later timing unit only after mapping is established.
   Check a failed 15-ammo/50-fuel unit remains unaffected by the result; for
   success record stock as no longer accessible, with carrier and final
   destruction accounting gated on accepted policy.
5. Mixed real/dummy and obscured stack states: keep discovery, destruction,
   target identities and private balances in authoritative state; use only
   authorized observations, including equivalent errors and replay behavior.

No runtime changes or tests. Vectors are synthetic and not claims of reachable
scenario contact or legal formation composition.

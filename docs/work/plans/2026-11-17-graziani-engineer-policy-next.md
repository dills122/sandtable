# Engineer parenthesized defense: bounded precedence proposal

**Status:** unadmitted project interpretation for review, not a source
correction, complete combat algorithm or evidence of six-turn reachability.
`gra-engineer-defense-v1-proposed` addresses only Italian `bbb` engineers'
parenthesized defensive close-assault rating. Retain the existing fail-closed
adjudication gate until the inputs, broader combat rules and this policy are
reviewed. Parenthesized maximum TOE is a distinct source mark; this proposal
does not determine starting TOE, casualty handling or attachment legality.

## Evidence and source boundary

Visually rechecked [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 27 §4.46b and key, [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 6 and 35 §§3.23, 3.31–.33, 3.4 and 23.11, and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
pp. 1–2 §§3.4 and 23.11. Their SHA-256 values match
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This is a prompted recheck of the same scans, not a blind reading or separate
edition. The [prior follow-up](2026-11-17-graziani-engineer-conditions-followup.md)
records engineer identities, original and corrected text, and limits.

Axis `bbb` lists CPA 25, close assault `0/(1)` and maximum TOE `(1)`. The
chart's parentheses key blocks the rating if any units in the hex have ammo
**and** an unparenthesized number for that rating, and says replacement points
do not prevent use. Errata §3.4 says parenthesized strengths are used only when
attacked alone or with other units having only parenthesized strengths. Errata
§23.11 says engineers may use them only if **not** stacked with a friendly
combat unit. The errata do not explicitly settle every relationship between
these descriptions. Land §3.31 says an HQ representing attached combat units
is itself a combat unit; §3.23 permits context-specific treatment of some
non-combat units. No source passage inspected establishes that every counter
without a relevant number satisfies or fails “only parenthesized strengths.”

## Proposed precedence and scope

At the defensive close-assault determination, use one authoritative stack and
combat snapshot. The policy decides **eligibility to use the printed `(1)`**,
not the final numerical combat result. Remaining applicable ammo, supply,
strength, attack-resolution and loss rules still apply. Adjacency, ZOC or
entry into a friendly-occupied enemy-controlled hex alone is not an attack.
Do not extend this proposal to barrage, anti-armor, offensive assaults or other
rating types; where the event's classification is unresolved return
`unsupported`, not a fabricated defensive result.

Apply the following checks in order; a known disqualifier is sufficient even
if another input is unknown. Otherwise missing required classification or
rating produces `unsupported` and blocks authoritative resolution without state
mutation.

1. Require an actual enemy attack against the engineer's hex in the supported
   defensive close-assault context. If definitely no attack, `(1)` is not
   usable. This does not settle other effects of enemy contact.
2. If any **other friendly combat unit** is stacked with the engineer, block
   `(1)` regardless of that unit's ammunition or rating. Apply corrected
   §23.11 as a separate necessary condition. Determine combat-unit status for
   this rule from the accepted classification and the actual represented
   attachments, not just the visible parent counter. An HQ with attached
   combat units is a combat unit under §3.31; unresolved attachments or status
   yield `unsupported` unless another known disqualifier decides the result.
3. Apply corrected §3.4 in the context of the **defensive close-assault
   rating being evaluated**, as a project narrowing of “strengths.” Excluding
   the engineer itself and replacement points covered below, each other
   non-combat unit with a numeric defensive close-assault rating must have that
   rating parenthesized. Any unparenthesized numeric value, including `0`,
   blocks use even without ammo. Parentheses on an offensive rating or maximum
   TOE neither substitute for nor disqualify a parenthesized defensive rating.
   A source-confirmed dash meaning no applicable defensive rating is neutral;
   an absent, unparsed or uncertain rating is **unknown**, not a dash or zero.
   This dash exception and same-rating scope are project choices; the erratum's
   broader phrasing could instead exclude such stacks or consider other ratings.
4. Give the chart's explicit replacement-point sentence a narrow exception to
   step 3: separately represented, positively identified replacement points do
   not prevent use merely by being present. This does not create a combat-unit
   exception to step 2 or ignore a carrier/parent unit's own status and rating.
   A point whose identity, representation or incorporation into a unit is
   unclear yields `unsupported`; do not infer that an assigned combat unit
   ceases to block because it contains replacement points. The chart exception's
   relationship to the later general clarification is interpretive, not a proven
   official hierarchy.
5. Also apply the chart key's ammo-qualified blocker to all relevant other
   units in the hex, preserving step 4's specific replacement-point exception.
   For cases step 3 already blocks, its ammo qualifier cannot
   restore eligibility. The check remains explicit for audit and for any future
   accepted change to the broader stack interpretation. Under the present
   cumulative policy, missing ammo information must not turn an otherwise
   determined eligibility result into `unsupported` when that redundant
   check cannot affect it. If all checks pass,
   `(1)` is eligible, subject to the remaining combat prerequisites; no
   ammunition-free attack or guaranteed defense follows.

This selects the corrected engineer restriction and general attacked/stack
condition as cumulative, with the chart's replacement-point sentence a bounded
specific exception. An alternate precedence could let an unparenthesized,
non-combat unit without ammo coexist with the engineer; another could apply
“only parenthesized strengths” to every printed rating or treat a dash as
blocking. Those alternatives produce different results and are not ruled out by
this reading. No general ammo model or per-unit ammo status is invented here.

## Distinguishing scenarios and proposed checks

Each case assumes an actual enemy defensive close assault, a valid engineer,
complete otherwise applicable state, and no other restriction unless stated.
These are policy vectors, not executed tests or source-proven legal stacks.

| Other contents in engineer hex | Proposed eligibility | Distinguishing reason |
| --- | --- | --- |
| None | Eligible | Attack and stack conditions met; other combat prerequisites remain. |
| Friendly combat unit, `(1)` or unparenthesized rating, with **or without** ammo | Blocked | Corrected engineer-specific condition, independent of ammo and parentheses. |
| Confirmed non-combat unit with defensive `(1)` and offensive `0` | Eligible | Same-rating interpretation; offensive zero is not evaluated as defensive rating. |
| Confirmed non-combat unit with defensive `1` or `0`, no ammo | Blocked | General clarification selected over a chart-only ammo exception. |
| Confirmed non-combat unit with defensive `1` and ammo | Blocked | Both proposed general test and chart key block. |
| Confirmed non-combat unit with source-verified defensive dash | Eligible | Explicit proposed neutral-dash exception; broader reading differs. |
| Only positively identified, separately represented replacement points | Eligible | Bounded chart-key exception; not a new combat unit. |
| Replacement points plus a friendly combat unit | Blocked | Replacement exception cannot remove combat-unit restriction. |
| HQ counter representing an attached friendly combat unit | Blocked | Treat represented actual attachment as combat presence under §3.31; this is not an empty HQ under §3.33. |
| HQ with no combat units attached | Not blocked by §23.11 alone | Apply its own rating and other conditions separately; §3.33 excludes it from combat-unit status. |
| Unknown HQ attachments, unit classification, rating or replacement status | Unsupported unless another known blocker applies | Missing evidence must not silently enable or disable defense. |

Add tests for no attack versus actual assault, adjacency and ZOC; exact `0/(1)`
and `(1)` maximum import and replay for the 201st, 203rd and 204th; parent
and attachment transitions; typed unsupported with no mutation; and
noninterference between a friendly-unit rating and the engineer's own ammo and
combat requirements. Explicitly test a known combat blocker alongside unknown
ratings, and an unknown classification without a known blocker. Do not convert
conditional ineligibility into an unconditional zero-strength final combat
outcome, or infer offensive assault permission from defensive `(1)`.

Before admission, independently review event scope, stack representation,
case-specific combat classification, dash and zero handling, point versus
unit identity, ammo snapshot and ordinary combat prerequisites; confirm source
hierarchy and version it in contracts and saved games. This research changes no
runtime or readiness flags and ran no gameplay tests. The
[independent policy review](2026-11-17-graziani-engineer-policy-review.md)
identifies the replacement exception, represented-HQ distinction and redundant
ammo-check guards reflected above. Source precedence and admission remain open.

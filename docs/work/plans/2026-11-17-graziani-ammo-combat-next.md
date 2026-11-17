# Land-only ammunition: anti-armor and repeated combat

**Status:** LA-14 source boundary, not an admitted expenditure schedule for
anti-armor or combined participation. Land-only §32.21 prices “Assault” and
“Barrage” but does not separately label an anti-armor amount. Other cases offer
evidence for a broad meaning of assault; they do not explicitly prescribe how
to charge a battalion splitting activities or repeat charges. Do not silently
choose zero, a second full charge, or a detailed-logistics rate.

## Checked sources

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 12, 14, 19–23, 25 and 43 §§5.2, 8.25, 11.2, 12.0–.32,
14.0–.26, 15.0–.25, 15.91 and 32.11, 32.21–.25; scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Visually inspected [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §§50.12, 50.2, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
bounded extracted-text search found no explicit §32.21–.22 correction. The
errata's §32.0 warning calls abstract rules untested. Other errata and combat
cases remain possible; detailed Logistics §50 source was not independently
inspected.

## What the rules say

* §32.21 counts participating individual battalion-equivalents, not a parent
  division as one expenditure: non-phasing participation costs 1 ammo for
  assault or 2 for barrage; corresponding phasing costs are 2 or 4. §32.22
  calls for ammo expenditure at the instant of combat. §32.25 exempts
  patrolling. These numbers do not themselves define the charge for a split
  battalion, repeated participation, a probe, or anti-armor by name.
* Combat sequence §5.2 puts barrage first, then retreat, assignment,
  anti-armor, and close assault. §14.0 and §15.0 describe anti-armor and close
  assault as distinct steps; §14.24 says anti-armor ammunition is removed as
  expended, without giving the abstract quantity. §15.0 says close assault
  expends ammunition for both sides. Under §32.0/.11, Land-only supply
  expenditures use §32's abstract costs. Section 14.24 describes the general
  activity but does **not** by itself prove a nonzero Land-only debit or an
  additional detailed rate; broad assault classification remains a separate
  interpretive question.
* There is affirmative evidence that “assault” can include anti-armor:
  §3.1's glossary describes Assault as comprising Anti-Armor and then Close
  Assault Combat. Section 11.21 describes forms of assault as “Anti-Tank, Close,” while §11.22
  separately lists anti-armor or close assault as attack circumstances. The
  errata's detailed-logistics §50.12 correction says an ammunition-less unit
  must be assaulted, “either anti-armor or close,” to surrender. This supports
  a candidate application of §32.21's assault rate to anti-armor, but neither
  passage expressly resolves §32.21's meaning, charge count or participation
  aggregation. The §50.2 erratum specifies one ammo per infantry TOE point in
  its detailed context; §32.11 does not license copying that rate into the
  Land-only abstraction. Exact no-ammo combat and surrender interactions
  require their own source review.
* §14.0/.26 permit splitting a unit's TOE points between anti-armor and
  close assault, but prohibit assigning a given TOE point to both in the same
  combat segment; §14.23 provides a conditional reallocation when no defender
  armor exists. §12.15 allows forward artillery to split fire across targets;
  §12.16 prohibits back-position artillery splitting fire, and §12.18 permits
  forward artillery with necessary ratings and ammunition to participate in
  anti-tank or close assault in the same segment. These legal allocation
  possibilities do **not** say whether the abstract battalion charge is per
  activity, per target, per participating fraction, or once per segment.
* §8.25 allows the same unit to attack the same enemy unit more than once in
  an Operations Stage if it has the necessary ammunition; §5.2 allows repeated
  movement/combat segments. This establishes possible repetition, not a
  printed stage-wide ammo cap or formula for each repetition. §12.32 limits
  barrage of a target to once per combat segment from each adjacent enemy
  occupied hex, without prohibiting coordinated forward fire. §15.16/.23
  likewise limit close assault of a given hex to once per segment, while
  §15.91 defines a probe as a form of close assault. Source does not separately
  specify its §32.21 price or resolve all fractional and split cases.
* §11.24–.25 caps and extends **capability point** expenditures for a combat
  segment, even to certain uninvolved units in a hex. Do not transplant that
  CP rule into §32.21's ammunition accounting: the latter explicitly refers
  to supplied individual battalion-equivalents and participation.

## Decision boundary and distinguishing tests

Pending a reviewed versioned ruling, retain anti-armor-only, close-assault-only,
combined anti-armor/close, barrage-plus-assault, probe, divided battalion and
repeat-segment cases as distinct. A candidate charging anti-armor at §32.21's
assault rate has textual support above; exact timing and whether a later close
assault triggers another debit remain open. A candidate treating the combined
combat segment as one charge also needs a specified accounting unit, split
policy, no-fire target handling and shortfall rule. Mark unsupported rather
than assume either policy or fabricate ammunition from a hidden source.

Conditional vectors, not executed tests:

1. A single non-phasing battalion in close assault costs 1 abstract ammo and
   its phasing counterpart 2; in barrage, 2 and 4 respectively. Record instant
   of each combat and actual component participation.
2. Anti-armor-only with phasing and non-phasing units: compare broad-assault
   interpretation to narrower readings, including no separate abstract debit.
   Do not present §14.24 alone as proof of either a nonzero Land-only cost or
   an additional detailed charge.
3. One battalion splits TOE across anti-armor and close assault; verify no
   same point assigned to both, then mark amount and sharing unresolved.
   Forward artillery barrages and takes a later role; compare distinct
   activities, split targets, back-position restriction and no armor
   reallocation without preselecting debit count.
4. Repeat an attack in a later combat segment, with enough versus short ammo;
   probe versus full assault, and target already barraged/assaulted within the
   same segment. Enforce each source restriction separately from cost choice.
5. Preserve exact source stock, audience-limited events, atomic debits and
   replay; no player-facing action result should leak hidden opposing stock
   or TOE allocations. Source selection, shortage and withdrawal remain open.

No runtime changes or gameplay tests. Scenario reachability, table outcomes
and detailed rules beyond the cited context were not established.

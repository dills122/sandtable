# Land-only motorized supply: escort and reaction

**Status:** bounded source research; do not implement a combined reaction
predicate until the special and general rules are reconciled. The printed
non-phasing exception to friendly movement escort does not necessarily waive
every general reaction restriction.

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12, 15, 43–44 §§5.2, 8.51–.56, 32.18, .24, .31–.33 and .51–.58;
SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
September errata and full special movement, scenario inventory and map rules
were not checked for this question. Synthetic cases do not establish a Graziani
route or contact.

## Printed baseline

* §32.32 requires **30 Motorization Points per real Supply Unit**, **6 per
  dummy**, and attachment/detachment only during Organization; a unit without
  its minimum assigned points may not move. §32.18 says dummies move as real
  ones, with reduced motorization, and are removed upon discovery. The checked
  passages do not settle public validation of an undiscovered marker or any
  partial loss/allocation timing.
* §32.33 says a transported Supply Unit may move in the owner's Movement/
  Combat Phase **or** Truck Convoy Phase, but not both. For the friendly
  Movement/Combat Phase it must begin and remain stacked with a friendly
  combat unit. The case says this restriction has no effect on normal
  non-phasing movement. It does not expressly waive other reaction rules or
  state whether a change of escort during a phase is permitted; “a Friendly
  combat unit” does not itself specify persistent identity.
* §32.58 gives Motorization Points transporting a supply unit CPA **15**;
  if forced to exceed it for any reason they return to the last hex in which
  they had not exceeded it. They need not expend CP when stacked with combat
  units participating in combat, and may react unless the adjacent moving
  unit has CPA **35 or more**. These are attributes of the transporting
  points; §32.33 is about the supply unit carried. Define a coupled movement
  transaction without assuming the cargo can move after a carrier is lost.
* §32.24 says Motorization Points transporting a **real** Supply Unit require
  one fuel point; grouping and source choice need separate interpretation.
  It also exempts Motorization Points used for purposes other than those
  listed, positive evidence for a no-fuel dummy reading. §32.18's same-movement
  wording and grouping require review; do not settle dummy expenditure solely
  by omission. Avoid a player-facing legality oracle revealing real/dummy
  status. Fuel timing in non-phasing movement and shortage remain open.

## Reaction conflict to resolve

§8.51 describes reaction to an enemy combat unit moving adjacent to a friendly
non-phasing combat unit. §8.53(a) says Truck Convoys **not stacked with a
friendly combat unit never react**. §8.53(b) bars reaction if the adjacent
enemy's CPA is six or more higher than the reacting unit's and its player
announces a close assault; §8.53(c–d) also excludes cases including already in
an enemy ZOC or in combat/engaged. §8.55 bars entering an enemy ZOC by reaction.

For a supply carrier CPA 15, §32.58's absolute **35** threshold differs from
the general **21** threshold that would arise under §8.53(b) when the close
assault condition is also met. A source-specific overriding interpretation
could use 35 instead of 21 for these carriers; a cumulative interpretation
would still apply the conditional 21 test. §32.33's exception to its own
friendly-phase escort rule also coexists with §8.53(a)'s convoy escort rule:
one could read the latter as still applicable, or §32.58(D) as a specific
permission to react under its stated threshold. The checked text does not
expressly decide both hierarchies or whether every motorization group carrying
a Supply Unit is a Truck Convoy for §8.53(a). Do not claim unescorted reaction
is always allowed, never allowed, or that 35 automatically overrides all
general terrain, ZOC and status restrictions.

§8.54 adds that, notwithstanding §8.53, a battalion-size unit cannot pin a
division, and a company-size unit cannot pin a brigade or larger; size depends
on actual strength/organization, not just the counter label. Whether a supply
carrier has an applicable size and how this exception interacts with §32.58(D)
remain unresolved. Do not declare every announced-assault 21-versus-15 case
pinned without checking size.

## Distinguishing vectors

1. Real 29 versus 30 points and dummy 5 versus 6; attachment outside versus
   within Organization. Retain hidden identity and no opponent-facing minimum
   count oracle. Test movement phase exclusivity with stage/phase history.
2. Friendly Movement/Combat transport beginning with and continuously stacked
   with a qualifying combat unit versus starting or becoming unescorted;
   independently test phase choice and carrier capacity. Escort replacement
   mid-path, unit death and missing occupancy need a documented interpretation.
3. Non-phasing reaction with a moving enemy CPA 20, 21, 34 and 35; pair 21/34
   cases with and without a declared close assault, and with/without a friendly
   combat escort. Assert only source-backed conditions until rule hierarchy
   and convoy classification are resolved. Add battalion-versus-division and
   company-versus-brigade pinning vectors under §8.54 with verified actual
   sizes. Compare already-in-ZOC, engaged and destination-ZOC cases under
   applicable general rules.
4. For forced movement exceeding CPA 15, test restoration to the last valid
   hex on a multi-edge path; exactly 15 versus exceeding it; resource debit
   and capture/occupancy collisions separately. Same stored path and accepted
   ordering must replay deterministically. Do not presume discretionary movement
   beyond CPA is permitted or the forced return makes an illegal hex legal.

No runtime changes or tests. Source hierarchy, unit identity, cargo visibility,
fuel, precise phase history and authoritative reaction timing remain open.

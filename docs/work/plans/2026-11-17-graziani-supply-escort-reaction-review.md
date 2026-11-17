# Independent review: motorized supply escort and reaction

**Disposition:** The note accurately identifies the special/general-rule
conflicts and does not establish a legal reaction merely from a CPA value.
Add §8.54's size-based anti-pinning exception to the adjudication gate, and
preserve §32.24's explicit fuel-exemption wording when evaluating dummies.
Neither resolves the remaining source hierarchy.

## Source verification and nuances

- Land §32.32, printed/PDF p. 43, explicitly requires **30** Motorization
  Points for one real Supply Unit and **6** for a dummy; attachment and
  detachment occur only in Organization. A unit without its required minimum
  **may not be moved**. §32.18 says dummy movement follows real-unit movement
  with reduced motorization and removes the dummy upon discovery. Neither case
  answers exactly how hidden unit type is protected in validation or how a
  later carrier loss updates cargo and identities.
- §32.33 allows the transported unit to move during the owning player's
  Movement/Combat Phase **or** Truck Convoy Phase, not both. In friendly
  Movement/Combat it must begin and remain stacked with a friendly combat
  unit; the last sentence says that restriction has no effect on *normal
  non-Phasing movement*. The source does not specify whether the same escort
  must remain throughout or whether §8.53's distinct convoy restriction is
  waived. The two phases are alternative modes, not a blanket rule forbidding
  all later non-phasing reaction.
- §32.58(A–D), p. 44, says motorization transporting a supply unit has CPA
  15, returns to the last hex where it did not exceed its CPA if **forced to
  exceed** it, need not expend CP while stacked with combat units participating
  in combat, and may react unless the unit moving adjacent has CPA 35 or more.
  Its permission concerns the carrier; the relationship to the transported
  unit and each general trigger/status rule needs a defined interpretation.
  It does not state that voluntarily exceeding 15 is legal, or what to do if
  return collides with another legal constraint.
- General §8.51, p. 15, describes reaction to an **enemy combat unit moving
  adjacent** to a friendly non-phasing combat unit. §8.53(a) says
  non-motorized units, SGSUs and truck convoys not stacked with a friendly
  combat unit never react. Whether these points transporting supply are such
  a convoy, and whether §32.58(D) supplies a special reaction permission for
  a noncombat carrier despite §8.51/.53(a), are live interpretation issues.
  Do not treat its 35 threshold as proof of all prerequisites.
- §8.53(b) has **both** conditions: the adjacent moving enemy has CPA at
  least six greater than the reacting unit and its player announces a Close
  Assault by that unit against the non-phasing unit. Assuming CPA 15, the
  numeric threshold is 21. Under a cumulative reading, CPA 21 or 34 plus
  that announced assault triggers the general bar; without that announced
  assault, this clause alone does not. Under a specific-override reading,
  §32.58(D)'s 35 cutoff can be selected instead. At CPA 35 the special clause
  itself bars reaction. These are conditional comparisons, not proven outcomes
  without adjacency, size, escort, ZOC, status and route checks.
- **Additional exception:** §8.54 says, notwithstanding §8.53, no
  battalion-size unit can pin a division and no company-size unit can pin a
  brigade or larger. It directs attention to actual size, stacking points and
  TOE rather than label alone. This can affect the general §8.53 pinning
  analysis; the original note's vectors should include it. Whether a
  motorization carrier or its represented supply unit has an applicable size,
  and its interaction with §32.58(D), is not determined by the cited source
  alone. Do not declare every 21-versus-15 case pinned independently of §8.54.
- §8.53(c) bars reaction while already in an enemy ZOC, and (d) when in combat
  or Engaged; §8.55 prohibits entering an enemy ZOC during reaction, with
  ordinary movement otherwise applying. These are distinct source conditions;
  §8.56 contains attachment and detachment constraints, including no detaching
  low-CPA units simply to enable a parent to react. Whether any special case
  overrides a particular condition must be adjudicated, not inferred from
  the general existence of §32.33's non-phasing exception.
- **Fuel nuance:** §32.24 expressly requires one Fuel Point for Motorization
  Points transporting a **real** Supply Unit and then says Motorization Points
  used for purposes other than those listed do not require fuel. That latter
  sentence is positive evidence for a no-fuel dummy interpretation, not only
  silence on dummies. Nevertheless §32.18's same-movement wording, grouping,
  non-phasing expenditure timing and hidden-status implications need their
  own resolution. §32.23 describes the first movement of a fuel-requiring
  unit in an Operations Stage, not a separate printed dummy or reaction rule.

## Conditional checks

1. Under separately accepted trigger and representation rules, compare a
   15-CPA carrier against adjacent moving enemy CPA 20, 21, 34, 35 with and
   without announced Close Assault. Add a battalion-versus-division or
   company-versus-brigade pinning case under §8.54; establish actual unit
   sizes before applying it. Keep special override versus cumulative policy
   results distinct.
2. Friendly Movement/Combat starts and remains with a friendly combat unit;
   compare leaving escort and changing escorts with §32.33's separate
   non-phasing exception. Test unescorted reaction only after deciding
   §8.51/.53(a)'s applicability. Neither a mandatory nor a forbidden reaction
   follows solely from §32.58(D).
3. Real transport at 29/30 points and dummy at 5/6: verify minimum movement
   rule without exposing secret identity through opponent-facing errors.
   Compare explicitly sourced real fuel debit with the alternative reading of
   the §32.24 residual exemption; do not assume either result was played.
4. Given a forced movement path reaching exactly 15 CP then exceeding it,
   compare return to the last compliant hex with unknown intervening state;
   test capture, collision, resource and replay semantics separately. No
   route, contact, force or carrier loss is established for Graziani here.

## Sources

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 15, 43–44 §§5.2, 8.51–.56, 32.18, .23–.24, .32–.33,
.58, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
September errata SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
no direct correction to these cases found in bounded inspection. This is a
same-edition source review, not an exhaustive special-movement, fuel or
scenario audit. No runtime changes or gameplay tests.

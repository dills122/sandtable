# Independent review: Land-only Supply Unit destruction

**Disposition:** §32.17's die thresholds, +1 modifier, pre-movement timing,
per-target retry limit and stated uncertainties are represented accurately.
One material qualification: the aggregate-rounding example must not be
implemented as a freely transferable shared CP pool. §6.16 prohibits
transferring CP between units, and §6.12 treats an action as a cost to a unit.
This favors per-unit charging, though §32.17 does not explicitly settle the
rounding syntax or every represented-unit case.

## Source check

- Land §32.17, printed/PDF p. 43, says players may attempt to destroy Supply
  Units they are with, spend one-half of the CPA of **all units in the hex**
  rounding upward, and roll one die for **each Supply Unit in the hex**.
  Adjusted 1–3 destroys; another result leaves that unit unaffected. If the
  sum of Stacking Points in the hex at the attempt is **one or less**, add 1
  to the die. Thus on d6: stacking 0 or 1 succeeds on raw 1–2; stacking 2
  succeeds on raw 1–3. A raw 6 with modifier gives 7; the source does not say
  to clamp it. Read “all units” and “each Supply Unit” as printed before
  choosing actor, payer or subset semantics; the case does not separately
  specify ownership, whether voluntary selection of a subset is possible, or
  dummy handling.
- The action takes place **prior to any movement in a given sub-segment of a
  Movement/Combat Segment**, and only one attempt per Supply Unit per
  sub-segment may be made. §5.2, p. 12, instead describes a repeatable
  Movement and Combat **Phase** with Movement, Breakdown, Combat and Reserve
  Release **Segments**, followed by the other player's phases. Its terminology
  does not directly identify §32.17's sub-segment. Do not collapse the limit
  into one per stage, nor assume every order or player switch creates a new
  source sub-segment. “Players” in §32.17 does not by itself resolve whether
  a non-phasing side may initiate the action, or whether another unit's prior
  movement closes the opportunity; both remain phase-contract questions.
- §32.14 gives Supply Units zero stacking value, but separately caps how many
  occupy a hex. Land §§9.12–.16 and 9.21–.29 determine the applicable
  stacking values and exceptions: an HQ represents attached combat units,
  a garrison at its assigned location can have zero, shells and unit
  equivalents require analysis, and attached truck points have specific
  treatment. The threshold is the **sum during the attempt**, not the number
  of counters or supply units, and a zero-value supply counter is not by
  itself proof that no other units contribute. Whether every zero-stacking or
  special counter with CPA contributes to the action cost is a different
  question from its contribution to the modifier.
- §6.12–.16, p. 12, supplies per-unit CPA accounting; §6.16 specifically
  disallows transferring CP between units. Two separate 15-CPA payers would
  each spend 8 if each half is rounded up. Rounding half the aggregate once
  yields 15, but **does not explain how that amount is debited consistently**
  from both distinct units' accounts; e.g. seven from one and eight from the
  other cannot be justified as an automatic CP transfer. It is an arithmetic
  alternative, not an equally complete expenditure model. Resolve payer
  identity, parent/subunit representation and any aggregate allocation with
  source-backed rules before implementation. Do not automatically charge
  a parent and each represented child independently. §6.0 permits exceeding
  CPA with consequences; an insufficient-remaining-CP blanket rejection is
  not justified without additional rules.
- §32.13 enemy combat entry captures a Supply Unit; §32.15 removes it when
  both stocks are zero; §32.18 removes a dummy on discovery. None of these
  replaces §32.17's destruction procedure. Distinguish resulting ownership,
  carrier status and contents rather than infer capture from a destruction
  die, or infer automatic discovery timing from co-location alone. A failed
  die says unaffected, and supplies are not spent merely by that failure.

## Discriminating checks

1. One eligible 15-CPA unit, with a certified stacking sum of 1: cost 8 CP;
   raw roll 2 becomes 3 and destroys, raw 3 becomes 4 and fails. At stacking
   sum 2, raw 3 destroys and 4 fails. Sum 0 also receives the modifier, but
   whether an actor with such a stack is eligible must be separately proven.
2. Two independent 15-CPA units: per-unit rounding charges 8 each. An
   aggregate 15-point candidate remains incomplete absent a valid allocation
   rule; do not debit 15 from one or spread 7/8 while presenting it as a
   source-established unit-specific charge. A parent with attached children
   requires its own CPA/stacking representation and cannot be modeled as
   these two independent payers by assumption.
3. Two eligible real Supply Units in one hex: source directs one die for
   each. Once a particular Supply Unit was attempted, reject a duplicate in
   the same identified sub-segment; a later eligible sub-segment is a separate
   question. Do not assume a first command may freely select only one of them
   without an explicit target-subset interpretation.
4. Test pre-movement and after-movement snapshots, both player roles, assigned
   carriers, and real/dummy mixtures only under separately accepted phase,
   identity and discovery rules. Crash/retry must retain recorded die results
   and prior commits without exposing hidden identity or duplicating an
   attempt. These are future contract checks, not evidence of an action.

## Provenance

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 16–17, 43–44 §§5.2, 6.0–.16, 9.1–.2, 32.13–.18,
32.32, 32.56, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
September errata PDF pp. 1–2, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
No §32.17 correction found in that bounded inspection; no later ruling or
complete stacking audit claimed. No runtime changes or gameplay tests.

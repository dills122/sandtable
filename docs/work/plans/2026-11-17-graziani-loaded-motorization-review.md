# Loaded motorization: source audit

**Conclusion:** [loaded-motorization note](2026-11-17-graziani-loaded-motorization-next.md)
faithfully captures the checked conversion, cargo alternative, no-transfer rule
and conditional detachment. Draw-lockout duration and mixed-resource shortfall
remain open; the explicit “only while transporting” time limit applies to the
**detachment restriction**, and must not automatically settle those questions.

Fresh visual pass of [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
printed p. 26 / PDF p. 2 §§59.42, 59.62–.66 and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed p. 44 §§32.51–.56, and p. 43 §32.32. Scan SHA-256 respectively
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Same source edition as the reviewed note. Scenario initial deployment,
remainder of attachment rules, supply-loss details and errata were not
independently checked in this pass.

## Verified and bounded interpretation

* §59.63(A) converts first-line trucks hex by hex: each medium and each heavy
  gives one attached Motorization Point, absent specific scenario change. Its
  own example counts 10 light + 25 medium + 10 heavy as 35. §59.42 assigns
  listed first-line trucks to units in their listed hex. §59.63(B) handles
  second/third-line points separately and says to ignore air-facility trucks
  if Air is abstracted. No extra point follows from loading cargo.
* §59.66(A–B) says additional supplies do not count against scenario supply
  dumps; each **attached** point not initially transporting men or guns may
  start with **one ammo point or three fuel points**. “May” makes loading
  optional. The per-point alternative supports choosing different loads for
  different eligible points; it does not allow both on a single point. For
  three eligible points, choosing one ammo and two fuel loads gives 1 ammo and
  6 fuel, not three added points or a new Supply Unit.
* §59.66(B) says the unit must consume these supplies first and parenthetically
  says it may not draw on a Supply Unit to meet **any supply expenditure
  requirement**. It prohibits transfer of these supplies to a Supply Unit.
  It does not illustrate one class exhausted while the other remains, a
  shortfall exceeding the relevant cargo, or partial satisfaction. Do not
  assert class-specific draw or total lockout as settled, nor infer general
  transfer/disposal permission from the named prohibition. Distinguish
  already-exhausted cargo from cargo of an irrelevant type still present.
* Cargo-bearing points may detach from their unit **only if remaining attached
  to a subsidiary combat unit of that parent**. The source expressly confines
  this detachment restriction to the time the point transports these initial
  supplies. This is an additional restriction, not proof every subsidiary
  attachment is otherwise legal. §59.64 allows initial distributions to be
  altered after play starts under attachment/detachment rules, explicitly
  excepting §59.66(B). Handling parent loss, transfers to other entities and
  identity of remaining attached cargo still needs other source review.
* §32.53 permits points to transport units and all guns with CPA 0+; normal
  units have built-in transport. §32.54 requires historically motorized
  infantry (CPA 8+ and 10+) to receive sufficient points before assignment to
  other infantry types. Neither passage alone quantifies scenario free points,
  all unit carrying requirements or cross-hex eligibility. Do not assume the
  35-point example has 35 points available for initial cargo.
* §59.66(C) concerns **unattached** points. Each complete group of 30 other
  unattached points in one hex may start with a regular Supply Unit; Tripoli/
  Tunisia boxes count as hexes. Air-facility points have separate choices
  when relevant. §32.32 requires 30 points to transport a real unit. §59.65
  expressly allows initial supply stacking exceptions only when unavoidable
  under scenario distribution instructions, with no additional supply unit
  stacked there until normal limits are met. Thus optional extra units should
  not be treated as automatically exempt from stacking rules.

## Focused acceptance vectors

1. Per point: unloaded, ammo, fuel, forbidden ammo-plus-fuel, and already
   transporting men/guns. Assert no added motorization or dump subtraction.
2. Matching expenditure uses carried stock first; disallow transfer to Supply
   Unit. Test zero matching stock with other carried stock and partial matching
   stock: preserve unresolved results until a versioned rule decides them.
3. Detach loaded point to a qualifying subsidiary versus unrelated unit or
   unattached pool, subject to ordinary legality. After that point's initial
   cargo is exhausted, this **detachment** restriction no longer applies;
   separately test ordinary attachment and unresolved supply-draw treatment.
4. Compare 30 qualifying unattached points in one hex against 15 in each of
   two hexes and 30 attached points; enforce placement/stacking separately.
   Include partial source coverage and prevent hidden-load information leaking
   through opponent-visible errors, event metadata or legal-action lists.

No runtime changes or tests. This audit does not decide the ambiguous
expenditure/shortfall policy or establish reachable historical inventories.

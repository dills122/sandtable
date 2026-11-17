# RUL-GRA-10: Tobruk port policy review

**Disposition:** The selected 2/5 setup is a coherent provisional source
precedence choice, not an official correction. Retain the admission gate. The
conditional threshold example is sound under the stated proportional-capacity
assumption. Initial efficiency and obstruction state still need a single
accounting invariant; capture, clearance and abstract-mode applicability remain
separate decisions. This is independent page inspection prompted by the known
conflict, not a blind reading or an independent edition.

## Source check

Visually checked the hash-matched [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.7; [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
p. 15 §55.3; [Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
PDF pp. 27–28 / printed pp. 23–24 §§55.12–.18 and 55.25–.27; and
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) p. 43
§32.36. Their SHA-256 values, respectively, are
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`,
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
The [prior port review](2026-11-17-graziani-tobruk-port-independent.md)
records a bounded September errata text search; this audit did not independently
repeat that search or inspect all later material.

Scenario §60.7 explicitly states Tobruk starts at efficiency level **7** and
identifies the partially sunk San Giorgio. Chart §55.3 lists efficiency **5**,
1 incoming and 3 outgoing stacking points, and 1,700 tons; its footnote says
starting efficiency is below five due to the obstruction. Air §55.25 assigns
that obstruction a three-level reduction. Thus 5 minus 3 = **2** is a valid
interpretation of the chart and obstruction rules, but conflicts with the
scenario's explicit 7. Seven is itself a starting value; automatically
subtracting the obstruction from it is unsupported. The source does not supply
a corrected maximum or reconcile the statements. RUL-GRA-10 chooses one side
of that conflict and should continue to record the rejected source statement.

## Findings and guard conditions

1. **Threshold:** Land §32.36 permits Commonwealth sea transport of one supply
   unit per OpStage among Alexandria, Benghazi and Tobruk, subject to its
   destination having an efficiency level allowing at least 50% of listed
   capacity. Under §55.14's proportional interpretation, 2/5 = 40%, so initial
   Tobruk fails that threshold with no other modifiers. The threshold is
   inclusive: exactly 50% passes, not only values above it. At integral Tobruk
   levels under this interpretation, 2/5 fails and 3/5 passes; a synthetic
   even-maximum port tests the exact 50% boundary. This checks destination
   eligibility, not control, availability, shipment capacity or historical
   six-turn reachability. Air §55.14 discusses capacity reductions and tonnage
   rounding; whether detailed rounding or another modifier alters the Land-only
   threshold requires its own contract, rather than silently applying the 40%
   shortcut to every state.
2. **Avoid double subtraction:** define the units of every persisted field and
   choose one authoritative representation. For example, store maximum 5,
   ordinary unblocked level 5 and remaining San Giorgio reduction 3, then
   derive effective 2 once. Alternatively store effective level 2 plus a
   separately tracked remaining blockage of 3, using the latter only to govern
   authorized clearance transitions, never subtracting it again when reading
   effective level. Do not store 2 as an unblocked base and also subtract 3.
   Ordinary damage, blockage and other modifiers must have explicit composition,
   bounds and timing before either representation is executable; the source
   passages reviewed do not settle all interaction order or clamping.
3. **Capture is not clearance:** a control change alone must not silently reset
   the 2/5 selected initial condition, erase the blockage, restore five or
   subtract the three levels again. Air §55.26 describes undoing blockage with
   eligible friendly engineer battalions or Commonwealth HQ engineering
   capability, with one level per OpStage and Tobruk's 25 ammunition and 10
   stores per level in detailed rules. Section 55.18 excepts blockage from its
   ordinary per-stage recovery. These provisions support preserving blockage
   separately, but do not by themselves establish all capture effects, whether
   clearance is legal in Land-only mode, its abstract costs, or the ordering of
   clearance versus damage. Leave those gates unknown until reviewed. If an
   admitted clearance operation removes one blockage level with no other
   modifier, its proposed accounting transition is effective 2 to 3 with
   remaining blockage 3 to 2; never apply both a recomputed gain and an extra
   increment. Do not treat that illustrative transition as an authorized action.
4. **Scope risk:** current efficiency, listed maximum, throughput in tons and
   stacking points, remaining blockage and actual permitted shipment are
   different values. Inferring that §32.36 is inapplicable merely because
   detailed logistics is abstracted is unsafe: the Land rule explicitly retains
   this destination condition. Conversely, importing all detailed harbor repair,
   bombing or resource rules into Land-only mode is also unproved. The chart
   footnote's campaign language plus §60.7's contrary scenario statement still
   requires the explicit precedence ruling; neither 2 nor 7 is independently
   established as the publisher's correction.

## Proposed verification before admission

- Assert initial maximum 5, effective 2 and remaining obstruction 3 with exactly
  one subtraction; serialize and restore without changing those values. A
  second effective-level evaluation must not turn 2 into zero or a negative.
- Check 2/5 below threshold, 3/5 above, and an exact half using an even maximum;
  cover unknown efficiency and unresolved rounding without treating either as
  eligible. Test other destination prerequisites independently.
- Capture with no other event retains the selected blockage state; it does not
  imply sea transport eligibility. For an independently admitted clearance,
  verify one-level accounting, repeated operations and no automatic repair of
  the obstruction; test damage and clearance interaction only after its ordering
  is defined. These are design vectors, not evidence of a legal action or
  reachable capture.
- Preserve the conflicting 7, source/ruling identity and alternate profiles in
  fixtures and replay. Validate player-visible projections independently if
  port state has restricted visibility. Review source hierarchy, capture,
  abstract clearance, control, costs, recovery, timing and coverage before
  changing readiness or runtime behavior.

No gameplay tests or runtime changes were made in this review.

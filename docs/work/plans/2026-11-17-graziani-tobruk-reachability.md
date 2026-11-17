# Tobruk sea supply: bounded six-turn reachability check

**Result: later reachability unknown.** Source setup places Italian forces in
Tobruk, and Commonwealth supply units at Alexandria; it does not provide a
complete legal six-turn capture and shipping sequence. No exhaustive search,
certified route, combat outcome or proof of impossibility was produced. The
conditional 2/5 threshold example in [RUL-GRA-10](2026-11-12-graziani-ruling-decisions.md#rul-gra-10-tobruk-initial-port-efficiency-planning-interpretation)
remains material if the relevant state and action occur, but this check does
not establish they occur.

## Source-backed start and time window

Visually inspected hash-matched [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 3–5 / printed pp. 27–29, especially §§60.22, 60.31, 60.41, 60.7,
60.81 and 60.92; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 12, 14 and 43, §§5.2, 8.13–.19, 8.21–.23 and 32.36. Their hashes are
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
This reading uses the existing scan edition, not an independent edition.

- §60.31 places the Italian Libyan Tank Command HQ and Tobruk Garrison at
  Tobruk C4807. It also lists the Italian 64 Catanzaro Division at C4707 and
  4 CCNN Division at C4507. §60.92(B) puts three Axis supply units at Tobruk.
  These are initial placements, not a determination of their subsequent orders
  or a complete defense. The setup and Libyan location support Italian initial
  possession; the exact control predicate for port use still needs a contract.
- §60.41 places Commonwealth forces at C3922, C3721, C3520, C3320 and C3020,
  among other locations; §60.92(C) places three Commonwealth supply units in
  Alexandria and three in Cairo, plus other initial distributions. These facts
  establish potential origin stock, not its availability at a later shipping
  phase. Tobruk is listed as a Commonwealth strategic victory objective in
  §60.81, evidence that its capture is contemplated, not proof it can happen
  within this specific scenario.
- §60.22 starts turn 1 OpStage 1 and ends at the conclusion of turn 6 OpStage
  3. Under the printed three-stage sequence, there are 18 OpStages. Land §5.2
  places Naval Convoy Arrival before each player's Movement and Combat phase.
  Therefore a capture in turn 6 stage 3's movement cannot enable a **later**
  §32.36 Naval Convoy Arrival within this six-turn window. Earlier capture may
  leave a later arrival phase, subject to the other rules and ordering. This
  observation does not prove capture is possible by then, or rule out an
  already eligible port at an earlier phase.

## Conditional route to an informative state

One candidate sequence for analysis, not a demonstrated legal playthrough:

1. A Commonwealth force advances generally west from its initial map-C
   positions toward C4807, handles enemy ZOCs and defenders, then acquires
   whatever occupation/control the accepted port-use rule requires. Land §8.13
   prohibits entering an enemy-occupied hex, and §8.14 requires a stop on
   entering enemy ZOC; combat, retreat and subsequent entry must be resolved
   legally. §8.19 prohibits Commonwealth Land units moving west of Marble Arch
   A2109. It does not itself say Tobruk is forbidden, but map geometry, seams,
   terrain and the precise west relation still need certification. The listed
   nearby Axis units can move or fight, so neither their start positions nor a
   straight line proves a traversable, undefended path.
2. At a later Naval Convoy Arrival phase, a Commonwealth supply unit is present
   at an eligible origin, such as Alexandria, and all port-use, destination,
   timing and stacking prerequisites are met. Land §32.36 allows one supply
   unit per OpStage by sea to/from Alexandria, Benghazi or Tobruk, with the
   **destination** required to permit at least 50% of its listed capacity; the
   shipment is in lieu of combat-unit transport from the origin and into the
   destination. Its wording alone does not explicitly define friendly control
   of the ports. Verify applicable general shipping/port control provisions and
   Land-only interaction instead of silently treating control as either
   sufficient or unnecessary.
3. If Tobruk is the destination, its applicable efficiency at that phase must
   meet the threshold. Under RUL-GRA-10's unadmitted 2/5 interpretation and no
   other modifiers, it fails; a legal, completed clearance or a different
   source-precedence ruling could change this, but Land-only clearance, cost,
   capture effects and timing are unresolved. The obstruction does not
   automatically disappear merely because capture is hypothesized.

Direction matters: §32.36 states the threshold for the **destination**, not
both ports. A hypothetical shipment **from** Tobruk to Alexandria tests
Alexandria's threshold under that wording; Tobruk's 2/5 alone does not prove
that outbound shipment fails. It still requires a Commonwealth supply unit at
Tobruk and all other applicable conditions. The Axis setup supply units are not
shown to become Commonwealth stock merely because capture is imagined; Land
§32.13 has its own capture and immediate-use wording, whose detailed interaction
with this scenario needs review. Do not infer a free outbound supply unit.

## What would close the question

A positive proof needs a reproducible legal sequence from an accepted setup:
versioned map and seams; unit composition, movement costs, weather and ZOC;
combat, retreats and control; resource expenditure and origin stock; any
clearance operation and timing; then a specific §32.36 phase and all applicable
shipping conditions before scenario end. Include opponent decisions and RNG
outcomes rather than assuming an empty corridor. A proof of impossibility needs
sound bounds over all legal initial placements, actions, outcomes and relevant
rules, including alternate paths and timing; failure to find one route is not
such a proof. Test at least capture before versus during the final stage,
inbound Tobruk versus outbound Alexandria, 2/5 versus a certified eligible
level, and absent/consumed origin stock. Synthetic tests alone establish
conditional behavior, not six-turn reachability.

No runtime changes or gameplay tests were made. Source precedence, port
clearance and complete map coverage remain open gates.

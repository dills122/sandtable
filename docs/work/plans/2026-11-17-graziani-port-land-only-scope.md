# Graziani Land-only port scope

**Disposition:** Land §32.36 retains a destination port-efficiency threshold, so
Land-only needs a way to determine that condition when the shipment is otherwise
legal. It does **not** by itself establish that the entire detailed port system,
its tonnage and stacking-point ledgers, clearance costs, or errata §30.57's
mixed-cargo formula operate in this mode. Neither wholesale inclusion nor
wholesale exclusion is established. No runtime or data policy is selected here.

## Source boundary

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed
  p. 43 §§32.0, 32.11 and 32.36: §32.0 applies when playing Land without Air
  and Logistics; §32.11 replaces ordinary resource expenditure and says only
  the supply rules given below apply. This is direct evidence limiting detailed
  supply rules, although its reach over port transfers remains unresolved.
  Section 32.36 nevertheless permits a
  Commonwealth supply unit by sea among Alexandria, Benghazi and Tobruk during
  Naval Convoy Arrival, at most one per Operations Stage, in place of specified
  combat-unit transport from the port of origin into the port of destination.
  The endpoint wording does not establish a global exclusion of all other
  combat-unit shipping. The **destination** must have an efficiency level
  permitting at least 50% of its listed capacity. The prior [transport source
  check](2026-11-13-graziani-supply-transport-matrix.md) records the printed
  channel rules; [RUL-10 review](2026-11-17-graziani-rul10-review.md) records
  the threshold and its limits. The condition is inclusive at 50% and does not
  itself establish control, shipment availability, or reachability.
- [Scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  printed p. 28 §60.7, explicitly gives Tobruk initial efficiency **7** and
  identifies the San Giorgio obstruction. [Common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
  §55.3 lists efficiency **5**, 1 incoming and 3 outgoing stacking points and
  1,700 tons, with a footnote indicating an obstructed starting efficiency below
  five. [Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
  §§55.12–.18 and 55.25–.26 describe capacity scaling, recovery and a
  three-level blockage with clearance procedures. Those facts conflict with the
  scenario's 7; they do not establish an official correction. The reviewed
  planning interpretation of 5 minus 3 = 2 is provisional; do not subtract the
  obstruction again from the scenario's already stated starting 7. Sources and
  page inspection are recorded in [independent port review](2026-11-17-graziani-tobruk-port-independent.md)
  and [RUL-10 review](2026-11-17-graziani-rul10-review.md).
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  PDF p. 2 §§30.5, 30.57–.59, corrects the Commonwealth transfer section:
  each incoming stacking point reduces maximum stage tonnage by 10%; outgoing
  points do not. Its Tobruk example reduces 1,700 to 1,530. At ports with more
  than one incoming SP capacity, receiving at least half the maximum supply
  tonnage reduces SP level by **at least** one-third; incoming supplies do not
  affect a one-SP maximum. Section 30.59 identifies the chart as §55.3. Chart
  §55.3 instead states SP and tonnage handling do not affect one another. The
  erratum does not expressly address §32.36 or decide its applicability in
  Land-only mode. The erratum literally says “Air incoming capacity”; its
  intended meaning and scope require interpretation. It does not correct
  Tobruk's starting level. Section 32.0 of
  that packet warns that abstract rules were untested. See [follow-up review](2026-11-17-graziani-tobruk-port-followup-review.md).

## Consequences for a future contract

1. **Required service, not a mandated ledger:** answer whether the destination's
   efficiency permits the stated fraction of listed capacity at the relevant
   phase, based on an explicitly selected rule and authoritative state. That
   may require tracking efficiency and changes that an admitted profile makes
   relevant; §32.36 does not specify a storage schema or independently require
   physical tonnage, SP, fuel, ammunition or stores balances. Its destination
   test is distinct from cargo accounting, the port's control and the one-unit
   limit.
2. **Conditional example only:** if the provisional 2/5 interpretation and
   proportional capacity apply with no other modifiers, 40% fails; 3/5 passes.
   Exactly 50% passes. This does not establish actual shipping, capture,
   six-turn reachability, or the meaning of the conflicting 7. The erratum's
   1,530 is an example using listed capacity, not initial throughput.
3. **Separate unresolved decisions:** source precedence for 7 versus 5 and
   blockage; applicable efficiency/capacity formula and rounding; mode-specific
   damage, recovery and clearance, including resource costs and timing; and
   whether §30.57 applies to abstract shipments or other retained transfers,
   how it interacts with §55.3 and §55.15, and its order and rounding. The
   erratum's incoming-SP formula cannot be inferred merely from the threshold;
   equally, §32.11's expenditure abstraction and its restriction to following
   supply rules do not explicitly resolve this transfer rule's mode scope.
   Decide each against complete source context before implementing.
4. **Admission evidence:** review both directions and destination identity,
   49%/50% boundaries, control and phase, the one-unit limit and substitution
   from the specified origin into the specified destination, unknown
   or changed efficiency, blockage accounting and replay. If mixed-cargo
   handling is separately admitted, test incoming versus outgoing SP, the
   one-SP exception, at-least-half threshold, at-least-one-third reduction,
   scaling and rounding. These are proposed vectors, not proof that those
   actions or quantities are reachable.

This review used prior documented visual readings of the original scans and
checked searchable text of the September errata; the [independent scope review](2026-11-17-graziani-port-land-only-scope-review.md)
also visually checked the relevant source pages. This note did not independently
re-render the source scans or survey later corrections. No gameplay tests or
runtime changes.

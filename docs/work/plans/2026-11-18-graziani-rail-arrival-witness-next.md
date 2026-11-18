# Graziani rail arrival: witness bounds

**Disposition:** No concrete Graziani Commonwealth supply arrival was found
that proves an otherwise eligible rail movement while failing §8.73's
start-of-Operations-Stage condition. An arrival after Stage start would be a
useful conditional discriminator, but arrival timing, location and the other
rail predicates must be established first.

## Source constraints

- Land §32.31 (PDF/printed p. 43) expressly allows a Supply Unit to move by
  land or rail in its arrival Operations Stage. Section 8.73 (p. 15) requires
  a unit using rail to start that Stage on a rail hex, and §32.34 (p. 43)
  applies normal rail movement rules. If an arriving counter is not on the
  map at Stage start, a literal start-on-rail test fails even if it later
  appears on eligible track. Whether the more specific arrival provision
  changes that test is a precedence question; the text inspected supplies no
  worked example answering it.
- Land-only sequence §5.2 (pp. 11–12) starts each Operations Stage with
  initiative declaration, weather and organization, then has Naval Convoy
  Arrival Phase D and the Commonwealth Rail Movement Phase J. Phase D names
  arriving reinforcements, replacement points and ammunition points. This
  makes a **Phase-D placement** occur after Stage start and before the rail
  phase, but does not itself expressly identify Commonwealth abstract Supply
  Unit placement as occurring in Phase D. Section 20.12's Phase-D rule is
  for reinforcements and does not alone prove the same timing for every
  abstract Supply Unit received under §32.42. Do not silently treat it as a
  completed supply-arrival witness.
- Land §§32.42–.45 (pp. 43–44) place Commonwealth arrivals in Cairo, with a
  conditional Alexandria option only if more than three Supply Units are
  received in one Game-Turn; they distinguish scenario opening stock from
  planned arrivals and exempt the scenario's starting Game-Turn from ordinary
  arrivals. Commonwealth §32.47, chart PDF p. 31, has period-I outputs
  `1, 1, 2, 2, 3, 3` and covers 1940 through April 1941. One ordinary table
  result alone therefore cannot satisfy the more-than-three threshold. Whether
  additional receipt categories or batches qualify is separately unresolved;
  neither the table nor this observation proves every Alexandria arrival
  impossible. The exact arrival OpStage, receipt identity, and placement
  boundary remain open.
- Graziani §60.92(C), scenario PDF p. 5 / printed p. 29, gives three initial
  regular Supply Units each at Alexandria and Cairo, plus other starting
  stock. These are setup allocations, not evidence that those counters first
  appeared after an Operations Stage began. Initial rail runs from Alexandria
  to Mersa Matruh per Land §8.71 and scenario §60.7; neither establishes a
  particular arrival counter's chosen rail hex, available slot, route, control,
  ZOC, CP history or destination occupancy. No route is certified here.

## Conditional fixture

Given a separately established Commonwealth **new** Supply Unit received and
placed during Phase D on a source-certified rail hex, and separately valid
rail capacity, track, direction, control, occupancy, CP and destination, compare:

1. Strict §8.73 start-location reading rejects same-stage rail because that
   counter was absent at Stage start.
2. A chosen §32.31 arrival exception allows the start-location prerequisite
   to be satisfied by placement, while retaining all other rail restrictions.

This compares proposed interpretations; the fixture's arrival facts are not
proven reachable in Graziani. A delayed unit that was present on track before
the Stage is a different case. Sea transfer's destination phase is not by
itself proof that a previously received counter is a new §32.31 arrival.

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 11–12, 15, 43–44; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 4–5 / printed pp. 28–29; and [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 31. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, and
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`.
This is a bounded source check, not a complete arrival-rules or errata census.
No runtime/data changes or gameplay tests.

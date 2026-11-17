# Land-only supply expenditure: §§32.21–.25

**Scope:** Source transcription and bounded vectors for the Land Game without Air and Logistics. Printed cost does not establish that a particular scenario unit is supplied, moves, attacks, or has a valid supplying counter. Several amount, selection, timing and rounding details still require explicit adjudication.

## Ammo at combat

Section 32.21 counts supplied **individual battalion-equivalents**, not the stack value of a parent counter: a division expends for its component counters. The printed base schedule is:

| Participating unit | Assault | Barrage |
| --- | --- | --- |
| Non-phasing battalion-equivalent | 1 Ammo Point | 2 Ammo Points |
| Phasing unit | Twice corresponding non-phasing amount: 2 | Twice corresponding non-phasing amount: 4 |

These are costs for the stated participation, not a claim that every nearby unit fires or that both costs always apply. Where the same eligible unit participates in more than one activity, their combination and timing require the applicable combat rules and an accepted policy; do not assign expenditure to an entire division by its printed five-point stack alone. §32.22 requires Ammo Points to be expended **at the instant of combat**. §32.25 makes patrolling require no supply expenditure; do not add an ammo charge merely for a patrol. A unit's ammunition eligibility, phasing status, action, actual participating components and accessible friendly supply are inputs.

## Fuel for movement

Section 32.23 says a battalion-equivalent generally expends 1 Fuel Point per Operations Stage in which it moves **by land**, as opposed to rail or sea. The first time a fuel-requiring unit moves in the stage, the required one or two Fuel Points are expended from a Supply Unit. It may then move up to its CPA in combinations of movement and halts during that stage. At the instant it exceeds CPA, additional Fuel Points must be expended. The text does not precisely settle every repeated-threshold or rounding case; no general “one per hex,” automatic fuel for all movement, or unlimited movement after one charge follows.

| Entity / purpose | Printed §32.24 amount or exception |
| --- | --- |
| Tank battalion-equivalent | 2 Fuel Points for movement. |
| Tank company-equivalent | 1 Fuel Point for movement. |
| Gun-class and recce-type battalion-equivalents | 1 Fuel Point for movement; camel reconnaissance explicitly excluded. |
| Motorization Points transporting infantry units (not Replacement Points), gun-unit battalion-equivalents, or a real Supply Unit | Require 1 Fuel Point. Exact grouping/accounting for multiple such loads or points is not stated in this sentence. |
| Company-equivalents, and Motorization Points for purposes other than those listed | No fuel required under stated exception. Respect explicit tank-company cost above; do not generalize the exception to tank companies. |

Whether additional fuel when exceeding CPA repeats at later multiples, how to combine a vehicle unit's fuel with motorization carrying it, how multiple motorization loads are grouped, and how dummy-unit carriage is categorized are not answered conclusively by these lines. Record them as questions, not quantities. §32.25's patrol exemption and the land-versus-rail/sea distinction remain separate from movement legality and other CP costs.

The [independent source check](2026-11-17-graziani-supply-expenditure-review.md)
confirms the printed amounts and examples, while noting that the additional
fuel amount on exceeding CPA is not expressly established as the same as the
first payment. Exempting a camel or company does not by itself make all
associated transport free. Section 32.16's ZOC exception says friendly
**units**, not only friendly combat units.

## Drawing from supply units

Section 32.15 gives each real Supply Unit an initial and maximum capacity of 40 Ammo and 60 Fuel Points, reduced by expenditures; it is removed only when **both** balances reach zero. It allows no-cost rearrangement among supply units occupying the same hex during Organization. Scenario §60.92 supplies different initial balances for specific locations, so capacity is not proof that every initial unit contains 40/60.

Section 32.16 permits draw from **any friendly** Supply Unit within one-half the combat unit's CPA. Distance is measured as if traversed by a medium truck, except non-motorized infantry traces as infantry; the trace may not run through impassable terrain or an enemy ZOC unoccupied by friendly units. Printed examples give a non-motorized infantry unit range 5, a heavy-weapons unit transported by Motorization Points range 10, and typical recce range 23. The 23 example does not by itself prove a universal rounding policy for every half-CPA case. The text does not settle source selection among several legal units, splitting a single cost across counters, insufficient stock, whether the same tracing formula governs every fuel-using supply transporter, or deterministic simultaneous reservations. Those need separate authority/ruling; never fabricate a source or debit an enemy unit.

§32.11's general abstraction and §32.0 replace ordinary supply expenditure with the stated abstract costs; the real Supply Unit's ammo and fuel must still be tracked. Other sections may establish additional legal restrictions. Capture's “immediately and freely” clause is a distinct unresolved case and must not silently inherit normal friendly-draw accounting.

## Distinguishing source vectors

1. A non-phasing battalion-equivalent participating in assault uses 1 Ammo; phasing counterpart uses 2. In barrage the respective printed amounts are 2 and 4. Preserve event timing at combat; do not spend on an uninvolved component or a patrol.
2. In a stage, a tank battalion begins land movement: 2 Fuel Points. Subsequent halts and resumed movement within CPA do not trigger a fresh first-move charge. Exceeding CPA triggers additional fuel at that instant; exact repeated-threshold behavior stays unknown pending policy.
3. Tank company on land requires 1; eligible gun or non-camel recce battalion requires 1. Camel recce and non-tank company exception must not be charged merely because present. An eligible motorization load is distinct from a Replacement Point load; verify exact entity and purpose before charging.
4. Rail/sea movement alone does not meet §32.23's land-movement event. A qualifying real supply transport, dummy transport and unrelated motorization purpose must remain distinguishable; do not invent grouping for several loads.
5. Trace to friendly supply across eligible and blocked ZOC, across impassable terrain, and exactly at/over half CPA; test printed 5/10/23 examples, without imposing a universal unproved rounding rule. Multiple legal sources and shortfall must not select or create fuel silently.
6. After expenditure, 0 ammo with positive fuel and positive ammo with 0 fuel survive §32.15's both-zero rule; 0/0 is removed. Verify atomic replay and conservation after source choice and amount policy are decided.

## Sources and limits

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 43 §§32.0, 32.11, 32.15–.16 and 32.21–.25, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), printed pp. 28–29 §60.92, SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`. September 1979 errata PDF p. 2 contains a general note about §32.0 but no explicit correction to §§32.21–.25 in the checked pages, SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Same source edition as prior research; other source interactions and later errata remain possible. No runtime changes or gameplay tests.

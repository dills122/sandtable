# Tripoli box: initial supply capacity and air-fuel boundary

**Date:** November 18, 2026. A distinct fixed-origin case from the [C0716 fuel example](2026-11-18-graziani-scenario-witness-next.md) and [Alexandria destination capacity bounds](2026-11-18-graziani-queue-next-source.md). The printed Tripoli box has eight initial regular Supply Units and an explicit unlimited Supply Unit stacking exception. This establishes neither a legal air shipment nor all box/hex interactions.

## Direct source and arithmetic

Scenario §60.92(B), printed p. 29 / PDF p. 5, lists **eight** regular Axis Supply Units in the **Tripoli box**, each with **5 Ammo and 60 Fuel**. Immediately after that listed setup and before any stock changes, those eight hold 40 Ammo and 480 Fuel. This is fixed initial placement, not an optional dump or an assumed city-hex distribution. Other counters and earlier effects remain separate.

Land §32.14, printed p. 43, states that Tripoli/Tunisia boxes may contain an **unlimited number of Supply Units**. Thus the eight listed units do not violate a five-unit cap merely by exceeding five: that five-unit clause concerns a major-city hex, while the box has its own explicit exception. The disputed meaning of §32.14's three-unit wording for other hexes is not needed for this box observation. This does not remove ordinary non-supply stacking restrictions or independently certify the whole setup.

Section 32.37 includes a Tripoli/Tunisia box among permitted air-transport origin and destination categories and requires 12 Fuel before transport from a Supply Unit in the origin hex. **Conditional payment example:** if a separately identified full Tripoli-box unit is an eligible origin payment source, 12 Fuel deducted from it leaves that unit with 5 Ammo and 48 Fuel; the original eight together have 40 Ammo and 468 Fuel before any further change. Another initially full unit could be selected as cargo without deciding whether cargo may instead pay itself or whether sources may split payment. Section 32.15 removes a unit only when *both* contents reach zero, which this one debit does not cause.

A qualifying box endpoint does not, by itself, answer how §32.37's “hex of origin” wording is applied to payment in that box; treating the separately listed co-box payer as a valid origin source is an explicit interpretation in the example. Scenario §59.66(C) calls Tripoli/Tunisia boxes hexes **for that case**, not expressly for every rule. Likewise, the arithmetic assumes the listed stock survives unchanged to the chosen prepayment snapshot and does not assert the payer or cargo was selected.

## Remaining transport gates

Section 32.37 separately requires no enemy combat unit within five hexes of the origin, a destination occupied by a friendly division or brigade HQ counter, the Axis easternmost-division boundary, and the player's once-per-Game-Turn allowance and Organization timing. Land §8.82 prohibits Commonwealth land units entering Tripoli/Tunisia boxes, but that alone does not define five-hex distance from an off-map box to all map positions, other enemy categories, or all relevant state. Do not infer an empty radius or a legal destination. The transported unit and its supplies also face the §32.37 movement restriction in that OpStage; permitted draw remains a separate case.

Compare, after accepting a box-origin payment interpretation, a snapshot with the eight listed full units and a distinct payer versus one where the only eligible origin source has under 12 Fuel. Independently test box capacity, source identity, no enemy witness, destination and allowance; a stock-only positive cannot authorize the shipment. These are unexecuted conditional cases, not a claim of a real move or choice.

## Provenance

Visually inspected [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), printed pp. 26 and 29 / PDF pp. 2 and 5, §§59.66(C) and 60.92(B), SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF pp. 16 and 43, §§8.82, 32.14–.15 and 32.37, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. Same scans, not an independent edition; no exhaustive errata, off-map metric, enemy-force or reachability audit. Documentation only; no data/runtime edits or gameplay tests.

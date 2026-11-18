# Abstract-air origin proximity: Bardia starting witness

**Date:** November 18, 2026. Under a same-sheet geometric hex-count reading of Land §32.37, the scenario's fixed Commonwealth combat unit at C3922 is **within five hexes** of the Axis Bardia supply origin C4321. At an Organization Phase snapshot where both remain at their printed starting positions and the unit remains present, this one witness blocks air transport **out of C4321**. This is a bounded positive proximity witness, not proof of a complete setup, a legal transport, or future persistence.

## Source and local count

- Scenario §60.92(B), Land-only replacement setup, places two regular Axis Supply Units at Bardia C4321, each with 10 Ammo and 40 Fuel. Map C identifies Bardia as a coastal major-city/port hex, though artwork covers part of the printed number; see the [six-side reading](2026-11-17-graziani-c4321-six-sides.md) and [review](2026-11-17-graziani-c4321-six-sides-review.md). This is a named potential origin, not proof every air-transport prerequisite is satisfied.
- Scenario §60.41 lists the **3rd Coldstream Guards**, an infantry formation, at C3922 alongside 1st King's Royal Rifle Corps, 4th RHA and 7th Medium Artillery Regiment. Land §3.23 includes infantry-type units among combat units. The listed unit is an enemy for an Axis-owned supply origin; this specific witness does not rely on AA, HQ or dummy classification. Actual organization, changes and presence at the checked phase remain separate from the printed setup.
- A direct visual count on [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3, gives a chain of four shared hexsides: `C4321 → C4220 → C4121 → C4021 → C3922`. The Bardia number is partially obscured and the Halfaya cell is overprinted; neighboring grid and source locators identify them, with [Halfaya edge reading](2026-11-17-graziani-halfaya-edges-next.md) and review retaining local ink qualifications. This is a **four-edge upper bound** on hex separation, enough for the five-hex test; no claim about a traversable route, road continuity, escarpment passage, ownership or ZOC follows. No cross-sheet seam is involved. A separately accepted metric that imposes terrain or another restriction on §32.37 proximity would need its own authority.
- Land §32.37 bars air transport from an origin if any enemy combat unit is within five hexes. Four is inside that boundary without relying on the disputed equality-at-five edge. The [classification follow-up](2026-11-18-graziani-supply-source-followup.md) explains why this is an origin-presence test and why mode-specific removals and context-specific unit categories matter.

## Remaining state and coverage

The scenario also lists Commonwealth forces at C3926, C3721, C3520, C3320, C3020, C4131, D3714, D3612 and D3615, plus choices elsewhere including forces anywhere on Map D or E and Alexandria/Cairo alternatives. This note does not measure or resolve all of them, enumerate optional units, establish their exact parent contents, or infer absence of any opponent. Player choices, setup organization, withdrawals, weather and other earlier events need an actual phase snapshot. The starting witness ceases to prove the gate if the identified combat unit has moved, been removed, or otherwise no longer satisfies the applicable presence/classification test; another witness might still block.

An allowed origin also requires all other §32.37 conditions, including eligible endpoints, Fuel payment, destination HQ, direction, allowance and timing. No enemy witness need be disclosed through an opponent-facing validation result. This note makes no assertion about any later air move, success or cargo identity.

## Reproduction

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3400 -y 3050 -W 1700 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/axis-air-c4321-c3922
shasum -a 256 /private/tmp/axis-air-c4321-c3922.png
```

Crop SHA-256 `74d8a6d04652468924707c2793224f21d9e4652f0411826e645abfdded6f9b94`; renderer may change PNG bytes. Verified [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF pp. 3–5, SHA-256 `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF pp. 6 and 43, `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; map PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same scans as existing work, not an independent edition; no exhaustive errata, force or rules audit. Documentation only; no data/runtime edits or gameplay tests.

# Bardia abstract-air proximity witness: independent source review

**Date:** November 18, 2026. Rechecked the [starting witness](2026-11-18-graziani-air-proximity-start-witness.md) against the same scenario, Land, map and counter scans. **Disposition:** the four-edge count and conditional proximity conclusion are supported. The Organization Phase snapshot must retain the stated presence and classification conditions; no particular choice of Organization ordering or later state is established.

## Setup and identity

Scenario §60.92(B), printed p. 28–29 / PDF pp. 4–5, lists two Axis regular Supply Units at Bardia C4321, each with 10 Ammo and 40 Fuel. Scenario §60.41, printed p. 27 / PDF p. 3, lists 3rd Coldstream Guards as infantry at C3922. The [original counter scan](https://spigames.net/PDFv10/CNA_Counters.pdf), PDF p. 4, counter section 7, contains an infantry-symbol counter marked `3CldGds`; the abbreviation is consistent with that setup entry. Land §3.23 includes infantry-type units in combat units. This source chain supports the named Commonwealth infantry witness without relying on AA or a dummy classification. Actual attachment and representation are separate from the setup name.

Scenario §59.2 permits units initially in the same hex to be organized within the applicable attachment and stacking rules. Land §5.2's Organization Phase C permits reorganization and its listed segments in any desired order; Weather Phase B precedes it. An eligible unit can therefore be attached or detached before a particular air-transport check, so the initially printed separate entry does **not** by itself establish a separately deployed counter or complete current composition at every later instant. Under Land §3.31 an HQ with attached combat units is itself treated as combat; co-location and current parent/contents still need confirmation. The witness correctly conditions its conclusion on an actual snapshot where the qualifying enemy combat unit remains present at C3922, rather than assuming reorganization has no effects. No evidence here proves an earlier loss, displacement or qualifying changed state.

## Four shared sides

The following sequence is visible as four distinct adjacent printed-grid cells on Map C, without using any seam. Geometric count does not assume legal movement or feature traversability:

| Pair | Source reading |
| --- | --- |
| `C4321–C4220` | Shared diagonal; Bardia artwork and numbers obscure the former label, so identification uses its named printed location and surrounding grid. |
| `C4220–C4121` | Shared diagonal; grid and adjacent row labels locate the pair. |
| `C4121–C4021` | Shared diagonal; coast/border and escarpment art crowd portions of the shared area. |
| `C4021–C3922` | Shared diagonal; Halfaya lettering and artwork obscure the latter numeral, so the locator is inferred from surrounding grid and scenario §60.81, not claimed independently legible. |

These four edges give an upper bound of four under the stated same-sheet geometric hex-count interpretation, not a terrain-permitted route or a claim that all edge features are resolved. Land §32.37 prohibits transport **from** an origin with any enemy combat unit within five hexes; it does not impose a ZOC or traversable-route requirement in that sentence. Under the named metric and a snapshot retaining that enemy combat presence at C3922, the witness suffices to block departure from C4321. It does not prove that either supply counter survives unchanged, that all other transport gates pass, or that an opponent-facing message may identify the witness.

## Reproduction and provenance

```sh
pdftoppm -f 3 -l 3 -r 300 -x 3400 -y 3050 -W 1700 -H 1500 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/airproximity-independent
pdftoppm -f 4 -l 4 -r 300 -x 2700 -y 900 -W 600 -H 350 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/airproximity-counter
```

Checked [map](https://spigames.net/PDFv10/CNA_Maps.pdf) SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; counter PDF `/private/tmp/sandtable-counters.pdf` `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`. Counter link is the original-source locator associated with the local scan; availability and edition independence are not inferred. Same-edition source, no exhaustive errata or full force/reachability audit. Documentation only; no data/runtime edits or gameplay tests.

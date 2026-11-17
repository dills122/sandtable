# Graziani Commonwealth initial motorization: bounded accounting

**Status:** same-edition source transcription and subtotal, not an admitted
complete setup or actual placement. Under §59.63's Land-only initial conversion,
the explicitly listed rows yield **147 attached** and **115 unattached**
Motorization Points, subtotal **262**. An unlisted truck type remains
`not-listed`, not a newly proven zero. Separate owner choices, corrections,
loads, stacking and actual state remain gates.

## Source and conversion boundary

Visually rechecked [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 2–5 / printed pp. 26–29, §§59.61–.66, 60.41, 60.43 and 60.92(A,C),
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–44 §§32.0, 32.32 and 32.51–.54. Their SHA-256 values match
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Compared the existing [independent truck reading](graziani-truck-review/reading-independent.json)
after source inspection. Same scanned edition, not an independent publisher
source or a complete errata audit.

§59.63(A) converts each listed initial first-line **medium or heavy** truck
point to one attached Motorization Point in its listed hex; §59.63(B) uses the
same calculation for second/third-line trucks as unattached points. When Air
is abstracted, §59.63(B) ignores the air-facility pool. Light points do not
contribute to this conversion. Whole listed points convert one-for-one with no
fractional rounding. Preserve placement and first-/second-line provenance:
conversion replaces representation of these truck points; it does not create an
additional duplicate pool.

## First-line rows: §60.41

`—` below means the truck type was **not stated in this row**, not an
independently verified zero. The numeric column adds only explicitly listed
medium and heavy figures; light is recorded for source comparison and excluded
from §59.63's conversion.

| Printed location | Light | Medium | Heavy | Listed conversion |
| --- | ---: | ---: | ---: | ---: |
| C4131 | — | 10 | — | 10 |
| C3926 | — | 2 | — | 2 |
| C3922 | — | 15 | 5 | 20 |
| C3721 | 5 | 5 | — | 5 |
| C3520 | — | 5 | — | 5 |
| C3320 | — | 10 | — | 10 |
| C3020 | — | 3 | — | 3 |
| D3714 | 5 | 15 | 5 | 20 |
| D3612 | 5 | 5 | 5 | 10 |
| D3615 | 5 | 20 | 2 | 22 |
| E1829 | — | 20 | — | 20 |
| Alexandria E3613 and/or E3714 | 10 | 15 | 5 | 20 |
| **Listed subtotal** | **30** | **125** | **22** | **147** |

Alexandria's `and/or` location requires an actual legal allocation; its 20
converted points are the row total, not 20 at each hex. Assignments of
first-line points among eligible units in a hex and formation-specific setup
remain separate. No additional truck numbers are silently inferred from
unquantified rows for Cairo, Helwan or map D/E units.

## Second/third-line pools: §60.43

| Printed placement | Light | Medium | Heavy | Listed conversion |
| --- | ---: | ---: | ---: | ---: |
| Any hex in Cairo | — | 40 | 10 | 50 |
| Alexandria | 10 | 20 | — | 20 |
| Anywhere, maps | 15 | 40 | 5 | 45 |
| Any Air Facility | 5 | 30 | 20 | 50 **excluded** when Air is abstracted |
| **Non-air subtotal** | **25** | **100** | **15** | **115** |

The listed non-air sums are 100 medium + 15 heavy = 115, so 147 + 115 = 262.
The separate air-facility row would arithmetically have 50 medium/heavy points
but is not added to this Land-only subtotal. Cairo, Alexandria and anywhere-map
entries remain distinct source pools and placement constraints. `Anywhere,
maps` is not a second copy at every location; actual legal locations and
allocation must be recorded before determining loads. Adding an air pool,
counting either pool twice, converting light or treating blank entries as
established zero would alter the subtotal without source support.

## Optional loads and future arrivals

§59.66 permits initial load choices under separate attached and unattached
conditions. For qualifying **other unattached** points, §59.66(C) offers one
additional regular supply unit for each complete 30 motorization points **in
a hex**; that unit is additional to scenario supplies and optional, not a new
motorization point. Do not pool fractional remainders across different hexes,
round 29 up to 30, or count one point in two groups. For example, if 50
qualifying points were legally together in one Cairo hex, they could support
one such extra unit with 20 points ungrouped; 20 at Alexandria alone supports
none. Combining any separately allocated pool at a shared eligible hex is a
conditional owner choice subject to all placement rules, not proof it occurs.
Existing §60.92(C) supply, attached point loads, dummy rules and stacking
remain separate inputs. The §59.66 air-facility 50-point alternative is not
created from the ignored air pool in this mode.

§60.92(A) describes the side's **ability to bring in** Motorization Points as
one per Medium Truck Point; it is a future entitlement rule, not a substitute
for initial §59.63's medium-plus-heavy conversion or proof that a particular
shipment arrives. Do not reuse the initial heavy conversion for incoming
allowances or count initial truck rows as fresh arrivals. Monthly motorization
loss and convoy loss have independent event and rounding rules.

## Verification and unknowns

Unexecuted vectors: check each row and Alexandria split without duplication;
C3926's two medium and D3615's two heavy; ignore air-facility and light points;
retain missing versus zero; compare 29/30/59/60 qualifying points in one hex
and split remainders across distinct hexes; decline optional loads; test
already-listed scenario supply and dummy/stacking rules; reject duplicate IDs
and replay exactly. Check actual incoming medium ability separately from
initial heavy points and preserve side-safe projections of private load and
dummy identity.

Before admission, review source hierarchy and corrections, meaning of blanks,
valid placements, unit attachment and motorization priorities, location and
stacking, optional-load contents, supply/dummy interactions and all actual
arrival events. This subtotal is not proof of full initial-state completeness
or six-turn behavior. No code, runtime or readiness flags changed; no gameplay
tests ran.
The [independent source review](2026-11-17-graziani-cw-motorization-review.md)
confirms the listed rows, 147/115 subtotals, single Alexandria allocation and
air exclusion. It does not certify a complete or legally placed setup.

# Graziani Axis initial motorization: bounded accounting

**Status:** source-backed subtotal and setup accounting, not an admitted
complete initial-state certificate or a ruling on missing quantities,
locations, additional supply or later losses. The subtotals below count only
explicitly listed numbers; blank truck-type fields are retained as
`not-listed`, not silently certified as zero. Source, owner choices and any
ruleset decisions must precede runtime admission.

## Sources and distinct rules

Visually rechecked hash-matched [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 2–5 / printed pp. 26–29, §§59.61–.66, 60.31, 60.33 and 60.92(A–B),
and [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–44 §§32.0, 32.32, 32.51–.54. SHA-256 respectively
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`
and `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Compared the [independent truck transcription](graziani-truck-review/reading-independent.json)
and [abstract-logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md).
Same edition, not a separate historical source. This is not an exhaustive
setup, errata or reachability audit.

- §59.63(A) converts each **medium and heavy first-line truck point** in the
  initial hex listing to one attached Motorization Point, unless scenario
  instructions specifically modify it. §59.63(B) applies the same calculation
  to second/third-line trucks for unattached motorization; ignore air-facility
  points when Air is abstracted. Light truck points do not become points under
  this stated calculation. These are initial-setup rules.
- §60.92(A), for the Land-only scenario profile, instead states each side's
  **ability to bring in** Motorization Points equals its ability to bring in
  Medium Truck Points, one-for-one. It does not say initial heavy truck points
  disappear under §59.63, nor establish a particular arrival event, loss or
  delivery time. Land §32.52 separately addresses arrivals/replacements.
  Do not apply initial medium-plus-heavy conversion again to incoming truck
  listings, or treat a printed heavy arrival as proof of an additional Land-only
  entitlement without an accepted source interpretation.
- One-for-one conversion itself produces whole points and requires **no
  fractional rounding** for whole listed truck points. Division by 30 in
  optional loaded supply accounting below has a separate remainder question;
  monthly and convoy percentage losses have their own unresolved rounding and
  must not be applied to initial conversion.

## Axis listed initial subtotal

In §60.31 the explicit first-line medium figures sum to **260** and heavy
figures to **45**, giving **305 attached Motorization Points** from those
listed figures under §59.63(A). In particular C4218 has 25 medium + 10 heavy
= 35, and C4507 likewise has 25 + 10 = 35. The latter heavy value must not be
omitted. Separately, §60.33 lists Tripoli second/third-line 140 medium + 40
heavy = **180**, and `Anywhere in Libya` 100 medium + 25 heavy = **125**;
these yield **305 unattached points**, before legal placement and optional
loads. The sum of these explicitly listed first- and second/third-line
contributions is **610**, subject to the qualifications above and any
applicable corrections or setup exceptions. Do not add the same pool once as
trucks and again as motorization.

The separate §60.33 `any Air Facility` entry has 50 medium and 10 light;
§59.63(B) says ignore the air-facility motorization when Air is abstracted.
Thus the stated 610 subtotal excludes those 50 medium, as well as all light
trucks. It is not evidence that every placement, unit attachment, additional
supply or absence of another source has been validated. §60.33's unrestricted
within-stated-limits assignment is an owner choice, not permission to duplicate
Tripoli or Libya pools or silently allocate Libya points to an uncertified hex.
Preserve the actual source row and initial ownership/location with every point.

## Loads, remainder and conservation

§59.66(A–B) permits initial attached motorization to start loaded under its
stated ammo/fuel choices and restrictions; this is a load **on the existing
point**, not a newly created motorization point. §59.66(C) separately permits
additional regular supply units on groups of 30 qualifying unattached
motorization points **in a hex**. In profiles where air-facility points apply,
the same case permits 30 per regular unit or 50 per depot; air-facility points
are ignored in this Land-only profile.
These extra supply units are optional and additional to scenario supply, not
an automatic replacement for §60.92(B)'s listed initial units. Availability,
stacking, dummy interactions and placement must still be adjudicated.

As a conditional arithmetic example, if all 125 Libya-pool points were
lawfully co-located and otherwise eligible, `floor(125/30) = 4` full groups
with five motorization points left over; it does not establish that such a
placement is legal or that the owner chose four supply units. If split between
hexes, group **per hex**: 60 and 65 points can carry at most two plus two,
while 29 and 96 can carry zero plus three; adding remainders across hexes would
incorrectly create a fourth group in the latter case. Do not round 29 points
up to 30, spend a point in two groups, or consume the 30 points merely by
assigning their load. Whether the rules permit movement, detachment or
redistribution in each later phase is separate. Reconcile real versus dummy
and the scenario's initial stacking exception before asserting any actual
optional-load result.

Preserve initial and incoming sources as separate provenance: raw truck-type
row → initial converted point → one allocation/load; scheduled medium-truck
arrival **ability** → separately authorized future entitlement and actual
receipt. No counter, truck, carried supply or observed repeat can be counted
as a second motorization point. State-changing capture, monthly loss and
convoy loss must journal against the same identity and should never resurrect
the original truck alongside converted motorization.

## Focused vectors and open questions

Unexecuted synthetic checks:

- Recalculate each listed row, especially C4218 and C4507, Tripoli and Libya;
  verify explicitly listed 305 + 305 = 610, exclude 50 air-facility medium
  when Air is abstracted, preserve `not-listed` and fail closed if a complete
  setup needs its meaning. Confirm medium and heavy initial one-for-one without
  converting light or inventing fractional points.
- Compare initial 25 medium/10 heavy → 35 points with a separately authorized
  incoming medium ability of 25; ensure no second heavy-derived incoming
  allowance or truck-plus-motorization double count is invented. Validate
  arrivals against actual rules and date, not setup formulas.
- Check 29/30/59/60 same-hex unattached points, split 29+96 across two hexes,
  optional versus declined loads, illegal/unknown placement, present scenario
  stock and stacking, no reuse of point identities, and attached load
  exclusions. Group only after accepted allocation is fixed; no averaging or
  floating-point rounding across hexes.
- Verify setup snapshot and replay, deterministic IDs, duplicate import,
  ownership, actual placement, delayed receipts and side-safe disclosure.
  Handle monthly loss and convoy losses only after their separate versioned
  rounding and event-order decisions.

Review source hierarchy, every blank type and setup correction, attachment and
pool eligibility, exact locations, air abstraction, §59.66 load restrictions,
dummy/stacking interactions and no double count before admission. No code,
runtime state or readiness flag changed; no gameplay tests ran.
An [independent source review](2026-11-17-graziani-initial-motorization-review.md)
confirms the bounded subtotal and identifies the air-facility qualification
above. It does not establish a complete initial setup or legal allocation.

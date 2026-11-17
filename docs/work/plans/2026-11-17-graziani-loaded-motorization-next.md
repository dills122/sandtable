# Initial attached motorization and loaded supplies

**Finding:** Initial attached Motorization Points may carry extra ammo or fuel,
subject to a special consumption and detachment restriction. This is a player
setup choice using already existing points, not a grant of extra motorization or
an automatic load. Transporting troops or guns and optional starting cargo must
be evaluated together, including the historically motorized infantry priority.

## Source and scope

Scenario booklet printed p. 26 / PDF p. 2 §§59.42, 59.63–.66; printed p. 27 /
PDF p. 3 §60.31; p. 28 / PDF p. 4 §60.92(A), SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/
PDF pp. 43–44 §§32.15–.16, .21–.24, .32, .51–.54, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
September 1979 errata SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
no direct correction to §§59.64/.66 or 32.53/.54 found in the inspected text.
§59.62 applies these setup abstractions when Logistics is abstracted; §32.0's
Land-only profile additionally abstracts Air. Do not carry these conclusions
unchanged to a full Logistics game.

## Assignment and eligible load

- §59.63(A) converts initial first-line trucks **hex by hex**: each medium and
  each heavy truck gives one attached point, unless scenario instructions
  specifically modify it. Its own 4th CCNN example is 10 light + 25 medium +
  10 heavy = 35 points, not 45. Section 60.31 locates that listing at C4507.
  §59.42 and scenario setup instructions assign listed first-line trucks to
  units in the same hex as the owning player chooses. Preserve that locality,
  compatible formation/stacking rules, and the distinction between an
  initial assignment and any later eligible redistribution. §60.92(A)'s
  medium-truck equivalence is about *ability to bring in* points; it does not
  expressly replace the initial medium-plus-heavy calculation.
- §32.53 allows points to transport units and all guns with CPA 0+; normal
  units are considered to have built-in transport. §32.54 allows motorizing
  leg infantry but requires enough points for all historically motorized
  infantry, identified there by CPA 8+ and 10+, **before** assignment to other
  infantry types. The printed rule states a priority, not a scenario fact
  that every possible setup has sufficient eligible points. Exact transport
  requirements, allocation granularity, formation and cross-hex mechanics need
  their own established rules; do not fill gaps by assuming every attached
  point is free cargo capacity.
- §59.66(A–B) permits each attached point **not initially transporting men or
  guns** to start with **one Ammo Point OR three Fuel Points**. It is optional;
  a player may allocate ammo/fuel separately by eligible point and need not
  load all of them. Both are additional to the supplies listed in scenario
  dumps; they are carried by the existing points and are not themselves a
  Supply Unit. Do not grant one ammo *and* three fuel on the same point, count
  these quantities against dump contents, or add another truck point.
- §59.66(B) says the unit must consume these supplies first and may not draw on
  a Supply Unit to meet supply expenditure; the initial supplies may not be
  transferred to a Supply Unit. While carrying them, points may detach from
  the unit **only if remaining attached to a subsidiary combat unit of that
  parent**. The special detachment restriction lasts only while transporting
  these initial supplies. Section 59.64 otherwise allows initial distributions
  to be changed once play begins according to Land attachment/detachment
  rules; it does not remove the temporary restriction or make redistribution
  free of other ordinary rules.

## Distinctions and open questions

- §59.66(C) is a different option for **unattached** motorization: each
  complete group of 30 other unattached points in the same hex may start
  transporting one regular Supply Unit. §32.32 likewise requires 30 to
  transport a real unit. Do not pool attached cargo points across hexes or
  create a regular unit from 30 individually loaded attached points. Tripoli/
  Tunisia boxes count as hexes for §59.66(C). Air-facility depot alternative
  applies only when Air is played; §59.63(B) removes air-facility trucks when
  Air is abstracted. Supply Unit stacking and loaded-unit initialization
  require separate checks.
- Source says consume *these supplies first* for *any supply expenditure* and
  may not draw on a Supply Unit. It does not explicitly illustrate a mixed
  case where only initial fuel remains but ammo is required, or where a
  required amount exceeds the relevant carried balance. Literal total-lockout
  versus class-specific exhaustion and partial-draw semantics need an explicit
  ruling; do not silently assume either. §§32.21–.24 determine expense types
  and timing, but do not resolve this special mixed-inventory wording here.
- §32.15 permits rearrangement among Supply Units in a hex, but §59.66(B)
  expressly bars transfer of these initial carried supplies **to** a Supply
  Unit. It does not establish an unrestricted transfer to other combat units,
  disposal rule, ownership after parent elimination, or how a point's carried
  inventory follows subsidiary attachment. Keep those unresolved rather than
  invent an event. §32.16 range and ordinary expenditure checks remain relevant
  when Supply Unit drawing becomes permitted.
- The number actually eligible depends on player assignment and transport
  obligations, not merely the hex's converted total. Ordinary source restrictions,
  initial scenario setup, and side-specific choices must be recorded before
  calculating loaded amounts. Avoid disclosing opposing private choices through
  derived or validation output without a separately established visibility rule.

## Conditional vectors

1. C4507's 35 converted points are a fixed source subtotal. If an otherwise
   legal assignment leaves exactly three attached points not transporting men
   or guns, an optional selection of one ammo load and two fuel loads gives
   **1 ammo and 6 fuel**, on those same three points. This does not assert
   three are actually free or choose their receiving units.
2. An attached point carrying one ammo may not simultaneously carry three
   fuel under the per-point *or* option; a point initially transporting men
   or guns is ineligible. A zero loaded choice is permitted. Test that no
   created point or Supply Unit appears as a side effect.
3. An owning unit with initial carried ammo and a matching ammo expenditure
   consumes that carried ammo first. Attempting to transfer it to a Supply
   Unit fails. While it remains carried, detach to an unrelated unit or into
   an unattached pool fails under §59.66(B); detachment while staying attached
   to a qualifying subsidiary combat unit is permitted by this restriction,
   subject to all other applicable rules. Once initial cargo is gone, this
   particular restriction ceases; other rules still apply.
4. Two same-hex groups of 15 qualifying **unattached** points can provide one
   optional real Supply Unit only if their grouping and all other placement
   rules permit. Groups of 15 in different hexes do not meet the same-hex 30
   condition. This tests §59.66(C), not attached point cargo.
5. With remaining carried fuel but an ammo-only need, or 1 carried ammo and
   a 2-ammo need, mark the treatment unresolved until an explicit policy is
   selected; do not make either example an authoritative shortfall result.

This is a bounded source analysis, not a full initial inventory, legal setup,
expenditure schedule, or six-turn forecast. No runtime edits or tests.
The [independent source review](2026-11-17-graziani-loaded-motorization-review.md)
corroborates the per-point load and detachment conditions. It retains the
mixed-resource lockout question and notes that optional extra supply units do
not automatically qualify for §59.65's unavoidable overstacking exception.

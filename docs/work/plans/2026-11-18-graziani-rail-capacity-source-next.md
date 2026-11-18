# Graziani rail capacity: further source and timing check

**Disposition:** Do not select cumulative boardings or peak onboard load from
these cases. Original Land rules supply new timing constraints and a useful
comparison with truck movement, but no inspected example resolves rail pickup
capacity. Keep both capacity readings as explicit, unimplemented alternatives.

## Scope and source

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 15–16 and 43–44, and [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 1, after reading the [capacity follow-up](2026-11-17-graziani-rail-capacity-followup.md)
and [policy review](2026-11-17-graziani-rail-policy-review.md). Land scan
SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
errata SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This is a targeted reread of the same original edition, not a new edition or
an exhaustive errata search. Land §§32.71–.72 concern bombardment of the
Commonwealth fleet, not rail; relevant rail cases are §§8.72–.77 and §32.34.

## New discriminators

- **Pickup is permitted, turnover count remains open.** Section 8.77 allows
  pickup and dropoff along the line, subject to stacking, direction and CP
  restrictions. Section 32.34 states two real and/or dummy Supply Units in one
  direction as the total load for an Operations Stage and applies normal rail
  rules. Neither case supplies an example defining whether a replacement
  counter boards against a consumed stage allowance or a freed onboard place.
  In a hypothetical single directional movement, drop A before boarding B,
  then drop B before boarding C: three distinct units travel, peak one aboard.
  A cumulative-two policy rejects C; a peak-two policy does not reject C on
  that measure alone. Neither result certifies the route, phase, direction,
  CP history or other conditions. Counting boarding events rather than unique
  identities would be a further separate choice if a counter reboards.
- **Truck example is not a rail ruling.** Section 8.98's original example
  explicitly splits a moving group of 50 truck points at C2515, sends 20 on,
  and later moves the remaining 30. Its text permits truck-convoy order,
  subdivision, stops and starts, subject to a stated grouping rule. That
  illustrates explicitly specified sequential movement in a *different* mode;
  it neither states a rail supply capacity nor authorizes additional rail
  stacks, reboarding, or turnover of the §32.34 load. It cannot settle the
  hypothetical above.
- **Arrival-stage transport needs a precedence decision.** Section 32.31
  expressly permits a Supply Unit to move by land or rail in its arrival
  Operations Stage. Section 8.73 requires a unit using rail to start that
  Stage on a rail hex, and §32.34 retains normal rail rules. For a unit that
  arrives after the Stage has begun, these provisions present a specific
  timing question: whether §32.31 overrides the literal start condition or
  some arrival convention satisfies it. Do not infer that every arrival is
  eligible, or that arrival-stage rail is categorically forbidden. Establish
  placement timing, location and remaining conditions separately.
- **Air followed by rail differs from arrival.** Section 32.37 places air
  transport in an Organization Phase and says an air-transported Supply Unit
  may not be moved by its owning player that Operations Stage, while still
  permitting it to be drawn upon. This directly constrains an air-then-rail
  proposal for that counter; §32.31's arrival-stage provision is not, by
  itself, a permission to chain air and rail. Involuntary movement and other
  transport interactions are outside this check.
- **CP timing still needs interpretation.** Section 8.73 refers to no CP
  expenditure for another reason during that Stage as a prerequisite to rail;
  §8.77 says no rail-using unit has expended CP in that Stage. Neither text
  provides an explicit post-rail example in the inspected pages. September
  errata §8.73 adds that units traveling by railroad may not earn
  reorganization points in that Stage. Preserve that prohibition without
  assuming it alone settles all subsequent CP expenditure or prior points.

Section 32.36's one-Supply-Unit-per-stage Commonwealth sea limit is a
separate mode and uses a different phase and port restrictions; it supplies no
rail turnover example. None of these comparisons establishes a reachable
Graziani movement or resolves §8.72's two directional stacks against §32.34's
one-direction total. No runtime or content changed; no gameplay tests run.

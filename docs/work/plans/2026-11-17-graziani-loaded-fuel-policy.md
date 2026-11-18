# Initial loaded fuel: bounded policy alternatives

**Disposition: Decision required; no gameplay admission.** The reviewed sources
support competing Land-only readings, not a settled precedence rule. These
options are project policies for comparison, not claims that the printed rules
resolve partial shortages or allocation. Scope is Land without Air and Logistics
under §32.0; a game with Air played needs its own rules analysis.

## Precedence choices

| Policy | Initial carried cargo and movement fuel | Main consequence |
| --- | --- | --- |
| **A: Scenario priority** | Preserve eligible §59.66(B) cargo and consume matching initial fuel before a permitted §32.16 draw for a §32.23 movement expense. Keep §32's amount, timing and draw conditions. | Gives effect to the scenario's special first-use provision, but requires explicit decisions for partial and mixed cargo. |
| **B: Section 32 exclusive** | Treat §32.11 and §32.23 as displacing carried-cargo expenditure and its draw restriction for Land-only movement. Pay from an eligible Supply Unit under §32. | Follows the broad exclusive language, but does not make the scenario's optional initial cargo useful for this expense. Its remaining status and detachment treatment still need a ruling. |

Section 59.66(B) expressly prohibits transferring these initial supplies to a
Supply Unit. Preserve that prohibition under A; allowing transfer under B would
be a further explicit departure, not a consequence of §32.11. Eligibility,
initial owner selection and quantities remain prerequisites:
§59.66(B) allows each eligible attached point not carrying men or guns to
start with one Ammo **or** three Fuel. Do not infer that every point starts
loaded or that an unattached point qualifies. The explicit duration note in
§59.66(B) addresses the restriction on detachment except when the point
remains attached to a subsidiary combat unit of its parent; it does not independently settle how
long the draw restriction lasts.

### Decisions needed under A

Assume **5 Fuel** as a synthetic aggregate of separately eligible movement
charges, with **3 Fuel** of matching initial cargo and an otherwise eligible
Supply Unit holding **10 Fuel**. Participating units, ownership and allocation
of cargo, payment timing and whether aggregation is permitted are deliberately
unspecified. Section 32.23 specifies one or two points for first movement and a
further cost when CPA is exceeded; it does not establish an individual 5-Fuel
charge. Compare these policies only after resolving those gates; none is
established by the printed text:

1. **Combine:** consume 3 carried Fuel and draw 2, provided all draw conditions
   hold and the selected restriction permits draw at that point. Commit the
   proposed transaction atomically; if its draw fails, leave its
   proposed debits unchanged. Separately decide the effect on any movement
   already completed before an additional charge became due.
2. **Cargo-only until exhausted:** consume no partial amount for an action that
   costs more than matching cargo; deny or defer that action, pending a separate
   lawful way to exhaust cargo. Specify eligible other expenditures or a
   deadlock disposition. No implicit free action or negative stock.
3. **Partial expenditure with separate continuation:** debit 3, then defer the
   remaining 2 and the action's outcome to a separately specified shortage
   procedure. Define durable pending state, interruption and idempotent retry,
   rollback, later payment and movement already underway before adoption; do
   not infer those rules.

Even option 1 needs a duration decision: allow draw once matching fuel is
exhausted, or only after **all** initial cargo is exhausted. With 3 Fuel and
1 Ammo carried by different eligible attached points of one unit and an assumed
5-Fuel aggregate, the former could combine 3 + 2 if allocation permits; the latter
blocks that draw while Ammo remains. With fuel-only cargo and an Ammo expense,
make the same matching-resource versus all-cargo choice explicitly; neither
result follows automatically. Decide how cargo from multiple eligible attached
points is selected and depleted, whether fractions can exist, and how zero
balances and later draws are recorded. The selected shortage procedure also
needs to cover absent, unreachable or insufficient Supply Units and competing
claims; aggregate stock alone does not establish a legal draw.

For option B, the same 5-Fuel example debits 5 from an eligible Supply Unit
and leaves the 3 carried Fuel unchanged for that expense, assuming other action
conditions hold. If no eligible unit can pay, apply the separately selected
shortage policy; do not manufacture fuel or assume failure consequences.

## Separate air prepayment decision

Section 32.37's player prepayment is **12 Fuel from an origin Supply Unit**,
not a combat-unit §32.16 draw. Initial carried cargo is not established as
payment and §59.66(B) does not authorize transfer of that cargo to the payer.
The following synthetic comparisons require all independent origin,
destination, timing, enemy-distance and other shipment conditions:

- A distinct origin Supply Unit with 12 Fuel can conditionally pay 12, leaving
  zero Fuel; check its Ammo and §32.15 removal separately, as well as survival
  of the transported unit. This arithmetic alone does not prove shipment.
- Two origin units with 7 and 5 Fuel illustrate **direct split**: the singular
  “any supply unit” supports one payer; direct split is a proposed extension.
  Decide whether the player may debit both directly. If rejected, aggregate 12
  does not itself pay the charge. If accepted, specify allocation, atomicity,
  zero-balance removal and ordering.
- The same 7 and 5 may instead be rearranged under §32.15 into a single payer
  holding 12, then paid from that unit, **if** same-hex Organization Phase
  timing, capacity, ownership, eligibility, stock, surviving payer and other
  conditions permit. This is a distinct consolidation step, not proof of direct split authority; ordering relative to
  shipment remains a separate gate. Record transfer and payment independently.
- Decide separately whether the transported Supply Unit may pay itself. If its
  payment leaves both Ammo and Fuel at zero, §32.15 removes it; decide whether
  any shipment can proceed with no surviving cargo counter. With positive Ammo
  and 12 Fuel, payment leaves the counter in play. Do not infer shipment
  legality from either arithmetic result.

## Admission gates and proposed checks

Select and version precedence, matching-resource duration, allocation, shortage
and transaction policies before authorizing gameplay. Resolve direct split,
consolidation ordering and self-payment separately; none is bundled into the
precedence choice. Establish eligible setup, actual owner selections, applicable
phase and routes. Check 0, insufficient, exact and surplus matching cargo;
mixed cargo; eligible and unavailable draw sources; failed atomic payment;
7 + 5 consolidation versus direct split; sole payer removal; deterministic
replay and conservation. Include failure and replay of each rearrangement and
payment step, no partial air-prepayment debit and the once-per-player-per-game-turn
limit.
These are unexecuted synthetic vectors, not legal historical setups or
established outcomes.

Record authoritative owner selections, point-to-unit attachments, resource
identities and amounts, policy and ruleset version, sources, ordered debits,
timing and outcomes, including failed or deferred attempts and continuations.
Replay from setup and snapshots must reproduce balances, attachments and
action outcomes without double spending; retries must be idempotent. Restrict
opposing cargo, source identities, stocks, failed-draw reasons and event details
to authorized views. Sanitize public events, legal-action responses and
intelligence requests, including denial distinctions; decide disclosure after
observation or capture separately. These are project requirements, not printed
rule claims.

**Evidence:** Based on [mode precedence analysis](2026-11-17-graziani-loaded-fuel-mode-precedence.md)
and its [source review](2026-11-17-graziani-loaded-fuel-mode-precedence-review.md),
with [independent policy review](2026-11-17-graziani-loaded-fuel-policy-review.md),
which visually checked scenario printed p. 26 / PDF p. 2 §§59.62, .66 and Land
printed/PDF p. 43 §§32.0, .11, .15–.16, .23, .37. Same scans; no independent
edition or exhaustive errata survey in this policy pass. No runtime or data
changes; no gameplay tests. Date: November 17, 2026.

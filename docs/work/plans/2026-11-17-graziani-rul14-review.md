# RUL-GRA-14 Commonwealth rail: independent source audit

**Disposition:** retain RUL-GRA-14 as a bounded *planning* choice, with
source-sensitive pickup/dropoff, western-rail restriction, capacity scope and
privacy gates made explicit before admission. The ruling does not falsely
claim that its capacity precedence is an official clarification. This is a
same-edition source check, not a completed rail contract or a historical-trip
proof.

## Findings

1. **Pickup and dropoff are source-authorized along the line.** Land §8.77
   explicitly permits units to be picked up and dropped off **anywhere along a
   rail line**, subject to stacking, direction and the no-CP condition.
   RUL-GRA-14 leaves the legal stack and batch undefined, which is appropriate,
   but its linked [original policy proposal](2026-11-17-graziani-rail-policy-next.md)
   says mixed-load units “move together” and leaves intermediate pickup open.
   Do not interpret the ruling's stack slot as requiring every unit to board at
   the same origin and remain until the same destination: that would exclude a
   source-authorized case. Define an ordered rail journey, changes in onboard
   stack, continuous compliance with the two-stacking-point cap, the two-supply
   ceiling and one supply direction, and actual timing/CP prerequisites. Whether
   a supply counter itself may be treated as a §8.77 “unit” during intermediate
   pickup remains an interpretation to check alongside §32.34, rather than a
   reason to deny ordinary-unit pickup. Test different boarding and alighting
   hexes, a load at capacity before and after a swap, and capacity exceeded at
   an intermediate segment despite valid endpoints.

2. **A separate west-of-Axis restriction needs an explicit gate.** Land
   §8.78 says no rail hex west of *any* rail hex occupied by an Axis combat
   unit may be used by the Allied player for railroad movement. This is not
   limited in the text to an adjacent enemy unit or the entered hex's ZOC.
   RUL-GRA-14's generic “controls” and “actual paths” remain open, but neither
   names this independent rule. Before admitting a trip, specify its westward
   ordering across map sheets, which rail hexes and combat units count, origin
   and path treatment, timing and incomplete-map/unknown-occupancy behavior.
   Do not replace the rule with a local ZOC test. Its answer may depend on
   hidden Axis position; action enumeration, route rejection and response
   timing need the same side-safe disclosure analysis as other hidden blockers.

3. **Two limits are correctly separate; their scope still needs definition.**
   Section 32.34's Land-only allowance is any two real and/or dummy Supply
   Units in one direction as the total load for an OpStage; §32.14 gives supply
   units zero stacking value. Section 8.75 separately says the maximum stacking
   points carried by Rail Movement is two. RUL-GRA-14 correctly retains both
   constraints and identifies its within-§8.72 interpretation as a project
   choice. The printed §8.75 wording does not explicitly say whether its two
   stacking points are evaluated per direction's stack, over both directions
   together, or at every segment during pickup/dropoff. Do not imply one of
   those scopes is source-settled. Also decide whether a dummy's zero stacking
   value and count apply to all independently authorized forms of supply
   movement under the selected precedence; do not expose its identity to an
   unauthorized audience through the choice.

4. **Opposite-direction ordinary movement is conditional, not free capacity.**
   Section 8.72 and §8.74 permit a Commonwealth stack in each direction.
   RUL-GRA-14's opposite-direction non-supply stack is conditioned on normal
   rules, so it does not claim automatic legality or a second supply allowance.
   Preserve that conditionality: route, phase, stack, §8.75, §8.77 and §8.78
   all remain relevant. The reading that all supply shares a single one-way
   stage ceiling is a deliberately selected reconciliation, not an express
   statement settling every possible §8.72/§32.34 mixed-service interaction.
   Test that the supply ceiling alone does not reject a separately valid
   ordinary opposite-direction movement, while a selected cumulative §8.75
   policy or another proven restriction may do so.

5. **ZOC is appropriately deferred; origin wording is accurate.** Section
   8.73 bans starting in enemy ZOC and entering enemy ZOC or an enemy-occupied
   hex during rail movement. Section 10.26 negates enemy-ZOC effect for all
   movement purposes in a hex with a friendly combat unit. RUL-GRA-14 does not
   select the proposal's narrower stationary-screen interpretation, and
   correctly says §8.73's **entry** wording does not expressly impose a
   separate enemy-occupancy ban on the origin. Retain absolute rail-ZOC versus
   §10.26 alternatives, whether the moving unit itself qualifies, and origin,
   transit and destination as separate tests. Unknown occupancy/ZOC/coverage
   must not be normalized to empty; a known enemy-occupied **entered** hex
   remains forbidden independent of screen policy. Other rules could govern an
   occupied origin, but were not established by the cited rail clause.

6. **Privacy and phase behavior remain admission requirements.** The ruling
   correctly warns against erasing source-authorized ZOC or dummy discovery
   and does not pretend that a private authoritative reason is a safe public
   response. Land §32.18 removes dummies on discovery; §10.16 requires a
   particular ZOC disclosure under specified movement triggers, whose rail
   application needs a separate decision. Test authorized owner, opponent and
   discovery views; equal-authorized histories must also cover legal-action
   enumeration, success/failure, event IDs, timing and reconnect. Do not insist
   on indistinguishability where the source authorizes an observation, or claim
   generic errors alone prevent an oracle. Errata §8.73 denies earning
   reorganization points to rail travelers in that stage; it does not itself
   forbid all organization actions or resolve earlier earned points. Empty
   stack, same-origin/destination, decline, failed path and phase advancement
   need their own contracts; do not silently consume a slot or partially move
   on an unresolved decision.

## Distinguishing vectors

- Within the selected cap, try two real, one real plus one dummy, two dummy,
  three counters, and one counter in each direction. Compare the alternative
  additional-service interpretation, without claiming source settlement.
- Exercise ordinary and mixed stacks at and above two stacking points, and
  test both possible per-direction versus combined-stage cap readings. Pick up
  and drop off ordinary units at different certified hexes, with intermediate
  load changes; test the same for supply only after its applicability is decided.
- Keep local path and enemy ZOC constant while moving a separate Axis combat
  unit onto a rail hex farther east: a western candidate now invokes §8.78.
  Check unknown remote rail occupancy, same-row/equality and seam ordering as
  separately specified cases; source wording alone does not settle every map
  identity or comparator.
- Compare both ZOC precedence readings at origin, transit and destination;
  test a friendly combat occupant, a traveler that may itself be combat,
  enemy-occupied entered hex and unknown incoming projection. Verify no
  premature or partial movement, no duplicate capacity consumption and
  appropriately authorized disclosure on replay.

These are proposed, unexecuted tests, not actual scenario conditions.

## Sources and limits

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 15–16 §§8.72–.78, p. 18 §10.26 and p. 43 §§32.14, 32.18,
32.34 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`),
and compared [RUL-GRA-14](2026-11-12-graziani-ruling-decisions.md#rul-gra-14-commonwealth-rail-supply-capacity-planning-interpretation)
with [prior independent policy review](2026-11-17-graziani-rail-policy-review.md).
The previous review checked [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 1 §8.73 (SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`).
Other movement, spatial, transport and later-errata rules were not exhausted.
No runtime changes or gameplay tests.

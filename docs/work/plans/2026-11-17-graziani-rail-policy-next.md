# Graziani Commonwealth rail: bounded capacity and ZOC proposal

**Status:** `gra-cw-rail-capacity-zoc-v1-proposed` is a project interpretation,
not an official clarification, complete rail contract or historical trip. It
chooses one deliberately restrictive resolution for the interactions identified
in the [source review](2026-11-17-graziani-rail-scope-review.md). Do not admit
runtime behavior until the remaining gates, source hierarchy and information
policy are accepted and versioned.

The [independent source review](2026-11-17-graziani-rail-policy-review.md)
identifies origin-occupancy, §8.75's separate two-stacking-point limit,
source-authorized disclosure and no-op distinctions that must be addressed.
The screen and stage-wide supply cap below remain policy choices, not
independently certified historical facts.

## Source and precedence

Land §8.72 permits the Commonwealth one rail-movement use per OpStage, one
stack in each direction, conveying units and/or supplies. Section 32.34, in
Land-only abstraction, permits any two real and/or dummy Supply Units in one
direction as the total load for an OpStage, subject to normal railroad rules.
The relationship between these cases is not spelled out as a transport schema.
Section 8.73 restricts starting point, CP history, enemy ZOC and occupancy;
§10.26 says the presence of a friendly combat unit in a hex negates the effect
of enemy ZOC for **all movement purposes**. Whether that general language also
limits the rail-specific ZOC prohibition is not expressly decided in §8.73.
Errata §8.73 independently denies reorganization-point earnings in a rail
stage. Scenario §60.7 starts the railroad at Mersa Matruh; a printed endpoint
does not establish complete rail topology or control.

The proposal treats §32.34 as a **supply-specific ceiling within**, not an
extra service beside, §8.72; treats §10.26 as applicable to rail ZOC effects;
and retains §8.73's enemy-occupancy prohibition. These are source-precedence
choices, not new facts. They must be selected together with their alternatives
and bound to scenario/ruleset identity.

## Proposed capacity contract

For one Commonwealth OpStage rail phase, the owner may use at most one
otherwise legal stack in each direction, as §8.72 permits. Across both
directions combined, count every real **and** dummy Supply Unit selected for
rail in that stage; at most two may be carried, and all such counters must
travel in **one** direction. No additional general-rule allowance can carry
supplies in the opposite direction. A second ordinary stack may travel in the
opposite direction only if it carries no Supply Units and independently meets
the general stack, route and other rules. Mixed supply and ordinary units in
one stack are permitted by this proposal only when a separate accepted
definition certifies a legal stack on each shared route segment. Section 8.77
permits pickup and dropoff along the line subject to its restrictions; this
does not require identical full-route origins or destinations. This policy
does not decide stack composition, loading, path continuity,
intermediate pickup, separate starting hexes or command batching. If any such
predicate is unknown, block that mixed movement; do not infer it from the
phrase “and/or.” Both directions still require their own accepted rail and
movement predicates. A direction is an accepted east/west line orientation,
not a guess from a coordinate suffix or shortest route.

The stage record retains one supply direction, the count, both stack slots,
unit/counter identities and prior usage. Treat a duplicate command or retry as
the same event, not a second load. Zero selected supplies do not choose a
supply direction. A valid second use of a stage slot is not created by sending
one supply counter first and another later; whether a legal single stack can be
constructed incrementally must be decided by the batching contract before
enabling it. Limits count Supply Unit **counters**, not ammo/fuel quantities,
and do not grant an Axis allowance. No-op, unknown, declined and invalid are
distinct authoritative states.

Alternatives: §32.34 could instead authorize a special two-counter movement
in addition to §8.72's stacks; or it could replace all general supply movement
while leaving ordinary stacks to other traffic; or it might be read as a
per-direction cap. Those choices change opposing-direction and mixed-load
outcomes. The selected total cap reflects §32.34's explicit one-direction and
total-load wording while retaining §8.72's general movement, but the source
does not explicitly prohibit every alternative service composition.

## Proposed ZOC contract

Evaluate a certified rail path and authoritative state at the accepted rail
movement snapshot. Section 8.73 prohibits entering an enemy-occupied hex; this
proposal also blocks an enemy-occupied origin as an additional project
invariant, not an express statement of that case. In each
hex with projected enemy ZOC, allow movement past that ZOC **only if** a
separately represented friendly combat unit is already present in that hex at
the accepted snapshot and remains present through passage; otherwise block.
Do not count the moving rail stack itself as that already-present screen under
this proposal. Confirm the friendly unit's combat classification and actual
position; a supply counter, dummy, unclassified parent or merely adjacent
friendly unit is not enough. A friendly combat screen negates the **ZOC
effect**, not enemy occupancy, destroyed track, other terrain or stacking
constraints. The same test at the starting rail hex is a policy choice applying
§10.26 to §8.73's starting prohibition as well as entry. Route and screen
updates must be atomic or revalidated against one consistent snapshot; do not
assume a screen remains after it moves away.

The stationary, already-present screen restriction deliberately chooses a
smaller certified set than the broad literal §10.26 text: that text does not
expressly require the screening combat unit to be separate from the moving
stack, stationary or present before entry. Alternatives are to allow the
traveling combat unit itself to supply the presence, or to treat §8.73 as a
specific absolute ZOC ban even with another friendly combat unit. Both require
an explicit decision for origin, transit and destination; they yield different
paths. This proposal does not extend movement negation to enemy **control** for
rail construction under §24.65.

## Authority, uncertainty and privacy

Only the authoritative engine may examine actual counter types, enemy
occupancy, projected/incoming ZOC and combat-unit classifications. A path is
eligible only when its loaded track, all traversed edges, relevant map
boundaries and all incoming ZOC coverage are certified. Missing map coverage,
track condition, unit identity, screen timing or capacity history yields an
authoritative `unsupported/unknown` and no partial travel, credit or capacity
consumption; a known violation is a separate private result. Neither outcome
proves that a legal historical move exists. Never compute capacity from the
opponent-facing dummy projection or reveal dummy identity to an unauthorized
audience before authorized discovery. Owners and the umpire retain authorized
access; §32.18 discovery and §10.16's applicable ZOC disclosure remain separate.

Before exposing legal actions or accepting rail orders, define an audience and
disclosure rule for attempted moves through hidden blockers. Different
validation failures, offered paths, event IDs, response timing, counter counts
or preview maps can reveal enemy units or dummy identity even when error text
is generic. If no accepted source-authorized disclosure makes two histories
distinguishable, the complete opponent-visible transcripts must remain equal;
if reliable noninterference cannot be achieved, do not expose that action
until the observation contract is resolved. This is an admission gate, not a
claim that merely hiding detailed failure codes prevents an oracle. Persist
owner choice, path, private validation, snapshot, supply identities and policy
version with exactly-once replay; publish only separately authorized projections.

## Remaining gates and distinguishing vectors

This policy does not resolve rail phase entry and actor order, stage-wide CP
before/after travel, earlier reorganization-point earnings, stack definition,
actual direction and route selection, real-map completeness, source/destination
capacity or enemy control/capture, track construction/destruction, arrival
times, and Axis rail or rolling stock. Version and implement those separately;
do not grant a rail command merely because these two decisions are made.

Synthetic tests after admission:

1. In one direction, two real, real+dummy and two dummy counters satisfy the
   count ceiling; three exceed it. One each in opposite directions fails this
   selected policy. One legal ordinary non-supply stack in the opposite
   direction is not blocked *by this supply ceiling*; mixed cargo is exercised
   only with an accepted stack/batching contract. Compare the additional-service
   and per-direction alternatives explicitly.
2. At origin, intermediate and destination independently, test projected
   enemy ZOC with a stationary friendly combat screen, without one, with only
   a non-combat counter, and with unknown classification. An enemy occupant
   blocks even with a friendly screen. Test a traveling combat unit alone:
   this proposal does not count it as an already-present screen, while a
   broader literal-presence alternative may. Unknown incoming ZOC or a missing
   seam does not prove a clear path.
3. Exercise both player A/B allocations, stage rollover, one stack per
   direction, stage-total count, direction, retry/replay and no partial move on
   unknown or rejected input. Test earlier and later CP use and reorganization
   only after their ordering policy is accepted.
4. Compare equal-authorized owner/opponent histories differing only in dummy
   identity or undisclosed enemy ZOC, including action enumeration, failure,
   timestamps, event type/order/ID and reconnect. Separately test any explicit
   source-authorized discovery. Verify no opposing audience receives the
   private path rationale or capacity identity.

No tests ran and no runtime, content or readiness state changed.

## Evidence

Primary source rechecked for the earlier independent [rail review](2026-11-17-graziani-rail-scope-review.md):
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) printed/PDF
pp. 12, 15, 18, 37 and 43 §§5.2, 8.72–.73, 10.26, 24.65 and 32.34
(SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`);
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.7
(`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`);
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 1 §8.73 (`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`).
Same-edition records; no exhaustive map, errata, other transport rules or
historical reachability audit is claimed.
